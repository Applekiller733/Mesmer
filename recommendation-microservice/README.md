# Recommendation microservice

Python recommendation service for Mesmer. Two workloads:

- **Online API** (`api.py`, FastAPI) — `POST /recommend-ids`, `GET /health`. Called
  by the .NET backend at `RecommendationService:BaseUrl`.
- **Background pipeline** (`batch_handler.py` on AWS; the CLI scripts locally) —
  enriches songs and writes their `PcaFeatures` pgvector embeddings:
  1. `bulkenricher.py` — MusicBrainz MBID resolution + AcousticBrainz features.
  2. `librosa_enricher.py` — librosa fallback for songs AcousticBrainz can't cover.
  3. `fit_and_transform.py` — StandardScaler(+PCA) → `PcaFeatures` embedding.

## Local development

```bash
cp env.template .env          # set DATABASE_URL (+ MusicBrainz UA, backend URL)
pip install -r requirements.txt
uvicorn api:app --host 0.0.0.0 --port 8000      # the API
python bulkenricher.py                          # then the pipeline, in order
python librosa_enricher.py
python fit_and_transform.py --fit               # first time (fits + saves .pkl)
python fit_and_transform.py                     # subsequent (incremental transform)
```

The enrichment stages are incremental and idempotent: each processes a bounded
batch and skips songs already done **or already permanently failed** (see below).
Re-run to process more.

### Retrying failures

By default a permanently-failed song (no MusicBrainz match, no AcousticBrainz
data, librosa extraction/audio failure) is skipped on later runs so it can't clog
the fixed-size batch. To deliberately re-attempt failures (e.g. after upstream
coverage improves), pass `--retry`:

```bash
python bulkenricher.py --retry
python librosa_enricher.py --retry
```

(`--reanalyze` is different: it re-processes *successful* rows after a schema/
extractor change.)

## AWS deployment

Deployed as its own SAM stack (`template.yaml` / `samconfig.toml`), independent
of the backend, matching the backend's free-plan posture: **VPC-less**, reaching
the shared Aurora "express" cluster over the internet with **IAM database auth**
(no stored password — `db.py` generates a short-lived RDS token when `DB_HOST` is
set).

- **API** → a small **zip Lambda** (serving deps only, `requirements-api.txt`,
  built via `Makefile`) behind an API Gateway HTTP API.
- **Batch** → a **container image** (`Dockerfile`, full `requirements.txt`) run on
  a schedule by EventBridge Scheduler. It's an image because librosa/scipy/
  scikit-learn don't fit a zip Lambda.

```bash
# from this directory
sam build --use-container
sam deploy                       # dev
sam deploy --config-env prod     # prod
```

After the first deploy:
1. Take the stack's `ApiBaseUrl` output and set it as `RecommendationBaseUrl` in
   the **backend** `../samconfig.toml`, then redeploy the backend so it can reach
   this service.
2. Set `DotnetApiBaseUrl` (backend `ApiBaseUrl`) and `MusicBrainzUserAgentContact`
   in this stack's `samconfig.toml` `parameter_overrides` — the first enables the
   librosa audio fallback, the second keeps MusicBrainz from throttling you.
3. Run `../scripts/set-recommendation-ecr-lifecycle.sh` once to prune old batch
   images (free-plan cost control).

### Schedule & the 15-minute cap

`batch_handler` sizes each stage to the Lambda's remaining time, so a run never
overruns the 900 s timeout — leftover work is picked up next schedule. Tune via
stack parameters (`ScheduleExpression`, default `rate(6 hours)`) and env
(`LIBROSA_LIMIT`, `MBID_LIMIT`, `AB_LIMIT`). librosa (~2–5 s/song) is the limiting
stage. For a large one-time backfill, run the CLI scripts locally rather than
draining thousands of songs through the scheduled Lambda.

### Free-plan cost

Everything is within AWS free tiers except **ECR storage** for the batch image
(~1–1.5 GB, above the 500 MB free allowance) ≈ $0.10–0.20/month with the
lifecycle policy applied. The `rate(6 hours)` default keeps the batch Lambda under
the 400,000 GB-s/month always-free tier. IAM auth avoids Secrets Manager charges.

## Manually running the pipeline on the stack

The scheduled batch Lambda is also directly invokable, and its behaviour is
driven by the invocation event — so you can trigger a run on demand, run a single
stage, or force a retry/reanalyze without waiting for the schedule. Empty event =
the normal scheduled run.

```bash
FN=mesmer-recommendation-batch-dev            # or -prod
R=eu-north-1

# Full pipeline now (same as the schedule)
aws lambda invoke --region $R --function-name $FN --payload '{}' /dev/stdout

# Only the AcousticBrainz stage, larger batch
aws lambda invoke --region $R --function-name $FN \
  --payload '{"stages":["ab"],"limits":{"ab":800}}' /dev/stdout

# Retry songs previously marked permanently failed
aws lambda invoke --region $R --function-name $FN \
  --payload '{"retry":true}' /dev/stdout

# Reanalyze already-succeeded songs (after an extractor change)
aws lambda invoke --region $R --function-name $FN \
  --payload '{"reanalyze":true}' /dev/stdout
```

Event fields: `action` (`"pipeline"` default | `"fit"`), `stages` (subset of
`mbid,ab,librosa,transform`), `retry`, `reanalyze`, `limits`
(`{"mbid":…, "ab":…, "librosa":…}`).

You can also run the CLI scripts **locally against the deployed cluster** (set
`DB_HOST`/`DB_PORT`/`DB_NAME`/`DB_USER` + AWS creds for IAM auth, plus
`DOTNET_API_BASE_URL` for the librosa stage): `python bulkenricher.py`,
`python librosa_enricher.py`, `python fit_and_transform.py`.

## Fit vs. transform, and when to refit

- **Transform (incremental)** runs on every scheduled batch. It only touches
  songs that have `RawFeatures` but no `PcaFeatures` yet, applying the *current*
  model. This is the steady state and is cheap.
- **Fit / refit** retrains the `StandardScaler` (+ optional PCA) on all enriched
  songs. **It is never run on the schedule.** Run it explicitly only when the
  feature schema or extractor changes (or you deliberately want to re-fit on a
  much larger dataset).

**Refitting recomputes every embedding.** A new model puts vectors in a new
space, so old and new `PcaFeatures` are not comparable — mixing them would break
the pgvector similarity search. `run_fit_mode` (`--fit`) therefore retrains and
**rewrites all songs' `PcaFeatures` in one pass**; never refit and then only
transform the new songs.

Two ways to refit:

```bash
# On the stack (model is persisted to the MODEL_BUCKET S3 bucket, and every
# later transform loads it from there — no redeploy needed):
aws lambda invoke --region $R --function-name $FN --payload '{"action":"fit"}' /dev/stdout

# Locally against the cluster (equivalent):
python fit_and_transform.py --fit
```

Model storage: locally the `.pkl` live next to the script (committed). On AWS,
`MODEL_BUCKET` (set by the stack) makes a refit write the retrained models to S3;
every transform loads from S3, falling back to the image's baked-in `.pkl` until
the first refit. So the committed `.pkl` only seed a brand-new deployment — after
that, S3 is authoritative and no image redeploy is needed to pick up a refit.

Caveats:
- A refit must finish within the 900 s Lambda timeout. For a very large catalog,
  run `fit_and_transform.py --fit` **locally** instead (no timeout).
- If the committed `.pkl` fail to unpickle under the pinned scikit-learn version,
  refit once with that version so a fresh model is written.
- Overlapping runs (a manual invoke while a scheduled run is mid-flight) just
  waste work — the pipeline is idempotent. To hard-prevent it, set the
  `BatchReservedConcurrency` stack parameter to 1 (needs an account Lambda
  concurrency limit high enough to reserve; off by default so the stack deploys
  on low-limit/new accounts).
- **Architecture:** the batch image is built for your Docker host's architecture.
  On Apple Silicon / ARM, set the `FunctionArchitecture` stack parameter to
  `arm64` (in `samconfig.toml` `parameter_overrides`), or the arm64 image won't
  match an x86_64 function and the stack CREATE fails.

### Free-plan note on the model bucket

The `MODEL_BUCKET` holds a few KB of `.pkl` — comfortably inside the S3 free tier;
it doesn't change the cost picture above.
