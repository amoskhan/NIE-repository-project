# Guide: Creating Data Flow Documentation

> **This is a GUIDE.** Each project creates its own `docs/data-flow.md` with project-specific data flow diagrams. This document explains HOW to create it.

---

## Purpose

The data flow document traces how data moves through the system â€” from user input, through API layers, services, database, and back. AI agents use this to understand the full request lifecycle and generate correct service/controller code.

## When to Create

- During Phase 1.2 (Technical Design) of AIDLC
- When implementing complex multi-step business processes
- When integrating external systems

## Format

Use **Mermaid.js** for all diagrams. Prefer `flowchart` for data flows and `sequenceDiagram` for request lifecycles.

## How to Create

### Step 1: Document the Standard NIE Template Data Flow

Every project shares this baseline. Include it in your `docs/data-flow.md`:

```markdown
# Data Flow

## Standard Request Lifecycle

\`\`\`mermaid
flowchart TD
User[ðŸ‘¤ User Browser] -->|HTTP Request| Nginx[nginx Reverse Proxy]
Nginx -->|/api/_| API[Main API :5002]
Nginx -->|/auth/_| Auth[Auth API :5001]

    subgraph APILayer["API Layer"]
        API --> MW[Middleware Pipeline]
        MW --> SessionAuth[Session Authentication]
        SessionAuth --> PermCheck[Permission Check]
        PermCheck --> Controller[Controller Action]
    end

    subgraph ServiceLayer["Service Layer"]
        Controller --> Service[BaseService&lt;T&gt;]
        Service --> Validation[Business Validation]
        Validation --> DataAccess[EF Core Query]
    end

    subgraph DataLayer["Data Layer"]
        DataAccess --> DbContext[MainDbContext]
        DbContext --> PG[(PostgreSQL)]
    end

    PG --> DbContext
    DbContext --> Service
    Service -->|ApiResponse&lt;T&gt;| Controller
    Controller -->|JSON Response| User

\`\`\`
```

### Step 2: Document Feature-Specific Data Flows

For each major feature, create a data flow diagram:

```markdown
## Feature: [Feature Name]

### Create Flow

\`\`\`mermaid
flowchart TD
A[User fills form] --> B[Frontend validates]
B --> C{Valid?}
C -->|No| D[Show validation errors]
C -->|Yes| E[POST /api/Entity]
E --> F[Controller receives CreateDto]
F --> G[Service.CreateAsync]
G --> H[Map DTO to Entity via Mapster]
H --> I[Set CreatedBy, CreatedAt]
I --> J[DbContext.Add]
J --> K[SaveChangesAsync]
K --> L{Success?}
L -->|Yes| M[Return ApiResponse.Success]
L -->|No| N[Return ApiResponse.Error]
M --> O[Frontend shows success toast]
N --> P[Frontend shows error toast]
\`\`\`

### List/Search Flow

\`\`\`mermaid
flowchart TD
A[User opens page] --> B[Frontend calls GET /api/Entity]
B --> C[Controller receives query params]
C --> D[Service.GetAllAsync]
D --> E{Has filters?}
E -->|Yes| F[Apply Where clauses]
E -->|No| G[Get all active records]
F --> H[Apply ordering]
G --> H
H --> I{Paginated?}
I -->|Yes| J[Apply Skip/Take]
I -->|No| K[Return all]
J --> L[Project to DTO via Mapster]
K --> L
L --> M[Return ApiResponse&lt;List&lt;Dto&gt;&gt;]
M --> N[Frontend renders table/list]
\`\`\`
```

### Step 3: Document Data Transformation Points

```markdown
## Data Transformation Map

| Layer               | Input      | Output        | Transformer           | Notes                             |
| ------------------- | ---------- | ------------- | --------------------- | --------------------------------- |
| Controller (Create) | CreateDto  | Entity        | Mapster               | Auto-mapped via TypeAdapterConfig |
| Controller (Edit)   | EditDto    | Entity        | Mapster               | Only mapped fields updated        |
| Controller (Read)   | Entity     | ViewDto       | Mapster               | Includes related entity names     |
| Service (List)      | IQueryable | List<ViewDto> | Mapster ProjectToType | DB-level projection               |
| Frontend (Form)     | Form state | CreateDto     | Manual                | Validate before sending           |
| Frontend (Display)  | ViewDto    | Template vars | Computed props        | Format dates, statuses            |
```

### Step 4: Document External Integration Flows

```markdown
## External Integration: [System Name]

\`\`\`mermaid
sequenceDiagram
participant S as Service Layer
participant H as HttpClient
participant E as External API
participant R as Valkey Cache

    S->>R: Check cache for data
    alt Cache hit
        R-->>S: Return cached data
    else Cache miss
        S->>H: Create request
        H->>E: GET /api/resource
        alt Success
            E-->>H: 200 OK + data
            H-->>S: Deserialized response
            S->>R: Store in cache (TTL: 5min)
        else Failure
            E-->>H: Error response
            H-->>S: Throw exception
            S->>S: Log error + return fallback
        end
    end

\`\`\`

### Error/Retry Flow

\`\`\`mermaid
flowchart TD
A[Call External API] --> B{Response?}
B -->|200 OK| C[Process response]
B -->|429 Rate Limited| D[Wait retry-after header]
D --> A
B -->|500 Server Error| E{Retry count < 3?}
E -->|Yes| F[Exponential backoff]
F --> A
E -->|No| G[Log failure]
G --> H[Return error to caller]
B -->|Timeout| E
\`\`\`
```

### Step 5: Document Background Job Flows

```markdown
## Background Jobs (TickerQ)

\`\`\`mermaid
flowchart TD
subgraph Triggers["Job Triggers"]
CRON[â° Scheduled CRON]
EVENT[ðŸ“¢ Event-triggered]
MANUAL[ðŸ‘¤ Manual trigger]
end

    subgraph TickerQ["TickerQ Pipeline"]
        CRON --> Q[Job Queue]
        EVENT --> Q
        MANUAL --> Q
        Q --> W[Worker picks up job]
        W --> J[Job Execution]
    end

    subgraph Processing["Job Processing"]
        J --> DB[(Read from DB)]
        DB --> PROCESS[Process data]
        PROCESS --> EXT[Call external service]
        EXT --> UPDATE[Update DB records]
        UPDATE --> NOTIFY[Send notifications]
    end

    J --> FAIL{Failed?}
    FAIL -->|Yes| RETRY[Auto-retry with backoff]
    RETRY --> Q
    FAIL -->|No| DONE[âœ… Complete]

\`\`\`
```

## Tips

1. **Start with the happy path** â€” Document the normal flow first, then add error branches
2. **One diagram per feature** â€” Don't cram everything into one massive diagram
3. **Show data transformations** â€” Where does a DTO become an Entity? Where does raw data become a response?
4. **Include caching** â€” If Valkey is used, show cache check/store steps
5. **Document async flows** â€” Background jobs, webhooks, and event-driven patterns need separate diagrams

## Review Checklist

- [ ] Standard request lifecycle documented
- [ ] Each major feature has a data flow diagram
- [ ] Data transformation points are mapped (DTO â†” Entity)
- [ ] External integration flows include error/retry paths
- [ ] Background job flows documented (if applicable)
- [ ] All diagrams use Mermaid.js syntax
- [ ] Cache strategies visible in diagrams
