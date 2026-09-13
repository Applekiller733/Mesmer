# scripts/

## `create-express-db.sh` — provision the Aurora database (free plan)

On the AWS **free plan**, Aurora PostgreSQL can only be created with **express
configuration**. Two consequences shaped the architecture:

1. **It can't live in `template.yaml`.** CloudFormation's `AWS::RDS::DBCluster`
   has no `WithExpressConfiguration` property ([coverage gap #2506](https://github.com/aws-cloudformation/cloudformation-coverage-roadmap/issues/2506)),
   so the cluster is created out of band by this script.
2. **It's VPC-less.** Express clusters aren't placed in a VPC — they're reached
   over a managed access endpoint. So the Lambda is **not** attached to a VPC
   (there is no VPC, no NAT, no VPC endpoints, no DB security group), and it
   reaches the database, S3, and Secrets Manager directly over the internet.

### Run it

Linux / macOS / WSL:

```bash
# defaults: CLUSTER_ID=mesmer-dev, AWS_REGION=eu-north-1, DB_NAME=mesmer,
#           MASTER_USER=mesmeradmin, MIN_ACU=0, MAX_ACU=2
./scripts/create-express-db.sh
```

Windows (PowerShell) — same defaults, override with env vars if needed:

```powershell
pwsh ./scripts/create-express-db.ps1
# or: powershell -ExecutionPolicy Bypass -File .\scripts\create-express-db.ps1
```

It prints the **endpoint, port, database name, and the Secrets Manager secret
ARN** for the managed master credentials. Keep those — Phase 3.4 wires them into
the Lambda (the app reads the secret at runtime and builds its Npgsql connection
string; the secret ARN is granted to the execution role).

### After it's up

- **Enable pgvector + apply migrations.** Connect with the master credentials and
  run `CREATE EXTENSION IF NOT EXISTS vector;` (the EF migrations also issue this),
  then apply the EF Core migrations against the new database. pgvector needs
  engine ≥ 15.3, which express clusters satisfy.
- **Auth.** `--manage-master-user-password` gives username/password auth via the
  secret (what the app uses). Express also supports IAM auth if you prefer.

### Tear down (stop paying)

```bash
aws rds delete-db-cluster --region eu-north-1 \
  --db-cluster-identifier mesmer-dev --skip-final-snapshot
```

With `MIN_ACU=0` the cluster scales to zero when idle (storage-only cost), but
deleting it between sessions is the cleanest way to stop credit burn.
