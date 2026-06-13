"""
Recommendation evaluation.

Runs the three recommend_for_playlist variants against the live
catalogue and reports Precision@K, Hit Rate@K, MRR, and Genre
Consistency under two evaluation setups:

  Setup A — Genre relevance
    Each playlist with at least one labelled song is a query. A
    recommendation is "relevant" if its Genre matches the playlist's
    dominant genre (modal). Captures the genre-coherence question.

  Setup B — Leave-one-out
    For each (playlist, song) pair where the playlist has >= 2 songs,
    hide that song and query with the remaining ones. The hidden song
    is the single relevant item. Captures the "would the system have
    predicted this song belongs in this playlist?" question.

A random baseline is included for both setups so the absolute numbers
have something to be compared against.

Outputs a markdown table to stdout and a CSV file for further analysis.

Run:
    python evaluation.py
    python evaluation.py --top-k 10 --csv results.csv
"""

import argparse
import csv
import logging
import random
import sys
from collections import Counter
from dataclasses import dataclass, field
from typing import Callable, Dict, List, Optional, Tuple

from db import get_connection
from recommendation_logic import (
    recommend_for_playlist,                  # hybrid (acoustic + genre RRF)
    recommend_for_playlist_acoustic_only,    # acoustic RRF
    recommend_for_playlist_centroid,         # legacy centroid
    GENRE_UNKNOWN,
)

logger = logging.getLogger(__name__)


@dataclass
class Playlist:
    id: str
    song_ids: List[str]
    song_genres: Dict[str, int] = field(default_factory=dict)


def load_playlists() -> List[Playlist]:
    """
    Load every playlist with its song list and per-song genre labels.
    The exact join-table name in the schema is "PlaylistSong" (EF Core
    convention for many-to-many). Adjust if your snapshot uses a
    different name.
    """
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT p."Id", ps."SongsId", s."Genre"
                FROM "Playlists" p
                JOIN "PlaylistSong" ps ON ps."SavedInPlaylistsId" = p."Id"
                JOIN "Songs" s ON s."Id" = ps."SongsId"
                ORDER BY p."Id"
                """
            )
            rows = cur.fetchall()

    by_id: Dict[str, Playlist] = {}
    for pid, sid, genre in rows:
        pid_s, sid_s = str(pid), str(sid)
        if pid_s not in by_id:
            by_id[pid_s] = Playlist(id=pid_s, song_ids=[])
        by_id[pid_s].song_ids.append(sid_s)
        by_id[pid_s].song_genres[sid_s] = int(genre)

    return list(by_id.values())


def load_catalogue_genres() -> Dict[str, int]:
    """All enriched songs and their genres. Used for the random baseline
    and for resolving the genre of recommended songs."""
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Id", "Genre"
                FROM "Songs"
                WHERE "PcaFeatures" IS NOT NULL
                """
            )
            rows = cur.fetchall()
    return {str(sid): int(genre) for sid, genre in rows}


@dataclass
class MetricResult:
    precision_at_k: float
    hit_rate_at_k: float
    mrr: float
    queries: int  # how many queries contributed (for sample-size context)


def evaluate_genre_relevance(
    recommender: Callable[[List[str], int], List[str]],
    playlists: List[Playlist],
    catalogue_genres: Dict[str, int],
    top_k: int,
) -> MetricResult:
    """
    Setup A. For each playlist with at least one labelled song, compute
    P@K, HR@K, and MRR with relevance = top-K item shares the
    playlist's dominant genre.
    """
    p_sum = 0.0
    hr_sum = 0
    mrr_sum = 0.0
    n = 0

    for pl in playlists:
        # dominant genre across labelled songs only.
        labels = [g for g in pl.song_genres.values() if g != GENRE_UNKNOWN]
        if not labels:
            continue

        dominant = Counter(labels).most_common(1)[0][0]
        recs = recommender(pl.song_ids, top_k)
        if not recs:
            continue

        hits = [
            catalogue_genres.get(r, GENRE_UNKNOWN) == dominant
            for r in recs
        ]

        p_sum += sum(hits) / top_k
        hr_sum += int(any(hits))
        try:
            first_hit_rank = hits.index(True) + 1
            mrr_sum += 1.0 / first_hit_rank
        except ValueError:
            pass  # no hit -> contributes 0 to MRR
        n += 1

    if n == 0:
        return MetricResult(0.0, 0.0, 0.0, 0)

    return MetricResult(
        precision_at_k=p_sum / n,
        hit_rate_at_k=hr_sum / n,
        mrr=mrr_sum / n,
        queries=n,
    )


def evaluate_leave_one_out(
    recommender: Callable[[List[str], int], List[str]],
    playlists: List[Playlist],
    top_k: int,
) -> MetricResult:
    """
    Setup B. For each (playlist, song) where the playlist has >= 2
    songs, query with the playlist minus that song and check whether
    the held-out song appears in the top-K.
    """
    p_sum = 0.0
    hr_sum = 0
    mrr_sum = 0.0
    n = 0

    for pl in playlists:
        if len(pl.song_ids) < 2:
            continue

        for held_out in pl.song_ids:
            remaining = [s for s in pl.song_ids if s != held_out]
            recs = recommender(remaining, top_k)

            if not recs:
                continue

            try:
                rank = recs.index(held_out) + 1  # 1-indexed
                hit = True
            except ValueError:
                rank = None
                hit = False

            p_sum += (1.0 / top_k) if hit else 0.0
            hr_sum += int(hit)
            mrr_sum += (1.0 / rank) if rank else 0.0
            n += 1

    if n == 0:
        return MetricResult(0.0, 0.0, 0.0, 0)

    return MetricResult(
        precision_at_k=p_sum / n,
        hit_rate_at_k=hr_sum / n,
        mrr=mrr_sum / n,
        queries=n,
    )


def random_recommender(catalogue_ids: List[str]):
    """
    Returns a recommender callable that picks K random songs from the
    catalogue, excluding the playlist's own songs. Used to establish
    the floor that real recommenders must clear.
    """
    rng = random.Random(42)

    def _recommend(playlist_song_ids: List[str], top_k: int) -> List[str]:
        excluded = set(playlist_song_ids)
        candidates = [s for s in catalogue_ids if s not in excluded]
        if len(candidates) <= top_k:
            return candidates
        return rng.sample(candidates, top_k)

    return _recommend


VARIANTS: List[Tuple[str, Callable]] = [
    ("Hybrid (acoustic + genre RRF)", recommend_for_playlist),
    ("Acoustic-only RRF", recommend_for_playlist_acoustic_only),
    ("Centroid (legacy)", recommend_for_playlist_centroid),
]


def format_markdown_table(
    setup_name: str,
    results: Dict[str, MetricResult],
) -> str:
    lines = [
        f"### {setup_name}",
        "",
        "| Variant | P@K | HR@K | MRR | Queries |",
        "|---|---:|---:|---:|---:|",
    ]
    for name, m in results.items():
        lines.append(
            f"| {name} | {m.precision_at_k:.3f} | {m.hit_rate_at_k:.3f} "
            f"| {m.mrr:.3f} | {m.queries} |"
        )
    return "\n".join(lines)


def write_csv(
    path: str,
    setup_name: str,
    top_k: int,
    results: Dict[str, MetricResult],
    append: bool,
) -> None:
    mode = "a" if append else "w"
    with open(path, mode, newline="") as f:
        w = csv.writer(f)
        if not append:
            w.writerow(["setup", "top_k", "variant", "p_at_k",
                        "hr_at_k", "mrr", "queries"])
        for name, m in results.items():
            w.writerow([
                setup_name, top_k, name,
                f"{m.precision_at_k:.6f}",
                f"{m.hit_rate_at_k:.6f}",
                f"{m.mrr:.6f}",
                m.queries,
            ])


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--top-k", type=int, default=5,
                        help="K for P@K / HR@K / MRR (default 5)")
    parser.add_argument("--csv", type=str, default=None,
                        help="Write results to this CSV path")
    parser.add_argument("--skip-loo", action="store_true",
                        help="Skip the leave-one-out setup (slow on big catalogues)")
    parser.add_argument("-v", "--verbose", action="store_true")
    args = parser.parse_args()

    logging.basicConfig(
        level=logging.DEBUG if args.verbose else logging.WARNING,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    )

    print(f"Loading playlists and catalogue...")
    playlists = load_playlists()
    catalogue_genres = load_catalogue_genres()
    catalogue_ids = list(catalogue_genres.keys())

    print(f"  {len(playlists)} playlists, {len(catalogue_ids)} enriched songs")
    n_labelled = sum(1 for g in catalogue_genres.values() if g != GENRE_UNKNOWN)
    print(f"  {n_labelled} / {len(catalogue_ids)} songs have a genre label")
    print()

    rng_rec = random_recommender(catalogue_ids)
    variants_with_random = VARIANTS + [("Random baseline", rng_rec)]

    # genre relevance
    print("Setup A: Genre relevance")
    print("-" * 60)
    results_a: Dict[str, MetricResult] = {}
    for name, recommender in variants_with_random:
        print(f"  running {name}...")
        results_a[name] = evaluate_genre_relevance(
            recommender, playlists, catalogue_genres, args.top_k,
        )
    print()
    print(format_markdown_table(
        f"Setup A — Genre relevance (K = {args.top_k})", results_a,
    ))
    print()

    if args.csv:
        write_csv(args.csv, "genre_relevance", args.top_k, results_a, append=False)

    # leave one out
    if not args.skip_loo:
        print("Setup B: Leave-one-out")
        print("-" * 60)
        results_b: Dict[str, MetricResult] = {}
        for name, recommender in variants_with_random:
            print(f"  running {name}...")
            results_b[name] = evaluate_leave_one_out(
                recommender, playlists, args.top_k,
            )
        print()
        print(format_markdown_table(
            f"Setup B — Leave-one-out (K = {args.top_k})", results_b,
        ))
        print()

        if args.csv:
            write_csv(args.csv, "leave_one_out", args.top_k, results_b, append=True)

    if args.csv:
        print(f"Results written to {args.csv}")


if __name__ == "__main__":
    sys.exit(main() or 0)