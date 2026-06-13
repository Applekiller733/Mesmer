import matplotlib.pyplot as plt
import numpy as np

from db import get_connection


GENRE_NAMES = [
    "Unknown",   
    "Pop",       
    "Rock",      
    "HipHop",    
    "RnB",       
    "Electronic", 
    "Dance",     
    "Jazz",      
    "Classical", 
    "Country",   
    "Folk",      
    "Metal",     
    "Latin",     
    "Reggae",    
    "World",     
    "Other",    
]


def load_genre_counts() -> dict[int, int]:
    """Returns {genre_int: count} for the full catalogue."""
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Genre", COUNT(*)
                FROM "Songs"
                GROUP BY "Genre"
                ORDER BY "Genre"
                """
            )
            rows = cur.fetchall()
    return {int(g): int(c) for g, c in rows}


def load_enriched_counts() -> dict[int, int]:
    """Genre counts restricted to enriched (recommendable) songs.
    Useful for understanding what proportion of each genre actually
    contributes to recommendations."""
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Genre", COUNT(*)
                FROM "Songs"
                WHERE "PcaFeatures" IS NOT NULL
                GROUP BY "Genre"
                ORDER BY "Genre"
                """
            )
            rows = cur.fetchall()
    return {int(g): int(c) for g, c in rows}


def print_table(total: dict[int, int], enriched: dict[int, int]) -> None:
    grand_total = sum(total.values())
    grand_enriched = sum(enriched.values())

    print(f"{'Genre':<12} {'Total':>8} {'Enriched':>10} {'% of total':>12}")
    print("-" * 46)

    sorted_genres = sorted(total.keys(), key=lambda g: total[g], reverse=True)
    for g in sorted_genres:
        name = GENRE_NAMES[g] if 0 <= g < len(GENRE_NAMES) else f"?{g}"
        t = total[g]
        e = enriched.get(g, 0)
        pct = 100 * t / grand_total if grand_total else 0
        print(f"{name:<12} {t:>8} {e:>10} {pct:>11.1f}%")

    print("-" * 46)
    print(f"{'TOTAL':<12} {grand_total:>8} {grand_enriched:>10}")
    print()
    labelled = sum(c for g, c in total.items() if g != 0)
    print(f"Labelled songs: {labelled} / {grand_total} "
          f"({100*labelled/grand_total:.1f}%)" if grand_total else "")


def plot_distribution(total: dict[int, int], enriched: dict[int, int]) -> None:
    """Horizontal bar chart, sorted by total count. Each genre gets
    two bars side by side: total catalogue count and enriched count."""

    sorted_genres = sorted(total.keys(), key=lambda g: total[g])

    names = [
        GENRE_NAMES[g] if 0 <= g < len(GENRE_NAMES) else f"?{g}"
        for g in sorted_genres
    ]
    totals = [total[g] for g in sorted_genres]
    enriched_counts = [enriched.get(g, 0) for g in sorted_genres]

    y = np.arange(len(names))
    bar_height = 0.38

    fig, ax = plt.subplots(figsize=(8, max(4, 0.4 * len(names) + 1)))

    total_color = "#6b8cae"
    enriched_color = "#c0392b"

    ax.barh(y - bar_height / 2, totals, bar_height,
            color=total_color, label="Total in catalogue", alpha=0.9)
    ax.barh(y + bar_height / 2, enriched_counts, bar_height,
            color=enriched_color, label="Enriched (recommendable)", alpha=0.9)

    #annotare la fiecare bara
    max_count = max(totals) if totals else 1
    offset = max_count * 0.01
    for i, (t, e) in enumerate(zip(totals, enriched_counts)):
        ax.text(t + offset, i - bar_height / 2, str(t),
                va="center", fontsize=8, color=total_color)
        ax.text(e + offset, i + bar_height / 2, str(e),
                va="center", fontsize=8, color=enriched_color)

    ax.set_yticks(y)
    ax.set_yticklabels(names)
    ax.set_xlabel("Number of songs")
    ax.set_title("Genre distribution across the catalogue")
    ax.legend(loc="lower right")
    ax.grid(axis="x", linestyle="--", alpha=0.4)

    #small headroom
    ax.set_xlim(0, max_count * 1.12)

    fig.tight_layout()
    fig.savefig("genre_distribution.pdf", bbox_inches="tight")
    fig.savefig("genre_distribution.png", dpi=200, bbox_inches="tight")
    print("Saved genre_distribution.pdf and genre_distribution.png")


def main():
    total = load_genre_counts()
    enriched = load_enriched_counts()

    if not total:
        print("No songs found in the catalogue.")
        return 1

    print_table(total, enriched)
    print()
    plot_distribution(total, enriched)
    return 0


if __name__ == "__main__":
    import sys
    sys.exit(main())