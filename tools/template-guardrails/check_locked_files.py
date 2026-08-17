#!/usr/bin/env python3
"""
App Template — locked-file guardrail.

Fails when a change set touches a TEMPLATE-OWNED (locked) file WITHOUT an accompanying
new template task (`.ai/tasks/NNNN-*/task.json`) in the same change set. The locked set
mirrors `.ai/common/11-customization-boundary.md`: the staff shell, the router/permission
machinery, `@apptemplate/ui` + `@apptemplate/shared`, and the backend base classes /
middleware / authorization attributes / DbContext / Mapster / Program /
AccessFunctionCatalog.

Feature work must put project data in `src/frontend/main/src/app-config/*` (frontend) or
its own feature files (backend) — never in these locked files. A genuine change to a
locked file is allowed only when it ships as a template task (so it gets a version bump,
a CHANGELOG entry, and a derived-repo upgrade path).

Modes:
  --staged          diff the staged index (`git diff --cached`) — for the Husky pre-commit hook
  --base <ref>      diff `<ref>...HEAD` — for CI (e.g. `--base origin/main`)
  <files...>        explicit file list — for tests / manual checks
  --verify-paths    self-check: confirm every LOCKED_EXACT path still exists on disk.
                    Run this after a refactor that moves files, so the list cannot
                    silently rot into a set of paths that guard nothing.
  (default)         --staged

Exit codes:
  0 — no locked files touched, OR a new template task accompanies the change, OR not a git repo
  1 — locked files touched with no accompanying template task (or --verify-paths found a stale entry)
  2 — invocation error
"""
from __future__ import annotations

import argparse
import fnmatch
import re
import subprocess
import sys
from pathlib import Path

# Windows consoles default to a legacy codepage; force UTF-8 so the failure
# message renders correctly inside the Husky pre-commit hook.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

# Exact locked files. Keep in sync with .ai/common/11-customization-boundary.md.
# Every entry is verified to exist by `--verify-paths`.
LOCKED_EXACT = {
    # Frontend shell
    "src/frontend/main/src/staff/layouts/StaffLayout.vue",
    "src/frontend/main/src/composables/useSidebar.ts",
    "src/frontend/main/src/composables/usePermissions.ts",
    "src/frontend/main/src/composables/navTypes.ts",
    "src/frontend/main/src/router/index.ts",
    "src/frontend/main/src/constants/permissions.ts",
    # Backend infrastructure
    "src/backend/Libraries/Domain/Models/BaseEntity.cs",
    "src/backend/Libraries/Domain/Models/TimestampedEntity.cs",
    "src/backend/Libraries/Shared/Models/IOwnedEntity.cs",
    "src/backend/Libraries/Services/Services/Base/BaseService.cs",
    "src/backend/Libraries/Services/Services/Base/IBaseService.cs",
    "src/backend/API/Controllers/BaseController.cs",
    "src/backend/Libraries/Data/Data/MainDbContext.cs",
    "src/backend/API/Mapping/MappingProfile.cs",
    "src/backend/API/Program.cs",
    "src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs",
}

# Locked path prefixes (glob form; git reports forward-slash paths on all OSes).
# fnmatch's `*` also matches `/`, so each pattern covers nested directories.
LOCKED_GLOBS = (
    "src/frontend/main/src/components/common/*",
    "src/frontend/packages/ui/src/*",
    "src/frontend/packages/shared/src/*",
    "src/backend/API/Middleware/*",
    "src/backend/API/Authorization/*",
)

NEW_TASK_RE = re.compile(r"^\.ai/tasks/\d{4}-[^/]+/task\.json$")


def _git(args: list[str]) -> list[str] | None:
    try:
        out = subprocess.check_output(["git", *args], text=True,
                                      stderr=subprocess.DEVNULL)
    except (subprocess.CalledProcessError, FileNotFoundError):
        return None
    return [line.strip() for line in out.splitlines() if line.strip()]


def _is_locked(path: str) -> bool:
    return path in LOCKED_EXACT or any(fnmatch.fnmatch(path, g) for g in LOCKED_GLOBS)


def _repo_root() -> Path:
    """Repo root via git, falling back to the current working directory."""
    out = _git(["rev-parse", "--show-toplevel"])
    if out:
        return Path(out[0])
    return Path.cwd()


def verify_paths() -> int:
    """Confirm the locked list still describes files that exist.

    A locked entry pointing at a moved or deleted file guards nothing, and the
    failure is silent — the guardrail just keeps passing. This turns that into a
    loud, fixable error."""
    root = _repo_root()
    missing = sorted(p for p in LOCKED_EXACT if not (root / p).is_file())
    empty_globs = sorted(
        g for g in LOCKED_GLOBS
        if not any((root / g.rstrip("*").rstrip("/")).glob("**/*"))
    )

    if not missing and not empty_globs:
        print(f"guardrail: OK ({len(LOCKED_EXACT)} locked paths and "
              f"{len(LOCKED_GLOBS)} locked globs all resolve under {root})")
        return 0

    print("guardrail: FAIL — the locked list has stale entries:")
    for p in missing:
        print(f"  - missing file: {p}")
    for g in empty_globs:
        print(f"  - glob matches nothing: {g}")
    print()
    print("Update LOCKED_EXACT / LOCKED_GLOBS in this file and the table in")
    print(".ai/common/11-customization-boundary.md so both describe the same set.")
    return 1


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--staged", action="store_true",
                    help="diff the staged index (pre-commit)")
    ap.add_argument("--base", help="diff <ref>...HEAD (CI), e.g. origin/main")
    ap.add_argument("--verify-paths", action="store_true",
                    help="check that every locked path still exists, then exit")
    ap.add_argument("files", nargs="*", help="explicit changed-file list (tests/manual)")
    args = ap.parse_args(argv)

    if args.verify_paths:
        return verify_paths()

    if args.files:
        changed = list(args.files)
        added = list(args.files)
    elif args.base:
        changed = _git(["diff", "--name-only", f"{args.base}...HEAD"])
        added = _git(["diff", "--name-only", "--diff-filter=A", f"{args.base}...HEAD"]) or []
    else:  # default: staged (Husky pre-commit)
        changed = _git(["diff", "--cached", "--name-only"])
        added = _git(["diff", "--cached", "--name-only", "--diff-filter=A"]) or []

    if changed is None:
        print("guardrail: SKIP (not a git repo / git unavailable)", file=sys.stderr)
        return 0

    locked_touched = sorted(p for p in changed if _is_locked(p))
    if not locked_touched:
        print("guardrail: OK (no locked template files touched)")
        return 0

    if any(NEW_TASK_RE.match(p) for p in (added or changed)):
        print(f"guardrail: OK ({len(locked_touched)} locked file(s) touched, "
              f"accompanied by a new template task)")
        return 0

    print("guardrail: FAIL — locked template-owned files changed without a template task:")
    for p in locked_touched:
        print(f"  - {p}")
    print()
    print("These files are TEMPLATE-OWNED (see .ai/common/11-customization-boundary.md).")
    print("Feature work must use src/frontend/main/src/app-config/* (frontend) or your own")
    print("feature files (backend) — not the shell or base classes. If this is a genuine")
    print("template change, ship it as a task under .ai/tasks/NNNN-*/ (with task.json) in")
    print("the same change set, per .ai/common/09-template-versioning.md.")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
