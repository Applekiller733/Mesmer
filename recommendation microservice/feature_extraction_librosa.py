import logging
import math
from typing import Optional, List, Tuple

import numpy as np
import librosa

from feature_schema import FEATURE_NAMES, FEATURE_COUNT

logger = logging.getLogger(__name__)

#sample rate
TARGET_SR = 22050
#mfcc coeffs
N_MFCC = 13
ANALYSIS_FRACTION = 0.80
TRIM_FRACTION = (1.0 - ANALYSIS_FRACTION) / 2 
MIN_DURATION_FOR_TRIM_SEC = 30.0
ENRICHMENT_SOURCE_TAG = "librosa:middle-80pct-schema2"


def extract_features_from_audio(audio_path: str) -> Optional[List[float]]:
    #extract with librosa, return None if any step fails or produces non-finite values
    try:
        y, sr = _load_audio_slice(audio_path)
    except Exception as e:
        logger.warning("Could not load audio %s: %s", audio_path, e)
        return None

    if y is None or len(y) == 0:
        logger.warning("Empty audio after slice: %s", audio_path)
        return None

    try:
        features = _compute_features(y, sr)
    except Exception as e:
        logger.warning("Feature computation failed for %s: %s", audio_path, e)
        return None

    if not all(math.isfinite(v) for v in features):
        logger.warning("Non-finite features extracted from %s", audio_path)
        return None

    if len(features) != FEATURE_COUNT:
        raise RuntimeError(
            f"Librosa extractor produced {len(features)} values, "
            f"schema expects {FEATURE_COUNT}. Update both together."
        )

    return features


# helpers


def _load_audio_slice(path: str) -> Tuple[np.ndarray, int]:
    #trim slice and load in librosa
    duration = librosa.get_duration(path=path)

    if duration >= MIN_DURATION_FOR_TRIM_SEC:
        offset = duration * TRIM_FRACTION
        slice_len = duration * ANALYSIS_FRACTION
    else:
        offset = 0.0
        slice_len = duration

    y, sr = librosa.load(
        path,
        sr=TARGET_SR,
        mono=True,
        offset=offset,
        duration=slice_len,
    )
    return y, sr


def _compute_features(y: np.ndarray, sr: int) -> List[float]:
    
    #get features
    tempo_arr = librosa.feature.tempo(y=y, sr=sr)
    tempo_bpm = float(tempo_arr.item() if tempo_arr.size == 1 else tempo_arr[0])

    rms = librosa.feature.rms(y=y).flatten()
    rms_max = float(np.max(rms)) if rms.size else 0.0
    loudness_mean = float(np.mean(rms) / rms_max) if rms_max > 0 else 0.0

    centroid = librosa.feature.spectral_centroid(y=y, sr=sr).flatten()
    centroid_mean = float(np.mean(centroid))
    centroid_std = float(np.std(centroid))

    rolloff = librosa.feature.spectral_rolloff(y=y, sr=sr).flatten()
    rolloff_mean = float(np.mean(rolloff))
    rolloff_std = float(np.std(rolloff))

    zcr = librosa.feature.zero_crossing_rate(y=y).flatten()
    zcr_mean = float(np.mean(zcr))
    zcr_std = float(np.std(zcr))

    mfcc_matrix = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=N_MFCC)
    mfcc_means = mfcc_matrix.mean(axis=1)
    if mfcc_means.shape[0] != N_MFCC:
        raise RuntimeError(
            f"Expected {N_MFCC} MFCC coefficients, got {mfcc_means.shape[0]}"
        )

    rms_energy_mean = float(np.mean(rms))

    chroma_matrix = librosa.feature.chroma_stft(y=y, sr=sr)
    chroma_means = chroma_matrix.mean(axis=1)
    if chroma_means.shape[0] != 12:
        raise RuntimeError(
            f"Expected 12 chroma values, got {chroma_means.shape[0]}"
        )

    contrast_matrix = librosa.feature.spectral_contrast(
        y=y, sr=sr, n_bands=5
    )
    contrast_means = contrast_matrix.mean(axis=1)
    if contrast_means.shape[0] != 6:
        raise RuntimeError(
            f"Expected 6 spectral contrast values, got "
            f"{contrast_means.shape[0]}"
        )

    return [
        tempo_bpm,
        loudness_mean,
        centroid_mean,
        centroid_std,
        rolloff_mean,
        rolloff_std,
        zcr_mean,
        zcr_std,
        *[float(v) for v in mfcc_means],
        rms_energy_mean,
        *[float(v) for v in chroma_means],
        *[float(v) for v in contrast_means],
    ]


#doc strings for each feature, same order as FEATURE_NAMES
FEATURE_SOURCE_DESCRIPTIONS = [
    "librosa.feature.tempo (BPM, scalar)",
    "mean(RMS) / max(RMS), 0-1 (proxy for AB's average_loudness)",
    "mean(spectral_centroid)",
    "std(spectral_centroid)",
    "mean(spectral_rolloff @ 85%)",
    "std(spectral_rolloff @ 85%)",
    "mean(zero_crossing_rate)",
    "std(zero_crossing_rate)",
    *[f"mean(mfcc[{i}]) over frames, n_mfcc=13" for i in range(13)],
    "mean(RMS)",
    *[f"mean(chroma_stft[{i}]) over frames" for i in range(12)],
    *[f"mean(spectral_contrast[{i}]) over frames, n_bands=5" for i in range(6)],
]
assert len(FEATURE_SOURCE_DESCRIPTIONS) == len(FEATURE_NAMES), (
    "FEATURE_SOURCE_DESCRIPTIONS got out of sync with FEATURE_NAMES — "
    "always update both together."
)