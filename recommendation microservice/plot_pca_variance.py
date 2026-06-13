import joblib
import numpy as np
import matplotlib.pyplot as plt

from fit_and_transform import PCA_PATH
from feature_schema import PCA_COMPONENTS, FEATURE_COUNT


def main():
    pca = joblib.load(PCA_PATH)

    per_component = pca.explained_variance_ratio_ * 100
    cumulative = np.cumsum(per_component)

    n = len(per_component)
    x = np.arange(1, n + 1)

    fig, ax1 = plt.subplots(figsize=(8, 5))

    # Bars: per-component variance.
    bar_color = "#6b8cae"
    ax1.bar(
        x, per_component, color=bar_color, alpha=0.85,
        edgecolor="white", label="Per-component variance",
    )
    ax1.set_xlabel("Principal component")
    ax1.set_ylabel("Variance explained (%)", color=bar_color)
    ax1.tick_params(axis="y", labelcolor=bar_color)
    ax1.set_xticks(x)

    # cumulative curve
    ax2 = ax1.twinx()
    line_color = "#c0392b"
    ax2.plot(
        x, cumulative, color=line_color, marker="o",
        linewidth=2, label="Cumulative variance",
    )
    ax2.set_ylabel("Cumulative variance (%)", color=line_color)
    ax2.tick_params(axis="y", labelcolor=line_color)
    ax2.set_ylim(0, 105)

    # 85-95 ref lines
    ax2.axhline(85, linestyle="--", color="gray", linewidth=0.8, alpha=0.6)
    ax2.axhline(95, linestyle="--", color="gray", linewidth=0.8, alpha=0.6)
    ax2.text(n + 0.1, 85, " 85%", va="center", fontsize=8, color="gray")
    ax2.text(n + 0.1, 95, " 95%", va="center", fontsize=8, color="gray")

    # annotate the final cumulative value
    final = cumulative[-1]
    ax2.annotate(
        f"{final:.1f}%",
        xy=(n, final),
        xytext=(n - 2.5, final - 12),
        fontsize=10,
        color=line_color,
        arrowprops=dict(arrowstyle="->", color=line_color, lw=0.8),
    )

    plt.title(
        f"PCA Explained Variance "
        f"({FEATURE_COUNT} → {PCA_COMPONENTS} components)"
    )
    fig.tight_layout()

    fig.savefig("pca_variance.pdf", bbox_inches="tight")
    fig.savefig("pca_variance.png", dpi=200, bbox_inches="tight")
    print(f"Saved pca_variance.pdf and pca_variance.png")
    print(f"Cumulative variance with {n} components: {final:.2f}%")


if __name__ == "__main__":
    main()