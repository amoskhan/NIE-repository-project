# Guide: Creating Design Specification & API Documentation

> **This is a GUIDE.** Each project creates its own `docs/design-spec.md` and `docs/api-spec.yaml` with project-specific low-level design and API contracts. This document explains HOW to create them.

---

## Purpose

The design specification bridges the gap between architecture (what components exist) and implementation (how they work internally). The API specification defines the contract between frontend and backend. AI agents use these to generate controllers, services, DTOs, and frontend API clients.

## When to Create

- During Phase 1.2 (Technical Design) of AIDLC
- After data model and architecture are defined
- Before Phase 2 (Construction) begins

## Format

- Design spec in Markdown with Mermaid.js diagrams
- API spec in OpenAPI 3.0 YAML (or Markdown table format)

---

# Part 1: Design Specification

## How to Create the Design Spec

### Step 1: Define Service Layer Design

```markdown
# Design Specification

## Service Layer

### Service: [EntityName]Service

Extends: `BaseService<EntityName>`

| Method          | Signature                                                      | Description                                |
| --------------- | -------------------------------------------------------------- | ------------------------------------------ |
| GetAllAsync     | `Task<ApiResponse<List<EntityViewDto>>>`                       | List all active entities with related data |
| GetByIdAsync    | `Task<ApiResponse<EntityViewDto>> (Guid id)`                   | Get single entity with full details        |
| CreateAsync     | `Task<ApiResponse<EntityViewDto>> (EntityCreateDto dto)`       | Create new entity                          |
| EditAsync       | `Task<ApiResponse<EntityViewDto>> (Guid id, EntityEditDto dto)` | Update existing entity                     |
| DeleteAsync     | `Task<ApiResponse<bool>> (Guid id)`                            | Soft delete (set IsActive = false)         |
| [Custom method] | `Task<ApiResponse<T>> (params)`                                | [Business logic description]               |

#### Business Logic Notes:

- [CreateAsync]: Auto-set Status to "Draft" on creation
- [EditAsync]: Can only edit if Status is "Draft" or "Returned"
- [DeleteAsync]: Cannot delete if Status is "Approved"
- [Custom]: [Describe any non-standard logic]
```

### Step 2: Define DTO Specifications

```markdown
## Data Transfer Objects

### EntityCreateDto

| Field       | Type      | Required | Validation               | Notes |
| ----------- | --------- | -------- | ------------------------ | ----- |
| Name        | string    | Yes      | MaxLength(200), NotEmpty |       |
| Description | string    | No       | MaxLength(2000)          |       |
| CategoryId  | Guid      | Yes      | Non-empty; must exist in Code table |       |
| StartDate   | DateTime? | No       | Must be future date      |       |
| EndDate     | DateTime? | No       | Must be > StartDate      |       |

### EntityEditDto

Same as CreateDto plus:
| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Id | Guid | Yes | Non-empty UUID; must exist | From route param |

### EntityViewDto

| Field        | Type      | Source                  | Notes                |
| ------------ | --------- | ----------------------- | -------------------- |
| Id           | Guid      | Entity.Id               | UUIDv7               |
| Name         | string    | Entity.Name             |                      |
| Description  | string    | Entity.Description      |                      |
| CategoryId   | Guid      | Entity.CategoryId       |                      |
| CategoryName | string    | Entity.Category.Value   | From Code table join |
| Status       | string    | Entity.Status           |                      |
| StatusName   | string    | Entity.StatusCode.Value | From Code table join |
| CreatedBy    | string    | Entity.CreatedBy        |                      |
| CreatedAt    | DateTime  | Entity.CreatedAt        |                      |
| UpdatedAt    | DateTime? | Entity.UpdatedAt        |                      |

### Mapster Configuration

\`\`\`csharp
// In MappingConfig.cs
TypeAdapterConfig<Entity, EntityViewDto>.NewConfig()
.Map(dest => dest.CategoryName, src => src.Category.Value)
.Map(dest => dest.StatusName, src => src.StatusCode.Value);

TypeAdapterConfig<EntityCreateDto, Entity>.NewConfig()
.Ignore(dest => dest.Id)
.Ignore(dest => dest.CreatedAt)
.Ignore(dest => dest.CreatedBy);
\`\`\`
```

### Step 3: Define Frontend Component Design

```markdown
## Frontend Components

### Page: Entity List

| Component       | Type      | Data Source       | Actions                    |
| --------------- | --------- | ----------------- | -------------------------- |
| EntityListPage  | Page      | GET /api/Entity   | Navigate to Create/Edit    |
| EntityTable     | Component | Props from parent | Sort, Filter, Paginate     |
| EntityFilterBar | Component | Local state       | Emit filter changes        |
| EntityRow       | Component | Single entity     | View, Edit, Delete buttons |

### Page: Entity Form (Create/Edit)

| Component        | Type      | Data Source                 | Actions               |
| ---------------- | --------- | --------------------------- | --------------------- |
| EntityFormPage   | Page      | GET /api/Entity/{id} (edit) | Submit, Cancel        |
| EntityForm       | Component | Props or empty state        | Validate, Emit submit |
| CategoryDropdown | Component | GET /api/Code?type=Category | Select category       |
| DateRangePicker  | Component | v-model                     | Set dates             |

### State Management

\`\`\`mermaid
flowchart TD
A[Route /entities] --> B[EntityListPage]
B --> C[onMounted: fetch entities]
C --> D[Store entities in ref]
D --> E[Render EntityTable]

    F[Route /entities/create] --> G[EntityFormPage]
    G --> H[Initialize empty form]
    H --> I[Fetch code table data]
    I --> J[Render EntityForm]
    J --> K[User submits]
    K --> L[POST /api/Entity]
    L --> M{Success?}
    M -->|Yes| N[Navigate to list + toast]
    M -->|No| O[Show errors on form]

\`\`\`
```

---

# Part 2: API Specification

## Option A: OpenAPI YAML Format

Create `docs/api-spec.yaml`:

```yaml
openapi: 3.0.3
info:
  title: "[Project Name] API"
  version: "1.0.0"
  description: "API specification for [Project Name]"

servers:
  - url: http://localhost:5002
    description: Development
  - url: https://production-url/api
    description: Production

paths:
  /api/Entity:
    get:
      summary: List all entities
      tags: [Entity]
      security:
        - sessionAuth: []
      parameters:
        - name: search
          in: query
          schema: { type: string }
        - name: categoryId
          in: query
          schema: { type: string, format: uuid }
      responses:
        "200":
          description: Success
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/ApiResponseEntityList"
        "401":
          description: Unauthorized

    post:
      summary: Create entity
      tags: [Entity]
      security:
        - sessionAuth: []
      requestBody:
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/EntityCreateDto"
      responses:
        "200":
          description: Created
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/ApiResponseEntity"
        "400":
          description: Validation error

  /api/Entity/{id}:
    get:
      summary: Get entity by ID
      tags: [Entity]
      parameters:
        - name: id
          in: path
          required: true
          schema: { type: string, format: uuid }
      responses:
        "200":
          description: Success
        "404":
          description: Not found

    put:
      summary: Update entity
      tags: [Entity]
      parameters:
        - name: id
          in: path
          required: true
          schema: { type: string, format: uuid }
      requestBody:
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/EntityEditDto"
      responses:
        "200":
          description: Updated
        "404":
          description: Not found

    delete:
      summary: Soft delete entity
      tags: [Entity]
      parameters:
        - name: id
          in: path
          required: true
          schema: { type: string, format: uuid }
      responses:
        "200":
          description: Deleted
        "404":
          description: Not found

components:
  securitySchemes:
    sessionAuth:
      type: apiKey
      in: header
      name: X-Session-Id

  schemas:
    ApiResponseEntityList:
      type: object
      properties:
        succeeded: { type: boolean }
        data:
          type: array
          items:
            $ref: "#/components/schemas/EntityViewDto"
        message: { type: string }

    EntityCreateDto:
      type: object
      required: [name, categoryId]
      properties:
        name: { type: string, maxLength: 200 }
        description: { type: string, maxLength: 2000 }
        categoryId: { type: string, format: uuid }

    EntityViewDto:
      type: object
      properties:
        id: { type: string, format: uuid }
        name: { type: string }
        description: { type: string }
        categoryId: { type: string, format: uuid }
        categoryName: { type: string }
        createdBy: { type: string }
        createdAt: { type: string, format: date-time }
```

## Option B: Markdown Table Format

If you prefer simpler documentation:

```markdown
## API Endpoints

### Entity Endpoints

| Method | Path             | Request Body    | Response              | Auth | Permission    |
| ------ | ---------------- | --------------- | --------------------- | ---- | ------------- |
| GET    | /api/Entity      | -               | ApiResponse<Entity[]> | Yes  | entity.view   |
| GET    | /api/Entity/{id} | -               | ApiResponse<Entity>   | Yes  | entity.view   |
| POST   | /api/Entity      | EntityCreateDto | ApiResponse<Entity>   | Yes  | entity.create |
| PUT    | /api/Entity/{id} | EntityEditDto   | ApiResponse<Entity>   | Yes  | entity.edit   |
| DELETE | /api/Entity/{id} | -               | ApiResponse<bool>     | Yes  | entity.delete |
```

## Tips

1. **Design spec before coding** — It's cheaper to fix design mistakes in a document than in code
2. **DTOs are the contract** — Define them clearly, AI agents will generate matching code
3. **Mapster config is critical** — Document any non-trivial mappings
4. **Frontend component tree** — List pages and their child components before building UI
5. **OpenAPI YAML/JSON** is useful if you want to auto-generate API clients or publish API reference docs
6. **Markdown tables** are easier for AI agents to read and use during code generation

## Review Checklist

- [ ] Service methods defined with signatures and return types
- [ ] All DTOs specified (Create, Edit, View) with field types and validation
- [ ] Mapster configuration noted for complex mappings
- [ ] Frontend page structure and component hierarchy documented
- [ ] API endpoints listed with methods, paths, and auth requirements
- [ ] Request/response schemas defined
- [ ] Business logic rules documented per service method
