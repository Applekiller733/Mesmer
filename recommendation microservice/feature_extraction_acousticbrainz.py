import math
from typing import Optional, List

from feature_schema import FEATURE_NAMES, FEATURE_COUNT


def extract_features_from_acousticbrainz(doc: dict) -> Optional[List[float]]:
    """
    Convert one AcousticBrainz lowlevel document into a 22-element list
    of floats in the canonical schema order.

    Returns None if the document is malformed or missing any required
    field — partial vectors aren't useful for similarity search and
    would corrupt the scaler/PCA fit.

    Why not fill missing fields with 0.0 or NaN? Because zeros lie about
    the song (zero centroid means "all energy at DC", which is
    nonsensical for music) and NaN cascades through scaler/PCA. Better
    to mark the song as failed and let Librosa try.
    """
    try:
        lowlevel = doc["lowlevel"]
        rhythm = doc["rhythm"]

        # Helper to pull mean from {"mean": x, "var": y}-shaped fields.
        def _mean(node) -> float:
            return float(node["mean"])

        def _std(node) -> float:
            # AcousticBrainz uses var; we want std.
            return math.sqrt(float(node["var"]))

        mfcc_means = lowlevel["mfcc"]["mean"]
        if len(mfcc_means) < 13:
            return None

        features: List[float] = [
            float(rhythm["bpm"]),                              # tempo_bpm
            float(lowlevel["average_loudness"]),               # loudness_mean
            _mean(lowlevel["spectral_centroid"]),              # centroid mean
            _std(lowlevel["spectral_centroid"]),               # centroid std
            _mean(lowlevel["spectral_rolloff"]),               # rolloff mean
            _std(lowlevel["spectral_rolloff"]),                # rolloff std
            _mean(lowlevel["zerocrossingrate"]),               # zcr mean
            _std(lowlevel["zerocrossingrate"]),                # zcr std
            *[float(mfcc_means[i]) for i in range(13)],        # mfcc 1-13
            _mean(lowlevel["spectral_rms"]),                   # rms_energy_mean
        ]
    except (KeyError, TypeError, ValueError) as e:
        # Any missing field, type mismatch, or unparseable value.
        # Log info would be nice but we don't have a logger plumbed in
        # here — caller tracks failures by getting None back.
        return None

    if len(features) != FEATURE_COUNT:
        # Defensive: the canonical schema has 22 entries; if we ever
        # change FEATURE_NAMES without updating this mapper, fail loud.
        raise RuntimeError(
            f"Feature extractor produced {len(features)} values, "
            f"schema expects {FEATURE_COUNT}. Update both together."
        )

    # Sanity check the values are finite — NaN/inf can sneak in if
    # AcousticBrainz had a parse glitch (rare but seen).
    if not all(math.isfinite(v) for v in features):
        return None

    return features


# Keep this for self-documentation: lets a developer eyeball that
# FEATURE_NAMES order matches what the extractor produces.
FEATURE_SOURCE_PATHS = [
    "rhythm.bpm",
    "lowlevel.average_loudness",
    "lowlevel.spectral_centroid.mean",
    "sqrt(lowlevel.spectral_centroid.var)",
    "lowlevel.spectral_rolloff.mean",
    "sqrt(lowlevel.spectral_rolloff.var)",
    "lowlevel.zerocrossingrate.mean",
    "sqrt(lowlevel.zerocrossingrate.var)",
    *[f"lowlevel.mfcc.mean[{i}]" for i in range(13)],
    "lowlevel.spectral_rms.mean",
]
assert len(FEATURE_SOURCE_PATHS) == len(FEATURE_NAMES), (
    "FEATURE_SOURCE_PATHS got out of sync with FEATURE_NAMES — "
    "always update both together."
)