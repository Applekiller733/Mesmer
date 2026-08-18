import os
import logging
import tempfile
from contextlib import contextmanager
from typing import Optional

import requests

logger = logging.getLogger(__name__)

API_BASE_URL = os.getenv("DOTNET_API_BASE_URL", "http://localhost:5050")


@contextmanager
def fetch_audio_to_tempfile(song_id: str, timeout: int = 60):
    
    url = f"{API_BASE_URL}/songs/{song_id}/audio"
    tmp_path: Optional[str] = None

    try:
        with requests.get(url, stream=True, timeout=timeout) as resp:
            if resp.status_code == 404:
                #song has no audio, just skip
                logger.debug("Song %s has no audio (404)", song_id)
                yield None
                return

            if not resp.ok:
                logger.warning(
                    "Audio fetch for %s returned %d", song_id, resp.status_code
                )
                yield None
                return

            # try to infer the file type from the Content-Type header
            suffix = ".audio"
            content_type = resp.headers.get("Content-Type", "")
            if "mpeg" in content_type or content_type.endswith("/mp3"):
                suffix = ".mp3"
            elif "wav" in content_type:
                suffix = ".wav"
            elif "flac" in content_type:
                suffix = ".flac"
            elif "ogg" in content_type:
                suffix = ".ogg"

            # delete=False is required on windows
            with tempfile.NamedTemporaryFile(
                suffix=suffix, delete=False
            ) as tmp:
                for chunk in resp.iter_content(chunk_size=64 * 1024):
                    if chunk:
                        tmp.write(chunk)
                tmp_path = tmp.name

            yield tmp_path

    except requests.RequestException as e:
        logger.warning("Audio fetch error for song %s: %s", song_id, e)
        yield None
    finally:
        if tmp_path and os.path.exists(tmp_path):
            try:
                os.unlink(tmp_path)
            except OSError as e:
                logger.debug("Couldn't remove temp file %s: %s", tmp_path, e)