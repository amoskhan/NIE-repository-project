#!/usr/bin/env python3
"""
Post-copy / post-update alignment for App Template derived repos.

Runs as a Copier _task after every `copier copy` and `copier update`. Two jobs:

  1. Initialise / refresh `.app-template-version.json` so the derived repo
     records which template version it's aligned with.
  2. Discover any tasks in `.ai/tasks/index.json` that are not yet recorded in
     `appliedTasks` and report them — non-blocking, just informational. The
     actual task application is delegated to the AI-driven .ai/ALIGN.md flow
     (which knows how to walk apply.md interactively).

Stdlib only — no dependencies.

Usage:
  python tools/template-align/align.py            # full report
  python tools/template-align/align.py --quiet    # only print non-empty actions
  python tools/template-align/align.py --json     # machine-readable output

Marker defaults (only used when a key is absent; existing values are never
overwritten). CLI beats environment:
  --timezone NAME       env APP_TEMPLATE_TIMEZONE      default "Asia/Singapore"
  --template-name NAME  env APP_TEMPLATE_NAME          default "App Template"
  --source-repo URL     env APP_TEMPLATE_SOURCE_REPO   default the public template repo
"""
from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path

# Windows consoles default to cp1252; force UTF-8 so Unicode in task titles
# (e.g. "HTML→PDF") doesn't crash the script.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

REPO_ROOT = Path.cwd()

# The version-marker filename is a contract shared by align.py, release.py and
# audit.py. If it ever changes, it changes in all three.
VERSION_MARKER_NAME = ".app-template-version.json"

VERSION_FILE = REPO_ROOT / VERSION_MARKER_NAME
TASK_INDEX = REPO_ROOT / ".ai" / "tasks" / "index.json"

# Marker defaults. These are DEFAULTS, not constants — see `configure()`.
DEFAULT_TIMEZONE = "Asia/Singapore"
DEFAULT_TEMPLATE_NAME = "App Template"
DEFAULT_SOURCE_TEMPLATE_REPO = "https://github.com/your-org/app-template.git"

TIMEZONE_NAME = DEFAULT_TIMEZONE
TEMPLATE_NAME = DEFAULT_TEMPLATE_NAME
SOURCE_TEMPLATE_REPO = DEFAULT_SOURCE_TEMPLATE_REPO


def configure(args: argparse.Namespace) -> None:
    """Resolve the marker defaults once, before anything is written."""
    global TIMEZONE_NAME, TEMPLATE_NAME, SOURCE_TEMPLATE_REPO
    TIMEZONE_NAME = (getattr(args, "timezone", None)
                     or os.environ.get("APP_TEMPLATE_TIMEZONE")
                     or DEFAULT_TIMEZONE)
    TEMPLATE_NAME = (getattr(args, "template_name", None)
                     or os.environ.get("APP_TEMPLATE_NAME")
                     or DEFAULT_TEMPLATE_NAME)
    SOURCE_TEMPLATE_REPO = (getattr(args, "source_repo", None)
                            or os.environ.get("APP_TEMPLATE_SOURCE_REPO")
                            or DEFAULT_SOURCE_TEMPLATE_REPO)


def _load_json(path: Path) -> dict | None:
    if not path.is_file():
        return None
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as e:
        print(f"ERROR: {path} is not valid JSON ({e})", file=sys.stderr)
        return None


def _save_json(path: Path, data: dict) -> None:
    # ensure_ascii=True matches the .NET serializer's `+`-style output, so
    # running align.py in the template repo doesn't churn the marker file.
    path.write_text(json.dumps(data, indent=2, ensure_ascii=True) + "\n",
                    encoding="utf-8")


def _is_template_repo() -> bool:
    """We are in the template repo (not a derived one) when the canonical
    template-only artefacts exist. Used to skip marker mutations that only
    make sense in derived repos."""
    return (REPO_ROOT / "docs" / "template-releases" / "index.json").is_file() \
        and (REPO_ROOT / ".ai" / "tasks" / "_TEMPLATE").is_dir()


def ensure_version_file() -> dict:
    """Make sure the version marker exists and has the fields align.py relies on.
    Preserves every existing field unchanged — we never strip data from the
    marker, only add defaults for missing keys.

    In the template repo (where the marker is the canonical release manifest
    and has no `appliedTasks` concept), we read but do not mutate."""
    existing = _load_json(VERSION_FILE) or {}
    if _is_template_repo():
        # In the template repo we read but never mutate. Synthesise the keys
        # align.py needs in-memory only; the file on disk stays untouched.
        view = dict(existing)
        view.setdefault("templateVersion", "unknown")
        view.setdefault("appliedTasks", [])
        return view

    record = dict(existing)  # shallow copy preserves all existing keys
    record.setdefault("templateName", TEMPLATE_NAME)
    record.setdefault("templateVersion", "unknown")
    record.setdefault("timezone", TIMEZONE_NAME)
    record.setdefault("appliedTasks", [])
    # `adoptedAtSgt` keeps its historical key name so existing markers and
    # derived-repo tooling stay readable; the value is a full ISO-8601 stamp in
    # whatever zone the project configured, so it carries its own UTC offset.
    record.setdefault("adoptedAtSgt", None)
    record.setdefault("sourceTemplateRepo", SOURCE_TEMPLATE_REPO)
    record.setdefault("localNotes", [])

    if record != existing:
        _save_json(VERSION_FILE, record)
    return record


def pending_tasks(applied: list[str]) -> list[dict]:
    """Return tasks in the index whose taskId isn't in `applied`, with appliesIf evaluated."""
    idx = _load_json(TASK_INDEX)
    if not idx:
        return []
    out = []
    for task in idx.get("tasks", []):
        tid = task["taskId"]
        if tid in applied:
            continue
        # Evaluate appliesIf if the task.json is local (it is, post-copy).
        task_path = REPO_ROOT / task["path"]
        task_json = _load_json(task_path / "task.json") or {}
        applies_if = task_json.get("appliesIf") or {}
        if not _applies(applies_if):
            continue
        out.append({
            "taskId": tid,
            "title": task.get("title"),
            "type": task.get("type"),
            "applyGuide": str(task_path / "apply.md"),
            "verifyScript": str(task_path / "verify.sh"),
            "runOnClone": task.get("runOnClone", False),
            "status": task.get("status", "released"),
        })
    return out


def _applies(applies_if: dict) -> bool:
    any_files = applies_if.get("anyFileExists") or []
    all_files = applies_if.get("allFilesExist") or []
    none_files = applies_if.get("noneFileExist") or []
    any_contains = applies_if.get("anyFileContains") or []
    all_contains = applies_if.get("allFilesContain") or []
    none_contains = applies_if.get("noneFileContains") or []

    if any_files and not any((REPO_ROOT / p).exists() for p in any_files):
        return False
    if all_files and not all((REPO_ROOT / p).exists() for p in all_files):
        return False
    if none_files and any((REPO_ROOT / p).exists() for p in none_files):
        return False
    if any_contains and not any(_content_spec_matches(s) for s in any_contains):
        return False
    if all_contains and not all(_content_spec_matches(s) for s in all_contains):
        return False
    if none_contains and any(_content_spec_matches(s) for s in none_contains):
        return False
    return True


CONTENT_SCAN_SKIP_DIRS = {
    ".git",
    ".mypy_cache",
    ".pytest_cache",
    ".turbo",
    ".vite",
    "bin",
    "coverage",
    "dist",
    "node_modules",
    "obj",
}


def _content_spec_matches(spec: dict) -> bool:
    """Return true when `pattern` appears in any file under `path`.

    Content predicates make task discovery precise for refactors where the
    trigger is a code pattern rather than a specific file.
    """
    if not isinstance(spec, dict):
        return False

    path = spec.get("path")
    pattern = spec.get("pattern")
    if not path or not pattern:
        return False

    target = REPO_ROOT / path
    if target.is_file():
        return _file_contains(target, pattern)
    if not target.is_dir():
        return False

    for root, dirs, files in os.walk(target):
        dirs[:] = [d for d in dirs if d not in CONTENT_SCAN_SKIP_DIRS]
        for filename in files:
            if _file_contains(Path(root) / filename, pattern):
                return True
    return False


def _file_contains(path: Path, pattern: str) -> bool:
    try:
        if path.stat().st_size > 2_000_000:
            return False
        return pattern in path.read_text(encoding="utf-8", errors="ignore")
    except OSError:
        return False


def prune_empty_feature_dirs() -> list[Path]:
    """Remove empty directories under common feature locations. Copier leaves
    these behind when every file in a directory matches an `_exclude` pattern.
    Returns the list of pruned paths."""
    candidates_roots = [
        REPO_ROOT / ".ai" / "features",
        REPO_ROOT / ".ai" / "tasks",
        REPO_ROOT / "src" / "backend" / "Libraries" / "Services" / "Services",
    ]
    pruned: list[Path] = []
    for root in candidates_roots:
        if not root.is_dir():
            continue
        for sub in sorted(root.iterdir()):
            if not sub.is_dir():
                continue
            # Only prune if entirely empty (no files, no subdirs)
            try:
                if not any(sub.iterdir()):
                    sub.rmdir()
                    pruned.append(sub)
            except OSError:
                pass
    return pruned


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--quiet", action="store_true",
                    help="Suppress output when there's nothing to do.")
    ap.add_argument("--json", action="store_true",
                    help="Emit a machine-readable JSON report.")
    ap.add_argument("--timezone", default=None, metavar="NAME",
                    help=f"Timezone recorded in a NEW marker "
                         f"(env APP_TEMPLATE_TIMEZONE, default {DEFAULT_TIMEZONE})")
    ap.add_argument("--template-name", default=None, metavar="NAME",
                    help=f"Template name recorded in a NEW marker "
                         f"(env APP_TEMPLATE_NAME, default {DEFAULT_TEMPLATE_NAME!r})")
    ap.add_argument("--source-repo", default=None, metavar="URL",
                    help="Upstream template repo recorded in a NEW marker "
                         "(env APP_TEMPLATE_SOURCE_REPO)")
    args = ap.parse_args()
    configure(args)

    if not TASK_INDEX.exists():
        msg = "WARNING: .ai/tasks/index.json missing — derived repo not aligned with any template release"
        if args.json:
            print(json.dumps({"status": "no-task-index", "message": msg}))
        else:
            print(f"[align] {msg}")
        return 0

    record = ensure_version_file()
    # Clean up empty directories left by Copier exclusions (only meaningful in
    # derived repos; in the template repo every directory has content).
    if not _is_template_repo():
        pruned = prune_empty_feature_dirs()
        if pruned and not args.quiet and not args.json:
            print(f"[align] pruned {len(pruned)} empty feature/task/service "
                  f"directories left by Copier exclusions")
    pending = pending_tasks(record["appliedTasks"])

    if args.json:
        print(json.dumps({
            "templateVersion": record["templateVersion"],
            "appliedTasks": record["appliedTasks"],
            "pending": pending,
        }, indent=2))
        return 0

    if not pending:
        if not args.quiet:
            print(f"[align] up to date with template {record['templateVersion']} "
                  f"({len(record['appliedTasks'])} tasks applied)")
        return 0

    print(f"[align] template version: {record['templateVersion']}")
    print(f"[align] {len(record['appliedTasks'])} tasks applied, "
          f"{len(pending)} pending:")
    for t in pending:
        flag = " [runOnClone]" if t["runOnClone"] else ""
        sflag = f" [{t['status']}]" if t["status"] != "released" else ""
        print(f"  - {t['taskId']} ({t['type']}){flag}{sflag}: {t['title']}")
        print(f"      apply:  {t['applyGuide']}")
    print()
    print("Walk these via .ai/ALIGN.md (paste into Claude/Copilot/Gemini/Kiro)")
    print("or read each apply.md manually. Each task records itself in")
    print(".app-template-version.json:appliedTasks on successful verify.sh.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
