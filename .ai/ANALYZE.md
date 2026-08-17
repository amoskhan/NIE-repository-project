# ANALYZE — Feature Inventory + Drift Report with a Clearance Gate

> **Paste the entire prompt below into any AI agent (Claude Code, Copilot Chat, Gemini, Kiro, Codex) running in this repository (the App Template itself, or any repo cloned from it). The agent will inventory which template features are actually implemented, detect drift from the locked-vs-project boundary, produce a numbered action list, STOP for your clearance, execute only the items you approve, and write a documented report.**

This is the **situation-report + remediation** command. It complements [`ALIGN.md`](./ALIGN.md):

| Command      | Purpose                                                                                                                                                        |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ALIGN.md`   | Adopt missing **template tasks** (upgrade a derived repo to the latest release).                                                                               |
| `ANALYZE.md` | Audit **feature coverage + boundary drift**, propose a prioritized action list, and apply approved inline fixes. Delegates actual task adoption to `ALIGN.md`. |

ANALYZE never authors or edits `.ai/tasks/NNNN` (that is a maintainer-only release step).

---

## Prompt to paste

```
You are an AI agent producing a feature + drift analysis for this repository. Operate strictly in the order below and STOP for confirmation where noted. Do NOT change any file before STEP 5, and only after explicit clearance.

STEP 0 — PREPARE
0.1. Read `.ai/common/11-customization-boundary.md` (the locked-vs-project boundary) and `.ai/README.md` (read order).
0.2. Read `.ai/common/10-agent-skills.md` if present; if the local tool supports Agent Skills and relevant .NET/Vue skills are missing, offer to install them (ask first).
0.3. Read `docs/FEATURE-ADOPTION-POLICY.md` if present. Use it to classify features as mandatory baseline, conditional mandatory, default-on feature pack, opt-in feature pack, or project-specific.
0.4. Resolve template tool commands. If local `tools/template-audit/audit.py` and `tools/template-align/align.py` exist, use them. Otherwise use the central App Template checkout. `audit.py` accepts `--repo <derived-repo>`; `align.py` must be run with the derived repo as the current working directory.

STEP 1 — DETECT CONTEXT
1.1. If BOTH `docs/template-releases/index.json` AND `.ai/tasks/_TEMPLATE/` exist, this is the TEMPLATE repo: the shell IS the source of truth, so SKIP drift detection (STEP 3) and task-adoption — run the feature inventory (STEP 2) and report only.
1.2. Otherwise this is a DERIVED repo. Read `.app-template-version.json` → capture `templateVersion` and `appliedTasks`.
     - If the marker is missing: report this and STOP. Ask whether to bootstrap it (see `ALIGN.md` STEP 1).

STEP 2 — FEATURE INVENTORY (always runs)
2.1. List every directory under `.ai/features/` whose name does NOT start with `_`.
2.2. For each feature, read `manifest.yaml` if present (fields: `category`, `status`, `copierFlag`, `removableInDerivedRepo`, `removalTaskId`, `files[].path`). If there is no manifest, parse the "Owned files" tables in `files.md`.
2.3. Classify each feature by checking its owned files on disk:
       - IMPLEMENTED — all owned files exist.
       - PARTIAL — some owned files exist.
       - ABSENT — none exist.
2.4. Add an adoption-policy column:
       - MANDATORY when `removableInDerivedRepo: false` and there is no `copierFlag`, plus any policy listed as mandatory in `docs/FEATURE-ADOPTION-POLICY.md`.
       - CONDITIONAL when the policy document says the feature is required only if a trigger exists (for example Sentry Cron when scheduled jobs exist, SSRF guard when outbound HTTP exists).
       - DEFAULT-ON when `copierFlag` exists and `copier.yml` defaults that flag to true.
       - OPT-IN when `copierFlag` exists and `copier.yml` defaults that flag to false.
       - PROJECT-SPECIFIC for substantial runtime code that is not a template feature.
2.5. Produce a table: feature | category | manifest status | adoption policy | state (implemented/partial/absent) | removable?  No STOP.

STEP 3 — DRIFT DETECTION (skip entirely if TEMPLATE repo)
3.1. Run the resolved audit command with `--repo . --json` (for example `python <template-root>/tools/template-audit/audit.py --repo . --json`). Parse `checks[]`; collect every entry where `category == "drift"` that did not pass, plus any other `critical` failures. Each drift check names the offending locked file and the remediation.
3.2. Run the resolved align command with `--json` from the derived repo root (for example `python <template-root>/tools/template-align/align.py --json` while the shell is in the derived repo). Parse `pending` — applicable template tasks not yet in `appliedTasks`.
3.3. OPTIONAL git baseline: only if a template remote is reachable, `git diff --name-only` the locked paths from `11-customization-boundary.md` against the matching template release to spot edited-but-not-data-leaked locked files. If no remote / not a git repo, SKIP silently — never block on git.
3.4. BACKEND boundary review (agent judgment, advisory only): scan the locked backend files named in `11-customization-boundary.md` (`MainDbContext.cs`, `MappingProfile.cs`, `Program.cs`, `AccessFunctionCatalog.cs`, base classes, middleware) for project additions that are NOT inside a `// === SAMPLE … ===` fence and NOT a registration of a project-owned extension/partial. Flag suspects as LOW-CONFIDENCE review items. Do NOT treat the absence of a SAMPLE fence as drift — a repo that correctly ran task 0003 removes those fences.

STEP 4 — COMPOSE THE ACTION LIST, THEN STOP
4.1. Produce ONE numbered action list, grouped and ordered by remediation path:
       A. Drift — critical (audit drift criticals) → INLINE fix.
       B. Drift — warning (audit drift warnings, e.g. leftover brand strings) → INLINE fix.
       C. Mandatory / conditional baseline gaps → INLINE fix or DELEGATE to `ALIGN.md`, depending on whether a released task exists.
       D. Missing template tasks (from align.py `pending`) → DELEGATE to `ALIGN.md`; tag each as mandatory, conditional, default-on, or opt-in.
       E. Default-on feature packs that are absent/partial → ask whether to ADOPT, COMPLETE, or DOCUMENT AS INTENTIONALLY DISABLED.
       F. Opt-in feature packs that are absent/partial → ask whether to ADD, COMPLETE, or SKIP.
       G. Backend boundary review suspects (STEP 3.4) → MANUAL, low-confidence.
     For each item give: an id (A1, A2, …), a one-line description, the exact file(s), the proposed action, whether it is inline or delegated, the adoption policy, and (for G) a confidence level.
4.2. STOP. Present the inventory table (STEP 2) + the action list. Ask the user: "Proceed with which mandatory/conditional repairs? Which default-on packs should be adopted, completed, or documented as disabled? Which opt-in packs should be added, if any?" Wait for an answer. Honor the exact subset chosen.

STEP 5 — EXECUTE APPROVED ITEMS ONLY
5.1. A / B (drift): MOVE the leaked data out of the locked file into the matching project-owned file (`app-config/navigation.ts` / `routes.ts` / `accessFunctions.ts` / `branding.ts`, or `theme/appTheme.ts` for the brand label) and restore the import in the locked file. Follow `11-customization-boundary.md` exactly — do NOT invent new shell logic.
5.2. C / D (baseline gaps and missing tasks): run the `ALIGN.md` flow for the approved task IDs. It walks each task's `apply.md`, runs its `verify.sh`, and records `appliedTasks`. ANALYZE itself never edits anything under `.ai/tasks/`.
5.3. E / F / G: perform only if explicitly approved. If the user skips a default-on or opt-in feature, document the decision; do not create files for it.
5.4. Re-run the resolved audit command with `--repo .` and any relevant task `verify.sh`. Surface exit codes verbatim. If a drift check is still red, report it and STOP.

STEP 6 — DOCUMENT
6.1. Create the directory `docs/analysis/` if absent, then write `docs/analysis/<YYYY-MM-DD>-analysis.md` (date = today in the repo's configured timezone — the `timezone` field in `.app-template-version.json`, default `Asia/Singapore`; if a file for today exists, append `-2`, `-3`, …). Record the zone you used in the header, e.g. `_Generated 2026-05-29 (Asia/Singapore)_`.
6.2. The report MUST contain: Context (template/derived, templateVersion, appliedTasks count); the feature inventory table; drift findings (each with severity + whether fixed); the action list exactly as presented in STEP 4; what was executed vs skipped (with the ids); verification commands run + their exit codes; open questions; recommended next actions (e.g. "run ALIGN for tasks NNNN..NNNN").
6.3. Commit ONLY if the user asks: `chore(analyze): feature + drift analysis <date>`.

CONSTRAINTS
- NEVER author, edit, or mint a `.ai/tasks/NNNN` task ID — delegate task adoption to ALIGN.md.
- NEVER execute an action the user did not approve in STEP 4.2.
- NEVER apply a default-on or opt-in feature silently. Ask for ADOPT / COMPLETE / DISABLE / SKIP and document the answer.
- NEVER add project data to a locked shell/infra file — always MOVE it to the project-owned config and import it back.
- NEVER block on a missing git baseline (STEP 3.3 is best-effort).
- In the TEMPLATE repo, SKIP drift detection and task-adoption (the shell is canonical there).
- The audit `drift` category is the deterministic gate; STEP 3.4 backend items are advisory, never an exit-code gate.
- NEVER touch `node_modules/`, `bin/`, `obj/`, `dist/`, or generated migrations.
```

---

## Tips for users running this prompt

- **Run it any time** to get a one-page picture of "what's built + where we drifted." It is read-only until you give clearance in STEP 4.
- **The deterministic backbone** is `template-audit/audit.py` (the `drift` category) + `template-align/align.py`. Derived repos do not need to carry these scripts locally; run them from the central App Template checkout when local `tools/template-*` is absent.
- **Drift vs. upgrade**: ANALYZE fixes boundary _drift_ inline and hands _upgrades_ (missing template tasks) to `ALIGN.md`. If you only want to adopt the latest template release, run `ALIGN.md` directly.
- **Mandatory vs. optional**: `docs/FEATURE-ADOPTION-POLICY.md` controls what must be repaired and what must be offered as a user choice.
- **CI can enforce drift**: wire the same audit into GitHub Actions (`.github/workflows/`) and the critical drift checks (D1/D2/D5/D6) fail a PR automatically in derived repos.
- **The report is durable**: each run leaves `docs/analysis/<date>-analysis.md` for review history.
