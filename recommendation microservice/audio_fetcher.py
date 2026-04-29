import os
import logging
import tempfile
from contextlib import contextmanager
from typing import Optional

import requests

logger = logging.getLogger(__name__)

# Where the .NET API lives. Defaults to localhost:5050 (typical dev
# binding). Override via env var when the API is on a different host.
API_BASE_URL = os.getenv("DOTNET_API_BASE_URL", "http://localhost:5050")


@contextmanager
def fetch_audio_to_tempfile(song_id: str, timeout: int = 60):
    """
    Context manager that fetches a song's audio to a temp file and
    cleans up on exit.

    Usage:
        with fetch_audio_to_tempfile(song_id) as path:
            if path is None:
                # song has no audio or fetch failed
                continue
            # use `path` to load the audio
        # tempfile is removed automatically here

    Yields:
        str path to the temp file on success, or None on failure
        (network error, 404, etc.). The caller is expected to skip
        the song if path is None.

    Why a context manager? Audio files are several MB each. If the
    caller forgot a cleanup step we'd accumulate gigabytes of temp
    files during a long enrichment run. The CM enforces cleanup.
    """
    url = f"{API_BASE_URL}/songs/{song_id}/audio"
    tmp_path: Optional[str] = None

    try:
        # stream=True so we don't load the whole file into memory before
        # writing to disk. Useful for the 50MB upper bound we set.
        with requests.get(url, stream=True, timeout=timeout) as resp:
            if resp.status_code == 404:
                # Song has no uploaded audio. Different from a network
                # error — caller probably wants to log this but not
                # treat it as a transient failure.
                logger.debug("Song %s has no audio (404)", song_id)
                yield None
                return

            if not resp.ok:
                logger.warning(
                    "Audio fetch for %s returned %d", song_id, resp.status_code
                )
                yield None
                return

            # Use the URL path's extension if present, else fall back to
            # whatever the Content-Type implies. We don't actually need
            # the right extension for librosa — it sniffs by content —
            # but a sensible suffix helps when debugging.
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

            # delete=False is required on Windows — the file can't be
            # opened by another process (librosa) while NamedTemporaryFile
            # is still holding it open with delete=True.
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
                # Not fatal — tempfiles in OS temp dir get cleaned up
                # on reboot if nothing else.
                logger.debug("Couldn't remove temp file %s: %s", tmp_path, e)