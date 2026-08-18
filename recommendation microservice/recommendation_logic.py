import logging
from collections import Counter, defaultdict
from typing import Dict, List, Optional, Tuple

import numpy as np

from db import get_connection

logger = logging.getLogger(__name__)

RRF_K = 60
DEFAULT_CANDIDATES_PER_SONG = 30
GENRE_UNKNOWN = 0


def load_playlist_song_metadata(
    song_ids: List[str],
) -> List[Tuple[str, np.ndarray, int]]:
    
    if not song_ids:
        return []

    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Id", "PcaFeatures", "Genre"
                FROM "Songs"
                WHERE "Id" = ANY(%s::uuid[])
                  AND "PcaFeatures" IS NOT NULL
                """,
                (song_ids,),
            )
            rows = cur.fetchall()

    return [
        (str(sid), np.asarray(vec, dtype=np.float32), int(genre))
        for sid, vec, genre in rows
    ]


def load_candidate_genres(candidate_ids: List[str]) -> Dict[str, int]:
    """
    Bulk-fetch Genre for a list of song ids. Returns {song_id: genre_int}.
    Songs not found are simply absent from the dict.
    """
    if not candidate_ids:
        return {}

    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Id", "Genre"
                FROM "Songs"
                WHERE "Id" = ANY(%s::uuid[])
                """,
                (candidate_ids,),
            )
            rows = cur.fetchall()

    return {str(sid): int(genre) for sid, genre in rows}


def fetch_top_similar_songs(
    query_vector: np.ndarray,
    top_k: int,
    exclude_ids: Optional[List[str]] = None,
) -> List[str]:
    
    exclude = list(exclude_ids) if exclude_ids else []

    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Id"
                FROM "Songs"
                WHERE "PcaFeatures" IS NOT NULL
                  AND "Id" <> ALL(%s::uuid[])
                ORDER BY "PcaFeatures" <=> %s::vector
                LIMIT %s
                """,
                (exclude, query_vector.tolist(), top_k),
            )
            rows = cur.fetchall()

    return [str(row[0]) for row in rows]


#rrf helpers

def _acoustic_rrf_scores(
    playlist_song_vectors: List[Tuple[str, np.ndarray]],
    exclude_ids: List[str],
    candidates_per_song: int,
) -> Dict[str, float]:
    
    scores: Dict[str, float] = defaultdict(float)
    exclude_set = set(exclude_ids)

    for _, vec in playlist_song_vectors:
        neighbors = fetch_top_similar_songs(
            query_vector=vec,
            top_k=candidates_per_song,
            exclude_ids=exclude_ids,
        )
        for rank, nid in enumerate(neighbors):
            if nid in exclude_set:
                continue
            scores[nid] += 1.0 / (RRF_K + rank + 1)

    return dict(scores)


def _playlist_genre_distribution(genres: List[int]) -> Dict[int, float]:
    # face distribution pt fiecare genre din playlist, total = 1
    counter = Counter(g for g in genres if g != GENRE_UNKNOWN)
    total = sum(counter.values())
    if total == 0:
        return {}
    return {g: c / total for g, c in counter.items()}




def recommend_for_playlist(
    song_ids: List[str],
    top_k: int = 5,
    candidates_per_song: int = DEFAULT_CANDIDATES_PER_SONG,
) -> List[str]:
    
    if not song_ids:
        return []

    playlist_meta = load_playlist_song_metadata(song_ids)
    if not playlist_meta:
        logger.info(
            "Playlist had %d songs but none have PcaFeatures yet — "
            "no recommendation can be computed.", len(song_ids),
        )
        return []

    #acoustic ranking
    playlist_vectors = [(sid, vec) for sid, vec, _ in playlist_meta]
    acoustic_scores = _acoustic_rrf_scores(
        playlist_song_vectors=playlist_vectors,
        exclude_ids=song_ids,
        candidates_per_song=candidates_per_song,
    )

    if not acoustic_scores:
        return []

    playlist_genres = [genre for _, _, genre in playlist_meta]
    genre_dist = _playlist_genre_distribution(playlist_genres)

    if not genre_dist:
        logger.debug("Playlist has no labelled genres; acoustic-only fallback.")
        ranked = sorted(acoustic_scores.items(), key=lambda x: x[1], reverse=True)
        return [nid for nid, _ in ranked[:top_k]]

    # genre rank 
    candidate_ids = list(acoustic_scores.keys())
    candidate_genres = load_candidate_genres(candidate_ids)

    genre_scores: Dict[str, float] = {
        cid: genre_dist.get(candidate_genres.get(cid, GENRE_UNKNOWN), 0.0)
        for cid in candidate_ids
    }

    acoustic_ranking = sorted(
        candidate_ids,
        key=lambda c: acoustic_scores[c],
        reverse=True,
    )
    genre_ranking = sorted(
        candidate_ids,
        key=lambda c: (genre_scores[c], acoustic_scores[c]),
        reverse=True,
    )

    #second rrf fuse
    fused: Dict[str, float] = defaultdict(float)
    for rank, cid in enumerate(acoustic_ranking):
        fused[cid] += 1.0 / (RRF_K + rank + 1)
    for rank, cid in enumerate(genre_ranking):
        fused[cid] += 1.0 / (RRF_K + rank + 1)

    ranked = sorted(fused.items(), key=lambda x: x[1], reverse=True)
    return [cid for cid, _ in ranked[:top_k]]


# helperi pt eval


def recommend_for_playlist_acoustic_only(
    song_ids: List[str],
    top_k: int = 5,
    candidates_per_song: int = DEFAULT_CANDIDATES_PER_SONG,
) -> List[str]:
    # rrf fara genres
    if not song_ids:
        return []

    meta = load_playlist_song_metadata(song_ids)
    if not meta:
        return []

    playlist_vectors = [(sid, vec) for sid, vec, _ in meta]
    scores = _acoustic_rrf_scores(
        playlist_song_vectors=playlist_vectors,
        exclude_ids=song_ids,
        candidates_per_song=candidates_per_song,
    )

    if not scores:
        return []

    ranked = sorted(scores.items(), key=lambda x: x[1], reverse=True)
    return [cid for cid, _ in ranked[:top_k]]


def compute_playlist_vector(song_ids: List[str]) -> Optional[np.ndarray]:
    # avg la playlist vector pt varianta veche
    pairs = load_playlist_song_metadata(song_ids)
    if not pairs:
        return None

    feature_matrix = np.stack([vec for _, vec, _ in pairs], axis=0)
    return feature_matrix.mean(axis=0)


def recommend_for_playlist_centroid(
    song_ids: List[str], top_k: int = 5,
) -> List[str]:
    #recomandarea veche
    vector = compute_playlist_vector(song_ids)
    if vector is None:
        return []

    return fetch_top_similar_songs(
        query_vector=vector,
        top_k=top_k,
        exclude_ids=song_ids,
    )