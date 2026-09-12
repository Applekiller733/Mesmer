# Media migration (local disk → S3)

One-off tool that copies media the backend previously stored under
`backend/Resources/` into the S3 media bucket and rewrites each `Files` row's
`FilePath` from a local path to the S3 object key.

Run it **once** after the S3 bucket exists but before (or right as) the S3-based
code goes live, so existing songs/profile pictures keep resolving.

## Prerequisites

- .NET 8 SDK.
- Network access to the Postgres database.
- AWS credentials with `s3:PutObject` + `s3:GetObject` on the bucket (resolved
  from the standard chain: env vars, shared profile, SSO, or an assumed role).
- Access to the original files on disk (either the DB still holds their absolute
  paths and you run this on that machine, or pass `--resources-dir`).

## Usage

Preview first (no uploads, no DB writes):

```bash
dotnet run --project tools/MediaMigration -- \
  --connection "Host=HOST;Port=5432;Database=DB;Username=USER;Password=PASS" \
  --bucket mesmer-media-dev-<account-id>-<region> \
  --resources-dir ./backend/Resources \
  --region eu-north-1 \
  --dry-run
```

Then run for real by dropping `--dry-run`.

`--connection` / `--bucket` fall back to the `ConnectionStrings__SongAppApiDatabase`
and `MEDIA_BUCKET_NAME` environment variables. `--resources-dir` is optional; it's
only needed when the paths stored in the DB don't resolve on the machine you run
this from (the tool then looks for each file under that directory using the
derived key).

## Behaviour

- **Key derivation:** the object key is everything after the last `Resources/`
  segment in the stored path (e.g. `/app/Resources/Songs/Audio/x.mp3` →
  `Songs/Audio/x.mp3`). Paths without that segment become `Migrated/<filename>`.
- **Idempotent:** if the object is already in S3 the file isn't re-uploaded; if a
  row already points at the key it's left untouched. Re-running only fixes what's
  left.
- **Summary:** prints per-row actions and a final tally
  (`migrated`, `key-updated`, `already-present`, `missing`, `errors`). Exit code
  is non-zero if any row errored.
- Rows whose source can't be found locally *and* aren't already in S3 are
  reported as `MISSING` and skipped (nothing to copy).

This tool is intentionally outside `backend/`, so it is not part of the SAM build
or the API's solution/CI.
