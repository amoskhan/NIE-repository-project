# NIE Template distribution and AI-managed updates

## Model

The NIE Template separates three concerns:

1. Copier creates a reproducible, complete reference tree, renders deployment identity, and records optional feature decisions.
2. Markdown rules in `.ai` define the required architecture, behavior, libraries, menus, tests, and evidence.
3. AI agents implement and verify application-specific changes. Standard toolchains provide evidence but do not decide the complete architectural verdict.

There are no numbered migration tasks, task ledgers, custom Python alignment engines, or custom conformance verdicts.

## Scaffolding with Copier

```bash
copier copy https://niegithub.nie.edu.sg/NIE/nie-template ./my-app
```

Copier records the selected stack and optional feature decisions in `.copier-answers.yml`, copies the complete buildable reference, renders `project_name` and `project_title` into Helm, Compose, Nginx, runtime deployment configuration, maintenance, Jenkins, and AWS release artifacts, and executes no post-copy code. The canonical .NET and Vue source identities remain unchanged. The first AI agent then:

1. reads `AGENTS.md` and `.ai/README.md`;
2. keeps generic source folders, assemblies, namespaces, and `@nie/*` packages stable, and applies the approved identity only through configuration, branding, catalog metadata, observability, and deployment values;
3. applies the recorded feature decisions without breaking shared dependencies and updates `.ai/APPLICATION.md` with explicit adopted/not-adopted reasons;
4. verifies library floors and application rules;
5. runs the standard gates and produces the evidence report.

Copier updates are not treated as proof of source conformance and do not overwrite the AI rule contract in a derived application.

### Deployment identity migration on update

Applications created before deployment identity rendering was introduced can
retain `deploy/helm/application`, `Start-ApplicationRelease.ps1`, and
`Start-ApplicationRelease.sh` when a `copier update` also adds app-named
replacements. Copier must not silently delete these files because they may
contain application-owned customization.

After every update:

1. start from a clean Git worktree, run the reviewed Copier update, and inspect
   all conflicts or rejection files;
2. run `deploy/pipeline/Test-DeploymentIdentity.ps1` on Windows or
   `bash deploy/pipeline/Test-DeploymentIdentity.sh` on Linux; do not deploy
   while it reports legacy generic deployment artifacts;
3. compare every legacy chart and release file with the previously pinned
   canonical commit and classify it as identical, behind, customized, ahead,
   conflict, or not applicable under `.ai/WORKFLOW.md`;
4. merge intentional application-owned settings into
   `deploy/helm/<project_name>`, `Start-<project_name>Release.ps1`, and
   `Start-<project_name>Release.sh`, without replacing a customized folder
   wholesale;
5. update application-infrastructure `CHART_PATH`, release commands, Jenkins
   job configuration, and operations documentation that still reference the
   legacy paths;
6. obtain the application owner's approval before removing customized legacy
   files, then remove the reconciled generic chart/script and commit that
   migration separately when practical;
7. rerun the identity guard, Copier render smoke test, Helm lint/template,
   container/Compose checks, and the application deployment smoke test.

The checked-in migration regression test is:

```powershell
.\deploy\pipeline\Test-DeploymentIdentityMigration.ps1
```

```bash
bash deploy/pipeline/Test-DeploymentIdentityMigration.sh
```

## Version pinning

`.nie-template-version.json` records:

- the last template and rules version assessed;
- the exact canonical repository and commit;
- the verification date and model;
- whether the version includes a breaking change.

The pin means “this application was assessed against this exact baseline,” not “all files are identical.” Advance it only after affected rules and source changes have evidence.

## Refreshing AI instructions

An AI agent performs a reviewed refresh:

1. clone or fetch `https://niegithub.nie.edu.sg/NIE/nie-template` into a temporary directory;
2. resolve and record an exact canonical commit;
3. compare canonical `.ai/*.md` with local `.ai/*.md`;
4. preserve the application-owned `.ai/APPLICATION.md`;
5. copy the reviewed canonical Markdown changes;
6. diff rule and library changes and reassess every affected adopted feature;
7. run tests and independent verification before advancing the pin.

Matching instruction files alone never proves that application code complies.

## Source update triage

From the last pinned commit to the target commit, the AI reads the changelog and Git diff and classifies changes:

- security/correctness fixes affecting used code are prioritized;
- library floors apply when the application is below them;
- mandatory rules apply whenever their conditions exist;
- default-on and conditional features apply only when adopted or triggered;
- opt-in features require a product decision;
- sample-only changes remain examples;
- breaking migrations and changed business requirements require authorized approval.

The AI writes a direct implementation plan from affected rule IDs and code, implements it, and records evidence. No intermediate task dossier is necessary.

## Dependency selection and vendor neutrality

Derived applications follow the package order in `.ai/LIBRARIES.md` and the `NIE-DEPS-*` rules. For .NET platform capabilities, use the runtime/shared framework and official Microsoft packages first. For external technologies, use the official open-source project package where suitable, then a mature leading open-source alternative. Current popularity is supporting evidence only; security, maintenance, license, fit, interoperability, and replacement cost decide the outcome.

External providers remain replaceable. Vendor SDKs belong in infrastructure adapters selected by dependency injection, with domain/application contracts kept provider-neutral. New proprietary or provider-exclusive dependencies require explicit approval, commercial/data-egress analysis, a credible alternative, and a tested exit or migration plan.

## Common Vue and .NET code

The AI compares each affected shared file against the pinned canonical commit and labels it identical, behind, customized, ahead, conflict, or not applicable.

- Behind and uncustomized: adopt the compatible canonical change.
- Customized: merge and preserve intentional domain behavior.
- Ahead: retain the improvement if it still satisfies the rules and consider contributing it upstream.
- Conflict: resolve explicitly with regression, domain, and security evidence.
- Not applicable: record why.

Never replace an entire customized source folder.

Vue shared code remains domain-neutral and is extended with props, slots, events, composables, wrappers, plugins, theme tokens, provide/inject, and `app-config`. .NET shared code remains domain-neutral and is extended with interfaces, DI, options, policies, strategies, decorators, adapters, and events.

## Tool responsibilities

| Tool | Responsibility | Not sufficient for |
| --- | --- | --- |
| Copier | Initial file selection, answer recording, and deployment identity rendering | Source renaming, semantic merging, security, or conformance verdicts |
| Roslyn analyzers and `dotnet format` | C# compiler, style, and analyzer findings | Domain correctness, authorization coverage, or UX |
| ESLint and `vue-tsc` | Vue/TypeScript lint and type findings | Runtime workflows and visual/access behavior |
| xUnit, Vitest, and Playwright | Executable behavior evidence | Uncovered requirements or architecture intent |
| Dependency/secret scanners and CodeQL | Known dependency, secret, and static code risks | Complete threat modeling or business authorization |
| Implementing AI | Rule assessment, implementation, tests, and evidence | Independent approval of its own material work |
| Independent AI verifier | Adversarial review and risk-relevant reruns | Human approval for requirements, destructive changes, or exceptions |

## Security and urgent fixes

Security releases and severe bug fixes are identified in the canonical changelog and commit diff. The AI must assess whether affected code or libraries are used, implement the fix promptly when applicable, run security-focused tests/scans, and obtain an independent security-oriented AI review. If the change is breaking or destructive, a human risk owner still approves the migration and rollback plan.
