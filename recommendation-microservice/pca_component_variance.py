import joblib
import numpy as np
from fit_and_transform import (
    load_all_raw_features, load_models, transform_to_storage_space,
)

scaler, pca = load_models()
_, raw = load_all_raw_features()

scaled = scaler.transform(raw)
projected = pca.transform(scaled)

variances = np.var(projected, axis=0)
print("Output variance per component (first 10):")
print(np.round(variances[:10], 3))
print(f"\nMin: {variances.min():.3f}, Max: {variances.max():.3f}")