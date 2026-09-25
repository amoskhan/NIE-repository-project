---
description: Generate native implementation tasks from the approved design and NIE repository guidance.
---

## NIE Ignite task guidance

Read the active feature's approved specification, screens, existing technical
plan and applicable repository instructions directly. Use the provisioned NIE
template source; do not clone a second template or request pasted file content.
Keep the selected feature directory and approved inputs unchanged.

Generate a concise, dependency-ordered native checklist:

```markdown
## Foundation
- [ ] T001 Establish the shared application foundations and relevant checks

## Booking journey
- [ ] T002 Define the journey's guided-tour steps and documentation
- [ ] T003 Implement the booking journey and its regression tests; depends on T001
```

Use unique stable IDs, clear descriptions and repository paths where useful.
Group work into coherent, independently testable user-story slices. Each task
should finish with its relevant checks passing; red-green development can occur
inside that task. Keep each phase together and put prerequisites before their
consumers. Do not multiply tasks to repeat every API, field or screen annotation.
Source references are useful when they clarify scope, but Ignite-specific graph
tags and a second semantic validation report are not required.

Honor the delivery assurance profile recorded by the repository and plan. Include
functional behavior, authorization and record scope, audit, data changes, API
contracts, integrations, UI permissions and meaningful regression evidence at
that depth. POC may consolidate work and evidence; Standard uses focused changed
behavior tests and journey checkpoints; Enterprise includes its required
independent verification. Record real open decisions without withholding useful
tasks because an earlier document contains a Constitution Check finding.

Approved screen sources own the reviewed visual design. Connect them to real
behavior and typed data without duplicating or redesigning their markup, shell,
branding, accessibility, responsive layout or stable tour targets. Preserve the
template's required administration, access-control, audit and operations
capabilities. Remove only reference samples identified by the application
profile. Durable tests should describe approved product behavior.

Before implementing a changed user workflow, define or update its role/access-
aware guided-tour outcome, step order, stable targets and affected user/technical
documentation. Keep code, tests, guidance and documentation synchronized in that
slice. Reuse the shared tour UI and describe only controls visible to the user's
effective access profile.

Ignite adds the phase's protected Verify and prepare live preview step. Plan
meaningful task-level checks without duplicating that general phase checkpoint.
Browser evidence should exercise real API behavior, persisted reload, relevant
failure/denied states and responsive critical journeys.

An ordinary checklist runs sequentially. Optional [P] waves can run concurrently
when every task declares a disjoint `writes: path/to/file, path/to/other-file`
set. A directory owns its descendants. Missing or overlapping ownership falls
back to sequential execution. Explicit `depends on T###` prerequisites must
exist earlier in the checklist. Consolidate same-file changes where practical.

Preserve completed checkboxes, IDs and evidence. Wording, paths, phases and
ordering may be corrected without blocking continuation or replaying finished
work. Do not demand old exact text or a new checklist solely for those differences.
Add pending follow-up work for requirements not yet implemented; do not claim
new evidence for completed tasks.
Write the finished checklist atomically to tasks.md. Do not start implementation,
commit, push or deploy.
