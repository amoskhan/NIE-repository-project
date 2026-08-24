# Guide: Creating Data Model Documentation

> **This is a GUIDE.** Each project creates its own `docs/data-model.md` with project-specific entity diagrams. This document explains HOW to create it.

---

## Purpose

The data model document defines all database entities, their relationships, and field specifications. AI agents use this to generate EF Core models, migrations, DTOs, and services.

## When to Create

- During Phase 1.2 (Technical Design) of AIDLC
- When adding new entities or changing relationships
- Before any database migration

## Format

- Entity Relationship diagrams in **Mermaid.js erDiagram** syntax
- Field specifications in Markdown tables
- All entities must follow NIE Template conventions

## How to Create

### Step 1: List Your Entities

Start by listing all entities your project needs. Every entity extends `TimestampedEntity`:

```csharp
// All entities inherit from TimestampedEntity which provides:
// - Id (Guid, initialized with the canonical UUIDv7 factory)
// - CreatedAt (DateTime)
// - CreatedBy (string)
// - UpdatedAt (DateTime?)
// - UpdatedBy (string?)
// - IsActive (bool, default true)
```

### Step 2: Create the ER Diagram

```markdown
# Data Model

## Entity Relationship Diagram

\`\`\`mermaid
erDiagram
%% NIE Template base entities (always present)
AppUser {
uuid Id PK
string UserName
string FirstName
string LastName
string Email
string DesignationId FK
bool IsActive
}

    Code {
        uuid Id PK
        int CodeType "ECodeType enum"
        int CodeName "ECodeName enum"
        string Value
        string Description
        int SortOrder
        bool IsActive
    }

    %% Add your project entities below:
    YourEntity {
        uuid Id PK
        string Name
        string Description
        uuid CategoryId FK
        datetime CreatedAt
        string CreatedBy
        bool IsActive
    }

    %% Define relationships:
    AppUser ||--o{ YourEntity : "creates"
    Code ||--o{ YourEntity : "categorizes"

\`\`\`
```

### Step 3: Define Field Specifications

For each entity, create a field table:

```markdown
## Entity: YourEntity

| Field        | Type      | Required | Max Length | Default         | Notes                     |
| ------------ | --------- | -------- | ---------- | --------------- | ------------------------- |
| Id           | Guid      | Yes      | -          | UUIDv7 factory  | PK from TimestampedEntity |
| Name         | string    | Yes      | 200        | -               | Display name              |
| Description  | string    | No       | 2000       | null            | Rich text                 |
| Status       | ECodeType | Yes      | -          | Active          | Code table reference      |
| CategoryId   | Guid      | Yes      | -          | -               | FK to Code table          |
| AssignedToId | Guid?     | No       | -          | null            | FK to AppUser             |
| StartDate    | DateTime  | No       | -          | null            |                           |
| EndDate      | DateTime  | No       | -          | null            | Must be > StartDate       |
| IsActive     | bool      | Yes      | -          | true            | Soft delete flag          |
| CreatedAt    | DateTime  | Yes      | -          | DateTime.UtcNow | From TimestampedEntity    |
| CreatedBy    | string    | Yes      | 100        | Current user    | From TimestampedEntity    |
| UpdatedAt    | DateTime? | No       | -          | null            | From TimestampedEntity    |
| UpdatedBy    | string?   | No       | -          | null            | From TimestampedEntity    |
```

### Step 4: Define Relationships Table

```markdown
## Relationships

| Parent Entity | Child Entity    | Type        | FK Column    | Cascade Delete | Notes                |
| ------------- | --------------- | ----------- | ------------ | -------------- | -------------------- |
| AppUser       | YourEntity      | One-to-Many | AssignedToId | No             | Nullable FK          |
| Code          | YourEntity      | One-to-Many | CategoryId   | No             | [Category] code type |
| YourEntity    | YourChildEntity | One-to-Many | YourEntityId | Yes            | Required FK          |
```

### Step 5: Document Code Table Usage

```markdown
## Code Table References

This project uses the following code types (add to `ECodeType` and `ECodeName` enums):

| ECodeType          | ECodeName                                        | Example Values         | Used By               |
| ------------------ | ------------------------------------------------ | ---------------------- | --------------------- |
| YourEntityCategory | YourEntityCategoryA, YourEntityCategoryB         | "Research", "Teaching" | YourEntity.CategoryId |
| YourEntityStatus   | YourEntityStatusActive, YourEntityStatusInactive | "Active", "Inactive"   | YourEntity.Status     |
```

### Step 6: Define Indexes and Constraints

```markdown
## Indexes

| Entity     | Columns              | Type       | Name                          |
| ---------- | -------------------- | ---------- | ----------------------------- |
| YourEntity | Name                 | Unique     | IX_YourEntity_Name            |
| YourEntity | CategoryId, IsActive | Non-Unique | IX_YourEntity_Category_Active |

## Business Constraints

- YourEntity.EndDate must be greater than StartDate (enforced in Service layer)
- YourEntity.Name must be unique within the same Category (enforced via unique index)
- Deleting a Code entry used by YourEntity is prevented (no cascade)
```

## NIE Template Entity Conventions

1. **Always** extend `TimestampedEntity`
2. **Always** use non-empty UUIDv7 application primary keys (`Guid` in .NET, `uuid` in PostgreSQL, canonical strings in Vue); integer/identity/serial keys and UUIDv4 are not allowed
3. **Always** carry foreign keys as `Guid`/`Guid?` and never coerce frontend ID strings to numbers
4. **Always** include `IsActive` for soft deletes
5. **Use Code tables** for dropdowns/lookups — never hardcode enum values in entities
6. **Navigation properties** use virtual keyword
7. **FK properties** named `{RelatedEntity}Id`
8. **DbContext** registers entities in `MainDbContext`
9. **Migrations** created via `dotnet ef migrations add {Name}` and reviewed to ensure every identifier column is PostgreSQL `uuid`

## Tips

- Draw the ER diagram FIRST — it forces you to think about relationships before implementation
- Use **AI assistance**: paste your requirements doc and ask the AI to suggest an ER diagram
- Keep entities focused — if an entity has >15 fields, consider splitting it
- Always document which Code Types your project adds

## Review Checklist

- [ ] All entities extend TimestampedEntity
- [ ] Every primary/foreign key is Guid/UUIDv7 and generated migration identifier column is PostgreSQL uuid
- [ ] ER diagram in Mermaid.js shows all entities and relationships
- [ ] Field specifications include type, required, max length, default
- [ ] Code table usage documented with ECodeType/ECodeName values
- [ ] Relationship table shows cascade behavior
- [ ] Indexes defined for common query patterns
- [ ] Business constraints documented
