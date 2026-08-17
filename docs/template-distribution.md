# App Template — Distribution Model

> **Audience:** template maintainers, and anyone maintaining a project that was scaffolded from this template.
> **Status:** v2.0 — Copier flow + Python tools verified end-to-end.

This document describes how App Template gets from its GitHub repository into your project, and how your project stays aligned with the template over time. It complements [`.ai/common/09-template-versioning.md`](../.ai/common/09-template-versioning.md), which covers _authoring_ a release; this one covers _distributing_ it.

If you only want to start a project, you need [Lifecycle A](#a-creating-a-new-project) and nothing else.

---

## The two planes

```
┌──────────────────────────────────────────────────────────────────┐
│  AUTHORING  (this repo)                                          │
│  • .ai/features/<feature>/   — per-feature dossiers              │
│  • .ai/tasks/NNNN-<slug>/    — units of change                   │
│  • docs/template-releases/   — release manifests                 │
│  • tools/template-versioning/release.py  — release CLI (Python)  │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  DISTRIBUTION  (Copier — external tool)                          │
│  • copier.yml                          — questions + excludes    │
│  • [[ _copier_conf.answers_file ]].jinja — answers persistence   │
│  • tools/template-rename/rename.py    — namespace substitution   │
│  • tools/template-align/align.py      — post-copy/update task    │
│                                          scan + empty-dir prune  │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  YOUR PROJECT                                                    │
│  • .app-template-version.json    — which release is applied here │
│  • .copier-answers.yml           — what was answered at scaffold │
│  • .github/workflows/            — CI (GitHub Actions)           │
└──────────────────────────────────────────────────────────────────┘
```

The split matters: you can change _how_ releases are authored without touching _how_ projects pull them, and vice versa.

---

## Lifecycles

### A. Creating a new project

```bash
# 1. Scaffold via Copier (three-way-merge aware; replaces a manual `git clone`)
pip install --user copier          # one-off
copier copy --trust gh:your-org/app-template ./my-app
cd ./my-app
# (Answers are stored in .copier-answers.yml; the namespace rename runs as a post-task.)

# 2. Initial commit — required for `copier update` to work later
git init && git add . && git commit -m "chore: scaffold from App Template"

# 3. Self-check via .ai/ALIGN.md
#    Paste the prompt into your AI agent and let it walk any baseline tasks
#    that align.py flagged as unapplied.
```

Replace `your-org/app-template` with the GitHub org and repository you are pulling from. Copier accepts any Git URL, so a fork, a private repo (`gh:` uses your existing GitHub credentials), or a local path all work:

```bash
copier copy --trust /path/to/app-template ./my-app        # local checkout
copier copy --trust https://github.com/your-org/app-template.git ./my-app
```

**`--trust` is required** because Copier runs an opt-in post-scaffold task (`python tools/template-rename/rename.py --quiet`). Without `--trust`, Copier refuses to execute it and your project keeps the `AppTemplate` namespace.

Copier asks for the project slug, human-readable title, .NET root namespace, and which optional feature packs to include. Answers land in `.copier-answers.yml` and are reused on every later update.

### B. Adopting a new template release

```bash
# In your project:
copier update --trust        # pulls the latest template, three-way merge
git diff                     # review the merge
python /path/to/app-template/tools/template-audit/audit.py --repo .
git add . && git commit -m "chore(template): adopt <release>"
git push
# CI runs the audit workflow; on green, merge.
```

**`copier update` does a real three-way merge:**

- File modified only in the template → applied cleanly.
- File modified only in your project → preserved.
- File modified in **both** on the same line → standard `<<<<<<<` / `>>>>>>>` conflict markers, `git status` shows `UU`. Resolve like any git merge, then commit.

For an existing project that was never scaffolded through Copier, bootstrap the answers file once against its current state:

```bash
copier copy --trust --vcs-ref=main gh:your-org/app-template . \
  --data-file=existing-answers.yml --force
```

Or skip Copier entirely and use [`.ai/ALIGN.md`](../.ai/ALIGN.md) for AI-driven manual updates. Both paths are supported and neither is deprecated.

### C. Applying a security task

When a task with `type: "security"` ships in the template:

1. The maintainer authors the task under `.ai/tasks/NNNN-<slug>/` and cuts a release.
2. Downstream projects learn about it either from `copier update` (which brings the task files in) or by running `python tools/template-align/align.py`, which lists tasks present in the template but not recorded in `.app-template-version.json`.
3. Apply the task by following its `apply.md`, then run its `verify.sh`. On a zero exit, record the task id in `.app-template-version.json:appliedTasks`.
4. CI re-runs the audit to confirm the change before review.

There is no bot and no central registry pushing changes at you. Adoption is pull-based on purpose: your project decides when to take a change.

---

## What each tool owns

| Tool                                                                                            | Owns                                                                                                                | Does NOT own                      |
| ----------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- | --------------------------------- |
| **Copier (external)** + [`copier.yml`](../copier.yml)                                           | Distribution: scaffold, exclude per feature toggle, write answers, three-way-merge updates                          | Authoring or audit                |
| [`tools/template-rename/rename.py`](../tools/template-rename/rename.py)                         | Namespace substitution (`AppTemplate` → your project name) across `src/`, `build/`, `.devcontainer/`, `.vscode/`    | Anything outside those four roots |
| [`tools/template-align/align.py`](../tools/template-align/align.py)                             | Task discovery for derived projects, marker-file care, empty-directory pruning                                      | Auto-applying tasks               |
| [`tools/template-audit/audit.py`](../tools/template-audit/audit.py) (+ optional `ast_check.py`) | Compliance check across five categories; AST checks for C# authorization and TypeScript quality when `--ast` is set | Auto-fixing findings              |
| [`tools/template-versioning/release.py`](../tools/template-versioning/release.py)               | Cutting a release: manifest, notes, index, marker, and CHANGELOG written atomically; validating consistency         | Distribution to derived projects  |
| [`tools/template-guardrails/`](../tools/template-guardrails/)                                   | Repository-level guardrail checks used in CI                                                                        | Application code                  |
| [`.ai/ALIGN.md`](../.ai/ALIGN.md)                                                               | AI-driven interactive task application                                                                              | Automated syncing                 |

`tools/template-align/`, `tools/template-audit/`, and `tools/template-versioning/` are **maintainer** tools. They are excluded from scaffolded projects by `copier.yml` and are meant to be run from a checkout of the template itself.

---

## Versioning and identity

Each derived project carries two identity files:

| File                                         | Set when                                                  | Answers                                   |
| -------------------------------------------- | --------------------------------------------------------- | ----------------------------------------- |
| `.app-template-version.json:templateVersion` | At scaffold, and on each `copier update` or task adoption | "Which template release am I aligned to?" |
| `.app-template-version.json:appliedTasks`    | When each task's `verify.sh` exits 0                      | "Which incremental tasks have I taken?"   |
| `.copier-answers.yml`                        | At scaffold, retained on every update                     | "What feature toggles did I pick?"        |

`.app-template-version.json` is listed in `_skip_if_exists`, so `copier update` never clobbers your applied-task history.

---

## CI

Projects use **GitHub Actions**. Workflow files live under `.github/workflows/`; a scaffolded project receives the caller workflows, while template-maintenance workflows are excluded by `copier.yml`.

A reasonable project pipeline builds the .NET solution, type-checks and builds the frontend workspace, runs the Playwright suites, and runs `tools/template-audit/audit.py` against the checkout. Add deployment jobs as your project needs them — the Helm chart under `deploy/helm/app-template/` and the Compose files under `build/` are both neutral and are meant to be edited.

---

## FAQ

**Q. Why both Copier and the task system?**
Copier handles _file-level distribution_ — which bytes move from the template into your project. Tasks handle _semantic change_ — apply this migration, run this verification. They compose: Copier brings the files in, then `align.py` (run as a Copier post-task) reports which task dossiers still need to be applied semantically.

**Q. Do I have to use Copier?**
No. Clone the repo, delete `.git`, and run `python tools/template-rename/rename.py --to MyApp`. Keep `.app-template-version.json` so your project still records its starting release, and use `.ai/ALIGN.md` when you want to pull an update. Copier is recommended because it makes updates a merge instead of a manual diff.

**Q. How do I stop my project from drifting away from the template?**
Run the audit in CI. It acts on every push, and for a single project that is enough. The extra layers a large organisation would add on top — central drift dashboards, bots opening pull requests across many repositories — are deliberately not part of this template.

**Q. Can I delete parts of the template I do not need?**
Yes, and you should. The cleanest route is to turn the feature off at scaffold time, so `copier.yml` never copies it. For something already in your tree, use the matching dossier in `.ai/features/` — each one has a `files.md` listing everything the feature owns, and the procurement sample has a full [`remove.md`](../.ai/features/_samples/procurement/remove.md). Deleting by hand and missing one file (a controller that still references a deleted service, an access-function code that no longer exists) is the usual source of a broken build.

**Q. Who decides when a template change lands in my project?**
You do. Nothing is pushed automatically.
