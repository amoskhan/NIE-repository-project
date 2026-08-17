# Data Model

> **Fill this in for your project.** This file ships as a stub. It captures _your_ entities, relationships, and lifecycle rules — the ones you add on top of the template's built-in tables (users, roles, access functions, codes, documents, audit logs, workflow state).
>
> Step-by-step help: [`templates/data-model-guide.md`](templates/data-model-guide.md).
> A worked example of the patterns lives in the procurement sample under `src/backend/Libraries/Domain/Models/Samples/`.

## Minimum Contents

- Core entities and what each one is for
- Relationship summary (an entity-relationship diagram in Mermaid)
- Required and optional fields
- State or lifecycle fields, and the transitions allowed between them
- Indexing, uniqueness, and retention constraints

## Suggested skeleton

```markdown
## Entities

### <EntityName>

| Field | Type | Required | Notes |
| ----- | ---- | -------- | ----- |

Purpose: one sentence.
Owner: who may read and write it.
Lifecycle: created when..., archived/deleted when...

## Relationships

<mermaid erDiagram>

## Constraints

| Constraint | Entity | Reason |
| ---------- | ------ | ------ |

## Retention

What gets deleted, after how long, and by which job.
```

## Conventions this template expects

- Entities live in `src/backend/Libraries/Domain/Models/` and contain nothing but entity types.
- Inherit `TimestampedEntity` when you want `CreatedOn` / `CreatedBy` / `UpdatedOn` / `UpdatedBy`; inherit `BaseEntity` only when you deliberately do not.
- Every schema change is an EF Core migration. Never edit the database by hand.
- Anything a user picks from a dropdown of fixed values belongs in the code tables, not in a hardcoded enum on the frontend.

## Update When

- A domain entity is added, removed, or renamed
- Relationship rules or ownership change
- Validation or retention rules change
