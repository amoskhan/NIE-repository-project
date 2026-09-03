---
description: Apply NIE Ignite architecture requirements to the native plan.
scripts:
  sh: scripts/bash/setup-plan.sh --json
  ps: scripts/powershell/setup-plan.ps1 -Json
  py: scripts/python/setup_plan.py --json
---


## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Pre-Execution Checks

**Check for extension hooks (before planning)**:
- Check if `.specify/extensions.yml` exists in the project root.
- If it exists, read it and look for entries under the `hooks.before_plan` key
- If the YAML cannot be parsed or is invalid, skip hook checking silently and continue normally
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Pre-Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```
  - **Mandatory hook** (`optional: false`):
    ```
    ## Extension Hooks

    **Automatic Pre-Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}

    Wait for the result of the hook command before proceeding to the Outline.
    ```
    After emitting the block above you MUST actually invoke the hook and wait for it to finish before continuing. Run it the same way you would run the command yourself in this agent/session (the invocation may differ from the literal `{command}` id shown above, e.g. a skills-mode agent runs it as `/skill:speckit-...` or `$speckit-...`). Emitting the block alone does not run the hook.
- If no hooks are registered or `.specify/extensions.yml` does not exist, skip silently

## Outline

1. **Setup**: Run `{SCRIPT}` from repo root and parse JSON for FEATURE_SPEC, IMPL_PLAN, SPECS_DIR, BRANCH. For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

2. **Load context**: Read FEATURE_SPEC and `/memory/constitution.md`. Load IMPL_PLAN template (already copied).

3. **Execute plan workflow**: Follow the structure in IMPL_PLAN template to:
   - Fill Technical Context (mark unknowns as "NEEDS CLARIFICATION")
   - Fill Constitution Check section from constitution
   - Evaluate gates (ERROR if violations unjustified)
   - Phase 0: Generate research.md (resolve all NEEDS CLARIFICATION)
   - Phase 1: Generate data-model.md, contracts/, quickstart.md
   - Re-evaluate Constitution Check post-design

## Mandatory Post-Execution Hooks

**You MUST complete this section before reporting completion to the user.**

Check if `.specify/extensions.yml` exists in the project root.
- If it does not exist, or no hooks are registered under `hooks.after_plan`, skip to the Completion Report.
- If it exists, read it and look for entries under the `hooks.after_plan` key.
- If the YAML cannot be parsed or is invalid, skip hook checking silently and continue to the Completion Report.
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Mandatory hook** (`optional: false`) — **You MUST emit `EXECUTE_COMMAND:` for each mandatory hook**:
    ```
    ## Extension Hooks

    **Automatic Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}
    ```
    After emitting the block above you MUST actually invoke the hook and wait for it to finish before continuing. Run it the same way you would run the command yourself in this agent/session (the invocation may differ from the literal `{command}` id shown above, e.g. a skills-mode agent runs it as `/skill:speckit-...` or `$speckit-...`). Emitting the block alone does not run the hook.
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```

## Completion Report

Command ends after Phase 1 design. Report branch, IMPL_PLAN path, and generated artifacts.

## Phases

### Phase 0: Outline & Research

1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:

   ```text
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

### Phase 1: Design & Contracts

**Prerequisites:** `research.md` complete

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Define interface contracts** (if project has external interfaces) → `/contracts/`:
   - Identify what interfaces the project exposes to users or other systems
   - Document the contract format appropriate for the project type
   - Examples: public APIs for libraries, command schemas for CLI tools, endpoints for web services, grammars for parsers, UI contracts for applications
   - Skip if project is purely internal (build scripts, one-off tools, etc.)

3. **Create quickstart validation guide** → `quickstart.md`:
   - Document runnable validation scenarios that prove the feature works end-to-end
   - Include prerequisites, setup commands, test/run commands, and expected outcomes
   - Use links or references to contracts and data model details instead of duplicating them
   - Do not include full implementation code, model/service/controller bodies, migrations, or complete test suites
   - Keep this artifact as a validation/run guide; implementation details belong in `tasks.md` and the implementation phase

**Output**: data-model.md, /contracts/*, quickstart.md

## Key rules

- Use absolute paths for filesystem operations; use project-relative paths for references in documentation
- ERROR on gate failures or unresolved clarifications

## Done When

- [ ] Plan workflow executed and design artifacts generated
- [ ] Extension hooks dispatched or skipped according to the rules in Mandatory Post-Execution Hooks above
- [ ] Completion reported to user with branch, plan path, and generated artifacts



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
