# Bumped from 1 (22 features) → 2 (40 features). Schema version is
# baked into the scaler/PCA file names so old artefacts are never
# accidentally loaded against the new feature layout.
SCHEMA_VERSION = 2

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

    # --- Timbral: 13 MFCC means ---
    "mfcc_1_mean", "mfcc_2_mean", "mfcc_3_mean", "mfcc_4_mean",
    "mfcc_5_mean", "mfcc_6_mean", "mfcc_7_mean", "mfcc_8_mean",
    "mfcc_9_mean", "mfcc_10_mean", "mfcc_11_mean", "mfcc_12_mean",
    "mfcc_13_mean",

    # --- Energy (1) ---
    "rms_energy_mean",          # short-term energy mean

    # --- Harmonic / tonal: 12 chroma means (HPCP in Essentia) ---
    # One value per pitch class (C, C#, D, ..., B). Captures harmonic
    # content and tonality. Strong genre discriminator that the
    # original schema lacked entirely — tonal pop and beat-driven
    # hip-hop differ markedly here even when their MFCCs look similar.
    "chroma_1_mean", "chroma_2_mean", "chroma_3_mean", "chroma_4_mean",
    "chroma_5_mean", "chroma_6_mean", "chroma_7_mean", "chroma_8_mean",
    "chroma_9_mean", "chroma_10_mean", "chroma_11_mean", "chroma_12_mean",

    # --- Spectral contrast (6) ---
    # Per-band difference between spectral peaks and valleys. High
    # contrast = harmonic / tonal content (peaks dominate); low contrast
    # = noise-like or beat-driven (valleys filled). Complements MFCCs
    # by separating melody-driven from percussion-driven material.
    "spectral_contrast_1_mean", "spectral_contrast_2_mean",
    "spectral_contrast_3_mean", "spectral_contrast_4_mean",
    "spectral_contrast_5_mean", "spectral_contrast_6_mean",
]

FEATURE_COUNT = len(FEATURE_NAMES)
assert FEATURE_COUNT == 40, f"Expected 40 features, got {FEATURE_COUNT}"

# Default PCA target dimensionality. 25 captures the bulk of variance
# on a 40-feature acoustic space while keeping the embedding compact
# for cosine search. Override via fit_and_transform.py --components N,
# or pass --no-pca to skip reduction entirely (only StandardScaler is
# applied, vectors stored at FEATURE_COUNT dimensions).
PCA_COMPONENTS = 25