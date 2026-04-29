import logging
import math
from typing import Optional, List, Tuple

import numpy as np
import librosa

from feature_schema import FEATURE_NAMES, FEATURE_COUNT

logger = logging.getLogger(__name__)

# Librosa's default sample rate. We resample to 22050 Hz on load — same
# rate AcousticBrainz uses for its analysis. Mismatched sample rates
# would shift all spectral features.
TARGET_SR = 22050

# 30-second slice. Long enough for stable feature aggregates, short
# enough that processing one song takes ~1-3 seconds on a modern CPU.
SLICE_DURATION_SEC = 30.0

# Where in the track to start the slice. 30s into the song is a sensible
# default: clears most intros, lands inside the first verse / chorus
# for typical pop structure.
SLICE_OFFSET_SEC = 30.0

# Number of MFCC coefficients. Matches AcousticBrainz's mfcc array
# length (13). Librosa defaults to 20; we override.
N_MFCC = 13


def extract_features_from_audio(audio_path: str) -> Optional[List[float]]:
    """
    Load an audio file, slice it, and produce the 22-element canonical
    feature vector.

    Returns None on any failure — bad file, too short to slice, librosa
    parse error, etc. Caller marks the song Failed and moves on.
    """
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
        # Inf or NaN — something went wrong (silent slice, all-zero
        # spectrum, etc.). Don't pollute the dataset.
        logger.warning("Non-finite features extracted from %s", audio_path)
        return None

    if len(features) != FEATURE_COUNT:
        # Defensive: this would be a code bug, not a data issue. Loud
        # error so it's caught immediately rather than producing garbage.
        raise RuntimeError(
            f"Librosa extractor produced {len(features)} values, "
            f"schema expects {FEATURE_COUNT}. Update both together."
        )

    return features


# ---- Internal helpers -------------------------------------------------------


def _load_audio_slice(path: str) -> Tuple[np.ndarray, int]:
    """
    Load a 30-second slice of the audio. Strategy:

      - Track > 60s: start at 30s in, take next 30s.
      - Track 30s-60s: start at midpoint, take rest (or 30s, whichever
        is shorter).
      - Track < 30s: take the whole thing.

    librosa.load with offset/duration is much cheaper than loading the
    full file and slicing — it uses libsoundfile to seek directly.

    Returns (waveform, sample_rate).
    """
    # First pass: just get the duration. Cheap — doesn't decode samples.
    duration = librosa.get_duration(path=path)

    if duration > 60.0:
        offset = SLICE_OFFSET_SEC
        slice_len = SLICE_DURATION_SEC
    elif duration > SLICE_DURATION_SEC:
        # Track is between 30s and 60s — use the back half (avoids the
        # intro), but cap at 30s.
        offset = duration / 2
        slice_len = min(SLICE_DURATION_SEC, duration - offset)
    else:
        offset = 0.0
        slice_len = duration

    # mono=True averages stereo to mono, matching AcousticBrainz
    # (Essentia's music extractor does the same). sr=TARGET_SR resamples
    # if needed.
    y, sr = librosa.load(
        path,
        sr=TARGET_SR,
        mono=True,
        offset=offset,
        duration=slice_len,
    )
    return y, sr


def _compute_features(y: np.ndarray, sr: int) -> List[float]:
    """
    Run the actual feature extractors on an in-memory waveform. This
    function MUST produce values in the order defined by FEATURE_NAMES
    in feature_schema.py — see comments alongside each block.
    """

    # --- Tempo (BPM) ----
    # librosa.feature.tempo as of 0.10+ (was librosa.beat.tempo). Returns
    # an ndarray even for mono input — convert to scalar with .item().
    tempo_arr = librosa.feature.tempo(y=y, sr=sr)
    tempo_bpm = float(tempo_arr.item() if tempo_arr.size == 1 else tempo_arr[0])

    # --- Loudness (proxy: RMS-based 0-1 dynamic range) ----
    # AcousticBrainz's average_loudness is a 0-1 dynamic-range descriptor
    # (1 = compressed/loud, 0 = wide dynamic range). Librosa doesn't
    # have a direct equivalent; we approximate with mean RMS energy
    # normalised by max RMS in the slice. Same direction, similar scale.
    rms = librosa.feature.rms(y=y).flatten()
    rms_max = float(np.max(rms)) if rms.size else 0.0
    loudness_mean = float(np.mean(rms) / rms_max) if rms_max > 0 else 0.0

    # --- Spectral centroid (mean + std) ----
    centroid = librosa.feature.spectral_centroid(y=y, sr=sr).flatten()
    centroid_mean = float(np.mean(centroid))
    centroid_std = float(np.std(centroid))

    # --- Spectral rolloff (mean + std) ----
    # Default rolloff is 85% — same as Essentia's default, so our values
    # are comparable.
    rolloff = librosa.feature.spectral_rolloff(y=y, sr=sr).flatten()
    rolloff_mean = float(np.mean(rolloff))
    rolloff_std = float(np.std(rolloff))

    # --- Zero-crossing rate (mean + std) ----
    zcr = librosa.feature.zero_crossing_rate(y=y).flatten()
    zcr_mean = float(np.mean(zcr))
    zcr_std = float(np.std(zcr))

    # --- 13 MFCC means ----
    # n_mfcc=13 to match AcousticBrainz. Matrix shape: (n_mfcc, n_frames).
    # We take the per-coefficient mean across frames.
    mfcc_matrix = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=N_MFCC)
    mfcc_means = mfcc_matrix.mean(axis=1)
    if mfcc_means.shape[0] != N_MFCC:
        raise RuntimeError(
            f"Expected {N_MFCC} MFCC coefficients, got {mfcc_means.shape[0]}"
        )

    # --- RMS energy mean ----
    # Already computed above for loudness. Just expose the raw mean.
    rms_energy_mean = float(np.mean(rms))

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
    ]


# Self-documentation matching feature_extraction_acousticbrainz.py.
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
]
assert len(FEATURE_SOURCE_DESCRIPTIONS) == len(FEATURE_NAMES), (
    "FEATURE_SOURCE_DESCRIPTIONS got out of sync with FEATURE_NAMES — "
    "always update both together."
)