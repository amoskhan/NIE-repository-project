# Guide: Creating Architecture Documentation

> **This is a GUIDE.** Each project creates its own `docs/architecture.md` with project-specific architecture diagrams. This document explains HOW to create it.

---

## Purpose

The architecture document defines system boundaries, service decomposition, external dependencies, and communication patterns. AI agents read this to understand the big picture before generating code.

## When to Create

- During Phase 1.2 (Technical Design) of AIDLC
- When adding new services or external integrations
- When fundamentally changing the deployment model

## Format

**All diagrams MUST use Mermaid.js syntax.** AI agents cannot read PNG/PDF/Visio files.

## How to Create

### Step 1: Start with the App Template Base Architecture

Every App Template project starts with this baseline. Copy it into `docs/architecture.md` and extend it:

```markdown
# Architecture

## System Context (C4 Level 1)

Who uses the system and what external systems does it connect to?

\`\`\`mermaid
graph TB
User[👤 User<br/>Describe your primary user] -->|HTTPS| WebApp[🌐 Web Application]
Admin[👤 Admin<br/>System Administrator] -->|HTTPS| WebApp

    WebApp -->|API calls| MainAPI[📦 Main API<br/>.NET 10]
    WebApp -->|Auth flow| AuthAPI[🔐 Auth API<br/>.NET 10]

    MainAPI -->|Read/Write| DB[(🗄️ PostgreSQL<br/>Database)]
    MainAPI -->|Cache/Sessions| Valkey[(⚡ Valkey<br/>Cache)]
    AuthAPI -->|Sessions| Valkey

    %% Add your external systems below:
    %% MainAPI -->|REST| ExternalSystem[🌐 External System]
    %% MainAPI -->|SMTP| EmailServer[📧 Email Server]

\`\`\`
```

### Step 2: Add Container Diagram (C4 Level 2)

Break down each container (service) and show what's inside:

```markdown
## Container Diagram (C4 Level 2)

\`\`\`mermaid
graph TB
subgraph Frontend["Frontend (Vue 3 + TypeScript)"]
MainApp[Main App<br/>Port 8002]
AuthApp[Auth App<br/>Port 8001]
UILib[@apptemplate/ui<br/>Shared Components]
SharedLib[@apptemplate/shared<br/>Utilities]
end

    subgraph Backend[".NET 10 Backend"]
        API[Main API<br/>Port 5002]
        Auth[Auth API<br/>Port 5001]

        subgraph Libraries["Shared Libraries"]
            Domain[Domain<br/>Entities only]
            Data[Data<br/>DbContext, Migrations]
            Services[Services<br/>Business Logic]
            Shared[Shared<br/>Utilities]
        end
    end

    subgraph Infrastructure["Infrastructure"]
        PG[(PostgreSQL)]
        RD[(Valkey)]
        %% Add your infra below
    end

    MainApp --> API
    AuthApp --> Auth
    API --> Domain
    API --> Services
    Services --> Data
    Data --> PG
    Auth --> RD
    API --> RD

\`\`\`
```

### Step 3: Add Project-Specific Architecture

Add sections for your project's unique aspects:

```markdown
## Communication Patterns

| From                    | To         | Protocol   | Pattern             | Auth                   |
| ----------------------- | ---------- | ---------- | ------------------- | ---------------------- |
| Frontend                | Main API   | HTTPS/REST | Request-Response    | Session (X-Session-Id) |
| Frontend                | Auth API   | HTTPS/REST | Request-Response    | None (login)           |
| Main API                | PostgreSQL | TCP        | EF Core             | Connection string      |
| Main API                | Valkey     | TCP        | StackExchange.Redis | Connection string      |
| [Add your integrations] |            |            |                     |                        |

## Deployment Architecture

\`\`\`mermaid
graph LR
subgraph Docker["Docker Compose"]
UI[nginx<br/>UI Container]
API[Main API<br/>Container]
Auth[Auth API<br/>Container]
PG[(PostgreSQL<br/>Container)]
RD[(Valkey<br/>Container)]
end

    LB[Load Balancer] --> UI
    UI -->|/api/*| API
    UI -->|/auth/*| Auth
    API --> PG
    API --> RD
    Auth --> RD

\`\`\`

## Scalability Considerations

- [How the system handles increased load]
- [Which components can be horizontally scaled]
- [Database scaling strategy]

## Security Boundaries

- [Where trust boundaries exist]
- [Which networks are internal vs. public]
- [Where encryption is required]
```

### Step 4: Add Sequence Diagrams for Key Flows

```markdown
## Key Flow: Authentication

\`\`\`mermaid
sequenceDiagram
participant U as User
participant AF as Auth Frontend
participant AA as Auth API
participant R as Valkey
participant MF as Main Frontend

    U->>AF: Enter credentials
    AF->>AA: POST /api/Auth/Login
    AA->>AA: Validate credentials
    AA->>R: Store session
    AA-->>AF: Session token
    AF->>MF: Redirect with session
    MF->>MF: Store session in cookie

\`\`\`

## Key Flow: [Your Custom Flow]

\`\`\`mermaid
sequenceDiagram
participant U as User
participant F as Frontend
participant A as API
participant S as Service
participant D as Database

    U->>F: [Action]
    F->>A: [API Call]
    A->>S: [Service Method]
    S->>D: [Query/Command]
    D-->>S: [Result]
    S-->>A: [Processed Result]
    A-->>F: [Response DTO]
    F-->>U: [UI Update]

\`\`\`
```

## Tips

1. **Start simple** — C4 Level 1 (Context) is often enough for small projects
2. **Add detail as complexity grows** — Only create Level 2/3 diagrams when needed
3. **Keep diagrams updated** — When you add a new service or integration, update the diagram
4. **Use Mermaid.js live editor** to preview: https://mermaid.live
5. **Link decisions** — If you chose a particular architecture pattern, create an ADR in `agents/`

## Review Checklist

- [ ] System context diagram shows all actors and external systems
- [ ] Container diagram shows all services and their relationships
- [ ] Communication patterns table is complete
- [ ] Key flows have sequence diagrams
- [ ] Security boundaries are identified
- [ ] All diagrams use Mermaid.js (no images)
