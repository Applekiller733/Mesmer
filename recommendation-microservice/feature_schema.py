SCHEMA_VERSION = 2

FEATURE_NAMES: list[str] = [
    "tempo_bpm",                
    "loudness_mean",            # average loudness, signed dB or RMS proxy

    # spectral features
    "spectral_centroid_mean",   # "brightness"
    "spectral_centroid_std",
    "spectral_rolloff_mean",    # frequency below which 85% of energy lies
    "spectral_rolloff_std",
    "zero_crossing_rate_mean",  # noisiness / percussiveness
    "zero_crossing_rate_std",

    # timbral features
    "mfcc_1_mean", "mfcc_2_mean", "mfcc_3_mean", "mfcc_4_mean",
    "mfcc_5_mean", "mfcc_6_mean", "mfcc_7_mean", "mfcc_8_mean",
    "mfcc_9_mean", "mfcc_10_mean", "mfcc_11_mean", "mfcc_12_mean",
    "mfcc_13_mean",

    "rms_energy_mean",          

    # harmonic content: 12 chroma means, 6 spectral contrast means
    # one chroma mean per pitch class (C, C#, D, ..., B), one contrast mean per spectral band
    "chroma_1_mean", "chroma_2_mean", "chroma_3_mean", "chroma_4_mean",
    "chroma_5_mean", "chroma_6_mean", "chroma_7_mean", "chroma_8_mean",
    "chroma_9_mean", "chroma_10_mean", "chroma_11_mean", "chroma_12_mean",

    "spectral_contrast_1_mean", "spectral_contrast_2_mean",
    "spectral_contrast_3_mean", "spectral_contrast_4_mean",
    "spectral_contrast_5_mean", "spectral_contrast_6_mean",
]

FEATURE_COUNT = len(FEATURE_NAMES)
assert FEATURE_COUNT == 40, f"Expected 40 features, got {FEATURE_COUNT}"

PCA_COMPONENTS = 25