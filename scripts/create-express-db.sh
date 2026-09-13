#!/usr/bin/env bash
#
# Create the Mesmer Aurora PostgreSQL cluster using EXPRESS CONFIGURATION.
#
# Why this is a script and not in template.yaml: on the AWS free plan Aurora must
# use express configuration, which (a) cannot be created via CloudFormation (the
# AWS::RDS::DBCluster resource has no WithExpressConfiguration property) and
# (b) is VPC-less — reached over a managed access endpoint, not from inside a VPC.
# So the database is created out of band here; the Lambda stays out of a VPC and
# reaches the DB, S3, and Secrets Manager directly over the internet.
#
# Express configuration is intentionally minimal. It auto-provisions networking,
# auto-adds the DB instance, and uses the default 'postgres' database and master
# user with IAM authentication. It REJECTS most create flags (--database-name,
# --manage-master-user-password, --master-username), so the create call takes
# only the essentials and scaling is set afterwards with modify-db-cluster.
#
# Prerequisites: AWS CLI configured (`aws configure`) with RDS permissions.
set -euo pipefail

CLUSTER_ID="${CLUSTER_ID:-mesmer-dev}"
REGION="${AWS_REGION:-eu-north-1}"
MIN_ACU="${MIN_ACU:-0}"     # 0 = scale to zero when idle (storage-only cost)
MAX_ACU="${MAX_ACU:-2}"     # free plan allows up to 4

echo "Creating express Aurora cluster '${CLUSTER_ID}' in ${REGION}..."
aws rds create-db-cluster \
  --region "${REGION}" \
  --db-cluster-identifier "${CLUSTER_ID}" \
  --engine aurora-postgresql \
  --with-express-configuration

echo "Waiting for the cluster to become available (this takes a few minutes)..."
aws rds wait db-cluster-available \
  --region "${REGION}" --db-cluster-identifier "${CLUSTER_ID}"

# Set Serverless v2 scaling after creation (min 0 = scale to zero). Non-fatal:
# if express doesn't allow it, the cluster is still usable at its defaults.
echo "Setting scaling to Min=${MIN_ACU}, Max=${MAX_ACU} ACUs..."
aws rds modify-db-cluster \
  --region "${REGION}" --db-cluster-identifier "${CLUSTER_ID}" \
  --serverless-v2-scaling-configuration "MinCapacity=${MIN_ACU},MaxCapacity=${MAX_ACU}" \
  --apply-immediately \
  || echo "warning: could not set scaling automatically; adjust it in the RDS console."

echo
echo "Connection details to wire into the app (Phase 3.4):"
aws rds describe-db-clusters \
  --region "${REGION}" --db-cluster-identifier "${CLUSTER_ID}" \
  --query 'DBClusters[0].{Endpoint:Endpoint,Port:Port,MasterUsername:MasterUsername,IamAuth:IAMDatabaseAuthenticationEnabled,SecretArn:MasterUserSecret.SecretArn}' \
  --output table

echo
echo "Express clusters default to the 'postgres' database and IAM authentication."
echo "Paste this table back so the app connection (Phase 3.4) can be wired to match."
