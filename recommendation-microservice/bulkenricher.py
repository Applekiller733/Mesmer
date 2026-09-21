import argparse
import logging
import sys
from typing import Optional, List

from db import get_connection
from musicbrainz_client import search_recording_mbid
from acousticbrainz_client import fetch_lowlevel_bulk, MAX_BATCH_SIZE
from feature_extraction_acousticbrainz import extract_features_from_acousticbrainz

# EnrichmentStatus enum values must match backend/Entities/EnrichmentStatus.cs
STATUS_PENDING = 0
STATUS_ENRICHED_ACOUSTICBRAINZ = 1
STATUS_ENRICHED_LIBROSA = 2
STATUS_FAILED = 3

DB_BATCH_SIZE = 100
MBID_CHUNK_FOR_AB = MAX_BATCH_SIZE  # 25 — AB's hard cap

# Terminal failure markers written to EnrichmentSource. A song carrying one has
# been permanently attempted at that stage; excluding it keeps a fixed-size
# LIMIT batch from being clogged by songs that can never progress. Because all
# stages share the single EnrichmentSource column and overwrite it, the librosa
# markers (the last stage) are fully terminal: every upstream stage must exclude
# them too, otherwise a song that failed AB and then failed librosa would
# ping-pong between the stages (each seeing only the other's marker) forever.
MBID_FAILURE_MARKER = "mb:no-match"
AB_FAILURE_MARKERS = ("ab:no-data", "ab:malformed")
LIBROSA_TERMINAL_MARKERS = ("librosa:extract-failed", "librosa:no-audio")


def _exclude_sources_clause(markers) -> str:
    # Build an "AND (EnrichmentSource IS NULL OR EnrichmentSource NOT IN (...))"
    # fragment from a list of marker strings (safe: markers are code constants).
    quoted = ", ".join("'%s'" % m for m in markers)
    return (
        'AND ("EnrichmentSource" IS NULL '
        'OR "EnrichmentSource" NOT IN (%s))' % quoted
    )


def setup_logging(verbose: bool):
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    )


# find MBIDs for songs that don't have them yet, and mark songs with no match as Failed
def resolve_mbids_for_pending_songs(
    limit: int = DB_BATCH_SIZE, retry: bool = False,
) -> int:
    """
    Iterate songs that need MBID resolution. Sets MusicBrainzId on each.
    Songs without a match end up with MusicBrainzId still NULL but get
    EnrichmentStatus=Failed and EnrichmentSource set to "mb:no-match".

    A no-match song keeps MusicBrainzId NULL and status Failed, so without
    excluding it, it would keep matching this selector forever — with a
    fixed LIMIT and no ordering it can fill every batch and starve genuinely
    pending songs. So by default we skip songs already marked 'mb:no-match'.
    Pass retry=True to re-attempt them (e.g. after MusicBrainz coverage
    improves). Ordering by "Id" keeps batches deterministic.

    Returns number of songs updated (whether resolved or marked failed).
    """
    log = logging.getLogger("resolve_mbids")
    updated = 0

    # Skip songs already attempted to a terminal state unless retrying: our own
    # no-match, plus the fully-terminal librosa markers (a no-MBID song with
    # audio can reach librosa, which would overwrite the source — excluding
    # those markers stops the mbid<->librosa ping-pong).
    source_filter = "" if retry else _exclude_sources_clause(
        (MBID_FAILURE_MARKER,) + LIBROSA_TERMINAL_MARKERS
    )

    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                f"""
                SELECT "Id", "Name", "Artist"
                FROM "Songs"
                WHERE "MusicBrainzId" IS NULL
                  AND "EnrichmentStatus" IN (%s, %s)
                  {source_filter}
                ORDER BY "Id"
                LIMIT %s
                """,
                (STATUS_PENDING, STATUS_FAILED, limit),
            )
            rows = cur.fetchall()

        if not rows:
            log.info("No songs need MBID resolution.")
            return 0

        log.info("Resolving MBIDs for %d songs (1 req/s, ~%ds total)...",
                 len(rows), len(rows))

        for song_id, name, artist in rows:
            mbid = search_recording_mbid(name, artist)

            with conn.cursor() as cur:
                if mbid is None:
                    cur.execute(
                        """
                        UPDATE "Songs"
                        SET "EnrichmentStatus" = %s,
                            "EnrichmentSource" = 'mb:no-match'
                        WHERE "Id" = %s
                        """,
                        (STATUS_FAILED, song_id),
                    )
                    log.info("  no MBID match: %s — %s", artist, name)
                else:
                    cur.execute(
                        """
                        UPDATE "Songs"
                        SET "MusicBrainzId" = %s
                        WHERE "Id" = %s
                        """,
                        (mbid, song_id),
                    )
                    log.debug("  %s — %s -> %s", artist, name, mbid)
                conn.commit()
                updated += 1

    return updated


# For songs with MBIDs, fetch AcousticBrainz lowlevel data and extract features.
def enrich_features_for_resolved_songs(
    limit: int = DB_BATCH_SIZE,
    reanalyze: bool = False,
    retry: bool = False,
) -> int:
    """
    Fetch AcousticBrainz lowlevel data for songs that have an MBID but
    no features yet.

    Default mode: skip songs that already have RawFeatures, and skip
    songs previously marked as permanent AB failures ('ab:no-data' /
    'ab:malformed') so they don't clog the fixed-size batch. Retry mode
    (retry=True): also re-attempt those failures. Reanalyze mode: also
    re-fetch AB data for previously AB-enriched songs and rebuild their
    RawFeatures with the current schema. Use after a schema bump (new
    features added) so existing rows aren't left on stale vectors.

    Returns number of songs successfully enriched.
    """
    log = logging.getLogger("enrich_features")
    enriched = 0

    if reanalyze:
        # Include previously AB-enriched songs so their RawFeatures get
        # rebuilt with whatever the current extractor produces. Reanalyze
        # deliberately re-does everything, so no failure-marker exclusion.
        status_filter: List[int] = [
            STATUS_PENDING, STATUS_FAILED, STATUS_ENRICHED_ACOUSTICBRAINZ,
        ]
        raw_features_filter = ""
        source_filter = ""
        log.info("Reanalyze mode: including previously AB-enriched songs.")
    else:
        status_filter = [STATUS_PENDING, STATUS_FAILED]
        raw_features_filter = 'AND "RawFeatures" IS NULL'
        # AB failures (no-data / malformed) keep RawFeatures NULL and status
        # Failed, so they'd otherwise re-match forever and, with a fixed LIMIT
        # and no ordering, starve genuinely-pending songs. Also exclude the
        # librosa terminal markers: a song AB failed then librosa failed carries
        # a librosa marker now, and without this AB would re-process it
        # endlessly (the ping-pong). Skip both unless retrying.
        source_filter = "" if retry else _exclude_sources_clause(
            AB_FAILURE_MARKERS + LIBROSA_TERMINAL_MARKERS
        )

    with get_connection() as conn:
        with conn.cursor() as cur:
            query = f"""
                SELECT "Id", "MusicBrainzId"
                FROM "Songs"
                WHERE "MusicBrainzId" IS NOT NULL
                  {raw_features_filter}
                  {source_filter}
                  AND "EnrichmentStatus" = ANY(%s::int[])
                ORDER BY "Id"
                LIMIT %s
            """
            cur.execute(query, (status_filter, limit))
            rows = cur.fetchall()

        if not rows:
            log.info("No songs awaiting AcousticBrainz data.")
            return 0

        log.info("Fetching AcousticBrainz data for %d songs in batches of %d",
                 len(rows), MBID_CHUNK_FOR_AB)

        mbid_to_song = {str(mbid).lower(): song_id for song_id, mbid in rows}

        all_mbids = list(mbid_to_song.keys())
        for i in range(0, len(all_mbids), MBID_CHUNK_FOR_AB):
            chunk = all_mbids[i : i + MBID_CHUNK_FOR_AB]
            log.debug("AB bulk fetch: %d MBIDs", len(chunk))

            results = fetch_lowlevel_bulk(chunk)

            with conn.cursor() as cur:
                for mbid in chunk:
                    song_id = mbid_to_song[mbid]

                    if mbid not in results:
                        cur.execute(
                            """
                            UPDATE "Songs"
                            SET "EnrichmentStatus" = %s,
                                "EnrichmentSource" = 'ab:no-data'
                            WHERE "Id" = %s
                            """,
                            (STATUS_FAILED, song_id),
                        )
                        continue

                    features = extract_features_from_acousticbrainz(results[mbid])
                    if features is None:
                        cur.execute(
                            """
                            UPDATE "Songs"
                            SET "EnrichmentStatus" = %s,
                                "EnrichmentSource" = 'ab:malformed'
                            WHERE "Id" = %s
                            """,
                            (STATUS_FAILED, song_id),
                        )
                        continue

                    cur.execute(
                        """
                        UPDATE "Songs"
                        SET "RawFeatures" = %s,
                            "EnrichmentStatus" = %s,
                            "EnrichmentSource" = 'acousticbrainz:lowlevel'
                        WHERE "Id" = %s
                        """,
                        (features, STATUS_ENRICHED_ACOUSTICBRAINZ, song_id),
                    )
                    enriched += 1

                conn.commit()

    return enriched



def main():
    parser = argparse.ArgumentParser(
        description="Enrich songs with AcousticBrainz audio features.",
    )
    parser.add_argument(
        "--limit", type=int, default=DB_BATCH_SIZE,
        help="Max songs to process per stage per run (default %(default)s)",
    )
    parser.add_argument(
        "--skip-mbid", action="store_true",
        help="Skip the MusicBrainz MBID-resolution step.",
    )
    parser.add_argument(
        "--skip-acousticbrainz", action="store_true",
        help="Skip the AcousticBrainz fetch.",
    )
    parser.add_argument(
        "--reanalyze", action="store_true",
        help="Also re-process songs previously enriched via "
             "AcousticBrainz. Use after a schema change or extractor "
             "update so existing rows get rebuilt with the current "
             "feature set. Implies that Stage B re-fetches AB docs.",
    )
    parser.add_argument(
        "--retry", action="store_true",
        help="Re-attempt songs previously marked as permanent failures "
             "(mb:no-match / ab:no-data / ab:malformed). By default these "
             "are skipped so they don't clog the fixed-size batch; use "
             "this to retry them (e.g. after upstream coverage improves).",
    )
    parser.add_argument("-v", "--verbose", action="store_true")
    args = parser.parse_args()

    setup_logging(args.verbose)
    log = logging.getLogger("main")

    if not args.skip_mbid:
        log.info("=== Stage A: resolving MBIDs ===")
        n = resolve_mbids_for_pending_songs(limit=args.limit, retry=args.retry)
        log.info("Stage A done. Updated %d rows.", n)

    if not args.skip_acousticbrainz:
        log.info("=== Stage B: fetching AcousticBrainz features ===")
        n = enrich_features_for_resolved_songs(
            limit=args.limit, reanalyze=args.reanalyze, retry=args.retry,
        )
        log.info("Stage B done. Enriched %d songs.", n)

    log.info("All done. Run again to process more songs, or proceed to "
             "Stage 3 (Librosa) for songs marked Failed.")


if __name__ == "__main__":
    sys.exit(main() or 0)