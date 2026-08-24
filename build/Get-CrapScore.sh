#!/usr/bin/env bash
set -Eeuo pipefail

configuration_error() {
  printf 'error: %s\n' "$1" >&2
  exit 2
}

usage() {
  cat <<'EOF'
Usage: bash build/Get-CrapScore.sh --path GLOB [options]

Computes CRAP scores from OpenCover XML. The calculation and cross-report
sequence-point merge match build/Get-CrapScore.ps1.

Options:
  --path GLOB                     OpenCover report glob (required).
  --maximum-crap-score N          Fail when the highest score exceeds N.
  --top N                         Number of hotspots to show (default: 25).
  --summary-path PATH             Optional Markdown summary file.
  --exclude-class-pattern REGEX   Additional exclusion; repeat as needed.
  -h, --help
EOF
}

report_glob=''
maximum_crap_score='0'
top='25'
summary_path=''
exclude_patterns=(
  '\.Generated\.'
  '^System\.'
  '^Microsoft\.'
  '<[A-Za-z]+_g>'
  '_generated'
)
custom_exclusions=false

while (($#)); do
  case "$1" in
    --path|-Path) report_glob=${2:?"$1 requires a value"}; shift 2 ;;
    --maximum-crap-score|-MaximumCrapScore) maximum_crap_score=${2:?"$1 requires a value"}; shift 2 ;;
    --top|-Top) top=${2:?"$1 requires a value"}; shift 2 ;;
    --summary-path|-SummaryPath) summary_path=${2:?"$1 requires a value"}; shift 2 ;;
    --exclude-class-pattern|-ExcludeClassPattern)
      if [[ $custom_exclusions == false ]]; then
        exclude_patterns=()
        custom_exclusions=true
      fi
      exclude_patterns+=("${2:?"$1 requires a value"}")
      shift 2
      ;;
    -h|--help) usage; exit 0 ;;
    *) configuration_error "Unknown option '$1'." ;;
  esac
done

[[ -n $report_glob ]] || configuration_error '--path is required.'
command -v python3 >/dev/null 2>&1 || \
  configuration_error "Required command 'python3' was not found in PATH."

python3 - "$report_glob" "$maximum_crap_score" "$top" "$summary_path" "${exclude_patterns[@]}" <<'PYTHON'
from __future__ import annotations

import glob
import math
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path


@dataclass
class Aggregate:
    assembly: str
    class_name: str
    method: str
    complexity: float
    sequence_points: set[str] = field(default_factory=set)
    covered_points: set[str] = field(default_factory=set)
    fallback_coverage: float = 1.0
    has_coverage_reading: bool = False


def fail(message: str) -> None:
    print(f"error: {message}", file=sys.stderr)
    raise SystemExit(1)


def format_number(value: float) -> str:
    if value.is_integer():
        return str(int(value))
    return f"{value:.1f}"


report_glob = sys.argv[1]
try:
    maximum_crap_score = float(sys.argv[2])
    top = int(sys.argv[3])
except ValueError as error:
    fail(f"Invalid numeric option: {error}")
summary_path = sys.argv[4]
try:
    exclusion_patterns = [re.compile(pattern) for pattern in sys.argv[5:]]
except re.error as error:
    fail(f"Invalid exclusion regex: {error}")

if top < 1:
    fail("--top must be at least 1.")

files = sorted(Path(path) for path in glob.glob(report_glob, recursive=True))
if not files:
    fail(f"No OpenCover reports matched '{report_glob}'.")

method_map: dict[tuple[str, str, str, str], Aggregate] = {}
for report_path in files:
    try:
        document = ET.parse(report_path)
    except (ET.ParseError, OSError) as error:
        fail(f"Could not parse '{report_path}': {error}")

    for module in document.findall("./Modules/Module"):
        assembly = module.findtext("ModuleName") or "(unknown)"
        file_paths = {
            source_file.get("uid", ""): source_file.get("fullPath", "")
            for source_file in module.findall("./Files/File")
        }

        for class_element in module.findall("./Classes/Class"):
            class_name = class_element.findtext("FullName") or "(unknown)"
            if any(pattern.search(class_name) for pattern in exclusion_patterns):
                continue

            for method in class_element.findall("./Methods/Method"):
                raw_complexity = method.get("cyclomaticComplexity")
                if not raw_complexity:
                    continue
                try:
                    complexity = float(raw_complexity)
                except ValueError:
                    fail(
                        f"Invalid cyclomaticComplexity '{raw_complexity}' "
                        f"in '{report_path}'."
                    )

                name = method.findtext("Name") or "(unknown)"
                key = (assembly, class_name, method.get("metadataToken", ""), name)
                aggregate = method_map.setdefault(
                    key,
                    Aggregate(assembly, class_name, name, complexity),
                )
                aggregate.complexity = max(aggregate.complexity, complexity)

                for point in method.findall("./SequencePoints/SequencePoint"):
                    file_id = point.get("fileid", "")
                    source_path = file_paths.get(file_id, f"fileid:{file_id}")
                    point_key = "|".join(
                        [
                            source_path,
                            point.get("sl", ""),
                            point.get("sc", ""),
                            point.get("el", ""),
                            point.get("ec", ""),
                            point.get("offset", ""),
                        ]
                    )
                    aggregate.sequence_points.add(point_key)
                    try:
                        covered = int(point.get("vc", "0")) > 0
                    except ValueError:
                        covered = False
                    if covered:
                        aggregate.covered_points.add(point_key)

                raw_coverage = method.get("sequenceCoverage")
                if raw_coverage:
                    try:
                        fallback_coverage = float(raw_coverage) / 100.0
                    except ValueError:
                        fail(
                            f"Invalid sequenceCoverage '{raw_coverage}' "
                            f"in '{report_path}'."
                        )
                    if not aggregate.has_coverage_reading:
                        aggregate.fallback_coverage = fallback_coverage
                        aggregate.has_coverage_reading = True
                    else:
                        aggregate.fallback_coverage = max(
                            aggregate.fallback_coverage, fallback_coverage
                        )

methods: list[dict[str, object]] = []
for aggregate in method_map.values():
    if aggregate.sequence_points:
        coverage = len(aggregate.covered_points) / len(aggregate.sequence_points)
    elif aggregate.has_coverage_reading:
        coverage = aggregate.fallback_coverage
    else:
        coverage = 1.0
    coverage = min(1.0, max(0.0, coverage))
    crap = aggregate.complexity**2 * math.pow(1.0 - coverage, 3) + aggregate.complexity
    methods.append(
        {
            "assembly": aggregate.assembly,
            "class": aggregate.class_name,
            "method": aggregate.method,
            "complexity": aggregate.complexity,
            "coverage": round(coverage * 100.0, 1),
            "crap": round(crap, 1),
        }
    )

if not methods:
    fail(
        "Parsed the reports but found no methods with cyclomaticComplexity. "
        "Confirm the reports are OpenCover format."
    )

ranked = sorted(methods, key=lambda item: float(item["crap"]), reverse=True)
worst = ranked[0]
hotspots = ranked[:top]

print()
print(f"CRAP analysis over {len(methods)} methods from {len(files)} report(s)")
print(
    "Highest CRAP score: "
    f"{format_number(float(worst['crap']))}  "
    f"({worst['class']}.{worst['method']})"
)
print()
print(f"{'CRAP':>8} {'Complexity':>10} {'Cov%':>7}  Assembly / Class / Method")
for hotspot in hotspots:
    print(
        f"{format_number(float(hotspot['crap'])):>8} "
        f"{format_number(float(hotspot['complexity'])):>10} "
        f"{format_number(float(hotspot['coverage'])):>7}  "
        f"{hotspot['assembly']} / {hotspot['class']} / {hotspot['method']}"
    )

if summary_path:
    summary_lines = [
        "### CRAP risk hotspots",
        "",
        f"Highest CRAP score **{format_number(float(worst['crap']))}** across "
        f"{len(methods)} methods. CRAP = complexity squared times "
        "(1 - coverage) cubed, plus complexity.",
        "",
        "| CRAP | Complexity | Coverage | Class | Method |",
        "| ---: | ---: | ---: | --- | --- |",
    ]
    for hotspot in hotspots:
        short_class = str(hotspot["class"]).split(".")[-1]
        summary_lines.append(
            f"| {format_number(float(hotspot['crap']))} "
            f"| {format_number(float(hotspot['complexity']))} "
            f"| {format_number(float(hotspot['coverage']))}% "
            f"| {short_class} | {hotspot['method']} |"
        )
    try:
        with Path(summary_path).open("a", encoding="utf-8", newline="\n") as summary:
            summary.write("\n".join(summary_lines) + "\n")
    except OSError as error:
        fail(f"Could not append summary '{summary_path}': {error}")

if maximum_crap_score > 0 and float(worst["crap"]) > maximum_crap_score:
    fail(
        f"CRAP score {format_number(float(worst['crap']))} exceeds the maximum "
        f"of {format_number(maximum_crap_score)} "
        f"({worst['class']}.{worst['method']}). Add tests for that method or "
        "reduce its complexity."
    )
if maximum_crap_score > 0:
    print(
        "CRAP gate passed "
        f"(maximum allowed {format_number(maximum_crap_score)})."
    )
else:
    print("CRAP analysis completed (report-only mode).")
PYTHON
