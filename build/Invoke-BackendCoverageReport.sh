#!/usr/bin/env bash
set -Eeuo pipefail

configuration_error() {
  printf 'error: %s\n' "$1" >&2
  exit 2
}

usage() {
  cat <<'EOF'
Usage: bash build/Invoke-BackendCoverageReport.sh [options]

Options:
  --reports PATH                  Cobertura report glob.
  --target-directory PATH        ReportGenerator output directory.
  --minimum-line-coverage N      Minimum line coverage percentage.
  --minimum-branch-coverage N    Minimum branch coverage percentage.
  --minimum-method-coverage N    Minimum method coverage percentage.
  --summary-path PATH             Optional GitHub Markdown summary file.
  --skip-tool-restore             Do not run dotnet tool restore.
  -h, --help                     Show this help.
EOF
}

reports='artifacts/test-results/**/*.cobertura.xml'
target_directory='artifacts/coverage-report'
minimum_line_coverage='33'
minimum_branch_coverage='28'
minimum_method_coverage='35'
summary_path=''
skip_tool_restore=false

while (($#)); do
  case "$1" in
    --reports|-Reports) reports=${2:?"$1 requires a value"}; shift 2 ;;
    --target-directory|-TargetDirectory) target_directory=${2:?"$1 requires a value"}; shift 2 ;;
    --minimum-line-coverage|-MinimumLineCoverage) minimum_line_coverage=${2:?"$1 requires a value"}; shift 2 ;;
    --minimum-branch-coverage|-MinimumBranchCoverage) minimum_branch_coverage=${2:?"$1 requires a value"}; shift 2 ;;
    --minimum-method-coverage|-MinimumMethodCoverage) minimum_method_coverage=${2:?"$1 requires a value"}; shift 2 ;;
    --summary-path|-SummaryPath) summary_path=${2:?"$1 requires a value"}; shift 2 ;;
    --skip-tool-restore|-SkipToolRestore) skip_tool_restore=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) configuration_error "Unknown option '$1'." ;;
  esac
done

command -v dotnet >/dev/null 2>&1 || configuration_error "Required command 'dotnet' was not found in PATH."

script_directory=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
repository_root=$(cd -- "$script_directory/.." && pwd -P)
cd -- "$repository_root"

if [[ $skip_tool_restore == false ]]; then
  dotnet tool restore
fi

dotnet tool run reportgenerator -- \
  "-reports:$reports" \
  "-targetdir:$target_directory" \
  '-reporttypes:Html;Cobertura;MarkdownSummaryGithub;TextSummary;SonarQube' \
  '-title:NIE Template backend coverage' \
  'riskHotspotsAnalysisThresholds:metricThresholdForCyclomaticComplexity=15' \
  'riskHotspotsAnalysisThresholds:metricThresholdForCrapScore=30' \
  "minimumCoverageThresholds:lineCoverage=$minimum_line_coverage" \
  "minimumCoverageThresholds:branchCoverage=$minimum_branch_coverage" \
  "minimumCoverageThresholds:methodCoverage=$minimum_method_coverage"

text_summary="$target_directory/Summary.txt"
if [[ -f $text_summary ]]; then
  cat -- "$text_summary"
fi

github_summary="$target_directory/SummaryGithub.md"
if [[ -n $summary_path && -f $github_summary ]]; then
  cat -- "$github_summary" >>"$summary_path"
fi
