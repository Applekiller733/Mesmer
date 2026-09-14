# Deploying the backend (CI/CD)

The backend deploys with AWS SAM. Two GitHub Actions workflows drive it:

- **`.github/workflows/pr.yml`** — on every PR to `main`: `dotnet build`, `dotnet test`,
  `sam validate`. Make it a required check in branch protection.
- **`.github/workflows/deploy.yml`** — on push to `main`: test → deploy **dev**
  automatically → deploy **prod** after a manual approval.

Environments are isolated by the `Stage` parameter: the stack, S3 bucket, and
Secrets Manager secret all include `dev`/`prod` in their names, so the two never
share resources. `samconfig.toml` holds the per-environment deploy settings
(default = dev, `[prod]` = prod).

## One-time setup

### 1. Bootstrap the pipeline roles per stage

`sam pipeline bootstrap` creates (per stage) a pipeline execution role, a
CloudFormation execution role, and an artifact bucket — reusing the account's
existing GitHub OIDC provider. Run it once per stage:

```bash
sam pipeline bootstrap --stage dev
sam pipeline bootstrap --stage prod
```

Answer the prompts (GitHub as the OIDC provider, repo `Applekiller733/Mesmer`,
branch `main`, region `eu-north-1`). Each run prints a **PipelineExecutionRole**
ARN and a **CloudFormationExecutionRole** ARN — note both for each stage.

### 2. Create the GitHub Environments

In the repo: **Settings → Environments**, create **`dev`** and **`prod`**. For
each, add these **secrets** (from that stage's bootstrap output):

- `AWS_PIPELINE_EXECUTION_ROLE_ARN`
- `AWS_CLOUDFORMATION_EXECUTION_ROLE_ARN`

Optionally set a repo/environment **variable** `AWS_REGION` (defaults to
`eu-north-1`).

On the **`prod`** environment, add **Required reviewers** — that turns the
`deploy-prod` job into a manual approval gate (the run pauses until someone
approves).

### 3. Branch protection

Require the PR workflow to pass before merging to `main`.

## The flow

Push to `main` (touching `backend/`, `template.yaml`, or `samconfig.toml`):

1. **test** — builds and runs the test suite.
2. **deploy-dev** — assumes the dev pipeline role and `sam deploy` (default env)
   to the `mesmer-backend-dev` stack.
3. **deploy-prod** — waits for approval (prod environment reviewers), then
   `sam deploy --config-env prod` to `mesmer-backend-prod`.

`workflow_dispatch` also lets you run it manually from the Actions tab.

## Per-stage data resources (manual, once)

Two things live outside the SAM stack and must exist per stage before the app is
fully functional there:

- **Aurora express cluster** — create one per stage with
  `scripts/create-express-db.*` (e.g. `CLUSTER_ID=mesmer-prod`), then set that
  stage's `DbHost`/`DbClusterResourceId` in `samconfig.toml`'s
  `[prod.deploy.parameters] parameter_overrides`. (dev already points at
  `mesmer-dev`.)
- **SMTP values** — after the first deploy of a stage, fill the SMTP keys in that
  stage's `mesmer-backend-<stage>-appsecrets` secret. The JWT secret is
  generated automatically and is independent per stage.
