# 05 — AIDLC Process (AI-Driven Software Development Life Cycle)

This is the canonical lightweight process that App Template and every project derived from it follows.

```
Phase 1: INCEPTION ──→ Phase 2: CONSTRUCTION ──→ Phase 3: OPERATIONS
   (Specs & Context)      (Build Loop)              (Validate & Ship)
         ↑                      ↻                          │
         └──────────────────────┴──────────────────────────┘
                          Feedback Loop
```

| Phase            | Focus                | Goal                           | Primary artifacts                                                                       |
| ---------------- | -------------------- | ------------------------------ | --------------------------------------------------------------------------------------- |
| 1 — Inception    | Context & definition | Turn ideas into concrete specs | `requirements/`, `docs/architecture.md`, `docs/data-model.md`, `docs/security-model.md` |
| 2 — Construction | Build loop           | Spec-driven, AI-assisted dev   | `src/`, `tests/`, `.ai/features/<feature>/`, `.ai/adrs/`                                |
| 3 — Operations   | Validate & maintain  | CI/CD, audits, doc updates     | `build/`, `tests/`, `.ai/tasks/`                                                        |

## Mandatory rules

| Rule                 | Description                                                                                        |
| -------------------- | -------------------------------------------------------------------------------------------------- |
| Specification first  | No code without reading the matching feature dossier or spec.                                      |
| Steering enforcement | Every prompt to an AI agent includes the agent's tool-route file and the relevant feature dossier. |
| Memory persistence   | Every architectural decision goes into `.ai/adrs/NNN-*.md`.                                        |
| Spec compliance      | When code conflicts with spec, fix the code or write an ADR superseding the spec.                  |
| No YAGNI             | Don't implement unspecified features.                                                              |
| Enum-first state     | Every status / state / type / category lives in an enum on BOTH backend and frontend.              |

## Quality gates

| Gate                      | Check                                                                             |
| ------------------------- | --------------------------------------------------------------------------------- |
| Inception → Construction  | Required spec docs exist for the feature being built                              |
| Per-feature completion    | Tests pass, dossier exists, no enum strings, ADR for tradeoffs, docs updated      |
| Construction → Deployment | CI green, security scan clean (`08-security-owasp-top10-2025.md`), version bumped |

## Decision log

When you make a non-trivial architectural decision: create `.ai/adrs/NNN-short-title.md` from `.ai/adrs/TEMPLATE.md`. Triggers:

- Choosing between technologies, libraries, or patterns
- Selecting a design pattern (CQRS vs CRUD, etc.)
- Adding a new external dependency
- Deviating from an existing spec
- Performance vs simplicity tradeoffs

## Documentation-as-code

| Format       | Use cases                                       |
| ------------ | ----------------------------------------------- |
| Markdown     | Requirements, design specs, decisions, dossiers |
| Mermaid.js   | Architecture (C4), ER diagrams, state machines  |
| OpenAPI YAML | API contracts (when needed)                     |
| JSON Schema  | Configuration validation                        |

**Forbidden for primary specs:** PNG, JPEG, PDF, Visio, Word. AI agents cannot parse them.
