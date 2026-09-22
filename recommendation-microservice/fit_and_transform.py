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

SCALER_PATH = f"scaler_schema{SCHEMA_VERSION}.pkl"
PCA_PATH = f"pca_schema{SCHEMA_VERSION}.pkl"

MIN_SAMPLES_FOR_RELIABLE_FIT = 50

UPDATE_BATCH_SIZE = 200

# --- Model artifact storage -------------------------------------------------
# Locally the .pkl models live next to this script (committed to the repo). On
# AWS the container filesystem is read-only, so when MODEL_BUCKET is set:
#   - a refit (run_fit_mode / --fit) writes the retrained scaler+PCA to S3, and
#   - every transform loads the models from S3 (authoritative once a refit has
#     run), falling back to the image's baked-in .pkl until the first refit.
# This keeps the model that produced the stored embeddings and the model used
# for new songs in lockstep without redeploying the image.
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MODEL_BUCKET = os.getenv("MODEL_BUCKET")
MODEL_PREFIX = os.getenv("MODEL_PREFIX", "models").strip("/")


def _writable_model_dir() -> str:
    # /tmp is the only writable path on Lambda; use the script dir locally.
    return "/tmp" if MODEL_BUCKET else BASE_DIR


def _s3_key(name: str) -> str:
    return f"{MODEL_PREFIX}/{name}" if MODEL_PREFIX else name


def _resolve_model_file(name: str) -> Optional[str]:
    # Return a local path to load `name` from: prefer S3 (authoritative once a
    # refit has run), else the baked/committed copy next to this script.
    if MODEL_BUCKET:
        import boto3
        import botocore
        dest = os.path.join("/tmp", name)
        try:
            boto3.client("s3").download_file(MODEL_BUCKET, _s3_key(name), dest)
            return dest
        except botocore.exceptions.ClientError as e:
            code = e.response.get("Error", {}).get("Code")
            if code not in ("404", "NoSuchKey", "NotFound"):
                # A real error (e.g. 403) — don't silently use a stale baked
                # model that may not match the stored embeddings.
                raise
            # Not uploaded yet: fall through to the baked artifact.
    baked = os.path.join(BASE_DIR, name)
    return baked if os.path.exists(baked) else None


def _upload_model_file(name: str, local_path: str):
    if MODEL_BUCKET:
        import boto3
        boto3.client("s3").upload_file(local_path, MODEL_BUCKET, _s3_key(name))


def _remove_model_file(name: str):
    # Remove a stale artifact both locally and (if configured) from S3.
    local = os.path.join(_writable_model_dir(), name)
    if os.path.exists(local):
        os.remove(local)
    if MODEL_BUCKET:
        import boto3
        boto3.client("s3").delete_object(Bucket=MODEL_BUCKET, Key=_s3_key(name))


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
    # load songs that have RawFeatures but no PcaFeatures yet, returns (song_ids, features_matrix)
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
    log = logging.getLogger("fit")

    scaler_local = os.path.join(_writable_model_dir(), SCALER_PATH)
    log.info("Saving scaler -> %s", scaler_local)
    joblib.dump(scaler, scaler_local)
    _upload_model_file(SCALER_PATH, scaler_local)

    if pca is None:
        _remove_model_file(PCA_PATH)
        log.info("No-PCA mode: removed any stale %s", PCA_PATH)
    else:
        pca_local = os.path.join(_writable_model_dir(), PCA_PATH)
        log.info("Saving pca -> %s", pca_local)
        joblib.dump(pca, pca_local)
        _upload_model_file(PCA_PATH, pca_local)

    if MODEL_BUCKET:
        log.info("Models persisted to s3://%s/%s/", MODEL_BUCKET, MODEL_PREFIX)


def load_models() -> Tuple[StandardScaler, Optional[PCA]]:
    scaler_path = _resolve_model_file(SCALER_PATH)
    if scaler_path is None:
        raise FileNotFoundError(
            f"Missing {SCALER_PATH} (checked "
            f"{'s3://' + MODEL_BUCKET + ' and ' if MODEL_BUCKET else ''}"
            f"{BASE_DIR}). Run with --fit first to train the scaler on "
            "enriched data."
        )
    scaler = joblib.load(scaler_path)
    pca_path = _resolve_model_file(PCA_PATH)
    pca = joblib.load(pca_path) if pca_path else None
    return scaler, pca



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