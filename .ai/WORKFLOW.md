# AI implementation, alignment, and verification workflow

Use this workflow for new features, bug fixes, security fixes, dependency upgrades, template alignment, and shared-code updates.

## 1. Establish the baseline

1. Inspect repository status and preserve all intentional user changes.
2. Read the contract in the order defined by `README.md`.
3. Record the local rules version from `.nie-template-version.json` and the canonical template commit previously verified.
4. Fetch or clone `https://niegithub.nie.edu.sg/NIE/nie-template` into a temporary location, without exposing credentials, and resolve the exact target commit.
5. Compare canonical instruction Markdown with local `.ai` files. Preserve local `APPLICATION.md`.
6. Read the canonical changelog and Git diff from the last verified commit to the target commit before deciding source impact.

If the canonical repository cannot be reached, continue only when freshness is not essential to the request. Report that limitation and do not invent newer rules or security fixes.

## 2. Triage canonical changes

Classify every relevant canonical change:

| Change | Decision |
| --- | --- |
| Critical/high security fix or correctness bug affecting used code | Adopt promptly; breaking data or external-contract changes still require authorized approval and rollback planning. |
| Minimum library or runtime increase | Adopt when the application is below the floor; review release notes, compatibility, migrations, lockfiles, and deployment. |
| Mandatory feature rule | Implement when applicable. |
| Default-on or conditional feature change | Implement when the feature is adopted or its trigger exists; otherwise record not-applicable. |
| Opt-in feature | Do not introduce without a product decision. |
| Shared Vue/.NET improvement | Compare each affected file and merge according to the shared-code procedure below. |
| Procurement/sample-only change | Use as reference only unless the application intentionally retains that sample. |
| Breaking migration, destructive action, or changed business requirement | Stop for authorized human direction before applying it. |

There are no numbered migration tasks. The AI derives the implementation plan from the rule diff, code diff, application context, and evidence required by affected dossiers.

## 3. Produce the before-change assessment

The implementing AI records:

- request and acceptance criteria;
- affected global and feature rule IDs;
- adopted/not-adopted feature decisions;
- security, privacy, data migration, compatibility, and rollback risks;
- package candidates and the selection order used, including publisher identity, open-source license, maintenance/security health, current ecosystem adoption, provider-neutral boundary, alternative provider, and exit path;
- files and extension points expected to change;
- tests that will first demonstrate missing or broken behavior;
- assumptions and decisions requiring human approval.

## 4. Implement through supported extension points

The AI owns the full approved implementation: code, migrations, configuration, tests, and documentation.

For common code, compare against the pinned canonical commit and classify each file:

| Classification | Required action |
| --- | --- |
| identical | No source change; retain evidence. |
| behind, uncustomized | Adopt the compatible canonical update and run affected tests. |
| customized | Merge deliberately; preserve domain behavior and move variation to supported extension points where practical. |
| ahead | Keep the application improvement, verify it still satisfies canonical rules, and consider contributing it upstream. |
| conflict | Resolve explicitly with domain, security, and regression evidence; never choose a whole-folder overwrite. |
| not applicable | Record the concrete reason. |

Preferred Vue extension points are typed props, slots, events, provide/inject, focused composables, wrappers, plugins, theme tokens, and `app-config`. Keep `@nie/ui` visual/domain-neutral and `@nie/platform` non-visual/domain-neutral.

Preferred .NET extension points are interfaces, dependency injection, validated options, policies, strategies, decorators, adapters, domain/application events, and provider abstractions. Keep domain-specific code in traceable feature slices rather than shared libraries.

Retain stable generic source identities while applying canonical updates. Do not rename shared folders, assemblies, namespaces, Vue app paths, or `@nie/*` packages per product; place the product name in configuration, branding, deployment, observability, and catalog metadata so the same canonical path remains mergeable.

For a derived application, first complete the Derived-application profile in
`.ai/APPLICATION.md` and the active specification's `application-profile.md`.
Record `remove` or `retain` for the Procurement reference. When removing it,
inventory and remove/deactivate its active frontend routes, navigation, page
registrations, backend registrations, seeds, permissions, and visible labels
before product routes are declared complete. Preserve its reusable patterns by
mapping product screens in `ui/reference-patterns.md`, using the shared shell and
components, and proving representative desktop and 390-pixel phone workflows in
a browser. Do not keep Procurement running beside the product merely as
documentation; the canonical repository and pinned commit are the reference.

Runtime topology is external input. Consume the typed same-origin
`__NIE_APPLICATION_CONFIG__` semantic service map, retain infrastructure-neutral
standalone fallbacks, and test unauthenticated Main entry through Auth and back to
the original validated deep link. Never derive a Coder/workspace/ingress path from
`window.location`.

When a dependency is needed, first determine whether the platform or an already-approved package provides the capability. Then compare official and leading open-source candidates under `NIE-DEPS-*`. Keep any external vendor SDK in infrastructure, behind an application-owned or open ecosystem abstraction, and add boundary contract tests. Do not add multiple competing packages for the same concern.

## 5. Test and inspect

Use the evidence named by each rule. At minimum, run all affected standard gates:

- C#: `dotnet format` verification, warnings-as-errors build with Roslyn analyzers, xUnit/architecture tests, PostgreSQL integration tests when provider behavior matters, and changed-code coverage review.
- Vue/TypeScript: ESLint, `vue-tsc`, Vitest unit/component tests, and Playwright for access, routing, responsive UI, and critical workflows.
- Security/dependencies: the repository's maintained dependency, secret, and static-analysis workflows, including CodeQL or an equivalent platform scanner.
- Delivery: artifact build, migration review, configuration validation, and health/readiness checks when affected.

A file search can support evidence but cannot prove behavior. Compilation can support type and analyzer claims but cannot prove authorization, audit, retention, user experience, or security outcomes.

## 6. Implementer report

Complete `REPORT.md` with rule-by-rule verdicts, file/line evidence, test commands and results, coverage, common-code decisions, exceptions, and residual risk. A failed required rule means the work is not complete.

## 7. Independent AI verification

For material work, a separate AI agent receives the request, acceptance criteria, relevant rule files, final diff, and implementer report, but must independently inspect the source and tests. It must:

1. challenge applicability and not-applicable claims;
2. verify the change did not weaken required libraries or controls;
3. inspect authorization, validation, audit, data, error, and failure paths;
4. independently verify every added dependency's publisher, source repository, license, maintenance/security posture, adoption evidence, abstraction boundary, transitive risk, and exit plan;
5. rerun risk-relevant deterministic checks;
6. identify missing tests or misleading evidence;
7. issue pass, fail, manual-review, or approved-exception for each challenged rule and an overall verdict.

High-risk authentication, authorization, uploads, outbound calls, cryptography, secrets, and personal-data changes require an explicitly security-focused independent review.

## 8. Advance the pin

Update `.nie-template-version.json` only after the selected canonical changes and affected rules have passed. Record the exact canonical commit, rules version, verification date, and evidence summary. Do not mark the application aligned merely because its `.ai` files match.
