#!/usr/bin/env python3
"""
App Template — namespace / branding rename.

This is the tool a student or a scaffold runs to turn the template into their own
project. Replaces the template placeholder strings with project-specific names:

  - `AppTemplate` -> `dotnet_root_namespace`  (the .NET namespace / solution name)
  - `App Template` -> `project_title`         (the human-readable product name)
  - File CONTENTS across every included root (see INCLUDE_ROOTS below)
  - File NAMES (e.g. AppTemplate.sln -> RoboticsPortal.sln)

Used in two ways:

  1. As a Copier `_tasks` step after `copier copy` / `copier update`.
     The task reads `dotnet_root_namespace` and `project_title` from the answers file.
  2. As a standalone CLI for repos that didn't scaffold via Copier:
       python tools/template-rename/rename.py --to RoboticsPortal --title "Robotics Portal"

Stdlib only. Idempotent — running twice with the same `--to` is a no-op.

WHAT GETS RENAMED
-----------------
Everything under INCLUDE_ROOTS plus a handful of root-level docs. That
deliberately includes `tests/`, `deploy/`, `docs/`, `.ai/`, `.github/`, `.kiro/`
and `tools/`: an earlier version of this script walked only `src/`, `build/`,
`.devcontainer/` and `.vscode/`, which left the template's name scattered through
the test suite, the deployment manifests, the docs and the agent instructions of
every rebranded project. A rebrand should be complete or it isn't a rebrand.

Including `tools/` means the governance scripts pick the new name up too — after a
rebrand `audit.py` prints "<Your Project> Audit" and `release.py` stamps the new
name into manifests. That is the intent.

DIRECTORY names are not rewritten — only file contents and file names. The one
directory that carries the template's name is `deploy/helm/app-template/`; rename
it yourself (and its `name:` in `Chart.yaml`) if you want the chart to match.

WHAT IS DELIBERATELY LEFT ALONE
-------------------------------
* `SKIP_DIRS` — VCS, dependency and build output: `.git/`, `node_modules/`,
  `bin/`, `obj/`, `dist/`, caches. Rewriting generated output is pointless and
  rewriting `.git/` is destructive.

* `Migrations/` — EF Core migrations are an immutable historical record. The
  migration id and class name are written into the `__EFMigrationsHistory` table
  of every database the project has ever provisioned, so renaming identifiers
  inside them desynchronises already-applied migrations and EF will try to
  re-run them. THE TRADE-OFF: any identifier or seeded literal inside a migration
  that embeds the template name keeps the old name. As shipped this costs
  nothing — the migrations declare `namespace Data.Migrations` and contain no
  `AppTemplate` / `App Template` occurrences at all. If you later add one and it
  bothers you, edit that single migration by hand BEFORE your first
  `dotnet ef database update`, or ship a follow-up data migration.

* `docs/template-releases/` — provenance, not branding. These manifests record
  which upstream template release the project came from; renaming them would
  claim your project published those releases. `.app-template-version.json`,
  `CHANGELOG.md` and `copier.yml` are left out of the rename for the same
  reason: they describe the template, not your project.

* This script itself — it is the mapping FROM the template placeholder, so it
  keeps referring to `AppTemplate` / `App Template` even after everything else
  has moved on.

Exit codes:
  0  changes made (or none needed)
  1  invocation error
  2  validation error (--to is invalid, etc.)
"""
from __future__ import annotations

import argparse
import os
import re
import sys
from pathlib import Path

# UTF-8 stdout on Windows
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

REPO = Path.cwd()

TEMPLATE_NAMESPACE = "AppTemplate"
TEMPLATE_TITLE = "App Template"

# Paths to walk for substitution. Anything outside these is left alone.
INCLUDE_ROOTS = [
    "src",
    "tests",
    "build",
    "deploy",
    "docs",
    "tools",
    ".ai",
    ".github",
    ".devcontainer",
    ".vscode",
    ".kiro",
    ".husky",
]
INCLUDE_FILES_AT_ROOT = [
    "README.md",
    "AGENTS.md",
    "GEMINI.md",
    "CLAUDE.md",
]

# Directory NAMES skipped anywhere inside an included root. See the module
# docstring for why Migrations is on this list.
SKIP_DIRS = {
    ".git", "node_modules", "bin", "obj", "dist",
    "__pycache__", ".venv", ".turbo", ".vite", "coverage",
    "Migrations",
}

# Repo-relative directories skipped wholesale (provenance, not branding).
SKIP_REL_DIRS = {
    "docs/template-releases",
}

# Repo-relative files skipped wholesale.
SKIP_REL_FILES = {
    # This script maps FROM the template name, so it must keep saying it.
    "tools/template-rename/rename.py",
}

# Binary or generated extensions never modified.
SKIP_SUFFIXES = {".png", ".jpg", ".jpeg", ".gif", ".ico", ".webp",
                 ".pdf", ".zip", ".gz", ".tar", ".7z",
                 ".dll", ".pdb", ".exe", ".so", ".dylib",
                 ".pem", ".pfx", ".key", ".crt", ".cer",
                 ".db", ".sqlite", ".bin",
                 ".lock", ".lockb"}

NAME_RE = re.compile(r"^[A-Z][A-Za-z0-9]{2,40}$")
TITLE_RE = re.compile(r'^[^"\\<>\r\n]{3,80}$')


def _is_valid_name(name: str) -> bool:
    return bool(NAME_RE.match(name))


def _is_valid_title(title: str) -> bool:
    return bool(TITLE_RE.match(title))


def _read(path: Path) -> str | None:
    try:
        return path.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError):
        return None


def _write(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8")


def _rel(p: Path) -> str:
    """Repo-relative POSIX path, or '' if p is outside the repo."""
    try:
        return p.relative_to(REPO).as_posix()
    except ValueError:
        return ""


def _is_skipped(p: Path) -> bool:
    """Decide whether a path is excluded from the rename.

    Every test runs against the REPO-RELATIVE path. Matching on absolute path
    parts would mis-skip a checkout that happens to live under a directory
    called `dist`, `bin` or `coverage`."""
    rel = _rel(p)
    if not rel:
        return True
    parts = rel.split("/")
    if any(part in SKIP_DIRS for part in parts):
        return True
    if rel in SKIP_REL_FILES:
        return True
    if any(rel == d or rel.startswith(d + "/") for d in SKIP_REL_DIRS):
        return True
    return p.suffix.lower() in SKIP_SUFFIXES


def _walk(roots: list[Path]) -> list[Path]:
    """Collect every renameable file under `roots`.

    Uses os.walk so skipped directories are PRUNED rather than merely filtered —
    without that, a `node_modules/` tree gets fully enumerated on every run."""
    out: list[Path] = []
    for root in roots:
        if not root.is_dir():
            continue
        for dirpath, dirnames, filenames in os.walk(root):
            here = Path(dirpath)
            dirnames[:] = [d for d in dirnames if not _is_skipped(here / d)]
            for name in filenames:
                p = here / name
                if not _is_skipped(p):
                    out.append(p)
    return out


def _read_copier_answers() -> dict[str, str]:
    """Return the flat Copier answers used by the rename task."""
    answers = REPO / ".copier-answers.yml"
    if not answers.is_file():
        return {}
    out: dict[str, str] = {}
    for line in answers.read_text(encoding="utf-8").splitlines():
        if ":" not in line or line.lstrip().startswith("#"):
            continue
        key, raw_value = line.split(":", 1)
        key = key.strip()
        if key not in {"dotnet_root_namespace", "project_title"}:
            continue
        value = raw_value.strip().strip("'\"")
        if value:
            out[key] = value
    return out


def replace_contents(target_files: list[Path], from_name: str, to_name: str,
                     dry_run: bool) -> tuple[int, int]:
    """Substitute from_name -> to_name in file contents.
    Returns (files_modified, total_replacements)."""
    files_modified = 0
    total_replacements = 0
    for f in target_files:
        text = _read(f)
        if text is None or from_name not in text:
            continue
        replacements = text.count(from_name)
        new_text = text.replace(from_name, to_name)
        if not dry_run:
            _write(f, new_text)
        files_modified += 1
        total_replacements += replacements
    return files_modified, total_replacements


def rename_files(target_files: list[Path], from_name: str, to_name: str,
                 dry_run: bool) -> int:
    """Rename files whose basename contains from_name.
    Returns the count of renamed files."""
    renamed = 0
    for f in target_files:
        if from_name not in f.name:
            continue
        new_name = f.name.replace(from_name, to_name)
        new_path = f.parent / new_name
        if new_path.exists():
            print(f"WARN: target {new_path.relative_to(REPO)} exists; skipping",
                  file=sys.stderr)
            continue
        if not dry_run:
            f.rename(new_path)
        print(f"  renamed: {f.relative_to(REPO)} -> {new_name}")
        renamed += 1
    return renamed


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--to", default=None,
                    help="Target namespace (e.g. RoboticsPortal). If omitted, reads "
                         "dotnet_root_namespace from .copier-answers.yml.")
    ap.add_argument("--title", default=None,
                    help="Display title (e.g. \"Robotics Portal\"). If omitted, reads "
                         "project_title from .copier-answers.yml.")
    ap.add_argument("--from", dest="from_name", default=TEMPLATE_NAMESPACE,
                    help=f"Source placeholder namespace (default {TEMPLATE_NAMESPACE}).")
    ap.add_argument("--from-title", dest="from_title", default=TEMPLATE_TITLE,
                    help=f"Source placeholder title (default {TEMPLATE_TITLE!r}).")
    ap.add_argument("--dry-run", action="store_true",
                    help="Show what would change without modifying anything.")
    ap.add_argument("--quiet", action="store_true",
                    help="Suppress info output.")
    args = ap.parse_args(argv)

    copier_answers = _read_copier_answers()
    to_name = args.to or copier_answers.get("dotnet_root_namespace")
    to_title = args.title or copier_answers.get("project_title")
    if not to_name:
        if not args.quiet:
            print("[rename] no --to given and .copier-answers.yml has no "
                  "dotnet_root_namespace; nothing to do.")
        return 0

    rename_namespace = to_name != args.from_name
    rename_title = bool(to_title) and to_title != args.from_title
    if not rename_namespace and not rename_title:
        if not args.quiet:
            print(f"[rename] target name '{to_name}' equals source and the title "
                  f"is unchanged — no-op.")
        return 0

    if not _is_valid_name(to_name):
        print(f"ERROR: target name '{to_name}' must match {NAME_RE.pattern}",
              file=sys.stderr)
        return 2

    if to_title and not _is_valid_title(to_title):
        print(f"ERROR: display title '{to_title}' must match {TITLE_RE.pattern}",
              file=sys.stderr)
        return 2

    # Build the file list
    roots = [REPO / r for r in INCLUDE_ROOTS]
    target_files = _walk(roots)
    for fname in INCLUDE_FILES_AT_ROOT:
        f = REPO / fname
        if f.is_file():
            target_files.append(f)

    if not args.quiet:
        plan = []
        if rename_namespace:
            plan.append(f"{args.from_name} -> {to_name}")
        if rename_title:
            plan.append(f"{args.from_title!r} -> {to_title!r}")
        print(f"[rename] {'; '.join(plan)} (scanning {len(target_files)} files)")

    files_modified = 0
    replacements = 0
    if rename_namespace:
        files_modified, replacements = replace_contents(
            target_files, args.from_name, to_name, args.dry_run)
    if rename_title:
        title_files_modified, title_replacements = replace_contents(
            target_files, args.from_title, to_title, args.dry_run)
        files_modified += title_files_modified
        replacements += title_replacements

    renamed = 0
    if rename_namespace:
        # Re-walk after content edits because filenames change too
        target_files = _walk(roots)
        for fname in INCLUDE_FILES_AT_ROOT:
            f = REPO / fname
            if f.is_file():
                target_files.append(f)
        renamed = rename_files(target_files, args.from_name, to_name, args.dry_run)

    if args.quiet:
        return 0

    verb = "would modify" if args.dry_run else "modified"
    print(f"[rename] {verb} {files_modified} file(s) "
          f"({replacements} replacement(s)); {verb} {renamed} filename(s)")
    if args.dry_run:
        print("[rename] dry run — no changes written")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
