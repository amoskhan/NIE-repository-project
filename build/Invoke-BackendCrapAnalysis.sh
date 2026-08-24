#!/usr/bin/env bash
set -Eeuo pipefail

configuration_error() {
  printf 'error: %s\n' "$1" >&2
  exit 2
}

usage() {
  cat <<'EOF'
Usage: bash build/Invoke-BackendCrapAnalysis.sh [options]

Options:
  --configuration Debug|Release
  --open-cover-directory PATH
  --report-directory PATH
  --maximum-crap-score N
  --maximum-cyclomatic-complexity N
  --top N
  --summary-path PATH
  --skip-build
  --skip-tool-restore
  -h, --help
EOF
}

configuration='Release'
open_cover_directory='artifacts/opencover'
report_directory='artifacts/crap-report'
maximum_crap_score='40000'
maximum_cyclomatic_complexity='200'
top='25'
summary_path=''
skip_build=false
skip_tool_restore=false

while (($#)); do
  case "$1" in
    --configuration|-Configuration) configuration=${2:?"$1 requires a value"}; shift 2 ;;
    --open-cover-directory|-OpenCoverDirectory) open_cover_directory=${2:?"$1 requires a value"}; shift 2 ;;
    --report-directory|-ReportDirectory) report_directory=${2:?"$1 requires a value"}; shift 2 ;;
    --maximum-crap-score|-MaximumCrapScore) maximum_crap_score=${2:?"$1 requires a value"}; shift 2 ;;
    --maximum-cyclomatic-complexity|-MaximumCyclomaticComplexity) maximum_cyclomatic_complexity=${2:?"$1 requires a value"}; shift 2 ;;
    --top|-Top) top=${2:?"$1 requires a value"}; shift 2 ;;
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

open_cover_path=$(resolve_repository_path "$open_cover_directory")
report_path=$(resolve_repository_path "$report_directory")
cd -- "$repository_root"

if [[ $skip_tool_restore == false ]]; then
  dotnet tool restore
fi
if [[ $skip_build == false ]]; then
  dotnet build src/backend/Backend.sln -c "$configuration" -warnaserror
fi

mapfile -d '' -t test_projects < <(
  find src/backend/Tests -mindepth 2 -maxdepth 2 -type f -name '*.Tests.csproj' -print0 | sort -z
)
((${#test_projects[@]} > 0)) || configuration_error 'No backend test projects were found.'

mkdir -p -- "$open_cover_path"
find "$open_cover_path" -maxdepth 1 -type f -name '*.opencover.xml' -delete

for test_project in "${test_projects[@]}"; do
  project_directory=$(dirname -- "$test_project")
  name=$(basename -- "$test_project" .csproj)
  test_assembly_path="$project_directory/bin/$configuration/net10.0/$name.dll"
  [[ -f $test_assembly_path ]] || \
    configuration_error "Expected built test assembly '$test_assembly_path' for '$test_project'."

  output_path="$open_cover_path/$name.opencover.xml"
  # Coverlet passes this string to a nested dotnet process. Keep the project
  # repository-relative so Linux and Git Bash do not double-convert an embedded
  # absolute path.
  target_arguments="test --project \"$test_project\" -c $configuration --no-build --no-progress"
  printf 'Collecting OpenCover coverage for %s\n' "$name"
  dotnet tool run coverlet -- \
    "$test_assembly_path" \
    --target dotnet \
    --targetargs "$target_arguments" \
    --format opencover \
    --output "$output_path" \
    --exclude '[*.Tests]*' \
    --exclude-by-file '**/Migrations/**'
  [[ -f $output_path ]] || \
    configuration_error "Coverage collection for '$name' did not produce '$output_path'."
done

report_count=$(find "$open_cover_path" -maxdepth 1 -type f -name '*.opencover.xml' -print | wc -l)
if ((report_count != ${#test_projects[@]})); then
  configuration_error "Expected ${#test_projects[@]} OpenCover reports but found $report_count."
fi

dotnet tool run reportgenerator -- \
  "-reports:$open_cover_directory/*.opencover.xml" \
  "-targetdir:$report_directory" \
  '-reporttypes:Html;MarkdownSummaryGithub;TextSummary' \
  '-title:NIE Template risk hotspots' \
  'riskHotspotsAnalysisThresholds:metricThresholdForCyclomaticComplexity=15' \
  "riskHotspotsAnalysisThresholds:maximumThresholdForCyclomaticComplexity=$maximum_cyclomatic_complexity"

complexity_summary="$report_path/SummaryGithub.md"
if [[ -n $summary_path && -f $complexity_summary ]]; then
  cat -- "$complexity_summary" >>"$summary_path"
fi

crap_arguments=(
  --path "$open_cover_directory/*.opencover.xml"
  --maximum-crap-score "$maximum_crap_score"
  --top "$top"
)
if [[ -n $summary_path ]]; then
  crap_arguments+=(--summary-path "$summary_path")
fi
bash "$script_directory/Get-CrapScore.sh" "${crap_arguments[@]}"
