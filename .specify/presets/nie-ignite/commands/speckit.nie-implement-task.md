---
description: Implement exactly one task from the active native Spec Kit feature.
---

## User Input

```text
$ARGUMENTS
```

The input MUST contain exactly one task identifier in the form `T001`.

## Current implementation authority

A task dispatched by Ignite's Run action is authorized against the current
approved pair and task dependencies. Recorded planning findings are advisory
during implementation too. Historical FAIL/BLOCKED labels, Tasks eligibility
rows, stale readiness/traceability counts, and generated statements forbidding
implementation or OpenAPI promotion in `plan.md`, `contracts/README.md`, research
or checklists do not override the current Run instruction. Do not require another
refine/approve/Plan cycle solely because of those statements.

For a validation or contract-promotion task, check the current source contracts
now. If its required checks pass, perform the requested promotion and record
current evidence. Repair issues within the selected task using established
product decisions. Retain remaining advisory findings honestly. Never invent
missing decisions, suppress actual failed checks, or claim unperformed work.
Stop for a concrete unmet dependency, unavailable required tool/source, actually
failing required check, necessary unanswered product decision, or unauthorized
action, and report the exact current evidence. A historical label alone is not
such evidence. Completion still requires the task's work and validation to pass.

## Procedure

Use only the provisioned workspace's local NIE template source. Never clone,
pull, or fetch a second template repository and never invoke a credential helper
for template discovery. A required missing source path blocks the task.

1. Read `.specify/feature.json`, the fixed constitution, applicable repository
   rules, the selected task and its direct dependencies. Search relevant
   artifact sections instead of dumping the entire checklist and unrelated
   feature documents. Resolve the same assurance value
   from `.ai/APPLICATION.md`, `application-profile.md`, and the Delivery
   assurance plan in `plan.md`; refuse a mismatch.
   Completed task wording, paths, phases, story labels and ordering may be
   corrected without blocking continuation. Preserve completed IDs, checkboxes
   and evidence; use the current checklist instead of requiring old exact text.
   Put new unfinished requirements in pending follow-up work.
2. Locate exactly one unchecked checklist entry with the requested identifier
   in `tasks.md`. Refuse a missing, duplicate, malformed, already-completed, or
   dependency-blocked task. An earlier unchecked ordinary task is a barrier.
   An earlier unchecked `[P]` task in the same contiguous phase group is not a
   barrier: native `[P]` explicitly declares that those tasks may overlap.
3. Implement only that task and the minimal supporting edits required for it.
   Listed paths describe scope; they do not prohibit a necessary supporting SDK
   or configuration repair. Preserve another concurrent task's owned paths and
   do not begin a later feature. Check required local tools and nested
   `global.json` resolution before lengthy work. On retry inspect existing
   changes and evidence, then rerun checks invalidated by the current changes.
4. Run the task's profile-required validation. For ordinary POC work, use the
   cheapest relevant compile/type/existing-test/direct-smoke proof and do not
   add or run a full automated test matrix unless the task is risk-escalated.
   For Standard, run focused changed-behavior regression checks and leave broad
   suites to the planned story/release checkpoint. For Enterprise or a
   risk-escalated slice, run the complete task-level tests and evidence named
   by the plan.
   Keep **Per-task evidence** separate from **Story/feature/release gates**.
   Configuration/command tasks prove their changed wiring and relevant behavior;
   they do not require later unfinished features, deployment-only Helm tests, or
   release-only scans to pass early. Record unrelated baseline failures and the
   checkpoint obligations without claiming those checks passed. Never weaken
   thresholds, disable release/security checks or waive an actual relevant
   failure. Independently review the final material diff, then only corrective
   deltas instead of repeating full unchanged reviews and suites.
   Parallel tasks use unique temporary outputs and serialize shared package,
   build/test and format mutations with the common workspace lock:
   `mkdir -p .ignite/locks; flock .ignite/locks/shared-validation.lock <command>`.
   If the task creates or removes an independently running frontend, backend,
   or worker, update `ignite.services.json` in the same task. Do not add a
   Coder, Terraform, Docker, or proxy resource per service; Ignite discovers
   the manifest and supplies hot reload and routing.
   When the task concerns a service that is not running, inspect
   `http://127.0.0.1:19000/__ignite/status`, supervisor state, and the matching
   `.ignite/logs/<service-id>*.log` before editing, then verify the required
   service is healthy.
   For browser or API tests, derive the active loopback ports from
   `.ignite/runtime-services.catalog.json` and pass those URLs through the
   test suite's existing environment variables. Static template `.env` ports
   are not evidence when the runtime catalog differs. Reuse the Chromium
   exposed by `PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH` and
   `PLAYWRIGHT_BROWSERS_PATH`; never run `playwright install`, `npx playwright
   install`, or download another browser during task execution. If the managed
   executable is absent or cannot launch, fail with that exact condition.
5. On success, change only its checkbox from `[ ]` to `[x]`. On failure, leave
   it unchecked and report the evidence. Exception: when the platform prompt
   explicitly says `Ignite will serialize that shared checkbox`, do not edit
   `tasks.md`; the platform owns that single serialized update. In that mode,
   always write the JSON completion receipt requested by the platform before
   stopping: use `completed` only after implementation and validation succeed,
   `blocked` with `blockingTaskKeys` for a dependency refusal, or
   `validation-failed` with the failing check in `detail`. The receipt is an
   execution outcome, not permission to mark incomplete work complete.
   Whenever a completion receipt path is supplied, write the outcome for
   sequential execution too. Use `tooling-required` for an unavailable required
   local executable or incompatible SDK, and include its exact command/version
   and recovery step. Keep `detail` under 4000 characters. An empty receipt or
   generic CLI exit is not task completion evidence.
6. Report changed paths, validation results, and the next unblocked task.

Do not commit, push, merge, publish, or deploy.
