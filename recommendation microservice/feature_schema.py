SCHEMA_VERSION = 1

FEATURE_NAMES: list[str] = [
    # --- High-level (2) ---
    "tempo_bpm",                # beats per minute
    "loudness_mean",            # average loudness, signed dB or RMS proxy

    # --- Spectral shape (6) ---
    "spectral_centroid_mean",   # "brightness"
    "spectral_centroid_std",
    "spectral_rolloff_mean",    # frequency below which 85% of energy lies
    "spectral_rolloff_std",
    "zero_crossing_rate_mean",  # noisiness / percussiveness
    "zero_crossing_rate_std",

    # --- Timbral (13 MFCC means) ---
    "mfcc_1_mean", "mfcc_2_mean", "mfcc_3_mean", "mfcc_4_mean",
    "mfcc_5_mean", "mfcc_6_mean", "mfcc_7_mean", "mfcc_8_mean",
    "mfcc_9_mean", "mfcc_10_mean", "mfcc_11_mean", "mfcc_12_mean",
    "mfcc_13_mean",

    # --- Energy (1) ---
    "rms_energy_mean",          # short-term energy / average loudness proxy
]

FEATURE_COUNT = len(FEATURE_NAMES)
assert FEATURE_COUNT == 22, f"Expected 22 features, got {FEATURE_COUNT}"

# PCA target dimensionality. Matches what the existing model file expected,
# but more importantly: 15 captures ~90%+ of variance for typical 22-D
# audio feature distributions on AcousticBrainz-style data.
PCA_COMPONENTS = 15