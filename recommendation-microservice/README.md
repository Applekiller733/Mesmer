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

### Model artifacts

`scaler_schema2.pkl` / `pca_schema2.pkl` are baked into the image and only **read**
by the scheduled transform. Re-fitting (`--fit`, which *writes* new `.pkl`) is a
local op: re-fit, commit the new `.pkl`, redeploy. If the committed `.pkl` fail to
unpickle under the pinned scikit-learn version, re-fit locally with that version
and commit the result.
