# Design Spec

> **Fill this in for your project.** This file ships as a stub. It is the implementation-oriented design that sits between your requirements and your code: what services exist, what they return, and which screens consume them.
>
> Step-by-step help: [`templates/design-spec-guide.md`](templates/design-spec-guide.md).
> The end-to-end walkthrough in [`GETTING-STARTED.md`](GETTING-STARTED.md#part-2--build-your-first-feature) shows the layers this document describes.

## Minimum Contents

- Service boundaries and responsibilities
- DTO contracts and mapping notes
- API endpoints and the screens that call them
- Shared component usage decisions
- Validation, error-handling, and dependency notes

## Suggested skeleton

```markdown
## Feature: <name>

### Services

| Service | Responsibility | Depends on |
| ------- | -------------- | ---------- |

### API

| Endpoint | Method | Access function | Request | Response |
| -------- | ------ | --------------- | ------- | -------- |

### DTOs

Which fields are exposed, which are deliberately not, and any mapping that is not a straight name match.

### Screens

| Route | Page component | Permission | Calls |
| ----- | -------------- | ---------- | ----- |

### Validation

Where each rule is enforced. Server-side is mandatory; client-side is a convenience.

### Open decisions
```

## Conventions this template expects

- Controllers handle HTTP only. Business logic goes in a service under `src/backend/Libraries/Services/Services/`.
- DTOs live in `src/backend/Libraries/Shared/Dto/`. Entities are never returned from a controller.
- Every endpoint declares an access function; the frontend mirrors the code in `app-config/accessFunctions.ts`.
- Screens use components from `@apptemplate/ui` rather than bespoke markup.
- Routes and navigation entries go in `src/frontend/main/src/app-config/`, never in the shell components.

## Update When

- A feature adds new DTOs, services, or endpoints
- A UI flow needs a new shared pattern
- The implementation diverges from an earlier assumption — record what changed and why
