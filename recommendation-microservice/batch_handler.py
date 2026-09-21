"""AWS Lambda entry point for the scheduled background song-processing pipeline.

Runs the same three stages an operator would run by hand, in order:
  A/B. bulkenricher  — resolve MusicBrainz IDs, fetch AcousticBrainz features
  C.   librosa       — fallback feature extraction for Failed songs with audio
  D.   fit_and_transform (transform mode) — write PcaFeatures embeddings

Each stage is sized to the Lambda's *live* remaining time so a single run can
never overrun the function timeout: MusicBrainz is rate-limited to 1 req/s and
librosa costs ~2-5s per song, so the per-run limit is capped at what fits in the
time left. Whatever isn't processed this run is picked up on the next schedule.
The CLI main() entry points in each module are untouched for local use.
"""
import logging
import os
import time

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
for _noisy in ("numba", "matplotlib", "PIL", "urllib3", "audioread", "soundfile"):
    logging.getLogger(_noisy).setLevel(logging.WARNING)

log = logging.getLogger("batch_handler")

# Per-run ceilings (env-overridable). These are upper bounds; the actual limit
# each run is the smaller of this and what fits in the remaining Lambda time.
MBID_LIMIT = int(os.getenv("MBID_LIMIT", "300"))
AB_LIMIT = int(os.getenv("AB_LIMIT", "500"))
LIBROSA_LIMIT = int(os.getenv("LIBROSA_LIMIT", "120"))

# Rough per-item costs used to fit a stage into the time left.
SECONDS_PER_MBID = float(os.getenv("SECONDS_PER_MBID", "1.2"))     # 1 req/s + overhead
SECONDS_PER_LIBROSA = float(os.getenv("SECONDS_PER_LIBROSA", "5.0"))  # download + extract

# Time to reserve for the fast stages (AB fetch + transform) and teardown.
SAFETY_MARGIN_S = float(os.getenv("BATCH_SAFETY_MARGIN_S", "45"))

# Fallback budget when there is no Lambda context (e.g. local `python batch_handler.py`).
LOCAL_BUDGET_S = float(os.getenv("BATCH_LOCAL_BUDGET_S", "840"))


def _remaining_seconds(context) -> float:
    if context is not None and hasattr(context, "get_remaining_time_in_millis"):
        return context.get_remaining_time_in_millis() / 1000.0
    return LOCAL_BUDGET_S


def _fit_limit(available_s: float, per_item_s: float, ceiling: int) -> int:
    usable = available_s - SAFETY_MARGIN_S
    if usable <= 0:
        return 0
    return max(0, min(ceiling, int(usable / per_item_s)))


def handler(event, context):
    # Imported lazily so an import error in one module doesn't stop the others
    # from being reported, and to keep cold-start import cost inside the invoke.
    from bulkenricher import (
        resolve_mbids_for_pending_songs,
        enrich_features_for_resolved_songs,
    )
    from librosa_enricher import enrich_failed_songs_with_audio
    from fit_and_transform import run_transform_mode

    started = time.time()
    summary = {"mbids_resolved": 0, "ab_enriched": 0, "librosa_enriched": 0,
               "transform_ran": False, "stages_skipped": [], "errors": []}

    # --- Stage A: MusicBrainz MBID resolution (1 req/s) ---
    limit_a = _fit_limit(_remaining_seconds(context), SECONDS_PER_MBID, MBID_LIMIT)
    if limit_a > 0:
        try:
            log.info("Stage A: resolving up to %d MBIDs", limit_a)
            summary["mbids_resolved"] = resolve_mbids_for_pending_songs(limit=limit_a)
        except Exception:
            log.exception("Stage A (MBID resolution) failed")
            summary["errors"].append("mbid")
    else:
        summary["stages_skipped"].append("mbid")

    # --- Stage B: AcousticBrainz feature fetch (bulk, fast) ---
    if _remaining_seconds(context) > SAFETY_MARGIN_S:
        try:
            log.info("Stage B: fetching AcousticBrainz features (limit %d)", AB_LIMIT)
            summary["ab_enriched"] = enrich_features_for_resolved_songs(limit=AB_LIMIT)
        except Exception:
            log.exception("Stage B (AcousticBrainz) failed")
            summary["errors"].append("acousticbrainz")
    else:
        summary["stages_skipped"].append("acousticbrainz")

    # --- Stage C: librosa fallback (~2-5s/song) ---
    limit_c = _fit_limit(_remaining_seconds(context), SECONDS_PER_LIBROSA, LIBROSA_LIMIT)
    if limit_c > 0:
        try:
            log.info("Stage C: librosa extraction for up to %d songs", limit_c)
            summary["librosa_enriched"] = enrich_failed_songs_with_audio(limit=limit_c)
        except Exception:
            log.exception("Stage C (librosa) failed")
            summary["errors"].append("librosa")
    else:
        summary["stages_skipped"].append("librosa")

    # --- Stage D: transform RawFeatures -> PcaFeatures embeddings (fast) ---
    if _remaining_seconds(context) > SAFETY_MARGIN_S:
        try:
            log.info("Stage D: transform mode (writing PcaFeatures)")
            run_transform_mode()
            summary["transform_ran"] = True
        except Exception:
            log.exception("Stage D (fit_and_transform transform) failed")
            summary["errors"].append("transform")
    else:
        summary["stages_skipped"].append("transform")

    summary["elapsed_s"] = round(time.time() - started, 1)
    log.info("Batch run complete: %s", summary)
    return summary


if __name__ == "__main__":
    # Local run: no Lambda context, uses LOCAL_BUDGET_S.
    print(handler({}, None))
