# 09 — Template Versioning (Task-Oriented)

Template versioning is **separate** from API versioning, application versioning, and deployment image tags. It tracks "what releases of the App Template has this repo adopted."

## Two contracts

| Contract | Purpose                                                                       | Where                                     |
| -------- | ----------------------------------------------------------------------------- | ----------------------------------------- |
| Release  | Human-readable history — what a release contains as a bundle                  | `CHANGELOG.md`, `docs/template-releases/` |
| Task     | Machine-executable unit — exact files to delete/edit/create with verification | `.ai/tasks/NNNN-<slug>/`                  |

A release MAY bundle multiple tasks. A derived repo agent applies tasks one at a time, oldest to newest.

## Version format

`YYYY.MM.DD.N`, stamped in the **release timezone**. `N` starts at 1 each local day and increments for additional same-day releases.

Example: `2026.08.14.1`. (The shipped baseline is `2026.07.31.1`.)

### Release timezone is configuration

The release timezone is a setting, not a rule. `Asia/Singapore` is only the shipped default — a project in another region sets its own IANA zone once and every later release inherits it. Resolution order used by `tools/template-versioning/release.py`:

1. `--timezone <IANA name>` on the command line
2. `APP_TEMPLATE_TIMEZONE` environment variable
3. the `timezone` field already recorded in `.app-template-version.json`
4. the `timezone` field in `docs/template-releases/index.json`
5. the default, `Asia/Singapore`

Setting `timezone` in `.app-template-version.json` is the normal way to change it. Never hardcode a zone into a task, workflow, or feature.

The same tool takes `--template-name` / `APP_TEMPLATE_NAME` for the product name and `--source-repo` / `APP_TEMPLATE_SOURCE_REPO` for the upstream URL.

## Canonical files

| File                                    | Purpose                                                  |
| --------------------------------------- | -------------------------------------------------------- |
| `.app-template-version.json`            | Current version marker. Carried into derived repos.      |
| `CHANGELOG.md`                          | Human-readable release history.                          |
| `docs/template-releases/index.json`     | Ordered list of releases.                                |
| `docs/template-releases/<version>.json` | Decision-complete release manifest (refers to tasks).    |
| `docs/template-releases/<version>.md`   | Human-readable release note.                             |
| `.ai/tasks/index.json`                  | Ordered list of all tasks across the template's history. |
| `.ai/tasks/NNNN-<slug>/task.json`       | Per-task machine-readable manifest.                      |
| `.ai/tasks/NNNN-<slug>/apply.md`        | Step-by-step apply guide for an AI agent.                |
| `.ai/tasks/NNNN-<slug>/verify.sh`       | Post-apply validation script.                            |

## Version marker fields

`.app-template-version.json` is the contract between the template and every repo derived from it.

| Field                | Meaning                                                                                                                                                  |
| -------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `templateName`       | Product name. `App Template` unless the project renames it.                                                                                              |
| `templateVersion`    | The release currently adopted.                                                                                                                           |
| `releasedAtSgt`      | Timestamp the release was minted, in the configured release timezone.                                                                                    |
| `timezone`           | IANA release timezone. Default `Asia/Singapore`; change it here to change it everywhere.                                                                 |
| `releaseType`        | `feature` \| `fix` \| `security` \| `breaking` \| `refactor`.                                                                                            |
| `breaking`           | Whether adopting this release requires manual intervention.                                                                                              |
| `sourceCommit`       | Git HEAD when the release artifacts were generated.                                                                                                      |
| `sourceTemplateRepo` | Upstream template URL. Ships as the placeholder `https://github.com/your-org/app-template.git` — replace `your-org` with wherever you host the template. |
| `releaseNotesPath`   | Path to this release's markdown note.                                                                                                                    |
| `sourceReleaseNotes` | Path to the upstream note a derived repo aligned against, when it differs.                                                                               |
| `appliedTasks`       | Ordered list of task IDs this repo has adopted. The authoritative upgrade state. `align.py` creates it as `[]` on first run if absent.                   |
| `adoptedAtSgt`       | When a derived repo adopted the release. `null` in the template itself.                                                                                  |
| `localNotes`         | Free-form provenance notes a derived repo may append.                                                                                                    |

The `…Sgt` suffixes on the two timestamp fields are historical names from when the zone was fixed. Their values follow whatever `timezone` resolves to. Renaming them would invalidate the marker in every derived repo, so they stay until a breaking release changes them deliberately.

## Task lifecycle

1. Maintainer makes the template change.
2. Maintainer creates `.ai/tasks/NNNN-<slug>/` from `.ai/tasks/_TEMPLATE/`.
3. Maintainer fills `task.json`, writes `apply.md`, writes `verify.sh`.
4. Maintainer runs `tools/template-versioning/...create-release` (extended to register the new task).
5. Maintainer commits everything together.
6. Husky pre-commit hooks and the GitHub Actions workflows validate the metadata.

## Derived-repo upgrade flow

A derived repo agent runs:

```text
1. Read .app-template-version.json → maxAppliedTaskId
2. Read .ai/tasks/index.json
3. For each task with id > maxAppliedTaskId, in order:
     a. Run pre-checks (does this apply to my repo?)
     b. Follow apply.md exhaustively
     c. Run verify.sh — must exit 0
     d. Append the taskId to .app-template-version.json:appliedTasks
4. Update templateVersion to the latest applied task's templateVersionAfterApply
5. Commit "chore: adopt template tasks 0002..0007"
```

The full executable prompt for derived-repo agents is `.ai/ALIGN.md`.

## Required `task.json` schema

```json
{
  "taskId": "0002",
  "slug": "remove-sample-model",
  "title": "Remove SampleModel scaffolding from derived repo",
  "type": "cleanup | feature | refactor | security | breaking",
  "breaking": false,
  "runOnClone": true,
  "supersedes": null,
  "dependsOn": ["0001"],
  "appliesIf": {
    "anyFileExists": ["src/backend/Libraries/Domain/Models/SampleModel.cs"],
    "allFilesExist": [],
    "noneFileExist": [],
    "anyFileContains": [
      { "path": "src/frontend", "pattern": "import.meta.env.VITE_" }
    ],
    "allFilesContain": [],
    "noneFileContains": []
  },
  "filesDeleted": ["..."],
  "filesEdited": [{ "path": "...", "reason": "..." }],
  "filesCreated": [],
  "verification": [
    {
      "type": "command",
      "run": "dotnet build src/backend/<sln>",
      "expectExit": 0
    },
    {
      "type": "grep",
      "pattern": "SampleModel",
      "paths": ["src/"],
      "expectMatches": 0
    }
  ],
  "minTemplateVersion": "2026.07.31.1",
  "templateVersionAfterApply": "2026.08.14.1",
  "docs": [".ai/features/_samples/sample-model/remove.md"]
}
```

`*FileContains` predicates accept objects with `path` and `pattern`. `path` may
point to a file or directory; directory scans skip generated folders such as
`node_modules`, `dist`, `bin`, and `obj`. Use these predicates for migrations
whose trigger is a code pattern rather than a stable file path.

## Source commit semantics

`sourceCommit` = the Git HEAD when the release artifacts were generated. It is provenance, not a self-referential merge hash. This avoids commit-amend workflows.
