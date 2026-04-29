import logging
from typing import List, Optional

import numpy as np

from db import get_connection

logger = logging.getLogger(__name__)


def compute_playlist_vector(song_ids: List[str]) -> Optional[np.ndarray]:
    """
    Load the PcaFeatures of the given songs and average them into a
    single 15-D vector.

    Returns None if none of the songs have PcaFeatures (the playlist is
    entirely un-enriched or doesn't exist). Logs a warning when only
    some songs have features — the average is computed over what's
    available, since that's still meaningful.
    """
    if not song_ids:
        return None

    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "PcaFeatures"
                FROM "Songs"
                WHERE "Id" = ANY(%s::uuid[])
                AND "PcaFeatures" IS NOT NULL
                        """,
                (song_ids,),
            )
            rows = cur.fetchall()

    if not rows:
        logger.info(
            "Playlist had %d songs but none have PcaFeatures yet — "
            "no vector can be computed.", len(song_ids),
        )
        return None

    if len(rows) < len(song_ids):
        logger.debug(
            "Playlist vector built from %d / %d enriched songs",
            len(rows), len(song_ids),
        )

    feature_matrix = np.stack([row[0] for row in rows], axis=0)
    return feature_matrix.mean(axis=0)


def fetch_top_similar_songs(
    playlist_vector: np.ndarray,
    top_k: int = 5,
    exclude_ids: Optional[List[str]] = None,
) -> List[str]:
    """
    Run a cosine-similarity search against the Songs table, returning
    the top_k most similar IDs.

    pgvector's <=> operator computes cosine DISTANCE (smaller = more
    similar), which matches ORDER BY ASC. Songs without PcaFeatures are
    naturally excluded because the IS NOT NULL filter is applied.
    """
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
                (exclude, playlist_vector.tolist(), top_k),
            )
            rows = cur.fetchall()

    return [str(row[0]) for row in rows]


def recommend_for_playlist(
    song_ids: List[str], top_k: int = 5
) -> List[str]:
    """
    Convenience wrapper that combines the two steps. Returns top_k
    recommended song IDs (as strings) for the given playlist, or an
    empty list if no recommendation could be computed.
    """
    vector = compute_playlist_vector(song_ids)
    if vector is None:
        return []

    return fetch_top_similar_songs(
        playlist_vector=vector,
        top_k=top_k,
        exclude_ids=song_ids,
    )