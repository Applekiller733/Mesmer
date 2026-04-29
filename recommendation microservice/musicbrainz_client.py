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
    """
    Look up a MusicBrainz recording MBID by song name + artist name.

    Returns the MBID of the top-ranked match, or None if no result. The
    "top-ranked" choice is what MusicBrainz's relevance scoring picks —
    we trust their scoring rather than implementing our own.

    Why no fuzzy threshold? MusicBrainz's score is already a fuzzy match
    quality (0-100). For names with typos or extra punctuation, the top
    hit is usually still the right recording. We accept it and move on.
    Errors propagate to the enricher which marks the song as Failed.

    On 503 (rate limited despite our 1s spacing), retry with exponential
    backoff up to max_retries times. Other errors return None and let
    the caller decide what to do.
    """
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
            # 4xx-level errors that aren't transient. No point retrying.
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