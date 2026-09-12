#!/usr/bin/env bash
#
# Create the Mesmer Aurora PostgreSQL cluster using EXPRESS CONFIGURATION.
#
# Why this is a script and not in template.yaml: on the AWS free plan Aurora must
# use express configuration, which (a) cannot be created via CloudFormation (the
# AWS::RDS::DBCluster resource has no WithExpressConfiguration property) and
# (b) is VPC-less — it is reached over a managed access endpoint, not from inside
# a VPC. So the database is created out of band here, and the app is given its
# endpoint + Secrets Manager secret at runtime (Phase 3.4). The Lambda stays out
# of a VPC and reaches the DB, S3, and Secrets Manager directly over the internet.
#
# Prerequisites: AWS CLI configured (`aws configure`) with RDS permissions.
# Re-running with an existing identifier errors — delete it first or set CLUSTER_ID.
set -euo pipefail

CLUSTER_ID="${CLUSTER_ID:-mesmer-dev}"
REGION="${AWS_REGION:-eu-north-1}"
DB_NAME="${DB_NAME:-mesmer}"
MASTER_USER="${MASTER_USER:-mesmeradmin}"
MIN_ACU="${MIN_ACU:-0}"     # 0 = scale to zero when idle (storage-only cost)
MAX_ACU="${MAX_ACU:-2}"     # free plan allows up to 4

echo "Creating express Aurora cluster '${CLUSTER_ID}' in ${REGION}..."

# Express configuration auto-provisions networking (no VPC/subnet/SG needed).
# --manage-master-user-password stores the master password in Secrets Manager so
# the app can read it at runtime; --database-name creates the initial database.
aws rds create-db-cluster \
  --region "${REGION}" \
  --db-cluster-identifier "${CLUSTER_ID}" \
  --engine aurora-postgresql \
  --with-express-configuration \
  --database-name "${DB_NAME}" \
  --master-username "${MASTER_USER}" \
  --manage-master-user-password \
  --serverless-v2-scaling-configuration "MinCapacity=${MIN_ACU},MaxCapacity=${MAX_ACU}"
#
# If the CLI rejects any flag above under express configuration (express auto-sets
# a lot), fall back to the minimal form and configure the rest afterwards:
#
#   aws rds create-db-cluster --region "$REGION" \
#     --db-cluster-identifier "$CLUSTER_ID" --engine aurora-postgresql \
#     --with-express-configuration
#   aws rds modify-db-cluster --region "$REGION" --db-cluster-identifier "$CLUSTER_ID" \
#     --serverless-v2-scaling-configuration "MinCapacity=$MIN_ACU,MaxCapacity=$MAX_ACU" \
#     --manage-master-user-password --apply-immediately
#   # then create the app database with a one-off `CREATE DATABASE mesmer;`

echo "Waiting for the cluster to become available (this takes a few minutes)..."
aws rds wait db-cluster-available \
  --region "${REGION}" --db-cluster-identifier "${CLUSTER_ID}"

echo
echo "Cluster available. Connection details to wire into the app (Phase 3.4):"
aws rds describe-db-clusters \
  --region "${REGION}" --db-cluster-identifier "${CLUSTER_ID}" \
  --query 'DBClusters[0].{Endpoint:Endpoint,Port:Port,Database:DatabaseName,SecretArn:MasterUserSecret.SecretArn}' \
  --output table
