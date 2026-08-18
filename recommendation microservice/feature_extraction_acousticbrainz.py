import math
from typing import Optional, List

from feature_schema import FEATURE_NAMES, FEATURE_COUNT


def extract_features_from_acousticbrainz(doc: dict) -> Optional[List[float]]:
    try:
        lowlevel = doc["lowlevel"]
        rhythm = doc["rhythm"]
        tonal = doc["tonal"]

        def _mean(node) -> float:
            return float(node["mean"])

        def _std(node) -> float:
            return math.sqrt(float(node["var"]))

        mfcc_means = lowlevel["mfcc"]["mean"]
        if len(mfcc_means) < 13:
            return None

        # hpcp is the chroma feature in AcousticBrainz, but it doesn't have the same
        # normalization as the chroma in Librosa, so we can't just take the first 12 values as chroma means
        # instead, we take the mean of each pitch class across all octaves
        hpcp_means = tonal["hpcp"]["mean"]
        if len(hpcp_means) < 12:
            return None

        sc_means = lowlevel["spectral_contrast_coeffs"]["mean"]
        if len(sc_means) < 6:
            return None

        features: List[float] = [
            float(rhythm["bpm"]),                              # tempo_bpm
            float(lowlevel["average_loudness"]),               # loudness_mean
            _mean(lowlevel["spectral_centroid"]),
            _std(lowlevel["spectral_centroid"]),
            _mean(lowlevel["spectral_rolloff"]),
            _std(lowlevel["spectral_rolloff"]),
            _mean(lowlevel["zerocrossingrate"]),
            _std(lowlevel["zerocrossingrate"]),
            *[float(mfcc_means[i]) for i in range(13)],
            _mean(lowlevel["spectral_rms"]),
            *[float(hpcp_means[i]) for i in range(12)],
            *[float(sc_means[i]) for i in range(6)],
        ]
    except (KeyError, TypeError, ValueError):
        return None

    if len(features) != FEATURE_COUNT:
        raise RuntimeError(
            f"Feature extractor produced {len(features)} values, "
            f"schema expects {FEATURE_COUNT}. Update both together."
        )

    if not all(math.isfinite(v) for v in features):
        return None

    return features


# doc strings for each feature, same order as FEATURE_NAMES
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
    *[f"tonal.hpcp.mean[{i}]" for i in range(12)],
    *[f"lowlevel.spectral_contrast_coeffs.mean[{i}]" for i in range(6)],
]
assert len(FEATURE_SOURCE_PATHS) == len(FEATURE_NAMES), (
    "FEATURE_SOURCE_PATHS got out of sync with FEATURE_NAMES — "
    "always update both together."
)