#!/usr/bin/env bash
set -Eeuo pipefail

configuration_error() {
  printf 'error: %s\n' "$1" >&2
  exit 2
}

usage() {
  cat <<'EOF'
Usage: bash build/Invoke-BackendMutation.sh [options]

Options:
  --project FILE             Project to mutate; repeat for multiple projects.
  --skip-tool-restore        Do not run dotnet tool restore.
  -h, --help
EOF
}

projects=('Domain.csproj' 'Application.csproj')
projects_supplied=false
skip_tool_restore=false

while (($#)); do
  case "$1" in
    --project|-Project|-Projects)
      if [[ $projects_supplied == false ]]; then
        projects=()
        projects_supplied=true
      fi
      projects+=("${2:?"$1 requires a value"}")
      shift 2
      ;;
    --skip-tool-restore|-SkipToolRestore) skip_tool_restore=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) configuration_error "Unknown option '$1'." ;;
  esac
done

((${#projects[@]} > 0)) || configuration_error 'At least one project is required.'
command -v dotnet >/dev/null 2>&1 || configuration_error "Required command 'dotnet' was not found in PATH."

script_directory=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
repository_root=$(cd -- "$script_directory/.." && pwd -P)
cd -- "$repository_root"

if [[ $skip_tool_restore == false ]]; then
  dotnet tool restore
fi

cd src/backend
for project in "${projects[@]}"; do
  project_name=$(basename -- "$project" .csproj)
  dotnet tool run dotnet-stryker -- \
    --config-file stryker-config.json \
    --project "$project" \
    --test-runner mtp \
    --output "StrykerOutput/$project_name"
done
