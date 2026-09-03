---
description: Implement exactly one task from the active native Spec Kit feature.
---


<!-- Source: nie-ignite -->
## User Input

```text
(the user will provide the argument in this conversation)
```

The input MUST contain exactly one task identifier in the form `T001`.

## Procedure

Use only the provisioned workspace's local NIE template source. Never clone,
pull, or fetch a second template repository and never invoke a credential helper
for template discovery. A required missing source path blocks the task.

1. Read `.specify/feature.json`, the fixed constitution, and every applicable
   artifact in the active feature directory. Resolve the same assurance value
   from `.ai/APPLICATION.md`, `application-profile.md`, and the Delivery
   assurance plan in `plan.md`; refuse a mismatch.
2. Locate exactly one unchecked checklist entry with the requested identifier
   in `tasks.md`. Refuse a missing, duplicate, malformed, already-completed, or
   dependency-blocked task. An earlier unchecked ordinary task is a barrier.
   An earlier unchecked `[P]` task in the same contiguous phase group is not a
   barrier: native `[P]` explicitly declares that those tasks may overlap.
3. Implement only that task and the minimal supporting edits required for it.
   Do not begin a later task.
4. Run the task's profile-required validation. For ordinary POC work, use the
   cheapest relevant compile/type/existing-test/direct-smoke proof and do not
   add or run a full automated test matrix unless the task is risk-escalated.
   For Standard, run focused changed-behavior regression checks and leave broad
   suites to the planned story/release checkpoint. For Enterprise or a
   risk-escalated slice, run the complete task-level tests and evidence named
   by the plan.
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
6. Report changed paths, validation results, and the next unblocked task.

Do not commit, push, merge, publish, or deploy.