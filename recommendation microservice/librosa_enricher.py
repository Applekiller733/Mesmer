import argparse
import logging
import sys
import warnings

from db import get_connection
from audio_fetcher import fetch_audio_to_tempfile
from feature_extraction_librosa import extract_features_from_audio

# Must match backend/Entities/EnrichmentStatus.cs
STATUS_PENDING = 0
STATUS_ENRICHED_ACOUSTICBRAINZ = 1
STATUS_ENRICHED_LIBROSA = 2
STATUS_FAILED = 3

DB_BATCH_SIZE = 50


def setup_logging(verbose: bool):
    """
    Configure logging so OUR messages are visible but Librosa's
    transitive dependencies stop spamming the console. Without this,
    numba's JIT compiler, matplotlib's font cache, and audioread's
    deprecation warnings flood the output.
    """
    level = logging.DEBUG if verbose else logging.INFO

    logging.basicConfig(
        level=level,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    )

    # Silence the chatty libraries that Librosa pulls in. WARNING level
    # means we still see real problems (e.g. file decode failures from
    # audioread) but lose the routine startup/info messages.
    for noisy in ("numba", "matplotlib", "audioread", "soundfile",
                  "urllib3", "PIL"):
        logging.getLogger(noisy).setLevel(logging.WARNING)

    # Suppress librosa's own deprecation/runtime warnings — most are
    # about minor API drift between versions of its deps and not
    # actionable on our side.
    warnings.filterwarnings("ignore", category=UserWarning, module="librosa")
    warnings.filterwarnings("ignore", category=FutureWarning, module="librosa")
    # Numpy's "Mean of empty slice" etc. warnings can fire on edge-case
    # audio files; we already handle those by returning None from the
    # extractor.
    warnings.filterwarnings("ignore", category=RuntimeWarning, module="numpy")


def enrich_failed_songs_with_audio(limit: int = DB_BATCH_SIZE) -> int:
    log = logging.getLogger("librosa_enricher")
    enriched = 0
    failed_extraction = 0

    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Id", "Name", "Artist"
                FROM "Songs"
                WHERE "EnrichmentStatus" = %s
                  AND "SoundId" IS NOT NULL
                LIMIT %s
                """,
                (STATUS_FAILED, limit),
            )
            rows = cur.fetchall()

        if not rows:
            log.info("No Failed-with-audio songs to process. Done.")
            return 0

        log.info(
            "Processing %d songs with Librosa (~1-3s per song)...", len(rows)
        )

        for song_id, name, artist in rows:
            log.info("  %s — %s", artist, name)

            with fetch_audio_to_tempfile(str(song_id)) as audio_path:
                if audio_path is None:
                    log.warning("    audio fetch returned no path; skipping")
                    continue

                features = extract_features_from_audio(audio_path)

            if features is None:
                with conn.cursor() as cur:
                    cur.execute(
                        """
                        UPDATE "Songs"
                        SET "EnrichmentSource" = 'librosa:extract-failed'
                        WHERE "Id" = %s
                        """,
                        (song_id,),
                    )
                    conn.commit()
                failed_extraction += 1
                log.warning("    Librosa extraction failed")
                continue

            with conn.cursor() as cur:
                cur.execute(
                    """
                    UPDATE "Songs"
                    SET "RawFeatures" = %s,
                        "EnrichmentStatus" = %s,
                        "EnrichmentSource" = 'librosa:30s-from-30s'
                    WHERE "Id" = %s
                    """,
                    (features, STATUS_ENRICHED_LIBROSA, song_id),
                )
                conn.commit()
                enriched += 1
                log.debug("    enriched (%d features)", len(features))

    log.info(
        "Done. Enriched=%d, ExtractionFailed=%d, NoChange=%d",
        enriched,
        failed_extraction,
        len(rows) - enriched - failed_extraction,
    )
    return enriched


def main():
    parser = argparse.ArgumentParser(
        description="Enrich Failed songs with Librosa-extracted audio features."
    )
    parser.add_argument(
        "--limit", type=int, default=DB_BATCH_SIZE,
        help="Max songs to process per run (default %(default)s)",
    )
    parser.add_argument("-v", "--verbose", action="store_true")
    args = parser.parse_args()

    setup_logging(args.verbose)
    log = logging.getLogger("main")

    log.info("=== Stage 3: Librosa enrichment ===")
    enrich_failed_songs_with_audio(limit=args.limit)
    log.info(
        "All done. Run again to process more, or proceed to Stage 4 "
        "(fit scaler + PCA)."
    )


if __name__ == "__main__":
    sys.exit(main() or 0)