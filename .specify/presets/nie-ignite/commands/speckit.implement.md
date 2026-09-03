---
description: Apply NIE Ignite safety and validation gates during implementation.
---

## NIE Ignite implementation requirements

- Work only in the current repository and active feature.
- Treat the provisioned workspace as the complete NIE template baseline. Never
  clone, pull, or fetch a second template repository and never invoke a
  credential helper for template discovery.
- Resolve the active assurance profile from `.ai/APPLICATION.md`,
  `application-profile.md`, and `plan.md` before executing tasks. Stop on a
  mismatch. Apply only the per-task and checkpoint checks due under that plan;
  do not upgrade ordinary POC/Standard tasks to Enterprise by habit.
- Process tasks in dependency order and mark a checkbox complete only after its
  implementation and listed validation pass.
- Use existing NIE shared patterns before adding abstractions. Preserve
  authorization, audit, API, data, and UI contracts across layers.
- POC runs the cheapest relevant compile/type/existing-test/direct-smoke proof
  during each task and its consolidated happy-path/service-health checkpoint;
  it does not add or run a new full test matrix per ordinary task. Standard
  runs focused regressions per task and affected suites at story/release
  checkpoints. Enterprise runs strict task tests and the plan's complete
  affected validation set. Risk-escalated slices always use Enterprise depth.
- Treat `ignite.services.json` as the staff workspace runtime contract. Add or
  remove a manifest entry whenever a task adds or removes an independently
  running frontend, backend, or worker. The workspace supplies supervision,
  hot reload, health discovery, and preview routing from that file.
- For "not running", preview, or runtime errors, inspect the runtime catalog,
  supervisor state, and service logs first; reproduce, fix, and re-check every
  required service rather than assuming the code edit started successfully.
- Stop on a failed test, missing dependency, ambiguous destructive action, or
  required external authorization. Leave the task unchecked and report the
  evidence.
- Do not commit, push, merge, publish, or deploy.
