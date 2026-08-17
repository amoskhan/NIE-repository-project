# App Template Tooling

Five small Python scripts that keep the template and the projects derived from it
in sync. Stdlib only — Python 3.11+ and nothing to `pip install`.

Scaffolding itself is delegated to **[Copier](https://copier.readthedocs.io/)**;
the questions live in [`../copier.yml`](../copier.yml).

## Overview

| Concern              | Tool                                                                                                                  | What it does                                                                                                                            |
| -------------------- | --------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| **Scaffolding**      | `copier` (external) + [`../copier.yml`](../copier.yml)                                                                | Create a new project: questions → files copied, answers saved to `.copier-answers.yml`                                                  |
| **Updates**          | `copier update`                                                                                                       | Re-render with the current template state; 3-way merge into the derived repo                                                            |
| **Naming**           | [`template-rename/rename.py`](./template-rename/rename.py)                                                            | Rebrand: substitute the template's placeholder namespace and product title everywhere. Runs automatically as a Copier task. Idempotent. |
| **Alignment**        | [`template-align/align.py`](./template-align/align.py)                                                                | Maintain the version marker; report template tasks the repo hasn't applied yet; prune empty dirs left by Copier exclusions              |
| **Audit**            | [`template-audit/audit.py`](./template-audit/audit.py)                                                                | Compliance check across six categories (structure / metadata / features / code / security / drift)                                      |
| **Guardrail**        | [`template-guardrails/check_locked_files.py`](./template-guardrails/check_locked_files.py)                            | Block commits that edit template-owned files without shipping a template task                                                           |
| **Releases**         | [`template-versioning/release.py`](./template-versioning/release.py)                                                  | Mint releases, validate manifests, propose the next version                                                                             |
| **AST audit**        | [`template-audit/ast_check.py`](./template-audit/ast_check.py)                                                        | Optional tree-sitter check pack (C# + TS). Wired into `audit.py --ast`; falls back to regex if the libs are missing.                    |
| **Feature analysis** | [`../.ai/ANALYZE.md`](../.ai/ANALYZE.md) + [`../docs/FEATURE-ADOPTION-POLICY.md`](../docs/FEATURE-ADOPTION-POLICY.md) | Agent-run inventory separating mandatory drift, conditional drift, default-on feature packs, and opt-in feature choices                 |

## Prerequisites

- Python 3.11+ (`python --version`)
- Copier 9.2+ for scaffolding (`pip install --user copier`)
- `git` (needed for `copier update`'s 3-way merge)

## Shared configuration

`release.py`, `align.py` and `audit.py` read the same three settings. A command-line
flag always wins over the environment variable.

| Setting                               | Flag              | Environment variable       | Default                  |
| ------------------------------------- | ----------------- | -------------------------- | ------------------------ |
| Timezone used to stamp release dates  | `--timezone`      | `APP_TEMPLATE_TIMEZONE`    | `Asia/Singapore`         |
| Product name in banners and manifests | `--template-name` | `APP_TEMPLATE_NAME`        | `App Template`           |
| Upstream repo recorded in the marker  | `--source-repo`   | `APP_TEMPLATE_SOURCE_REPO` | the public template repo |

Regional settings are **configuration, not compliance**. The template ships
`Asia/Singapore` and `en-SG` as defaults; a project that runs elsewhere changes
them and stays perfectly compliant. `audit.py` reports a timezone mismatch as a
warning and never as a critical failure.

`release.py` additionally falls back to whatever `timezone` is already recorded in
`.app-template-version.json` (or the releases index), so setting your zone once in
the marker is enough — every later release inherits it.

> IANA zone names need the `tzdata` package on Windows. Without it, `Asia/Singapore`
> still resolves via a built-in `+08:00` fallback; any other zone falls back to
> system local time with a warning. `pip install tzdata` if you set a different one.

## Quick Start

### New project — scaffold via Copier

```bash
pip install --user copier

copier copy --trust gh:your-org/app-template ./my-app

cd ./my-app
git init && git add . && git commit -m "chore: scaffold from App Template"
```

`--trust` lets the post-scaffold `_tasks` (the namespace rename) run. Copier treats
arbitrary shell commands as untrusted by default, so it is required.

### Existing project — pull template updates

```bash
cd ./my-app
copier update --trust            # 3-way merge the new template state in
git diff                         # review the merge
python /path/to/app-template/tools/template-audit/audit.py --repo .
git add . && git commit -m "chore: adopt template <version>"
```

If a file was edited in both the template and your repo on the same line you get
standard `<<<<<<<` / `=======` / `>>>>>>>` markers. Resolve them like any git merge.

### Audit — gate CI

```bash
# Default: fails only on critical findings
python tools/template-audit/audit.py --repo /path/to/my-app

# Strict: any warning fails too
python tools/template-audit/audit.py --repo /path/to/my-app --strict

# Machine-readable
python tools/template-audit/audit.py --repo /path/to/my-app --json
```

Exit codes: `0` no critical findings, `1` at least one critical, `2` invocation error.

Wire it into your CI provider by running the same command — the script has no
dependencies, so a checkout plus `setup-python` is the whole job. GitHub Actions
workflows live in [`../.github/workflows/`](../.github/workflows/).

### Analyze — choose which features to adopt

Use `.ai/ANALYZE.md` before applying broad template changes to an existing project.
The agent reads `docs/FEATURE-ADOPTION-POLICY.md`, produces a feature table, and
stops for your decision:

```text
1. Repair which mandatory or conditional drift items?
2. Adopt, complete, or document as disabled which default-on feature packs?
3. Add which opt-in feature packs, if any?
```

Optional feature packs are never applied just because they appear in
`.ai/tasks/index.json`. You choose them explicitly.

### Template maintenance — cut a release

```bash
# What's the current version?
python tools/template-versioning/release.py current

# What would the next version be? (YYYY.MM.DD.N in the configured timezone)
python tools/template-versioning/release.py propose

# Mint a release including specific tasks
python tools/template-versioning/release.py create-release \
  --summary "Add the workflow engine and PDF reporting feature packs" \
  --release-type feature \
  --task 0010 --task 0011

# Verify every version artefact is in sync
python tools/template-versioning/release.py validate
```

## Typical workflow — onboarding a new project

```text
1. Scaffold
   copier copy --trust gh:your-org/app-template ./my-app
   cd ./my-app && git init && git add . && git commit -m "scaffold"

2. Audit (initial)
   python /path/to/app-template/tools/template-audit/audit.py --repo .
   → expect warnings; fix them in the next step

3. Customise
   - Confirm the namespace rename landed (Copier runs it for you)
   - Configure the DB connection string and your secret store
   - Inspect pending tasks: run tools/template-align/align.py from the repo root,
     or paste .ai/ALIGN.md into Claude / Copilot / Gemini

4. Audit (final)
   python /path/to/app-template/tools/template-audit/audit.py --repo . --strict
   → all green before the first deploy

5. Stay aligned
   copier update --trust   # any time the template ships a new release
```

## Typical workflow — maintaining the template

```text
1. Author the change
   - Add or modify files
   - Create/update a task dossier in .ai/tasks/<NNNN>-<slug>/

2. Cut a release
   python tools/template-versioning/release.py create-release \
     --summary "..." --release-type feature --task NNNN

3. Validate
   python tools/template-versioning/release.py validate

4. Tag and push
   git tag <version> && git push origin <version>

5. Notify derived repos
   Open a PR in one pilot repo via `copier update`, then roll wider once it's stable.
```

## Tool internals

### `template-rename/rename.py`

Replaces the template's placeholder namespace with the project's
`dotnet_root_namespace`, and the placeholder product title with `project_title`,
across file contents **and** file names. Three ways to run it:

1. **Automatically via Copier** — listed in `copier.yml:_tasks`; reads both answers
   from `.copier-answers.yml`.
2. **Manually after a plain clone** —
   `python tools/template-rename/rename.py --to MyApp --title "My App"`.
3. **Dry run** — add `--dry-run` to preview without writing.

It walks `src/`, `tests/`, `build/`, `deploy/`, `docs/`, `tools/`, `.ai/`,
`.github/`, `.devcontainer/`, `.vscode/`, `.kiro/`, `.husky/` and the root-level
`README.md` / `AGENTS.md` / `GEMINI.md` / `CLAUDE.md`. A rebrand should be
complete, so template metadata is renamed along with everything else.

Deliberately left alone, each for a reason spelled out in the script's docstring:
`.git/`, `node_modules/`, `bin/`, `obj/`, `dist/` and caches; `Migrations/` (EF
migration ids are recorded in `__EFMigrationsHistory`, so renaming identifiers
inside them desynchronises applied migrations); `docs/template-releases/`,
`.app-template-version.json`, `CHANGELOG.md` and `copier.yml` (provenance — they
describe the template you came from, not your project); and the rename script
itself (it is the mapping _from_ the template name, so it keeps saying it).

Directory names are not rewritten — only file names and contents. The one
directory carrying the template name is `deploy/helm/app-template/`; rename it and
its `Chart.yaml` `name:` yourself if you want the chart to match.

The application DbContext stays `MainDbContext` in every derived project.

### `template-align/align.py`

Two responsibilities:

1. **Marker care** — make sure `.app-template-version.json` exists with the keys
   `align.py` needs (`appliedTasks` and friends), without ever stripping fields the
   release process wrote.
2. **Pending-task discovery** — read `.ai/tasks/index.json`, evaluate each task's
   `appliesIf` predicate, and report tasks not yet listed in `appliedTasks`. It
   **never auto-applies a task** — that's the job of the `.ai/ALIGN.md` AI-driven
   flow, which knows how to walk `apply.md` interactively.

It detects "I'm running inside the template repo" from the presence of
`docs/template-releases/index.json` plus `.ai/tasks/_TEMPLATE/`, and skips marker
mutation in that case.

### `template-audit/audit.py`

Six categories:

| Category      | Examples                                                                                                                                                                                                    |
| ------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **structure** | `.ai/`, `.ai/common/`, `.ai/features/`, `.ai/tasks/` exist; CLAUDE.md present                                                                                                                               |
| **metadata**  | `.app-template-version.json` shape valid; CHANGELOG mentions the current version; declared timezone (warning only)                                                                                          |
| **features**  | every `.ai/features/<feature>/` has README.md + files.md + do-dont.md                                                                                                                                       |
| **code**      | no hardcoded enum strings (heuristic); API responses validated (heuristic)                                                                                                                                  |
| **security**  | security-headers middleware, SSRF guard, `IOwnedEntity` marker and `PagedSearchDto` cap all present; concrete controllers carry `[Authorize]`, `[RequireAccessFunction]`, or an explicit `[AllowAnonymous]` |
| **drift**     | locked shell files hold no project data; the `app-config` layer is intact (derived repos only — skipped in the template repo)                                                                               |

`--strict` turns warnings into failures. `--list` prints the rule families and exits.

The security checks locate each artefact by trying every path it has lived at, so a
backend refactor that moves a file between `Domain/` and `Shared/` doesn't produce a
phantom failure.

The drift brand check knows the shell must not hardcode a product name. It looks for
the configured brand _and_ for whatever `brandLabel` your `theme/appTheme.ts`
declares, so it keeps working after you have rebranded.

### `template-audit/ast_check.py`

Optional tree-sitter check pack, enabled with `--ast`. Falls back to regex with a
warning if `tree_sitter`, `tree_sitter_c_sharp` or `tree_sitter_typescript` is
missing. Four rules the regex pass cannot compute accurately:

| Rule                   | What it catches                                                                                                                                                            |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `cs/missing-authorize` | Public `*Controller` methods with no `[Authorize]` / `[RequireAccessFunction]` / `[AllowAnonymous]` at method OR class level. Reports exact method names and line numbers. |
| `cs/unbounded-take`    | `.Take(N)` where N > 100 — review it against `PagedSearchDto`.                                                                                                             |
| `ts/as-any`            | `as any` casts in TS source.                                                                                                                                               |
| `ts/unvalidated-json`  | `.json()` calls with no parse / type guard nearby.                                                                                                                         |

```bash
pip install tree_sitter tree_sitter_c_sharp tree_sitter_typescript
python tools/template-audit/audit.py --repo . --ast
```

### `template-guardrails/check_locked_files.py`

Fails a change set that edits a template-owned file without shipping a new template
task (`.ai/tasks/NNNN-*/task.json`) alongside it. The locked set mirrors
[`.ai/common/11-customization-boundary.md`](../.ai/common/11-customization-boundary.md):
the staff shell, the router and permission machinery, `@apptemplate/ui` and
`@apptemplate/shared`, and the backend base classes, middleware, authorization
attributes, DbContext, Mapster profile, `Program.cs` and `AccessFunctionCatalog`.

```bash
python tools/template-guardrails/check_locked_files.py --staged          # pre-commit
python tools/template-guardrails/check_locked_files.py --base origin/main # CI
python tools/template-guardrails/check_locked_files.py path/to/file.cs    # manual
python tools/template-guardrails/check_locked_files.py --verify-paths     # self-check
```

`--verify-paths` confirms every locked path still exists. Run it after any refactor
that moves files: a locked entry pointing at a path that no longer exists guards
nothing, and it fails silently. It is wired into the Husky pre-commit hook via
`--staged`.

### `template-versioning/release.py`

`create-release` writes five artefacts in one go:

- `docs/template-releases/<version>.json` — manifest
- `docs/template-releases/<version>.md` — human-readable notes
- `docs/template-releases/index.json` — appended; `currentVersion` bumped
- `.app-template-version.json` — marker updated
- `CHANGELOG.md` — new section prepended

`--release-type` accepts `baseline`, `feature`, `fix`, `security`, `breaking` or
`refactor`. Versions are `YYYY.MM.DD.N`, where `N` restarts at 1 each day in the
configured timezone.

`validate` checks that the marker, index, manifests and CHANGELOG all name the same
current version, and that every release listed in the index has its manifest and
notes file on disk. In a derived repo (no `docs/template-releases/`) it validates
the marker's shape only.

The `releasedAtSgt` and `adoptedAtSgt` keys keep their historical names so existing
markers stay readable. Their values follow whatever timezone you configured and are
full ISO-8601 stamps, so each one carries its own UTC offset.

## Design philosophy

- **Stdlib only** — a derived repo needs nothing beyond Copier itself
- **Idempotent** — re-running `rename.py`, `align.py`, `audit.py` or
  `release.py validate` is always safe
- **Non-destructive by default** — `--dry-run` where it mutates; `align.py` never
  strips fields from the marker
- **Composable** — `--json` output for piping into dashboards or CI

## Contributing

When adding a template feature:

1. Create the feature dossier — `.ai/features/<feature>/{README,files,do-dont,verify}.md`
2. Create the task dossier — `.ai/tasks/NNNN-<slug>/{task.json,apply.md,verify.sh}`
3. Run `python tools/template-versioning/release.py create-release --task NNNN ...`
4. Commit it all together

See [`../.ai/README.md`](../.ai/README.md) for the full instruction structure.
