# Create the Mesmer Aurora PostgreSQL cluster using EXPRESS CONFIGURATION.
#
# Windows/PowerShell equivalent of create-express-db.sh — same behaviour, same
# flags. See scripts/README.md for why the database is created out of band.
#
# Run it:  pwsh ./scripts/create-express-db.ps1
#     or:  powershell -ExecutionPolicy Bypass -File .\scripts\create-express-db.ps1
#
# Prerequisites: AWS CLI configured (`aws configure`) with RDS permissions.
$ErrorActionPreference = "Stop"

$ClusterId  = if ($env:CLUSTER_ID)  { $env:CLUSTER_ID }  else { "mesmer-dev" }
$Region     = if ($env:AWS_REGION)  { $env:AWS_REGION }  else { "eu-north-1" }
$DbName     = if ($env:DB_NAME)     { $env:DB_NAME }     else { "mesmer" }
$MasterUser = if ($env:MASTER_USER) { $env:MASTER_USER } else { "mesmeradmin" }
$MinAcu     = if ($env:MIN_ACU)     { $env:MIN_ACU }     else { "0" }   # 0 = scale to zero when idle
$MaxAcu     = if ($env:MAX_ACU)     { $env:MAX_ACU }     else { "2" }   # free plan allows up to 4

Write-Host "Creating express Aurora cluster '$ClusterId' in $Region..."

# Express configuration auto-provisions networking (no VPC/subnet/SG) and adds
# the DB instance for you. --manage-master-user-password stores the master
# password in Secrets Manager so the app can read it at runtime.
# NOTE: express config does NOT accept --database-name; create the app database
# after the cluster is available (see below), or use the default 'postgres' DB.
aws rds create-db-cluster `
  --region $Region `
  --db-cluster-identifier $ClusterId `
  --engine aurora-postgresql `
  --with-express-configuration `
  --master-username $MasterUser `
  --manage-master-user-password `
  --serverless-v2-scaling-configuration "MinCapacity=$MinAcu,MaxCapacity=$MaxAcu"
#
# If the CLI rejects any flag above under express configuration (express auto-sets
# a lot), fall back to the minimal form and configure the rest afterwards:
#
#   aws rds create-db-cluster --region $Region `
#     --db-cluster-identifier $ClusterId --engine aurora-postgresql `
#     --with-express-configuration
#   aws rds modify-db-cluster --region $Region --db-cluster-identifier $ClusterId `
#     --serverless-v2-scaling-configuration "MinCapacity=$MinAcu,MaxCapacity=$MaxAcu" `
#     --manage-master-user-password --apply-immediately
#   # then create the app database with a one-off `CREATE DATABASE mesmer;`

Write-Host "Waiting for the cluster to become available (this takes a few minutes)..."
aws rds wait db-cluster-available --region $Region --db-cluster-identifier $ClusterId

Write-Host ""
Write-Host "Cluster available. Connection details to wire into the app (Phase 3.4):"
aws rds describe-db-clusters `
  --region $Region --db-cluster-identifier $ClusterId `
  --query "DBClusters[0].{Endpoint:Endpoint,Port:Port,SecretArn:MasterUserSecret.SecretArn}" `
  --output table

Write-Host ""
Write-Host "Next: the app can use the default 'postgres' database, or create a"
Write-Host "dedicated one once, connecting with the master creds from the secret:"
Write-Host "    CREATE DATABASE $DbName;"
