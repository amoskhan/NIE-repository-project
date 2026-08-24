#!/usr/bin/env bash
set -Eeuo pipefail

fail() {
  printf 'error: %s\n' "$1" >&2
  exit 1
}

script_directory=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
guard_path="$script_directory/Test-DeploymentIdentity.sh"
[[ -f $guard_path ]] || fail "Deployment identity guard was not found: $guard_path"
command -v git >/dev/null 2>&1 || fail "Required command 'git' was not found in PATH."

test_root=$(mktemp -d "${TMPDIR:-/tmp}/nie-deployment-identity.XXXXXX")
trap 'rm -rf -- "$test_root"' EXIT

# Simulate the output state produced when Copier preserves customized legacy
# deployment files while also rendering the new app-named artifacts.
chart_path="$test_root/deploy/helm/sample-app"
pipeline_path="$test_root/deploy/pipeline"
mkdir -p -- "$chart_path" "$pipeline_path"
: >"$chart_path/Chart.yaml"
: >"$pipeline_path/Start-sample-appRelease.ps1"
: >"$pipeline_path/Start-sample-appRelease.sh"

bash "$guard_path" --expected-app-name sample-app --repository-root "$test_root"

legacy_chart_path="$test_root/deploy/helm/application"
mkdir -p -- "$legacy_chart_path"
printf 'customized: true\n' >"$legacy_chart_path/Chart.yaml"
printf '# customized\n' >"$pipeline_path/Start-ApplicationRelease.ps1"
printf '# customized\n' >"$pipeline_path/Start-ApplicationRelease.sh"

if legacy_output=$(bash "$guard_path" --expected-app-name sample-app --repository-root "$test_root" 2>&1); then
  fail 'Expected the migration guard to reject legacy artifacts.'
fi
grep -Fq 'Legacy generic deployment artifacts were found' <<<"$legacy_output" || \
  fail 'Migration guard failed without the expected legacy-artifact diagnostic.'

git -C "$test_root" init --quiet
git -C "$test_root" config user.name 'Deployment Identity Test'
git -C "$test_root" config user.email 'deployment-identity-test@example.invalid'
git -C "$test_root" add --all
git -C "$test_root" commit --quiet -m 'legacy deployment layout'
legacy_commit=$(git -C "$test_root" rev-parse HEAD)

rm -rf -- "$legacy_chart_path"
rm -f -- \
  "$pipeline_path/Start-ApplicationRelease.ps1" \
  "$pipeline_path/Start-ApplicationRelease.sh"
git -C "$test_root" add --all
git -C "$test_root" commit --quiet -m 'app-named deployment layout'

bash "$guard_path" --expected-app-name sample-app --repository-root "$test_root"
if legacy_commit_output=$(bash "$guard_path" \
  --expected-app-name sample-app \
  --repository-root "$test_root" \
  --git-commit "$legacy_commit" 2>&1); then
  fail 'Expected the commit-bound migration guard to reject a legacy SourceRef.'
fi
grep -Fq 'Legacy generic deployment artifacts were found' <<<"$legacy_commit_output" || \
  fail 'Commit-bound guard failed without the expected legacy-artifact diagnostic.'

printf 'Deployment identity migration regression test passed.\n'
