---
description: Apply NIE Ignite architecture requirements to the native plan.
---

## NIE Ignite planning requirements

- Inspect the actual repository before selecting paths, projects, packages,
  commands, or framework versions. The provisioned workspace is the complete
  NIE template baseline. Never run `git clone`, `git pull`, a credential helper,
  or a network fetch to obtain or assume a second NIE template.
- Discover and follow the repository's own instruction hierarchy before making
  architectural choices: root and nested `AGENTS.md`, `.ai/README.md`,
  `.ai/APPLICATION.md`, `.ai/WORKFLOW.md`, `.ai/GLOBAL-RULES.md`,
  `.ai/LIBRARIES.md`, applicable `.ai/FEATURE-*.md`, README files, and the
  repository's manifests. Inspect `.ai/ASSURANCE-PROFILES.md` when the
  repository provides it. The provisioned local repository and its NIE template baseline
  define the stack; do not recreate a competing stack policy from memory.
- Complete the Repository instruction manifest in `plan.md`, including every
  applicable instruction path, its scope, the constraint or decision used, and
  the exact completed labels `**Inspected source revision**:` and
  `**Technical manifests inspected**:` with real values. If Git metadata is unavailable,
  record a deterministic repository baseline identifier from the visible
  template/application manifests and state that `.git` is unavailable; this is
  not a planning blocker. A file required by repository-owned instructions that
  is missing, or an unresolved contradiction, is a failed planning gate.
- Inspect `.ai/APPLICATION.md` when present. If it still contains template
  placeholders, record that state and design its product-specific update from
  the approved specification. Preserve the repository-defined schema and any
  established non-placeholder decisions.
- Resolve `Delivery assurance profile` from the canonical repository selection
  in `.ai/APPLICATION.md` and the active `application-profile.md`, applying any
  repository-provided assurance-profile guidance. Require exactly `poc`,
  `standard`, or `enterprise` and fail Plan if the selected values disagree.
  An absent optional `.ai/ASSURANCE-PROFILES.md` is not by itself a blocker.
  Complete the Delivery assurance plan in `plan.md` with the exact labels
  `**Resolved profile**:`, `**Risk-escalated slices**:`,
  `**Per-task evidence**:`, `**Story/feature/release gates**:`, and
  `**Deferred evidence or promotion debt**:`. Give each label a real value;
  placeholders and prose-only substitutes fail the Plan gate.
- Treat the completed Specify review set (`overview.md`, `requirements.md`,
  `data-model.md`, `workflows.md`, `api.md`, `integrations.md`, and
  `ui/screens.md`) as required product-design inputs. They must already contain
  reviewable content. The complete reviewed product contract is immutable during
  Plan: do not edit `spec.md`, `overview.md`, `requirements.md`, `data-model.md`,
  `workflows.md`, `api.md`, `integrations.md`, `application-profile.md`,
  `ui/screens.md`, `ui/reference-patterns.md`, or generated screen source. Put
  physical mappings and implementation decisions only in `plan.md`,
  `research.md`, `contracts/`, and `quickstart.md`.
- A Plan whose Constitution Check or final gate result contains `FAIL` or
  `BLOCKED` is not complete and must not proceed to Tasks. Resolve a technical
  choice only when the approved product contracts permit it. When the conflict
  belongs to those contracts, fail deliberately for specification refinement;
  never hide, relabel, or task around the blocker.
- Produce the implementation-specific `plan.md`, `research.md`, and
  `quickstart.md`, and machine-readable contracts under `contracts/` whenever
  applicable. Map the reviewed product design onto the repository's actual
  stack, packages, services, persistence, security, and validation approach.
- If a technical decision conflicts with or requires a change to the immutable
  review set, fail Plan deliberately and return to specification refinement.
  Never silently rewrite an approved artifact or make the approved screens
  stale while generating tasks.
- Complete the NIE Design Artifact Index in `plan.md`. Record a concrete reason
  for each artifact that is not applicable.
- Plan the repository's shared guided-tour primitive, feature-owned step
  definitions, stable `data-tour` targets, effective role/access-function
  filtering, documentation updates, focused unit/component tests, and a
  representative desktop/390-pixel replay. Do not plan a product-private
  overlay or defer the tour until final polish.
- Include exact validation commands derived from the repository and assign
  them to the cadence required by the selected assurance profile. Do not turn
  every repository gate into a per-task POC or Standard command.
- Do not commit, push, or deploy.
