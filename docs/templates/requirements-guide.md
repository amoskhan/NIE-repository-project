# Guide: Creating Requirements Documentation

> **This is a GUIDE.** Each project creates its own `requirements/` folder with project-specific content. This document explains HOW to create it.

---

## Purpose

The requirements documentation captures WHAT the application should do, WHO uses it, and WHY it exists — before any technical design or coding begins. This feeds directly into Phase 1.2 (Technical Design).

## When to Create

- At the very start of a new project (Phase 1.1)
- When a major new module is added to an existing project
- When modernizing a legacy application

## How to Create

### Option A: AI-Assisted (Recommended)

1. Open `.ai/README.md`, then use `.ai/IMPLEMENT.md` with the objective and resolve applicable numbered policy rules before implementation.
2. Provide your 1–3 paragraph project description
3. AI will ask discovery questions — answer them thoroughly
4. AI generates the full `requirements/` folder structure
5. Review with stakeholders and refine

### Option B: Manual

Create the `requirements/` folder at repository root with these files:

## Required Files

### `requirements/README.md`

```markdown
# [Project Name] — Requirements

## Business Purpose

[1-2 paragraphs: What does this application do and why is it needed?]

## Scope

### In Scope

- [Feature 1]
- [Feature 2]

### Out of Scope

- [Explicitly excluded item 1]
- [Explicitly excluded item 2]

## Users

[Who will use this application: NIE staff, students, external parties?]

## Key Entities

| Entity | Description          | Estimated Records   |
| ------ | -------------------- | ------------------- |
| [name] | [what it represents] | [approximate count] |

## Key Workflows

1. [Workflow name] — [brief description]
2. [Workflow name] — [brief description]

## Technology Stack

- Backend: .NET 10 (NIE Template)
- Frontend: Vue 3 + TypeScript
- Database: PostgreSQL
- Cache: Valkey
- Auth: Session-based (NIE Template)
```

### `requirements/personas.md`

Define every user role with their permissions:

```markdown
# User Personas

## Persona: [Role Name]

**Who**: [Description]
**Count**: [Approximate number of users]
**Goals**:

- [Goal 1]
- [Goal 2]

**Permissions**:
| Action | Allowed |
|--------|---------|
| View all records | Yes/No |
| Create records | Yes/No |
| Edit own records | Yes/No |
| Edit all records | Yes/No |
| Delete records | Yes/No |
| Access admin panel | Yes/No |
| Export data | Yes/No |
```

### `requirements/use-cases.md`

Write one use case per major user workflow:

```markdown
## UC-001: [Use Case Title]

**Actor**: [Role]
**Priority**: Must Have / Should Have / Nice to Have
**Preconditions**: [What must be true before this starts]

**Main Flow**:

1. [Step 1]
2. [Step 2]
3. [Step 3]

**Alternative Flows**:

- [Alt flow]

**Exception Flows**:

- [Error scenario and handling]

**Postconditions**: [System state after completion]
**Business Rules Applied**: BR-001, BR-002
**Test Cases**: TC-001, TC-002
```

### `requirements/entity-model.md`

Define all business entities with fields and relationships:

```markdown
## Entity: [Name]

**Description**: [What it represents]
**Base Class**: TimestampedEntity

### Fields

| Field  | Type                | Required | Max Length | Description    |
| ------ | ------------------- | -------- | ---------- | -------------- |
| Name   | string              | Yes      | 200        | [description]  |
| Status | string (Code table) | Yes      | -          | Current status |

### Relationships

| Related Entity | Type | Description   |
| -------------- | ---- | ------------- |
| [Entity]       | 1:N  | [description] |

## Entity Relationship Diagram

(Use Mermaid.js — see `docs/templates/data-model-guide.md`)
```

### `requirements/business-rules.md`

Document every validation, calculation, and workflow rule:

```markdown
## BR-001: [Rule Name]

**Category**: Validation / Calculation / Workflow / Security
**Entities Affected**: [list]
**Trigger**: On Save / On Field Change / Scheduled

### Description

[What the rule does in plain English]

### Logic

IF [condition]
THEN [action]
ELSE [alternative]

### Acceptance Criteria

- **Given** [precondition]
- **When** [action]
- **Then** [expected result]
```

### `requirements/screen-wireframes.md`

Define every screen the user will see:

```markdown
## Navigation Structure

Sidebar:
├── Dashboard
├── [Module 1]
│ ├── [Screen 1]
│ └── [Screen 2]
└── Admin (admin only)

## Screen: [Name]

**Route**: /[path]
**Type**: List / Form / Dashboard
**Access**: [roles]

### For List Screens

| Column  | Source       | Sortable | Filterable |
| ------- | ------------ | -------- | ---------- |
| [label] | entity.field | Yes/No   | Yes/No     |

### For Form Screens

| Field | Label | Component | Required | Validation     |
| ----- | ----- | --------- | -------- | -------------- |
| name  | Name  | NieInput  | Yes      | maxLength: 200 |
```

### `requirements/data-flow.md`

Document how data moves for major operations:

```markdown
## DF-001: [Operation Name]

**Trigger**: [user action / scheduled / external event]

### Flow

Input → [source]
↓
Validation → [what's validated]
↓
Processing → [business logic]
↓
Storage → [where data persists]
↓
Output → [what's returned/displayed]
↓
Side Effects → [notifications, audit logs, cache updates]
```

### `requirements/test-cases.md`

Write test cases for every use case and business rule:

```markdown
## TC-001: [Test Case Title]

**Type**: API / E2E
**Priority**: Critical / High / Medium / Low
**Related**: UC-001, BR-001

### Steps

| Step | Action   | Expected Result |
| ---- | -------- | --------------- |
| 1    | [action] | [result]        |
| 2    | [action] | [result]        |
```

### `requirements/non-functional.md`

Define performance, security, accessibility requirements:

```markdown
## Performance

- Page load: < 2 seconds
- API response: < 500ms for standard CRUD

## Security

- Session-based auth (NIE Template)
- Role-based access control
- Input sanitization on all forms

## Accessibility

- Keyboard navigation support
- Screen reader friendly labels

## Browser Support

- Chrome, Firefox, Edge, Safari (latest 2 versions)
```

### `requirements/integrations.md` _(if applicable)_

Document any external system connections.

## Review Checklist

Before proceeding to Phase 1.2:

- [ ] All user roles are defined in `personas.md`
- [ ] All major workflows have use cases
- [ ] All entities have field definitions
- [ ] Business rules have acceptance criteria
- [ ] Screen list covers all user needs
- [ ] Test cases exist for every use case
- [ ] Non-functional requirements are specified
- [ ] Stakeholders have reviewed and approved

