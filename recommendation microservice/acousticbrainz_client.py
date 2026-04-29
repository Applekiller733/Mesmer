import time
import logging
from typing import Iterable, Mapping, Optional

import requests

logger = logging.getLogger(__name__)

BASE_URL = "https://acousticbrainz.org"
BULK_LOWLEVEL_PATH = "/api/v1/low-level"
MAX_BATCH_SIZE = 25  # AcousticBrainz's documented MAX_ITEMS_PER_BULK_REQUEST


def fetch_lowlevel_bulk(
    mbids: Iterable[str],
    timeout: int = 30,
    max_retries: int = 3,
) -> Mapping[str, dict]:
    """
    Fetch lowlevel feature data for up to MAX_BATCH_SIZE MBIDs in one call.

    Returns a dict keyed by normalised lowercase MBID, containing the
    lowlevel document at offset "0" (we don't care about multiple
    submissions per recording for our use case — we use the first one).

    MBIDs not present in AcousticBrainz are silently omitted from the
    returned dict. The caller compares input MBIDs to the keys to learn
    which ones had no data.

    Implementation detail: AcousticBrainz returns the raw response in
    the shape:
        {
          "mbid1": {"0": {...lowlevel doc...}, "1": {...alt submission...}},
          "mbid2": {"0": {...}},
          "mbid_mapping": {...}
        }
    We flatten this to {mbid: doc_at_offset_0} for the caller's
    convenience.
    """
    mbid_list = list(mbids)
    if not mbid_list:
        return {}
    if len(mbid_list) > MAX_BATCH_SIZE:
        raise ValueError(
            f"AcousticBrainz bulk endpoint accepts at most {MAX_BATCH_SIZE} MBIDs"
            f" per request, got {len(mbid_list)}."
        )

    # MBIDs go in semicolon-separated, no spaces. The endpoint takes them
    # as a query parameter rather than POST body.
    params = {"recording_ids": ";".join(mbid_list)}

    for attempt in range(max_retries):
        try:
            resp = requests.get(
                f"{BASE_URL}{BULK_LOWLEVEL_PATH}",
                params=params,
                timeout=timeout,
            )
        except requests.RequestException as e:
            wait = 2 ** attempt
            logger.warning(
                "AcousticBrainz request error: %s; retry in %ds (%d/%d)",
                e, wait, attempt + 1, max_retries,
            )
            time.sleep(wait)
            continue

        # 429: explicit "you're over the limit". Use the Retry-After or
        # X-RateLimit-Reset-In header rather than guessing.
        if resp.status_code == 429:
            wait = int(resp.headers.get("X-RateLimit-Reset-In", "10")) + 1
            logger.warning(
                "AcousticBrainz rate limited (429); sleeping %ds", wait
            )
            time.sleep(wait)
            continue

        if not resp.ok:
            logger.warning(
                "AcousticBrainz returned %d: %s",
                resp.status_code, resp.text[:200],
            )
            return {}

        # Pre-emptive throttle: if we're about to run out of budget,
        # sleep until the window resets. Avoids the 429 dance entirely.
        _maybe_sleep_for_rate_limit(resp.headers)

        try:
            data = resp.json()
        except ValueError:
            logger.warning("AcousticBrainz returned non-JSON response")
            return {}

        # The mbid_mapping key (if present) maps user-supplied MBID forms
        # back to the canonical lowercase form. We don't need it because
        # we always send canonical form. Strip it from the output.
        data.pop("mbid_mapping", None)

        # Flatten {mbid: {"0": doc, "1": doc}} → {mbid: doc_at_0}.
        # We use offset 0 — the first submission for this recording.
        # Multiple submissions exist when the same audio file was
        # analyzed multiple times; their feature values are nearly
        # identical so picking the first is fine.
        flattened = {}
        for mbid, offsets in data.items():
            if isinstance(offsets, dict) and "0" in offsets:
                flattened[mbid] = offsets["0"]

        return flattened

    logger.error(
        "AcousticBrainz bulk fetch exhausted retries (%d MBIDs)", len(mbid_list)
    )
    return {}


def _maybe_sleep_for_rate_limit(headers: Mapping[str, str]) -> None:
    """
    If the response headers indicate we're nearly out of budget, sleep
    until the window resets. Conservative: triggers when remaining <= 1
    so we always have at least one in reserve for a retry.
    """
    try:
        remaining = int(headers.get("X-RateLimit-Remaining", "999"))
        reset_in = int(headers.get("X-RateLimit-Reset-In", "0"))
    except ValueError:
        return

    if remaining <= 1 and reset_in > 0:
        # +0.5s margin to avoid races with the server's clock.
        sleep_for = reset_in + 0.5
        logger.info(
            "AcousticBrainz rate limit nearly exhausted (remaining=%d), "
            "sleeping %.1fs", remaining, sleep_for,
        )
        time.sleep(sleep_for)