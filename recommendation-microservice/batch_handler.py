"""AWS Lambda entry point for the recommendation background pipeline.

Two actions, selected by the invocation event's "action" field:

  "pipeline" (default — this is what the schedule fires): run the enrichment
    stages then an incremental transform, in order:
      A/B. bulkenricher  — resolve MusicBrainz IDs, fetch AcousticBrainz features
      C.   librosa       — fallback feature extraction for Failed songs w/ audio
      D.   transform     — write PcaFeatures for songs that don't have them yet
    Each stage is sized to the Lambda's *live* remaining time so a run never
    overruns the timeout; leftover work is picked up next run.

  "fit": REFIT — retrain the scaler/PCA on all enriched songs and RECOMPUTE
    EVERY song's PcaFeatures embedding, then persist the new model to S3. This
    is never run on the schedule; invoke it manually after a feature-schema or
    extractor change. It rewrites all embeddings because a new model puts them
    in a new vector space — mixing old and new vectors would break similarity
    search. For very large catalogs prefer running the CLI (`fit_and_transform
    --fit`) locally, as a refit must finish within the 15-min Lambda timeout.

Event fields (all optional):
  action:    "pipeline" (default) | "fit"
  stages:    subset of ["mbid","ab","librosa","transform"] (pipeline only)
  retry:     re-attempt permanently-failed songs (pipeline only)
  reanalyze: re-process already-succeeded songs (pipeline only)
  limits:    {"mbid": int, "ab": int, "librosa": int} per-run ceilings
  no_pca / components: PCA options (fit only)

The module-level CLI entry points in each script still work locally unchanged.
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

ALL_STAGES = ["mbid", "ab", "librosa", "transform"]

# Per-run ceilings (env-overridable, and overridable per-invocation via the
# event's "limits"). These are upper bounds; the actual limit each run is the
# smaller of this and what fits in the remaining Lambda time.
MBID_LIMIT = int(os.getenv("MBID_LIMIT", "300"))
AB_LIMIT = int(os.getenv("AB_LIMIT", "500"))
LIBROSA_LIMIT = int(os.getenv("LIBROSA_LIMIT", "120"))

# Rough per-item costs used to fit a stage into the time left.
SECONDS_PER_MBID = float(os.getenv("SECONDS_PER_MBID", "1.2"))     # 1 req/s + overhead
SECONDS_PER_LIBROSA = float(os.getenv("SECONDS_PER_LIBROSA", "5.0"))  # download + extract

# Time to reserve for the fast stages (AB fetch + transform) and teardown.
SAFETY_MARGIN_S = float(os.getenv("BATCH_SAFETY_MARGIN_S", "45"))

# Fallback budget when there is no Lambda context (e.g. local run).
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


def _run_pipeline(event, context):
    from bulkenricher import (
        resolve_mbids_for_pending_songs,
        enrich_features_for_resolved_songs,
    )
    from librosa_enricher import enrich_failed_songs_with_audio
    from fit_and_transform import run_transform_mode

    stages = set(event.get("stages") or ALL_STAGES)
    retry = bool(event.get("retry", False))
    reanalyze = bool(event.get("reanalyze", False))
    limits = event.get("limits") or {}
    mbid_ceiling = int(limits.get("mbid", MBID_LIMIT))
    ab_ceiling = int(limits.get("ab", AB_LIMIT))
    librosa_ceiling = int(limits.get("librosa", LIBROSA_LIMIT))

    started = time.time()
    summary = {"action": "pipeline", "retry": retry, "reanalyze": reanalyze,
               "mbids_resolved": 0, "ab_enriched": 0, "librosa_enriched": 0,
               "transform_ran": False, "stages_skipped": [], "errors": []}

    # --- Stage A: MusicBrainz MBID resolution (1 req/s) ---
    if "mbid" in stages:
        limit_a = _fit_limit(_remaining_seconds(context), SECONDS_PER_MBID, mbid_ceiling)
        if limit_a > 0:
            try:
                log.info("Stage A: resolving up to %d MBIDs", limit_a)
                summary["mbids_resolved"] = resolve_mbids_for_pending_songs(
                    limit=limit_a, retry=retry)
            except Exception:
                log.exception("Stage A (MBID resolution) failed")
                summary["errors"].append("mbid")
        else:
            summary["stages_skipped"].append("mbid")

    # --- Stage B: AcousticBrainz feature fetch (bulk, fast) ---
    if "ab" in stages:
        if _remaining_seconds(context) > SAFETY_MARGIN_S:
            try:
                log.info("Stage B: fetching AcousticBrainz features (limit %d)", ab_ceiling)
                summary["ab_enriched"] = enrich_features_for_resolved_songs(
                    limit=ab_ceiling, reanalyze=reanalyze, retry=retry)
            except Exception:
                log.exception("Stage B (AcousticBrainz) failed")
                summary["errors"].append("acousticbrainz")
        else:
            summary["stages_skipped"].append("acousticbrainz")

    # --- Stage C: librosa fallback (~2-5s/song) ---
    if "librosa" in stages:
        limit_c = _fit_limit(_remaining_seconds(context), SECONDS_PER_LIBROSA, librosa_ceiling)
        if limit_c > 0:
            try:
                log.info("Stage C: librosa extraction for up to %d songs", limit_c)
                summary["librosa_enriched"] = enrich_failed_songs_with_audio(
                    limit=limit_c, reanalyze=reanalyze, retry=retry)
            except Exception:
                log.exception("Stage C (librosa) failed")
                summary["errors"].append("librosa")
        else:
            summary["stages_skipped"].append("librosa")

    # --- Stage D: transform RawFeatures -> PcaFeatures embeddings (fast) ---
    if "transform" in stages:
        if _remaining_seconds(context) > SAFETY_MARGIN_S:
            try:
                log.info("Stage D: transform mode (writing new PcaFeatures)")
                run_transform_mode()
                summary["transform_ran"] = True
            except Exception:
                log.exception("Stage D (transform) failed")
                summary["errors"].append("transform")
        else:
            summary["stages_skipped"].append("transform")

    summary["elapsed_s"] = round(time.time() - started, 1)
    log.info("Pipeline run complete: %s", summary)
    return summary


def _run_fit(event, context):
    # REFIT: retrain the model and recompute ALL embeddings, persist to S3.
    from fit_and_transform import run_fit_mode
    from feature_schema import PCA_COMPONENTS

    if event.get("no_pca"):
        n_components = None
    else:
        n_components = int(event.get("components") or PCA_COMPONENTS)

    started = time.time()
    log.warning(
        "REFIT requested: retraining the model and RECOMPUTING every song's "
        "embedding (n_components=%s). This is not the scheduled path.",
        "none (no-PCA)" if n_components is None else n_components,
    )
    result_code = run_fit_mode(n_components)
    summary = {"action": "fit", "result_code": result_code,
               "elapsed_s": round(time.time() - started, 1)}
    log.info("Refit complete: %s", summary)
    return summary


def handler(event, context):
    event = event or {}
    action = event.get("action", "pipeline")
    if action == "fit":
        return _run_fit(event, context)
    if action == "pipeline":
        return _run_pipeline(event, context)
    raise ValueError(f"Unknown action {action!r}; expected 'pipeline' or 'fit'.")


if __name__ == "__main__":
    # Local run: no Lambda context, uses LOCAL_BUDGET_S. Pass an action arg to
    # test, e.g. `python batch_handler.py fit`.
    import sys
    ev = {"action": sys.argv[1]} if len(sys.argv) > 1 else {}
    print(handler(ev, None))
