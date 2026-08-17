# [Title of Decision]

## Metadata

- **Date:** YYYY-MM-DD
- **Status:** Proposed | Accepted | Deprecated | Superseded
- **Deciders:** [Team members involved]
- **AI Model Used:** [e.g., Claude Opus 4, GPT-4o, Claude Sonnet]

## Context

Why did we need to make this decision? What problem were we trying to solve?

## Options Considered

### Option A: [Name]

**Description:** [What this option involves]

- **Pros:** [Benefits]
- **Cons:** [Drawbacks]

### Option B: [Name]

**Description:** [What this option involves]

- **Pros:** [Benefits]
- **Cons:** [Drawbacks]

### Option C: [Name] _(optional)_

**Description:** [What this option involves]

- **Pros:** [Benefits]
- **Cons:** [Drawbacks]

## Decision

What did we choose and why?

## Consequences

- **Positive:** [Expected benefits]
- **Negative:** [Known tradeoffs]
- **Risks:** [What could go wrong]

## AI Reasoning Chain

> Copy/paste the AI's full reasoning here for traceability.
> This preserves the "why" even when team members change.

---

## When to Create a Decision Record

Create a new ADR in `.ai/adrs/` when any of these occur:

- Choosing between two or more technologies, libraries, or frameworks
- Selecting a design pattern (e.g., CQRS vs simple CRUD)
- Deciding on data storage approach (embed vs reference, SQL vs NoSQL)
- Changing an existing architectural decision
- Adding a new external dependency
- Deviating from an existing specification
- Making a performance vs simplicity tradeoff

## Naming Convention

Files should be named: `NNN-short-description.md`

Take the next free number. Numbers are never reused, even when an ADR is removed — a gap in the sequence is normal.

Examples:

- `005-chose-postgresql-over-sqlserver.md`
- `006-valkey-session-management.md`
- `007-mapster-over-automapper.md`
