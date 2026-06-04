import joblib
from feature_schema import FEATURE_NAMES
from fit_and_transform import PCA_PATH

pca = joblib.load(PCA_PATH)
loadings = pca.components_[0]  # PC1

# Top features pulling PC1 in either direction
ranked = sorted(zip(FEATURE_NAMES, loadings), key=lambda x: abs(x[1]), reverse=True)
print(f"PC1 explains {pca.explained_variance_ratio_[0]:.1%} of variance.\n")
print("Top 10 features by absolute loading:")
for name, w in ranked[:10]:
    print(f"  {w:+.3f}  {name}")