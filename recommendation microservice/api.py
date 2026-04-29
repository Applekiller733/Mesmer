import logging
from typing import List, Optional

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, ConfigDict

from db import get_connection
from recommendation_logic import recommend_for_playlist

# Logging setup. App-level INFO, libraries quieted.
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
for noisy in ("urllib3", "asyncio"):
    logging.getLogger(noisy).setLevel(logging.WARNING)

logger = logging.getLogger("api")

app = FastAPI(title="Recommendation Service")

# Default top-K. Matches the previous DSSM API's default so the .NET
# caller behaves identically without changing.
DEFAULT_TOP_K = 5


# ---- DTOs (preserve previous shapes for compat) ---------------------------


class SongRecommendationDTO(BaseModel):
    # populate_by_name=True: accept either "id" or "Id" on input.
    # alias_generator: when serializing OUT, fields with no explicit
    # alias get camelCased. Doesn't matter for our outputs, but it's
    # the standard pairing.
    model_config = ConfigDict(populate_by_name=True)

    id: str
    name: str
    artist: str
    embedding: Optional[List[float]] = None


class PlaylistRecommendationDTO(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    id: str
    songs: List[SongRecommendationDTO]


class RecommendationResponse(BaseModel):
    recommendedIds: List[str]


# ---- Endpoints -------------------------------------------------------------


@app.post("/recommend-ids", response_model=RecommendationResponse)
async def get_recommendation_ids(request: PlaylistRecommendationDTO):
    """
    Given a playlist (id + list of songs), return up to DEFAULT_TOP_K
    recommended song IDs based on cosine similarity in PCA-reduced
    feature space.

    Behavior:
      - Empty playlist → 200 with empty list.
      - All input songs un-enriched → 200 with empty list (logs a hint).
      - DB unreachable / unexpected error → 500.
    """
    try:
        if not request.songs:
            logger.info("Empty playlist for %s; returning no recommendations.",
                        request.id)
            return RecommendationResponse(recommendedIds=[])

        song_ids = [s.id for s in request.songs]
        recommended = recommend_for_playlist(
            song_ids=song_ids,
            top_k=DEFAULT_TOP_K,
        )

        if not recommended:
            # Could be because no input songs are enriched, or because
            # there are no other enriched songs in the catalog yet.
            logger.info(
                "No recommendations for playlist %s (input songs: %d). "
                "Most likely cause: input songs not yet enriched. Run "
                "the bulk enricher then fit_and_transform.",
                request.id, len(song_ids),
            )

        return RecommendationResponse(recommendedIds=recommended)

    except Exception as e:
        # Catch-all so unexpected errors return a clean 500 rather than
        # leaking stack traces to the .NET caller.
        logger.exception("Recommendation failed for playlist %s: %s",
                         request.id, e)
        raise HTTPException(status_code=500, detail="Internal processing error.")


@app.get("/health")
async def health():
    """
    Liveness + readiness check. Returns DB connectivity and how many
    songs are currently recommendable (have PcaFeatures populated).
    A 200 here means the service can answer recommendation requests.
    """
    try:
        with get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    """
                    SELECT
                        COUNT(*) FILTER (WHERE "PcaFeatures" IS NOT NULL),
                        COUNT(*)
                    FROM "Songs"
                    """
                )
                with_pca, total = cur.fetchone()

        return {
            "status": "ok",
            "songs_total": total,
            "songs_recommendable": with_pca,
            "coverage_pct": round(100 * with_pca / total, 1) if total else 0.0,
        }
    except Exception as e:
        logger.exception("Health check failed: %s", e)
        raise HTTPException(status_code=503, detail="Service unavailable")