# Create the Mesmer Aurora PostgreSQL cluster using EXPRESS CONFIGURATION.
#
# Windows/PowerShell equivalent of create-express-db.sh — same behaviour. See
# scripts/README.md for why the database is created out of band.
#
# Run it:  pwsh ./scripts/create-express-db.ps1
#     or:  powershell -ExecutionPolicy Bypass -File .\scripts\create-express-db.ps1
#
# Express configuration is intentionally minimal: it auto-provisions networking,
# auto-adds the DB instance, and uses the default 'postgres' database and master
# user with IAM authentication. It REJECTS most create flags (--database-name,
# --manage-master-user-password, --master-username), so the create call takes
# only the essentials and scaling is set afterwards with modify-db-cluster.
#
# Prerequisites: AWS CLI configured (`aws configure`) with RDS permissions.
$ErrorActionPreference = "Stop"

$ClusterId = if ($env:CLUSTER_ID) { $env:CLUSTER_ID } else { "mesmer-dev" }
$Region    = if ($env:AWS_REGION) { $env:AWS_REGION } else { "eu-north-1" }
$MinAcu    = if ($env:MIN_ACU)    { $env:MIN_ACU }    else { "0" }   # 0 = scale to zero when idle
$MaxAcu    = if ($env:MAX_ACU)    { $env:MAX_ACU }    else { "2" }   # free plan allows up to 4

Write-Host "Creating express Aurora cluster '$ClusterId' in $Region..."
aws rds create-db-cluster `
  --region $Region `
  --db-cluster-identifier $ClusterId `
  --engine aurora-postgresql `
  --with-express-configuration

Write-Host "Waiting for the cluster to become available (this takes a few minutes)..."
aws rds wait db-cluster-available --region $Region --db-cluster-identifier $ClusterId

# Set Serverless v2 scaling after creation (min 0 = scale to zero). Non-fatal.
Write-Host "Setting scaling to Min=$MinAcu, Max=$MaxAcu ACUs..."
aws rds modify-db-cluster `
  --region $Region --db-cluster-identifier $ClusterId `
  --serverless-v2-scaling-configuration "MinCapacity=$MinAcu,MaxCapacity=$MaxAcu" `
  --apply-immediately
if ($LASTEXITCODE -ne 0) {
  Write-Host "warning: could not set scaling automatically; adjust it in the RDS console."
}

Write-Host ""
Write-Host "Connection details to wire into the app (Phase 3.4):"
aws rds describe-db-clusters `
  --region $Region --db-cluster-identifier $ClusterId `
  --query "DBClusters[0].{Endpoint:Endpoint,Port:Port,MasterUsername:MasterUsername,IamAuth:IAMDatabaseAuthenticationEnabled,SecretArn:MasterUserSecret.SecretArn}" `
  --output table

Write-Host ""
Write-Host "Express clusters default to the 'postgres' database and IAM authentication."
Write-Host "Paste this table back so the app connection (Phase 3.4) can be wired to match."
