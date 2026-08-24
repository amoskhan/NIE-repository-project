#!/bin/bash
# LocalStack S3 init hook — runs once LocalStack's S3 service is ready.
# Creates the local development bucket so FileStorage:Provider=S3 works out of the box.
# Idempotent: re-running on an existing bucket is a no-op.
set -eu

BUCKET="${LOCAL_S3_BUCKET:-application-local}"

awslocal s3 mb "s3://${BUCKET}" 2>/dev/null || true
# Permissive CORS so a local frontend can read pre-signed/static objects during dev.
awslocal s3api put-bucket-cors --bucket "${BUCKET}" --cors-configuration '{
  "CORSRules": [
    { "AllowedOrigins": ["*"], "AllowedMethods": ["GET", "PUT", "POST", "DELETE", "HEAD"], "AllowedHeaders": ["*"] }
  ]
}' 2>/dev/null || true

echo "LocalStack: ensured s3://${BUCKET} (dev file-storage bucket)"
