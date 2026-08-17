# Task 0001 — Apply Guide

Adopt the App Template **2026.07.31.1** baseline.

This is the first task in the index and it is deliberately a **no-op adoption marker**. It
touches no application code. Its only job is to guarantee that the repo carries
`.app-template-version.json`, which is what every later task, `.ai/ALIGN.md`, and
`tools/template-align/align.py` read to decide what still needs applying.

A project scaffolded from the template already has that file, so in a fresh project this
task reports "not applicable" and exits — that is the expected outcome.

## Pre-checks

Pull the rules from `task.json:appliesIf`. There is a single rule:
`noneFileExist: [".app-template-version.json"]`.

```bash
# Applies ONLY when the adoption marker is missing.
test -f .app-template-version.json && { echo "Baseline marker already present; skipping."; exit 0; }
```

If the pre-checks fail, the agent records "task not applicable" and continues.

## Files to delete

```text
(none)
```

## Files to edit (line by line)

```text
(none)
```

---

## Files to create

For each new file, give the full content and the path.

```text
PATH: .app-template-version.json
CONTENT:
{
  "templateName": "App Template",
  "templateVersion": "2026.07.31.1",
  "releasedAtSgt": "2026-07-31T09:00:00+08:00",
  "timezone": "Asia/Singapore",
  "timezoneNote": "Default only. Change this together with the app's regional settings; nothing in the template hardcodes Asia/Singapore.",
  "releaseType": "baseline",
  "breaking": false,
  "sourceCommit": null,
  "sourceTemplateRepo": "https://github.com/your-org/app-template.git",
  "releaseNotesPath": "docs/template-releases/2026.07.31.1.md",
  "sourceReleaseNotes": null,
  "adoptedAtSgt": null,
  "appliedTasks": ["0001"],
  "localNotes": []
}
```

Then adjust two fields for the repo you are in:

- `adoptedAtSgt` — set to the current timestamp in the project's timezone when this repo
  adopts the baseline (leave `null` inside the template repo itself).
- `sourceTemplateRepo` — set to the actual template remote if you forked it somewhere else.

Leave `appliedTasks` containing this task's **`taskId`** (`"0001"` — the bare four-digit ID, not
the folder name `0001-baseline`), and append later task IDs in the same form as they are applied.
`tools/template-align/align.py` matches these entries against `taskId` in `.ai/tasks/index.json`;
a folder-name entry never matches and the task would report as permanently pending.

## Verification

```bash
# Run from repo root
test -f .app-template-version.json                         # marker exists
dotnet build src/backend/AppTemplate.sln                   # exit 0
python tools/template-versioning/release.py validate       # exit 0
bash .ai/tasks/0001-baseline/verify.sh                     # exit 0
```

## Rollback

If verification fails, run the steps in `rollback.md` (or `git restore .` if no specific rollback is needed).

## Notes for the agent

- If a file is missing, skip its edits with a note in the report — do not synthesize.
- If a file diverges from the expected `old_string`, STOP and ask the user.
- Never amend a prior commit. Create a new commit per task.
- Do not invent history: this baseline supersedes nothing, so leave `supersedes` and
  `sourceReleaseNotes` as `null`.
