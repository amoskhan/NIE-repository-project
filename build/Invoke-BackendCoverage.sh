#!/usr/bin/env bash
set -Eeuo pipefail

configuration_error() {
  printf 'error: %s\n' "$1" >&2
  exit 2
}

usage() {
  cat <<'EOF'
Usage: bash build/Invoke-BackendCoverage.sh [options]

Options:
  --configuration Debug|Release
  --results-directory PATH
  --report-directory PATH
  --minimum-expected-tests N
  --minimum-line-coverage N
  --minimum-branch-coverage N
  --minimum-method-coverage N
  --summary-path PATH
  --skip-build
  --skip-tool-restore
  -h, --help
EOF
}

configuration='Release'
results_directory='artifacts/test-results'
report_directory='artifacts/coverage-report'
minimum_expected_tests='1100'
minimum_line_coverage='33'
minimum_branch_coverage='28'
minimum_method_coverage='35'
summary_path=''
skip_build=false
skip_tool_restore=false

while (($#)); do
  case "$1" in
    --configuration|-Configuration) configuration=${2:?"$1 requires a value"}; shift 2 ;;
    --results-directory|-ResultsDirectory) results_directory=${2:?"$1 requires a value"}; shift 2 ;;
    --report-directory|-ReportDirectory) report_directory=${2:?"$1 requires a value"}; shift 2 ;;
    --minimum-expected-tests|-MinimumExpectedTests) minimum_expected_tests=${2:?"$1 requires a value"}; shift 2 ;;
    --minimum-line-coverage|-MinimumLineCoverage) minimum_line_coverage=${2:?"$1 requires a value"}; shift 2 ;;
    --minimum-branch-coverage|-MinimumBranchCoverage) minimum_branch_coverage=${2:?"$1 requires a value"}; shift 2 ;;
    --minimum-method-coverage|-MinimumMethodCoverage) minimum_method_coverage=${2:?"$1 requires a value"}; shift 2 ;;
    --summary-path|-SummaryPath) summary_path=${2:?"$1 requires a value"}; shift 2 ;;
    --skip-build|-SkipBuild) skip_build=true; shift ;;
    --skip-tool-restore|-SkipToolRestore) skip_tool_restore=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) configuration_error "Unknown option '$1'." ;;
  esac
done

[[ $configuration == Debug || $configuration == Release ]] || \
  configuration_error "Configuration must be Debug or Release."
command -v dotnet >/dev/null 2>&1 || configuration_error "Required command 'dotnet' was not found in PATH."
command -v realpath >/dev/null 2>&1 || configuration_error "Required command 'realpath' was not found in PATH."

script_directory=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
repository_root=$(cd -- "$script_directory/.." && pwd -P)

resolve_repository_path() {
  local candidate=$1
  if [[ $candidate != /* ]]; then
    candidate="$repository_root/$candidate"
  fi
  local resolved
  resolved=$(realpath -m -- "$candidate")
  case "$resolved" in
    "$repository_root"/*) printf '%s\n' "$resolved" ;;
    *) configuration_error "Path '$1' resolves outside the repository." ;;
  esac
}

results_path=$(resolve_repository_path "$results_directory")
cd -- "$repository_root"

if [[ $skip_tool_restore == false ]]; then
  dotnet tool restore
fi
if [[ $skip_build == false ]]; then
  dotnet build src/backend/Backend.sln -c "$configuration" -warnaserror
fi

mkdir -p -- "$results_path"
find "$results_path" -type f -name '*.cobertura.xml' -delete

dotnet test --solution src/backend/Backend.sln \
  -c "$configuration" \
  --no-build \
  --results-directory "$results_path" \
  --minimum-expected-tests "$minimum_expected_tests" \
  --coverage \
  --coverage-output-format cobertura \
  --coverage-settings src/backend/coverage.runsettings

test_project_count=$(find src/backend/Tests -mindepth 2 -maxdepth 2 -type f -name '*.Tests.csproj' -print | wc -l)
coverage_report_count=$(find "$results_path" -type f -name '*.cobertura.xml' -print | wc -l)
if ((test_project_count == 0 || coverage_report_count != test_project_count)); then
  configuration_error "Expected one coverage report for each of $test_project_count test projects but found $coverage_report_count."
fi

report_arguments=(
  --reports "$results_directory/**/*.cobertura.xml"
  --target-directory "$report_directory"
  --minimum-line-coverage "$minimum_line_coverage"
  --minimum-branch-coverage "$minimum_branch_coverage"
  --minimum-method-coverage "$minimum_method_coverage"
  --skip-tool-restore
)
if [[ -n $summary_path ]]; then
  report_arguments+=(--summary-path "$summary_path")
fi
bash "$script_directory/Invoke-BackendCoverageReport.sh" "${report_arguments[@]}"
