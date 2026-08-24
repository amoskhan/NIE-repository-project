# NIE AI application contract

The canonical source for these instructions is `https://niegithub.nie.edu.sg/NIE/nie-template`. This directory intentionally contains Markdown rules only. It does not contain migration tasks, ledgers, policy engines, or custom AI-verdict scripts.

Standard compilers, linters, analyzers, tests, coverage collectors, dependency scanners, and CodeQL provide deterministic evidence. AI agents interpret the rules, implement the work, inspect behavior and context, and issue evidence-backed verdicts.

## Required reading order

For every material change, read:

1. Root `AGENTS.md`.
2. This file and `WORKFLOW.md`.
3. `APPLICATION.md` for application-specific scope and decisions.
4. `GLOBAL-RULES.md` and `LIBRARIES.md`.
5. Every `FEATURE-*.md` affected directly or indirectly by the change.
6. Relevant source, tests, architecture documentation, and the Procurement sample when a real vertical example is useful.

Do not claim compliance from this reading alone. Inspect and test the implementation.

## Canonical files

| File | Purpose |
| --- | --- |
| `GLOBAL-RULES.md` | Global architecture, structure, security, UUIDv7, testing, AI workflow, shared-code, and evidence rules |
| `LIBRARIES.md` | Minimum supported platform, NuGet, npm, and service versions |
| `FEATURE-*.md` | Per-feature adoption, menu, libraries, backend, frontend, data, security, reference paths, and verification rules |
| `WORKFLOW.md` | Exact implementer, update, and independent-verifier process |
| `REPORT.md` | Required evidence report format |
| `APPLICATION.md` | Application-owned product context, adopted-feature decisions, exceptions, and additional constraints |

## Non-negotiable boundaries

- In derived applications, canonical rule files are read-only. Only `APPLICATION.md` is application-owned.
- Application guidance may strengthen rules but never weaken them.
- Never reveal, inspect, print, copy, transform, or summarize secrets, tokens, credentials, credential files, or secret-bearing configuration.
- Never remove or replace required libraries, access controls, validation, audit, tests, or gates merely because custom code is easier for an AI to produce.
- Select new dependencies under the `NIE-DEPS-*` rules: Microsoft/platform packages first for .NET platform capabilities, official project packages next, then mature leading open source. Popularity supports a decision but never overrides security, maintenance, license, interoperability, or exit-path requirements.
- Never overwrite a customized application source tree with a template folder. Compare and merge deliberately.
- Never rename the stable generic source folders, assemblies, namespaces, Vue app paths, or `@nie/*` package identities to the product name. Product identity is configuration and branding, not a source-tree fork.
- Procurement is an example to learn from, not a runtime dependency or a universal domain model.
- Derived applications must record an owned Procurement `remove` or `retain` decision in `.ai/APPLICATION.md` and the active specification profile. They must not expose `NIE Template` or append the real product beside the active sample.
- Product screens reuse the reference shell, components, states, and responsive design through a documented `ui/reference-patterns.md` mapping; Procurement labels, data, routes, permissions, and workflows do not transfer.
- Frontend topology comes from the typed same-origin `__NIE_APPLICATION_CONFIG__` semantic service map. Application code never constructs Coder, workspace, ingress, or cluster paths.

## Outcome

A change is complete only when the implementing AI has made the in-scope changes, added appropriate tests, run standard gates, produced the rule-by-rule report, and a separate AI verifier has issued an independent verdict for material work.
