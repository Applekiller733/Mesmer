import logging
from typing import List, Optional

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, ConfigDict

from db import get_connection
from recommendation_logic import recommend_for_playlist

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
for noisy in ("urllib3", "asyncio"):
    logging.getLogger(noisy).setLevel(logging.WARNING)

logger = logging.getLogger("api")

app = FastAPI(title="Recommendation Service")

DEFAULT_TOP_K = 5

# DTOs
class SongRecommendationDTO(BaseModel):
    # populate_by_name=True: accepta "id" sau "Id" on input.
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



@app.post("/recommend-ids", response_model=RecommendationResponse)
async def get_recommendation_ids(request: PlaylistRecommendationDTO):

    try:
        if not request.songs:
            logger.info("Empty playlist for %s, returning no recommendations.",
                        request.id)
            return RecommendationResponse(recommendedIds=[])

        song_ids = [s.id for s in request.songs]
        recommended = recommend_for_playlist(
            song_ids=song_ids,
            top_k=DEFAULT_TOP_K,
        )

        if not recommended:
            logger.info(
                "No recommendations for playlist %s (input songs: %d). "
                "Most likely cause: input songs not yet enriched. Run "
                "the bulk enricher then fit_and_transform.",
                request.id, len(song_ids),
            )

        return RecommendationResponse(recommendedIds=recommended)

    except Exception as e:
        
        logger.exception("Recommendation failed for playlist %s: %s",
                         request.id, e)
        raise HTTPException(status_code=500, detail="Internal processing error.")


@app.get("/health")
async def health():
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