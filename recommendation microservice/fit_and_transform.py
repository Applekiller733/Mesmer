import argparse
import logging
import os
import sys
import warnings
from typing import List, Tuple

import numpy as np
from sklearn.preprocessing import StandardScaler
from sklearn.decomposition import PCA
import joblib
from psycopg2.extras import execute_values

from db import get_connection
from feature_schema import FEATURE_COUNT, PCA_COMPONENTS, SCHEMA_VERSION

SCALER_PATH = "scaler_v2.pkl"
PCA_PATH = "pca_v2.pkl"

MIN_SAMPLES_FOR_RELIABLE_FIT = 50

# Updates to PcaFeatures happen in batches via execute_values for speed.
UPDATE_BATCH_SIZE = 200


def setup_logging(verbose: bool):
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    )
    # Same noisy-library suppression as the librosa enricher uses.
    for noisy in ("numba", "matplotlib", "PIL", "urllib3"):
        logging.getLogger(noisy).setLevel(logging.WARNING)
    warnings.filterwarnings("ignore", category=FutureWarning, module="sklearn")


# ---- DB I/O -----------------------------------------------------------------


def load_all_raw_features() -> Tuple[List[str], np.ndarray]:
    """
    Load every song's RawFeatures. Returns (song_ids, features_matrix)
    where features_matrix has shape (n_songs, FEATURE_COUNT).

    Songs without RawFeatures (Pending or Failed-without-audio) are
    skipped — they contribute nothing to the fit and won't be
    transformable until they get features.
    """
    log = logging.getLogger("load")
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Id", "RawFeatures"
                FROM "Songs"
                WHERE "RawFeatures" IS NOT NULL
                """
            )
            rows = cur.fetchall()

    if not rows:
        return [], np.empty((0, FEATURE_COUNT), dtype=np.float32)

    song_ids: List[str] = []
    features: List[List[float]] = []
    for sid, raw in rows:
        # Sanity: drop anything that doesn't match the canonical length.
        # Could happen if the schema was bumped and old rows survived.
        if raw is None or len(raw) != FEATURE_COUNT:
            log.warning(
                "Song %s has %s features (expected %d) — skipping",
                sid, "no" if raw is None else len(raw), FEATURE_COUNT,
            )
            continue
        song_ids.append(str(sid))
        features.append(list(raw))

    return song_ids, np.asarray(features, dtype=np.float32)


def load_untransformed_raw_features() -> Tuple[List[str], np.ndarray]:
    """
    Like load_all_raw_features, but only returns rows where PcaFeatures
    is still NULL. Used by the transform-only mode so we don't waste
    work re-transforming already-done rows.
    """
    log = logging.getLogger("load")
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Id", "RawFeatures"
                FROM "Songs"
                WHERE "RawFeatures" IS NOT NULL
                  AND "PcaFeatures" IS NULL
                """
            )
            rows = cur.fetchall()

    song_ids: List[str] = []
    features: List[List[float]] = []
    for sid, raw in rows:
        if raw is None or len(raw) != FEATURE_COUNT:
            log.warning("Song %s skipped (bad feature length)", sid)
            continue
        song_ids.append(str(sid))
        features.append(list(raw))

    return song_ids, np.asarray(features, dtype=np.float32)


def write_pca_features(rows: List[Tuple[str, List[float]]]) -> int:
    """
    Bulk-update PcaFeatures for the given (song_id, vector) pairs.

    pgvector accepts a Python list of floats directly when the column is
    declared as `vector(N)` — psycopg2's `register_vector` (called in
    db.get_connection) handles the conversion.

    Uses execute_values for efficiency; one round-trip per chunk
    instead of one per row.
    """
    if not rows:
        return 0

    written = 0
    with get_connection() as conn:
        with conn.cursor() as cur:
            for i in range(0, len(rows), UPDATE_BATCH_SIZE):
                chunk = rows[i : i + UPDATE_BATCH_SIZE]
                # execute_values with UPDATE...FROM(VALUES) is the
                # standard psycopg2 idiom for bulk updates.
                execute_values(
                    cur,
                    """
                    UPDATE "Songs" AS s
                    SET "PcaFeatures" = data.vec
                    FROM (VALUES %s) AS data(id, vec)
                    WHERE s."Id" = data.id::uuid
                    """,
                    chunk,
                    template="(%s, %s::vector)",
                )
                conn.commit()
                written += len(chunk)
    return written


# ---- Fit / transform --------------------------------------------------------


def fit_scaler_and_pca(features: np.ndarray) -> Tuple[StandardScaler, PCA]:
    """
    Fit a StandardScaler and PCA(15) on the given (n_songs, 22) matrix.
    Saves both to disk with version-suffixed names.
    """
    log = logging.getLogger("fit")

    if features.shape[0] < FEATURE_COUNT:
        raise RuntimeError(
            f"Need at least {FEATURE_COUNT} samples to fit PCA with "
            f"{PCA_COMPONENTS} components — got {features.shape[0]}. "
            "Run more enrichment first."
        )
    if features.shape[0] < MIN_SAMPLES_FOR_RELIABLE_FIT:
        log.warning(
            "Fitting on only %d samples. PCA will work but components may "
            "be noisy. Recommend at least %d for stable results.",
            features.shape[0], MIN_SAMPLES_FOR_RELIABLE_FIT,
        )

    log.info("Fitting StandardScaler on %s...", features.shape)
    scaler = StandardScaler()
    scaled = scaler.fit_transform(features)

    log.info("Fitting PCA (%d components)...", PCA_COMPONENTS)
    pca = PCA(n_components=PCA_COMPONENTS)
    pca.fit(scaled)

    cumvar = pca.explained_variance_ratio_.cumsum()[-1]
    log.info(
        "PCA fit done. Cumulative explained variance: %.1f%% "
        "(higher is better; >85%% is good for 15 components on 22 features)",
        cumvar * 100,
    )

    log.info("Saving scaler -> %s, pca -> %s", SCALER_PATH, PCA_PATH)
    joblib.dump(scaler, SCALER_PATH)
    joblib.dump(pca, PCA_PATH)

    return scaler, pca


def load_scaler_and_pca() -> Tuple[StandardScaler, PCA]:
    """
    Load the saved scaler + PCA from disk. Errors loudly if they don't
    exist (caller should run --fit first).
    """
    if not os.path.exists(SCALER_PATH) or not os.path.exists(PCA_PATH):
        raise FileNotFoundError(
            f"Missing {SCALER_PATH} or {PCA_PATH}. Run with --fit first "
            "to train the scaler and PCA on your enriched data."
        )
    return joblib.load(SCALER_PATH), joblib.load(PCA_PATH)


def transform_to_pca_space(
    features: np.ndarray, scaler: StandardScaler, pca: PCA
) -> np.ndarray:
    """Apply scaler then PCA. Returns shape (n_songs, PCA_COMPONENTS)."""
    return pca.transform(scaler.transform(features))


# ---- Modes ------------------------------------------------------------------


def run_fit_mode():
    log = logging.getLogger("fit_mode")
    log.info("=== Stage 4: FIT mode ===")
    log.info("Schema version: %d, expected feature count: %d",
             SCHEMA_VERSION, FEATURE_COUNT)

    song_ids, raw = load_all_raw_features()
    if len(song_ids) == 0:
        log.error(
            "No enriched songs in the DB. Run Stage 2 (AcousticBrainz) "
            "and/or Stage 3 (Librosa) first."
        )
        return 1

    log.info("Loaded %d enriched songs.", len(song_ids))

    scaler, pca = fit_scaler_and_pca(raw)

    log.info("Transforming all %d songs into PCA space...", len(song_ids))
    pca_features = transform_to_pca_space(raw, scaler, pca)

    rows = [(sid, vec.tolist()) for sid, vec in zip(song_ids, pca_features)]
    written = write_pca_features(rows)
    log.info("PcaFeatures updated for %d songs.", written)
    return 0


def run_transform_mode():
    log = logging.getLogger("transform_mode")
    log.info("=== Stage 4: TRANSFORM mode (incremental) ===")

    scaler, pca = load_scaler_and_pca()

    song_ids, raw = load_untransformed_raw_features()
    if len(song_ids) == 0:
        log.info("All enriched songs already have PcaFeatures. Nothing to do.")
        return 0

    log.info("Transforming %d new song(s)...", len(song_ids))
    pca_features = transform_to_pca_space(raw, scaler, pca)

    rows = [(sid, vec.tolist()) for sid, vec in zip(song_ids, pca_features)]
    written = write_pca_features(rows)
    log.info("PcaFeatures updated for %d songs.", written)
    return 0


def main():
    parser = argparse.ArgumentParser(
        description="Fit scaler + PCA and/or transform raw features into PCA space."
    )
    parser.add_argument(
        "--fit", action="store_true",
        help="Re-fit scaler + PCA from all enriched songs and re-transform "
             "every row. Use after major dataset or schema changes.",
    )
    parser.add_argument("-v", "--verbose", action="store_true")
    args = parser.parse_args()

    setup_logging(args.verbose)

    if args.fit:
        return run_fit_mode()
    return run_transform_mode()


if __name__ == "__main__":
    sys.exit(main() or 0)