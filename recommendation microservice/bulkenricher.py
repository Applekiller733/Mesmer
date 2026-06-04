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


def setup_logging(verbose: bool):
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    )


# ---- Stage A: resolve MBIDs for songs that don't have one yet --------------


def resolve_mbids_for_pending_songs(limit: int = DB_BATCH_SIZE) -> int:
    """
    Iterate songs that need MBID resolution. Sets MusicBrainzId on each.
    Songs without a match end up with MusicBrainzId still NULL but get
    EnrichmentStatus=Failed and EnrichmentSource set to "mb:no-match".

    Returns number of songs updated (whether resolved or marked failed).
    """
    log = logging.getLogger("resolve_mbids")
    updated = 0

    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Id", "Name", "Artist"
                FROM "Songs"
                WHERE "MusicBrainzId" IS NULL
                  AND "EnrichmentStatus" IN (%s, %s)
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


# ---- Stage B: fetch AcousticBrainz data for songs with known MBIDs ---------


def enrich_features_for_resolved_songs(
    limit: int = DB_BATCH_SIZE,
    reanalyze: bool = False,
) -> int:
    """
    Fetch AcousticBrainz lowlevel data for songs that have an MBID but
    no features yet.

    Default mode: skip songs that already have RawFeatures. Reanalyze
    mode: also re-fetch AB data for previously AB-enriched songs and
    rebuild their RawFeatures with the current schema. Use after a
    schema bump (new features added) so existing rows aren't left on
    stale vectors.

    Returns number of songs successfully enriched.
    """
    log = logging.getLogger("enrich_features")
    enriched = 0

    if reanalyze:
        # Include previously AB-enriched songs so their RawFeatures get
        # rebuilt with whatever the current extractor produces.
        status_filter: List[int] = [
            STATUS_PENDING, STATUS_FAILED, STATUS_ENRICHED_ACOUSTICBRAINZ,
        ]
        raw_features_filter = ""
        log.info("Reanalyze mode: including previously AB-enriched songs.")
    else:
        status_filter = [STATUS_PENDING, STATUS_FAILED]
        raw_features_filter = 'AND "RawFeatures" IS NULL'

    with get_connection() as conn:
        with conn.cursor() as cur:
            query = f"""
                SELECT "Id", "MusicBrainzId"
                FROM "Songs"
                WHERE "MusicBrainzId" IS NOT NULL
                  {raw_features_filter}
                  AND "EnrichmentStatus" = ANY(%s::int[])
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


# ---- Entry point ------------------------------------------------------------


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
    parser.add_argument("-v", "--verbose", action="store_true")
    args = parser.parse_args()

    setup_logging(args.verbose)
    log = logging.getLogger("main")

    if not args.skip_mbid:
        log.info("=== Stage A: resolving MBIDs ===")
        n = resolve_mbids_for_pending_songs(limit=args.limit)
        log.info("Stage A done. Updated %d rows.", n)

    if not args.skip_acousticbrainz:
        log.info("=== Stage B: fetching AcousticBrainz features ===")
        n = enrich_features_for_resolved_songs(
            limit=args.limit, reanalyze=args.reanalyze,
        )
        log.info("Stage B done. Enriched %d songs.", n)

    log.info("All done. Run again to process more songs, or proceed to "
             "Stage 3 (Librosa) for songs marked Failed.")


if __name__ == "__main__":
    sys.exit(main() or 0)