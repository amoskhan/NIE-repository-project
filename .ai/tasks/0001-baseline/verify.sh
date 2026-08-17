#!/usr/bin/env bash
# verify.sh — runs after apply.md. Exit 0 = task verified. Non-zero = revert and stop.
set -euo pipefail

PYTHON="${PYTHON:-python3}"

# 1. The baseline adoption marker must exist and name a template version.
test -f .app-template-version.json
grep -q '"templateVersion"' .app-template-version.json

# 2. The backend solution must still build.
dotnet build src/backend/AppTemplate.sln

# 3. Version metadata must be self-consistent (template repo: marker + index +
#    manifests + CHANGELOG; derived repo: marker shape only).
if [ -f tools/template-versioning/release.py ]; then
  "$PYTHON" tools/template-versioning/release.py validate
fi

echo "verify 0001: OK"
