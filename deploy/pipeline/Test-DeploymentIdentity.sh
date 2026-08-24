#!/usr/bin/env bash
set -Eeuo pipefail

fail() {
  printf 'error: %s\n' "$1" >&2
  exit 1
}

usage() {
  cat <<'EOF'
Usage: bash deploy/pipeline/Test-DeploymentIdentity.sh [options]

Options:
  --expected-app-name NAME    Expected lower-case application slug.
  --repository-root PATH      Repository to inspect.
  --git-commit REF            Inspect the exact committed tree instead of disk.
  -h, --help
EOF
}

script_directory=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
expected_app_name='application'
repository_root=$(cd -- "$script_directory/../.." && pwd -P)
git_commit=''

while (($#)); do
  case "$1" in
    --expected-app-name|-ExpectedAppName) expected_app_name=${2:?"$1 requires a value"}; shift 2 ;;
    --repository-root|-RepositoryRoot) repository_root=${2:?"$1 requires a value"}; shift 2 ;;
    --git-commit|-GitCommit) git_commit=${2:?"$1 requires a value"}; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) fail "Unknown option '$1'." ;;
  esac
done

grep -Eq '^[a-z][a-z0-9-]{2,39}$' <<<"$expected_app_name" || \
  fail "Expected app name '$expected_app_name' must be lower-case kebab-case, 3-40 characters, starting with a letter."
[ -d "$repository_root" ] || fail "Repository root was not found: $repository_root"
repository_path=$(cd -- "$repository_root" && pwd -P)

if [ "$expected_app_name" = application ]; then
  expected_release_stem='Start-ApplicationRelease'
else
  expected_release_stem="Start-${expected_app_name}Release"
fi
expected_chart_relative="deploy/helm/$expected_app_name/Chart.yaml"
expected_release_files=(
  "deploy/pipeline/$expected_release_stem.ps1"
  "deploy/pipeline/$expected_release_stem.sh"
)
legacy_release_files=(
  'deploy/pipeline/Start-ApplicationRelease.ps1'
  'deploy/pipeline/Start-ApplicationRelease.sh'
)
problems=()

if [ -n "$git_commit" ]; then
  command -v git >/dev/null 2>&1 || fail "Required command 'git' was not found in PATH."
  resolved_commit=$(git -C "$repository_path" rev-parse --verify "${git_commit}^{commit}") || \
    fail "Git commit '$git_commit' could not be resolved in $repository_path."
  tree_files=$(git -C "$repository_path" ls-tree -r --name-only "$resolved_commit" -- deploy/helm deploy/pipeline) || \
    fail "Git tree inspection failed for commit '$resolved_commit'."

  if ! grep -Fqx -- "$expected_chart_relative" <<<"$tree_files"; then
    problems+=("Expected Helm chart is missing from commit $resolved_commit: $expected_chart_relative")
  fi
  for expected_release_file in "${expected_release_files[@]}"; do
    if ! grep -Fqx -- "$expected_release_file" <<<"$tree_files"; then
      problems+=("Expected release script is missing from commit $resolved_commit: $expected_release_file")
    fi
  done

  if [ "$expected_app_name" != application ]; then
    if grep -q '^deploy/helm/application/' <<<"$tree_files"; then
      problems+=("Legacy generic Helm chart still contains files in commit $resolved_commit: deploy/helm/application")
    fi
    for legacy_release_file in "${legacy_release_files[@]}"; do
      if grep -Fqx -- "$legacy_release_file" <<<"$tree_files"; then
        problems+=("Legacy generic release script still exists in commit $resolved_commit: $legacy_release_file")
      fi
    done
  fi
else
  if [ ! -f "$repository_path/$expected_chart_relative" ]; then
    problems+=("Expected Helm chart is missing: $repository_path/$expected_chart_relative")
  fi
  for expected_release_file in "${expected_release_files[@]}"; do
    if [ ! -f "$repository_path/$expected_release_file" ]; then
      problems+=("Expected release script is missing: $repository_path/$expected_release_file")
    fi
  done

  if [ "$expected_app_name" != application ]; then
    legacy_chart_path="$repository_path/deploy/helm/application"
    if [ -d "$legacy_chart_path" ] && find "$legacy_chart_path" -type f -print -quit | grep -q .; then
      problems+=("Legacy generic Helm chart still contains files: $legacy_chart_path")
    fi
    for legacy_release_file in "${legacy_release_files[@]}"; do
      if [ -f "$repository_path/$legacy_release_file" ]; then
        problems+=("Legacy generic release script still exists: $repository_path/$legacy_release_file")
      fi
    done
  fi
fi

if ((${#problems[@]} > 0)); then
  {
    printf 'Deployment identity validation failed.\n'
    printf '%s\n' "${problems[@]}"
    cat <<'EOF'

Legacy generic deployment artifacts were found or app-named artifacts are missing.
Do not deploy or delete customized files automatically. Follow the non-destructive
'Deployment identity migration on update' workflow in docs/template-distribution.md,
then rerun this guard.
EOF
  } >&2
  exit 1
fi

printf "Deployment identity is unambiguous for '%s'.\n" "$expected_app_name"
