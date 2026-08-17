#!/usr/bin/env python3
"""
App Template — compliance audit.

Stdlib-only. Checks that a template or derived repo still holds its shape.

Six check categories:
  - structure   .ai/ folders + CLAUDE.md presence
  - metadata    version-marker validity, CHANGELOG sync
  - features    every feature dossier has README.md, files.md, do-dont.md
  - code        regex code smells (hardcoded enums, unvalidated API calls)
  - security    security middleware, SSRF guard, ownership marker, paging cap,
                and [Authorize] coverage on controllers
  - drift       locked frontend shell files hold no project data; app-config layer intact
                (derived repos only; skipped in the template repo)

Usage:
  python tools/template-audit/audit.py                # run all checks
  python tools/template-audit/audit.py --strict       # exit 1 on any non-critical finding too
  python tools/template-audit/audit.py --json         # machine-readable
  python tools/template-audit/audit.py --list         # explain every rule and exit
  python tools/template-audit/audit.py --repo /path   # audit another repo

Configuration (CLI beats environment):
  --template-name NAME  Product name used in the banner.
                        env APP_TEMPLATE_NAME. Default "App Template".
  --brand LABEL         Brand literal the locked shell must NOT hardcode.
                        env APP_TEMPLATE_BRAND. Defaults to the template name;
                        the project's own `brandLabel` from theme/appTheme.ts is
                        detected and checked as well.
  --timezone NAME       Timezone the marker is expected to declare.
                        env APP_TEMPLATE_TIMEZONE. Default "Asia/Singapore".
                        Regional settings are project configuration, so a
                        mismatch is a warning, never a critical failure.

Exit codes:
  0 — no critical findings (or only warnings)
  1 — at least one critical check failed
  2 — invocation error
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
from dataclasses import dataclass, field, asdict
from pathlib import Path

# UTF-8 stdout on Windows consoles
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

# The version-marker filename is a contract shared by audit.py, align.py and
# release.py. If it ever changes, it changes in all three.
VERSION_MARKER_NAME = ".app-template-version.json"

# Configurable settings. These are DEFAULTS, not constants — see `resolve_*` below.
DEFAULT_TEMPLATE_NAME = "App Template"
DEFAULT_TIMEZONE = "Asia/Singapore"

# ---------------------------------------------------------------------------
# Result types
# ---------------------------------------------------------------------------

@dataclass
class Check:
    name: str
    category: str
    passed: bool
    critical: bool = False
    message: str | None = None
    remediation: list[str] = field(default_factory=list)


@dataclass
class Report:
    repo_path: str
    template_version: str
    checks: list[Check] = field(default_factory=list)

    def add(self, c: Check) -> None:
        self.checks.append(c)

    @property
    def passed(self) -> int:
        return sum(1 for c in self.checks if c.passed)

    @property
    def total(self) -> int:
        return len(self.checks)

    @property
    def has_critical(self) -> bool:
        return any(not c.passed and c.critical for c in self.checks)

    @property
    def has_any_failure(self) -> bool:
        return any(not c.passed for c in self.checks)


# ---------------------------------------------------------------------------
# The auditor
# ---------------------------------------------------------------------------

class Auditor:
    REQUIRED_FEATURE_FILES = ("README.md", "files.md", "do-dont.md")

    def __init__(self, repo_root: Path, use_ast: bool = False,
                 template_name: str = DEFAULT_TEMPLATE_NAME,
                 brand: str | None = None,
                 timezone: str = DEFAULT_TIMEZONE):
        self.root = repo_root.resolve()
        self.use_ast = use_ast
        self.template_name = template_name
        self.brand = brand or template_name
        self.timezone = timezone
        self._ast = None
        if use_ast:
            try:
                # Local import — only loaded when --ast is passed.
                import sys as _sys
                _sys.path.insert(0, str(Path(__file__).resolve().parent))
                import ast_check  # type: ignore
                self._ast = ast_check
            except ImportError as e:
                print(f"WARN: --ast requested but tree-sitter libs missing "
                      f"({e}); falling back to regex checks", file=sys.stderr)
                self.use_ast = False

    def run(self) -> Report:
        version = self._read_template_version()
        report = Report(repo_path=str(self.root), template_version=version)
        self._check_structure(report)
        self._check_metadata(report)
        self._check_features(report)
        self._check_code_quality(report)
        self._check_security(report)
        self._check_drift(report)
        if self.use_ast:
            self._check_ast(report)
        return report

    # -- helpers ------------------------------------------------------------

    def _read_template_version(self) -> str:
        f = self.root / VERSION_MARKER_NAME
        if not f.is_file():
            return "unknown"
        try:
            return json.loads(f.read_text(encoding="utf-8")).get("templateVersion", "unknown")
        except json.JSONDecodeError:
            return "invalid-json"

    def _first_existing(self, *candidates: str) -> Path | None:
        """Return the first repo-relative candidate that exists.

        Backend layout has shifted between template generations (Domain/ vs
        Shared/, Services/ vs Services/Base/), so security artefacts are located
        by trying every known home rather than a single frozen path."""
        for rel in candidates:
            p = self.root / rel
            if p.is_file():
                return p
        return None

    def _add(self, report: Report, name: str, category: str, passed: bool,
             critical: bool = False, message: str | None = None,
             remediation: list[str] | None = None) -> None:
        report.add(Check(
            name=name, category=category, passed=passed, critical=critical,
            message=message, remediation=remediation or []
        ))

    # -- structure ----------------------------------------------------------

    def _check_structure(self, r: Report) -> None:
        for sub, critical in [
            (".ai",                 True),
            (".ai/common",          True),
            (".ai/features",        True),
            (".ai/tasks",           True),
            (".ai/tool-routes",     False),
        ]:
            self._add(r, f"{sub}/ folder exists", "structure",
                      (self.root / sub).is_dir(), critical=critical,
                      remediation=[f"Restore {sub}/ from the template repo"] if not (self.root / sub).is_dir() else None)

        self._add(r, "CLAUDE.md exists at repo root", "structure",
                  (self.root / "CLAUDE.md").is_file(), critical=False,
                  remediation=["Copy CLAUDE.md from the template — points AI agents at .ai/"])

    # -- metadata -----------------------------------------------------------

    def _check_metadata(self, r: Report) -> None:
        version_file = self.root / VERSION_MARKER_NAME
        version_exists = version_file.is_file()
        self._add(r, f"{VERSION_MARKER_NAME} exists", "metadata", version_exists,
                  critical=True,
                  remediation=[f"Restore {VERSION_MARKER_NAME} from the template "
                               f"release metadata, or run template-align from the "
                               f"derived repo root"])

        if version_exists:
            try:
                data = json.loads(version_file.read_text(encoding="utf-8"))
            except json.JSONDecodeError as e:
                self._add(r, "version file shape valid", "metadata", False,
                          critical=True, message=f"JSON parse error: {e}")
                data = None

            if data is not None:
                # CRITICAL: the version is what every alignment decision hangs off.
                has_version = bool(data.get("templateVersion"))
                self._add(r, "version file shape valid", "metadata", has_version,
                          critical=True,
                          message=None if has_version else "marker has no templateVersion",
                          remediation=["Set `templateVersion` in the marker to the "
                                       "template release this repo is aligned with"]
                                      if not has_version else None)

                # WARNING ONLY: timezone is regional CONFIGURATION, not compliance.
                # The template ships Asia/Singapore as a default; a project that
                # runs elsewhere changes it and is still perfectly compliant. We
                # only care that the field is present and matches what this audit
                # run was told to expect.
                declared_tz = data.get("timezone")
                if not declared_tz:
                    self._add(r, "version file declares a timezone", "metadata",
                              False, critical=False,
                              message="marker has no `timezone` field",
                              remediation=[f'Add "timezone": "{self.timezone}" to '
                                           f'{VERSION_MARKER_NAME} (template default '
                                           f'is {DEFAULT_TIMEZONE})'])
                else:
                    matches = declared_tz == self.timezone
                    self._add(r, "version file declares a timezone", "metadata",
                              matches, critical=False,
                              message=None if matches else
                                      f"marker timezone {declared_tz!r} differs from the "
                                      f"expected {self.timezone!r}",
                              remediation=[f"Either set the marker to {self.timezone!r} or "
                                           f"re-run the audit with --timezone {declared_tz} "
                                           f"(env APP_TEMPLATE_TIMEZONE)"]
                                          if not matches else None)

        changelog = self.root / "CHANGELOG.md"
        self._add(r, "CHANGELOG.md exists", "metadata", changelog.is_file(),
                  critical=False)

        if changelog.is_file() and version_exists:
            try:
                ver = json.loads(version_file.read_text(encoding="utf-8")).get("templateVersion")
                if ver:
                    text = changelog.read_text(encoding="utf-8")
                    has_entry = f"## [{ver}]" in text or f"## {ver}" in text
                    self._add(r, "CHANGELOG.md mentions current templateVersion",
                              "metadata", has_entry, critical=False,
                              remediation=[f"Add a `## [{ver}]` section to CHANGELOG.md"]
                                          if not has_entry else None)
            except json.JSONDecodeError:
                pass

    # -- features -----------------------------------------------------------

    def _check_features(self, r: Report) -> None:
        feat_dir = self.root / ".ai" / "features"
        if not feat_dir.is_dir():
            return
        for d in sorted(feat_dir.iterdir()):
            if not d.is_dir() or d.name.startswith("_"):
                continue
            for fname in self.REQUIRED_FEATURE_FILES:
                exists = (d / fname).is_file()
                self._add(r, f"feature '{d.name}' has {fname}", "features",
                          exists, critical=False,
                          remediation=[f"Create .ai/features/{d.name}/{fname} per the dossier template"]
                                      if not exists else None)

    # -- code quality -------------------------------------------------------

    HARDCODED_ENUM_PATTERNS = [
        re.compile(r'==\s*"[A-Z][a-zA-Z]+"'),                    # status == "Approved"
        re.compile(r"case\s+\"[A-Z][a-zA-Z]+\"\s*:"),            # case "Approved":
    ]
    UNVALIDATED_API_RE = re.compile(
        r"(fetch|\.get)\([^)]*\)[^;\n]*\.json\(\)", re.DOTALL)

    def _check_code_quality(self, r: Report) -> None:
        src = self.root / "src"
        if not src.is_dir():
            return

        # Hardcoded enum strings in C#
        cs_files = list(src.rglob("*.cs"))
        # Skip generated migration/snapshot files — they're not author code
        cs_files = [f for f in cs_files if "Migrations" not in f.parts and "obj" not in f.parts]
        hardcoded = 0
        for f in cs_files:
            try:
                text = f.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            for pat in self.HARDCODED_ENUM_PATTERNS:
                hardcoded += len(pat.findall(text))
        self._add(r, "no hardcoded enum strings in backend (heuristic)",
                  "code", hardcoded == 0, critical=False,
                  message=f"{hardcoded} matches" if hardcoded else None,
                  remediation=["Replace with Domain.Enum.E* values"]
                              if hardcoded else None)

        # Unvalidated API calls in TS — sample-only heuristic
        ts_files = list(src.rglob("*.ts"))
        ts_files = [f for f in ts_files if "node_modules" not in f.parts and "dist" not in f.parts]
        unvalidated = 0
        for f in ts_files:
            try:
                text = f.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            for m in self.UNVALIDATED_API_RE.finditer(text):
                section = m.group(0)
                if not any(tok in section for tok in (" as ", ": typeof ", "zod", "guard", "parse(")):
                    unvalidated += 1
        threshold = max(1, int(len(ts_files) * 0.1))
        passed = unvalidated < threshold
        self._add(r, "API responses are validated (heuristic)", "code", passed,
                  critical=False,
                  message=f"{unvalidated} suspicious calls in {len(ts_files)} TS files (threshold {threshold})"
                          if not passed else None,
                  remediation=["Add type guards / zod schemas around fetch().json() results"]
                              if not passed else None)

    # -- security -----------------------------------------------------------

    # Security artefacts the template ships, each with every path it has lived at.
    # Paths are repo-relative. Deleted features are NOT listed here — this table
    # only covers artefacts that are still part of the template.
    SECURITY_ARTEFACTS = (
        (
            "SecurityHeadersMiddleware exists",
            ("src/backend/API/Middleware/SecurityHeadersMiddleware.cs",),
            "Restore src/backend/API/Middleware/SecurityHeadersMiddleware.cs "
            "and re-register it in Program.cs",
        ),
        (
            "SsrfGuard exists",
            ("src/backend/Libraries/Shared/Helpers/SsrfGuard.cs",
             "src/backend/Libraries/Domain/Helpers/SsrfGuard.cs"),
            "Restore the SSRF allowlist helper — every outbound HTTP call from "
            "the backend must go through it",
        ),
        (
            "IOwnedEntity ownership marker exists",
            ("src/backend/Libraries/Shared/Models/IOwnedEntity.cs",
             "src/backend/Libraries/Domain/Models/IOwnedEntity.cs"),
            "Restore IOwnedEntity — it is what RequireOwnershipAttribute uses to "
            "block broken-object-level-authorization (BOLA) access",
        ),
        (
            "PagedSearchDto page-size cap exists",
            ("src/backend/Libraries/Shared/Dto/PagedSearchDto.cs",
             "src/backend/Libraries/Domain/Dto/PagedSearchDto.cs"),
            "Restore PagedSearchDto — it clamps pageSize so a caller cannot ask "
            "for the whole table",
        ),
    )

    def _check_security(self, r: Report) -> None:
        backend = self.root / "src" / "backend"
        if not backend.is_dir():
            return

        for name, candidates, fix in self.SECURITY_ARTEFACTS:
            found = self._first_existing(*candidates)
            self._add(r, name, "security", found is not None, critical=False,
                      message=None if found else f"not found at {candidates[0]}",
                      remediation=[fix] if found is None else None)

        # [Authorize] / [RequireAccessFunction] on controllers — improved over the
        # dotnet version: also recognise class-level attribute application and
        # intentional public entry points.
        cs_controllers = [
            f for f in backend.rglob("*Controller.cs")
            if "Migrations" not in f.parts and "obj" not in f.parts
        ]
        unguarded: list[str] = []
        for f in cs_controllers:
            try:
                text = f.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            if re.search(r"\babstract\s+class\s+\w*Controller\b", text):
                continue
            # accept any of: [Authorize], [Authorize(...)], [RequireAccessFunction(...)],
            # [AllowAnonymous] for endpoints that intentionally start authentication.
            if not re.search(r"\[(Authorize|RequireAccessFunction|AllowAnonymous)\b", text):
                unguarded.append(str(f.relative_to(self.root)))

        passed = not unguarded
        self._add(r, "all concrete controllers are guarded or explicitly public",
                  "security", passed, critical=False,
                  message=None if passed else f"{len(unguarded)} unguarded controller(s)",
                  remediation=[f"Add attribute to {p}" for p in unguarded[:5]]
                              if not passed else None)

    # -- drift (locked-vs-project boundary) ---------------------------------

    def _is_template_repo(self) -> bool:
        """We are in the template repo (not a derived one) when the canonical
        template-only artefacts exist. In the template the shell IS the source of
        truth, so drift is meaningless and is skipped. Mirrors the same test used by
        tools/template-align/align.py."""
        return (self.root / "docs" / "template-releases" / "index.json").is_file() \
            and (self.root / ".ai" / "tasks" / "_TEMPLATE").is_dir()

    BRAND_LABEL_RE = re.compile(r"""brandLabel\s*:\s*["']([^"']+)["']""")

    def _detect_brand_label(self, frontend_src: Path) -> str | None:
        """Read `brandLabel` out of the project-owned theme config, if present.

        theme/appTheme.ts is PROJECT-owned: it is the sanctioned home for the
        product name. Knowing its value lets the drift check stay meaningful
        after a project has rebranded away from the template's name."""
        theme = frontend_src / "theme" / "appTheme.ts"
        if not theme.is_file():
            return None
        try:
            m = self.BRAND_LABEL_RE.search(theme.read_text(encoding="utf-8", errors="ignore"))
        except OSError:
            return None
        return m.group(1) if m else None

    def _check_drift(self, r: Report) -> None:
        """Detect project data leaked into LOCKED frontend shell files, plus broken
        app-config wiring (see .ai/common/11-customization-boundary.md).

        Each check is pinned to a SINGLE named locked file — never a repo-wide scan —
        because the project-owned app-config/* twins legitimately contain the same
        tokens (AccessFunctionCode, PRIMARY_NAV_ITEMS, a .svg import). Skipped entirely
        in the template repo, and any locked file that is absent is skipped (absence is
        not drift)."""
        if self._is_template_repo():
            return

        fe = self.root / "src" / "frontend" / "main" / "src"
        if not fe.is_dir():
            return

        boundary = ".ai/common/11-customization-boundary.md"

        def _read(rel: str) -> str | None:
            f = fe / rel
            if not f.is_file():
                return None
            try:
                return f.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                return None

        # D1 — nav arrays belong in app-config/navigation.ts, not the locked composable.
        use_perms = _read("composables/usePermissions.ts")
        if use_perms is not None:
            leaked = bool(re.search(r"const\s+(PRIMARY_NAV_ITEMS|ADMIN_NAV_ITEMS)\b", use_perms))
            self._add(r, "usePermissions.ts defines no nav arrays", "drift",
                      not leaked, critical=True,
                      message="a nav array is defined in the locked composable" if leaked else None,
                      remediation=[f"Move PRIMARY_NAV_ITEMS/ADMIN_NAV_ITEMS to src/frontend/main/src/app-config/navigation.ts — see {boundary}"]
                                  if leaked else None)

        # D2 — access codes belong in app-config/accessFunctions.ts.
        perms = _read("constants/permissions.ts")
        if perms is not None:
            leaked = "AccessFunctionCode" in perms
            self._add(r, "constants/permissions.ts holds no access codes", "drift",
                      not leaked, critical=True,
                      message="AccessFunctionCode found in the locked resolver" if leaked else None,
                      remediation=[f"Move access codes to src/frontend/main/src/app-config/accessFunctions.ts — see {boundary}"]
                                  if leaked else None)

        # D3/D4/D5 — StaffLayout.vue must carry no brand literal, feedback namespace, or asset import.
        layout = _read("staff/layouts/StaffLayout.vue")
        if layout is not None:
            # The brand literal to look for is configurable, because after a
            # rebrand the shell would be hardcoding the PROJECT's name rather
            # than the template's. Check the configured brand plus whatever
            # `brandLabel` the project's own theme declares.
            brands = {self.brand}
            project_brand = self._detect_brand_label(fe)
            if project_brand:
                brands.add(project_brand)
            found_brands = sorted(b for b in brands if b and b in layout)
            self._add(r, "StaffLayout.vue holds no hardcoded brand name", "drift",
                      not found_brands, critical=False,
                      message=(f"brand literal(s) {found_brands} in the shell"
                               if found_brands else None),
                      remediation=["Use useTheme().brandLabel; set the product name "
                                   "in src/frontend/main/src/theme/appTheme.ts"]
                                  if found_brands else None)

            has_ns = bool(re.search(r"procurement\.", layout))
            self._add(r, "StaffLayout.vue holds no hardcoded feedback namespace", "drift",
                      not has_ns, critical=False,
                      message="literal 'procurement.' in the shell" if has_ns else None,
                      remediation=["Use FEEDBACK_FUNCTION_PREFIX from app-config/branding.ts"]
                                  if has_ns else None)

            has_asset = bool(re.search(r"""import\s+\w+\s+from\s+["'][^"']*\.svg["']""", layout))
            self._add(r, "StaffLayout.vue imports no logo asset directly", "drift",
                      not has_asset, critical=True,
                      message="a direct .svg import in the shell" if has_asset else None,
                      remediation=[f"Consume BRAND_LOGO from app-config/branding.ts instead — see {boundary}"]
                                  if has_asset else None)

        # D6 — every app-config/* module the shell imports must exist.
        shell_sources = [s for s in (use_perms, perms, layout, _read("router/index.ts")) if s]
        joined = "\n".join(shell_sources)
        for name in ("navigation", "routes", "accessFunctions", "branding"):
            if f"@/app-config/{name}" not in joined:
                continue
            exists = (fe / "app-config" / f"{name}.ts").is_file()
            self._add(r, f"app-config/{name}.ts exists (imported by the shell)", "drift",
                      exists, critical=True,
                      message=f"the shell imports @/app-config/{name} but the file is missing" if not exists else None,
                      remediation=[f"Restore src/frontend/main/src/app-config/{name}.ts"]
                                  if not exists else None)

    # -- AST (opt-in via --ast) ---------------------------------------------

    def _check_ast(self, r: Report) -> None:
        """Run tree-sitter-based checks. Each finding becomes a Check entry
        in the matching category; passes/totals reflect file-level aggregates."""
        if self._ast is None:
            return

        backend = self.root / "src" / "backend"
        frontend = self.root / "src" / "frontend"

        # ---- C# checks
        if backend.is_dir():
            cs_files = [f for f in backend.rglob("*.cs")
                        if "Migrations" not in f.parts and "obj" not in f.parts]
            ast_findings = []
            for f in cs_files:
                ast_findings.extend(self._ast.run_csharp_checks(f))

            # Group findings by rule
            by_rule: dict[str, list] = {}
            for fi in ast_findings:
                by_rule.setdefault(fi.rule, []).append(fi)

            # Authorize coverage — one aggregate check
            authz = by_rule.get("cs/missing-authorize", [])
            self._add(r,
                      "AST: every public controller method carries [Authorize] "
                      "/[RequireAccessFunction]/[AllowAnonymous]",
                      "security", not authz, critical=False,
                      message=(f"{len(authz)} method(s) without an auth attribute"
                               if authz else None),
                      remediation=[
                          f"{fi.file.replace(str(self.root) + chr(92), '')}:{fi.line} — "
                          f"{fi.message}" for fi in authz[:8]
                      ] if authz else None)

            # Pagination cap heuristic
            takes = by_rule.get("cs/unbounded-take", [])
            self._add(r,
                      "AST: no `.Take(N)` calls with N>100 (use PagedSearchDto)",
                      "security", not takes, critical=False,
                      message=(f"{len(takes)} suspicious .Take call(s)"
                               if takes else None),
                      remediation=[fi.message for fi in takes[:5]] if takes else None)

        # ---- TS checks
        if frontend.is_dir():
            ts_files = [f for f in frontend.rglob("*.ts")
                        if "node_modules" not in f.parts and "dist" not in f.parts]
            ast_findings = []
            for f in ts_files:
                ast_findings.extend(self._ast.run_typescript_checks(f))

            by_rule: dict[str, list] = {}
            for fi in ast_findings:
                by_rule.setdefault(fi.rule, []).append(fi)

            as_any = by_rule.get("ts/as-any", [])
            self._add(r, "AST: no `as any` casts in TS source",
                      "code", not as_any, critical=False,
                      message=f"{len(as_any)} `as any` cast(s)" if as_any else None,
                      remediation=[
                          f"{fi.file.replace(str(self.root) + chr(92), '')}:{fi.line}"
                          for fi in as_any[:8]
                      ] if as_any else None)

            unguarded = by_rule.get("ts/unvalidated-json", [])
            self._add(r, "AST: `.json()` calls have a parse / type guard nearby",
                      "code", not unguarded, critical=False,
                      message=(f"{len(unguarded)} suspicious .json() call(s)"
                               if unguarded else None),
                      remediation=[
                          f"{fi.file.replace(str(self.root) + chr(92), '')}:{fi.line}"
                          for fi in unguarded[:8]
                      ] if unguarded else None)


# ---------------------------------------------------------------------------
# CLI / output
# ---------------------------------------------------------------------------

ICONS = {True: "OK", False: "FAIL"}

def print_text(report: Report, template_name: str = DEFAULT_TEMPLATE_NAME) -> None:
    print(f"\n=== {template_name} Audit — {report.repo_path} ===")
    print(f"Template version: {report.template_version}\n")
    cats = ["structure", "metadata", "features", "code", "security", "drift"]
    for cat in cats:
        cat_checks = [c for c in report.checks if c.category == cat]
        passed = sum(1 for c in cat_checks if c.passed)
        total = len(cat_checks)
        if total == 0:
            continue
        status = "PASS" if passed == total else "FAIL"
        print(f"[{status}] {cat:<10} {passed}/{total}")
        for c in cat_checks:
            if c.passed:
                continue
            tag = "CRIT" if c.critical else "warn"
            print(f"    [{tag}] {c.name}")
            if c.message:
                print(f"        {c.message}")
            for fix in c.remediation:
                print(f"        - {fix}")
    print(f"\nSummary: {report.passed}/{report.total} checks passed",
          end="")
    if report.has_critical:
        print("  -- CRITICAL ISSUES PRESENT")
    elif report.has_any_failure:
        print("  -- warnings only")
    else:
        print("  -- all green")


def list_rules(template_name: str = DEFAULT_TEMPLATE_NAME) -> None:
    rules = [
        ("structure",  "all required .ai/ folders are present"),
        ("metadata",   f"{VERSION_MARKER_NAME} shape + CHANGELOG sync "
                       f"(timezone mismatch is a warning, not a failure)"),
        ("features",   "every feature has README.md + files.md + do-dont.md"),
        ("code",       "no hardcoded enum strings; API calls have validation"),
        ("security",   "security headers, SSRF guard, ownership marker and paging "
                       "cap all present; controllers guarded"),
        ("drift",      "locked shell files hold no project data; app-config layer intact (derived repos)"),
    ]
    print(f"\n{template_name} — audit rule families")
    print("-" * 50)
    for cat, desc in rules:
        print(f"  {cat:<10} {desc}")
    print()


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--repo", default=".", help="Repository root (default cwd)")
    ap.add_argument("--strict", action="store_true",
                    help="Exit non-zero on any failure, not just critical ones")
    ap.add_argument("--json", action="store_true", help="Emit JSON")
    ap.add_argument("--list", action="store_true",
                    help="Print rule families and exit")
    ap.add_argument("--ast", action="store_true",
                    help="Enable AST-based checks (requires tree_sitter, "
                         "tree_sitter_c_sharp, tree_sitter_typescript). "
                         "If libs are missing, falls back to regex with a warning.")
    ap.add_argument("--template-name", default=None, metavar="NAME",
                    help=f"Product name for the banner "
                         f"(env APP_TEMPLATE_NAME, default {DEFAULT_TEMPLATE_NAME!r})")
    ap.add_argument("--brand", default=None, metavar="LABEL",
                    help="Brand literal the locked shell must not hardcode "
                         "(env APP_TEMPLATE_BRAND, defaults to --template-name)")
    ap.add_argument("--timezone", default=None, metavar="NAME",
                    help=f"Timezone the version marker is expected to declare "
                         f"(env APP_TEMPLATE_TIMEZONE, default {DEFAULT_TIMEZONE}). "
                         f"A mismatch is reported as a warning, never critical.")
    args = ap.parse_args(argv)

    template_name = (args.template_name
                     or os.environ.get("APP_TEMPLATE_NAME")
                     or DEFAULT_TEMPLATE_NAME)
    brand = (args.brand
             or os.environ.get("APP_TEMPLATE_BRAND")
             or template_name)
    timezone = (args.timezone
                or os.environ.get("APP_TEMPLATE_TIMEZONE")
                or DEFAULT_TIMEZONE)

    if args.list:
        list_rules(template_name)
        return 0

    root = Path(args.repo).resolve()
    if not root.is_dir():
        print(f"ERROR: not a directory: {root}", file=sys.stderr)
        return 2

    report = Auditor(root, use_ast=args.ast, template_name=template_name,
                     brand=brand, timezone=timezone).run()

    if args.json:
        out = {
            "repo": report.repo_path,
            "templateVersion": report.template_version,
            "passed": report.passed,
            "total": report.total,
            "hasCritical": report.has_critical,
            "checks": [asdict(c) for c in report.checks],
        }
        print(json.dumps(out, indent=2))
    else:
        print_text(report, template_name)

    if args.strict and report.has_any_failure:
        return 1
    if report.has_critical:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
