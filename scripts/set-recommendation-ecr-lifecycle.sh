#!/usr/bin/env bash
#
# Attach an ECR lifecycle policy to the recommendation batch image repository so
# only the most recent images are kept. Old image versions accumulate on every
# deploy; on the free plan ECR storage over 500 MB is billed (~$0.10/GB-mo), and
# a librosa image is ~1-1.5 GB, so pruning keeps the cost to a few cents.
#
# The batch image is pushed to a SAM-managed ECR repo (created by
# `sam deploy --resolve-image-repos`). Run this ONCE after the first deploy (and
# it's safe to re-run). Find the repo name from the deployed image if the
# default guess doesn't match your account's SAM-managed naming.
#
# Prerequisites: AWS CLI configured with ECR permissions.
set -euo pipefail

REGION="${AWS_REGION:-eu-north-1}"
# SAM-managed image repos are typically named after the stack + function. Pass
# REPO explicitly if your account uses a different managed name.
REPO="${REPO:-}"
KEEP="${KEEP:-2}"   # number of most-recent images to retain

if [[ -z "${REPO}" ]]; then
  echo "Discovering recommendation batch image repositories in ${REGION}..."
  aws ecr describe-repositories --region "${REGION}" \
    --query "repositories[?contains(repositoryName, 'recommendation') || contains(repositoryName, 'batch')].repositoryName" \
    --output table || true
  echo
  echo "Set REPO=<repositoryName> and re-run, e.g.:"
  echo "  REPO=<name> $0"
  exit 1
fi

echo "Applying keep-last-${KEEP} lifecycle policy to '${REPO}' in ${REGION}..."
aws ecr put-lifecycle-policy \
  --region "${REGION}" \
  --repository-name "${REPO}" \
  --lifecycle-policy-text "{
    \"rules\": [
      {
        \"rulePriority\": 1,
        \"description\": \"Keep only the ${KEEP} most recent images\",
        \"selection\": {
          \"tagStatus\": \"any\",
          \"countType\": \"imageCountMoreThan\",
          \"countNumber\": ${KEEP}
        },
        \"action\": { \"type\": \"expire\" }
      }
    ]
  }"

echo "Done. ECR will now prune all but the latest ${KEEP} images in '${REPO}'."
