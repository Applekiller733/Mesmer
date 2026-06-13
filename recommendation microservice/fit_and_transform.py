import argparse
import logging
import os
import sys
import warnings
from typing import List, Tuple, Optional

import numpy as np
from sklearn.preprocessing import StandardScaler
from sklearn.decomposition import PCA
import joblib
from psycopg2.extras import execute_values

from db import get_connection
from feature_schema import FEATURE_COUNT, PCA_COMPONENTS, SCHEMA_VERSION

# schema-versioned artefact names so old files are never
# accidentally loaded against the newer pipeline.
SCALER_PATH = f"scaler_schema{SCHEMA_VERSION}.pkl"
PCA_PATH = f"pca_schema{SCHEMA_VERSION}.pkl"

MIN_SAMPLES_FOR_RELIABLE_FIT = 50

UPDATE_BATCH_SIZE = 200


def setup_logging(verbose: bool):
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    )
    for noisy in ("numba", "matplotlib", "PIL", "urllib3"):
        logging.getLogger(noisy).setLevel(logging.WARNING)
    warnings.filterwarnings("ignore", category=FutureWarning, module="sklearn")




# db
def load_all_raw_features() -> Tuple[List[str], np.ndarray]:
    """
    Load every song's RawFeatures. Returns (song_ids, features_matrix)
    where features_matrix has shape (n_songs, FEATURE_COUNT).
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
    Vectors are always FEATURE_COUNT-dimensional — see transform_to_storage_space
    for the padding rationale.
    """
    if not rows:
        return 0

    written = 0
    with get_connection() as conn:
        with conn.cursor() as cur:
            for i in range(0, len(rows), UPDATE_BATCH_SIZE):
                chunk = rows[i : i + UPDATE_BATCH_SIZE]
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



#fit & transform

def fit_scaler_and_optional_pca(
    features: np.ndarray,
    n_components: Optional[int],
) -> Tuple[StandardScaler, Optional[PCA]]:
    """
    Fit a StandardScaler on (n_songs, FEATURE_COUNT). If n_components
    is set and < FEATURE_COUNT, also fit a PCA. None means no PCA —
    only standardisation is applied.
    """
    log = logging.getLogger("fit")

    if features.shape[0] < FEATURE_COUNT:
        raise RuntimeError(
            f"Need at least {FEATURE_COUNT} samples to fit, got "
            f"{features.shape[0]}. Enrich more songs first."
        )
    if features.shape[0] < MIN_SAMPLES_FOR_RELIABLE_FIT:
        log.warning(
            "Fitting on only %d samples. Components may be noisy. "
            "Recommend at least %d for stable results.",
            features.shape[0], MIN_SAMPLES_FOR_RELIABLE_FIT,
        )

    log.info("Fitting StandardScaler on %s...", features.shape)
    scaler = StandardScaler()
    scaled = scaler.fit_transform(features)

    if n_components is None or n_components >= FEATURE_COUNT:
        log.info(
            "Skipping PCA — features stored at full dimension (%d). "
            "All variance retained.", FEATURE_COUNT,
        )
        return scaler, None

    log.info("Fitting PCA (%d components)...", n_components)
    pca = PCA(n_components=n_components, whiten=True)
    # pca = PCA(n_components=n_components);
    pca.fit(scaled)

    cumvar = pca.explained_variance_ratio_.sum()
    log.info(
        "PCA fit done. Cumulative explained variance: %.1f%%",
        cumvar * 100,
    )
    return scaler, pca


def transform_to_storage_space(
    features: np.ndarray,
    scaler: StandardScaler,
    pca: Optional[PCA],
) -> np.ndarray:
    """
    Apply scaler (and PCA if provided), then pad to FEATURE_COUNT dims.

    Why pad? The PcaFeatures column is declared as vector(FEATURE_COUNT)
    so the same column accommodates both PCA-reduced and no-PCA modes
    without a schema migration. Padding with trailing zeros does NOT
    affect cosine similarity rankings: the dot product is unchanged
    (zeros contribute zero), and the magnitudes are unchanged (zeros
    don't increase |v|), so cosine(A_padded, B_padded) == cosine(A, B).
    """
    scaled = scaler.transform(features)

    if pca is None:
        # scaled is already (n, FEATURE_COUNT)
        return scaled.astype(np.float32)

    reduced = pca.transform(scaled)  # (n, n_components)
    n_samples, k = reduced.shape

    if k == FEATURE_COUNT:
        return reduced.astype(np.float32)

    padded = np.zeros((n_samples, FEATURE_COUNT), dtype=np.float32)
    padded[:, :k] = reduced
    return padded


def save_models(scaler: StandardScaler, pca: Optional[PCA]):
    """Persist scaler and (optional) PCA. Removes stale PCA file when
    switching from PCA mode to no-PCA mode."""
    log = logging.getLogger("fit")
    log.info("Saving scaler -> %s", SCALER_PATH)
    joblib.dump(scaler, SCALER_PATH)

    if pca is None:
        if os.path.exists(PCA_PATH):
            os.remove(PCA_PATH)
            log.info("Removed stale %s (no-PCA mode active)", PCA_PATH)
    else:
        log.info("Saving pca -> %s", PCA_PATH)
        joblib.dump(pca, PCA_PATH)


def load_models() -> Tuple[StandardScaler, Optional[PCA]]:
    """Load scaler (required) and PCA (optional)."""
    if not os.path.exists(SCALER_PATH):
        raise FileNotFoundError(
            f"Missing {SCALER_PATH}. Run with --fit first to train the "
            "scaler on your enriched data."
        )
    scaler = joblib.load(SCALER_PATH)
    pca = joblib.load(PCA_PATH) if os.path.exists(PCA_PATH) else None
    return scaler, pca


# 2 run modes


def run_fit_mode(n_components: Optional[int]):
    log = logging.getLogger("fit_mode")
    log.info("=== Stage 4: FIT mode ===")
    log.info(
        "Schema version: %d, feature count: %d, target components: %s",
        SCHEMA_VERSION, FEATURE_COUNT,
        "none (no-PCA)" if n_components is None else n_components,
    )

    song_ids, raw = load_all_raw_features()
    if len(song_ids) == 0:
        log.error(
            "No enriched songs in the DB. Run Stage 2 (AcousticBrainz) "
            "and/or Stage 3 (Librosa) first."
        )
        return 1

    log.info("Loaded %d enriched songs.", len(song_ids))

    scaler, pca = fit_scaler_and_optional_pca(raw, n_components)
    save_models(scaler, pca)

    log.info("Transforming all %d songs into storage space...", len(song_ids))
    stored = transform_to_storage_space(raw, scaler, pca)

    rows = [(sid, vec.tolist()) for sid, vec in zip(song_ids, stored)]
    written = write_pca_features(rows)
    log.info("PcaFeatures updated for %d songs.", written)
    return 0


def run_transform_mode():
    log = logging.getLogger("transform_mode")
    log.info("=== Stage 4: TRANSFORM mode (incremental) ===")

    scaler, pca = load_models()
    log.info(
        "Loaded models. Mode: %s",
        "no-PCA (scaler only)" if pca is None else f"PCA({pca.n_components_})",
    )

    song_ids, raw = load_untransformed_raw_features()
    if len(song_ids) == 0:
        log.info("All enriched songs already have PcaFeatures. Nothing to do.")
        return 0

    log.info("Transforming %d new song(s)...", len(song_ids))
    stored = transform_to_storage_space(raw, scaler, pca)

    rows = [(sid, vec.tolist()) for sid, vec in zip(song_ids, stored)]
    written = write_pca_features(rows)
    log.info("PcaFeatures updated for %d songs.", written)
    return 0


def main():
    parser = argparse.ArgumentParser(
        description="Fit scaler (+ optional PCA) and/or transform raw "
                    "features into the storage space.",
    )
    parser.add_argument(
        "--fit", action="store_true",
        help="Re-fit scaler (+ PCA) from all enriched songs and "
             "re-transform every row. Use after dataset growth or "
             "schema changes.",
    )
    parser.add_argument(
        "--no-pca", action="store_true",
        help="Skip PCA entirely. Features are only standardized. "
             "Stores full-dimensional vectors. All variance retained.",
    )
    parser.add_argument(
        "--components", type=int, default=None,
        help=f"PCA component count override (default {PCA_COMPONENTS}). "
             "Ignored with --no-pca.",
    )
    parser.add_argument("-v", "--verbose", action="store_true")
    args = parser.parse_args()

    setup_logging(args.verbose)

    if args.fit:
        if args.no_pca:
            n_components = None
        else:
            n_components = args.components if args.components else PCA_COMPONENTS
        return run_fit_mode(n_components)

    if args.no_pca or args.components is not None:
        logging.getLogger("main").warning(
            "--no-pca / --components are only meaningful with --fit. "
            "Transform mode uses whatever models are saved on disk."
        )
    return run_transform_mode()


if __name__ == "__main__":
    sys.exit(main() or 0)