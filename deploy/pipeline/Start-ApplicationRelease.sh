#!/usr/bin/env bash
set -Eeuo pipefail

fail() {
  printf 'error: %s\n' "$1" >&2
  exit 1
}

usage() {
  cat <<'EOF'
Usage: bash deploy/pipeline/Start-ApplicationRelease.sh [options]

Required:
  --environment dev|stg|prd
  --infra-repo-path PATH

Optional:
  --source-repo-path PATH      Defaults to this repository.
  --source-ref REF             Defaults to HEAD.
  --aws-profile PROFILE        Defaults to aws_profile in the tfvars file.
  --region REGION              Defaults to ap-southeast-1.
  --wait                       Wait for the pipeline's terminal status.
  -h, --help
EOF
}

environment=''
infra_repo_path=''
source_repo_path=''
source_ref='HEAD'
aws_profile=''
region='ap-southeast-1'
wait_for_pipeline=false

while (($#)); do
  case "$1" in
    --environment|-Environment) environment=${2:?"$1 requires a value"}; shift 2 ;;
    --infra-repo-path|-InfraRepoPath) infra_repo_path=${2:?"$1 requires a value"}; shift 2 ;;
    --source-repo-path|-SourceRepoPath) source_repo_path=${2:?"$1 requires a value"}; shift 2 ;;
    --source-ref|-SourceRef) source_ref=${2:?"$1 requires a value"}; shift 2 ;;
    --aws-profile|-AwsProfile) aws_profile=${2:?"$1 requires a value"}; shift 2 ;;
    --region|-Region) region=${2:?"$1 requires a value"}; shift 2 ;;
    --wait|-Wait) wait_for_pipeline=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) fail "Unknown option '$1'." ;;
  esac
done

case "$environment" in
  dev|stg|prd) ;;
  *) fail '--environment must be dev, stg, or prd.' ;;
esac
[ -n "$infra_repo_path" ] || fail '--infra-repo-path is required.'
for required_command in git aws terraform; do
  command -v "$required_command" >/dev/null 2>&1 || \
    fail "Required command '$required_command' was not found in PATH."
done

export AWS_PAGER=''
export AWS_CLI_AUTO_PROMPT='off'

script_directory=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
if [ -z "$source_repo_path" ]; then
  source_repo_path="$script_directory/../.."
fi
[ -d "$source_repo_path" ] || fail "Source repository path was not found: $source_repo_path"
source_repo_path=$(cd -- "$source_repo_path" && pwd -P)
resolved_commit=$(git -C "$source_repo_path" rev-parse --verify "${source_ref}^{commit}") || \
  fail "Git ref '$source_ref' could not be resolved in $source_repo_path."

identity_guard_path="$script_directory/Test-DeploymentIdentity.sh"
[ -f "$identity_guard_path" ] || fail "Deployment identity guard was not found: $identity_guard_path"
bash "$identity_guard_path" --repository-root "$source_repo_path" --git-commit "$resolved_commit"

[ -d "$infra_repo_path" ] || fail "Infrastructure repository path was not found: $infra_repo_path"
infra_repo_path=$(cd -- "$infra_repo_path" && pwd -P)
app_infra_path="$infra_repo_path/app/application"
tfvars_path="$app_infra_path/environments/$environment.tfvars"
[ -f "$tfvars_path" ] || fail "Terraform tfvars file not found: $tfvars_path"

if [ -z "$aws_profile" ]; then
  aws_profile=$(awk '
    /^[ \t]*aws_profile[ \t]*=/ {
      line=$0
      sub(/^[^=]*=[ \t]*"/, "", line)
      sub(/".*/, "", line)
      print line
      exit
    }
  ' "$tfvars_path")
fi
[ -n "$aws_profile" ] || \
  fail "AWS profile was not provided and could not be read from $tfvars_path."

pipeline_name=$(terraform "-chdir=$app_infra_path" output -raw pipeline_name 2>/dev/null || true)
if [ -z "$pipeline_name" ]; then
  pipeline_name="application-$environment-pipeline"
fi

source_configuration=$(aws codepipeline get-pipeline \
  --name "$pipeline_name" \
  --region "$region" \
  --profile "$aws_profile" \
  --query "pipeline.stages[?name=='Source'].actions[?name=='Source'].configuration.[S3Bucket,S3ObjectKey] | [0][0]" \
  --output text \
  --no-cli-pager)
read -r source_bucket source_object_key <<<"$source_configuration"
if [ -z "${source_bucket:-}" ] || [ "$source_bucket" = None ] || \
  [ -z "${source_object_key:-}" ] || [ "$source_object_key" = None ]; then
  fail "Pipeline '$pipeline_name' does not have an S3 source bucket/key configured."
fi

artifact_path=$(mktemp "${TMPDIR:-/tmp}/application-$environment-source.XXXXXX.zip")
trap 'rm -f -- "$artifact_path"' EXIT
printf 'Packaging %s to %s\n' "$source_repo_path" "$artifact_path"
git -C "$source_repo_path" archive --format=zip --output="$artifact_path" "$resolved_commit"
[ -f "$artifact_path" ] || fail "git archive did not create the source artifact for '$resolved_commit'."
printf 'Packaged immutable source commit: %s\n' "$resolved_commit"

printf 'Uploading source artifact to s3://%s/%s\n' "$source_bucket" "$source_object_key"
aws s3 cp "$artifact_path" "s3://$source_bucket/$source_object_key" \
  --region "$region" --profile "$aws_profile" --no-cli-pager

printf 'Starting CodePipeline execution: %s\n' "$pipeline_name"
execution_id=$(aws codepipeline start-pipeline-execution \
  --name "$pipeline_name" \
  --region "$region" \
  --profile "$aws_profile" \
  --query pipelineExecutionId \
  --output text \
  --no-cli-pager)
if [ -z "$execution_id" ] || [ "$execution_id" = None ]; then
  fail 'CodePipeline did not return an execution ID.'
fi
printf 'Pipeline execution ID: %s\n' "$execution_id"

if [ "$wait_for_pipeline" = true ]; then
  while :; do
    sleep 30
    status=$(aws codepipeline get-pipeline-execution \
      --pipeline-name "$pipeline_name" \
      --pipeline-execution-id "$execution_id" \
      --region "$region" \
      --profile "$aws_profile" \
      --query 'pipelineExecution.status' \
      --output text \
      --no-cli-pager)
    printf 'Pipeline status: %s\n' "$status"
    case "$status" in
      InProgress|Stopping) ;;
      *) break ;;
    esac
  done
  if [ "$status" != Succeeded ]; then
    aws codepipeline list-action-executions \
      --pipeline-name "$pipeline_name" \
      --region "$region" \
      --profile "$aws_profile" \
      --filter "pipelineExecutionId=$execution_id" \
      --query 'actionExecutionDetails[].{stage:stageName,action:actionName,status:status,error:errorDetails.message}' \
      --output table \
      --no-cli-pager
    fail "Pipeline execution finished with status '$status'."
  fi
fi
