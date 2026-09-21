import os
import time
import logging
from typing import Optional

import musicbrainzngs

logger = logging.getLogger(__name__)

_USER_AGENT_APP = os.getenv("MUSICBRAINZ_USER_AGENT_APP", "SongAppRecommender")
_USER_AGENT_VERSION = os.getenv("MUSICBRAINZ_USER_AGENT_VERSION", "0.1")
_USER_AGENT_CONTACT = os.getenv("MUSICBRAINZ_USER_AGENT_CONTACT", "noreply@example.com")

musicbrainzngs.set_useragent(
    _USER_AGENT_APP, _USER_AGENT_VERSION, _USER_AGENT_CONTACT
)

musicbrainzngs.set_rate_limit(limit_or_interval=1.0, new_requests=1)

def search_recording_mbid(
    name: str, artist: str, max_retries: int = 3
) -> Optional[str]:
    
    #try to get mbid based on song name si artist
    for attempt in range(max_retries):
        try:
            query = f'recording:"{name}" AND artist:"{artist}"'
            result = musicbrainzngs.search_recordings(query=query, limit=1)
        except musicbrainzngs.NetworkError as e:
            # 503 / connection errors: retry with backoff.
            wait = 2 ** attempt
            logger.warning(
                "MusicBrainz network error (%s); retrying in %ds (attempt %d/%d)",
                e, wait, attempt + 1, max_retries,
            )
            time.sleep(wait)
            continue
        except musicbrainzngs.WebServiceError as e:
            # 4xx-level errors no point retrying.
            logger.warning(
                "MusicBrainz web service error for %s by %s: %s",
                name, artist, e,
            )
            return None

        recordings = result.get("recording-list", [])
        if not recordings:
            return None

        return recordings[0].get("id")

    logger.error(
        "MusicBrainz lookup exhausted retries for %s by %s",
        name, artist,
    )
    return None