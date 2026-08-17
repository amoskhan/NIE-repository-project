# Guide: Creating Infrastructure Documentation

> **This is a GUIDE.** Each project creates its own `docs/infrastructure.md` with project-specific deployment and infrastructure details. This document explains HOW to create it.

---

## Purpose

The infrastructure document defines how the application is built, deployed, and operated. AI agents use this to generate Docker configurations, CI/CD pipelines, and deployment scripts.

## When to Create

- During Phase 1.2 (Technical Design) of AIDLC
- When changing deployment targets or adding services
- When modifying CI/CD pipelines

## Format

Use **Mermaid.js** for deployment diagrams. Use Markdown tables for configuration details.

## How to Create

### Step 1: Document the App Template Deployment Baseline

```markdown
# Infrastructure

## Deployment Architecture

\`\`\`mermaid
graph TB
subgraph CI["CI/CD (GitHub Actions)"]
Build[Build Stage] --> Test[Test Stage]
Test --> DockerBuild[Docker Build]
DockerBuild --> Push[Push to Registry]
Push --> Deploy[Deploy to Server]
end

    subgraph Production["Production Server"]
        subgraph Docker["Docker Compose"]
            Nginx[nginx<br/>:80/:443]
            APIContainer[Main API<br/>:5002]
            AuthContainer[Auth API<br/>:5001]
            PGContainer[(PostgreSQL<br/>:5432)]
            ValkeyContainer[(Valkey<br/>:6379)]
        end
    end

    Deploy -->|docker compose up| Docker
    User[Users] -->|HTTPS| Nginx
    Nginx -->|/api/*| APIContainer
    Nginx -->|/auth/*| AuthContainer
    APIContainer --> PGContainer
    APIContainer --> RedisContainer
    AuthContainer --> RedisContainer

\`\`\`
```

### Step 2: Document Container Configuration

```markdown
## Docker Containers

| Container  | Base Image                           | Port (Internal) | Port (External) | Build File            |
| ---------- | ------------------------------------ | --------------- | --------------- | --------------------- |
| UI         | nginx:alpine                         | 80              | 80/443          | build/Dockerfile.ui   |
| Main API   | mcr.microsoft.com/dotnet/aspnet:10.0 | 5002            | -               | build/Dockerfile.api  |
| Auth API   | mcr.microsoft.com/dotnet/aspnet:10.0 | 5001            | -               | build/Dockerfile.auth |
| PostgreSQL | postgres:18-alpine                   | 5432            | 5432            | - (official image)    |
| Valkey     | valkey/valkey:9-alpine               | 6379            | 6379            | - (official image)    |
| [Custom]   | [Image]                              | [Port]          | [Port]          | [Dockerfile]          |

All base images are public. Nothing here needs a private registry to build.

## Docker Compose Configuration

File: `build/docker-compose.yml`

| Service  | Depends On       | Restart Policy | Volumes                         | Networks          |
| -------- | ---------------- | -------------- | ------------------------------- | ----------------- |
| ui       | api, auth        | always         | -                               | frontend          |
| api      | postgres, valkey | always         | logs:/app/logs                  | backend, frontend |
| auth     | valkey           | always         | logs:/app/logs                  | backend, frontend |
| postgres | -                | always         | pgdata:/var/lib/postgresql/data | backend           |
| valkey   | -                | always         | -                               | backend           |
```

### Step 3: Document Environment Configuration

```markdown
## Environment Variables

### Main API (appsettings.json)

| Variable                           | Description                   | Default (Dev)      | Production       |
| ---------------------------------- | ----------------------------- | ------------------ | ---------------- |
| ConnectionStrings:MainDbConnection | PostgreSQL connection         | Host=localhost;... | Via env / secret |
| Valkey:ConnectionString            | Valkey connection             | localhost:6379     | valkey:6379      |
| ValidSessionTimeInMins             | Session idle window           | 90                 | Project decision |
| AuditLog:RetentionMonths           | How long audit rows are kept  | 6                  | Project decision |
| Sentry:Dsn                         | Error reporting (blank = off) | (blank)            | Via secret       |
| [Custom config]                    | [Description]                 | [Default]          | [Production]     |

### Auth API (appsettings.json)

| Variable                | Description                   | Default (Dev)  | Production       |
| ----------------------- | ----------------------------- | -------------- | ---------------- |
| Valkey:ConnectionString | Valkey connection             | localhost:6379 | valkey:6379      |
| AllowedCORSOrigin       | Frontend origins allowed      | localhost SPAs | Your real hosts  |
| ValidSessionTimeInMins  | Must match the Main API value | 90             | Same as Main API |
| [Custom config]         | [Description]                 | [Default]      | [Production]     |

`ValidSessionTimeInMins` has to be the same number in both services — the Auth API sets the session expiry from it and the Main API re-checks the idle window against it on every request.

### Build Configuration

| File                  | Purpose                 | Location |
| --------------------- | ----------------------- | -------- |
| appsettings.api.json  | API production config   | build/   |
| appsettings.auth.json | Auth production config  | build/   |
| nginx.conf            | Reverse proxy config    | build/   |
| docker-compose.yml    | Container orchestration | build/   |
```

### Step 4: Document CI/CD Pipeline

```markdown
## Build Pipeline (GitHub Actions)

Files: `.github/workflows/`

\`\`\`mermaid
flowchart LR
A[Push / PR] --> B[Workflow Trigger]
B --> C[Checkout Code]
C --> D[Build Frontend]
D --> E[Build Backend]
E --> F[Run Tests]
F --> G{Tests Pass?}
G -->|Yes| H[Build Docker Images]
G -->|No| I[Fail the check]
H --> J[Push to Registry]
J --> K[Deploy]
K --> L[Health Check]
L --> M{Healthy?}
M -->|Yes| N[Deployed]
M -->|No| O[Rollback]
\`\`\`

### Build Commands

| Stage          | Command                      | Notes                                      |
| -------------- | ---------------------------- | ------------------------------------------ |
| Frontend Build | `pnpm install && pnpm build` | Builds both main and auth apps             |
| Backend Build  | `dotnet publish -c Release`  | Publishes API and Auth projects            |
| Docker Build   | `docker compose build`       | Builds all containers                      |
| Deploy         | `docker compose up -d`       | Or `helm upgrade --install` for Kubernetes |

Store registry credentials, connection strings, and any DSN as **repository secrets**. Never commit them, and never echo a secret into a workflow log.
```

### Step 4b. Document the Kubernetes path (if you use one)

The template ships a Helm chart at `deploy/helm/app-template/`. If your project deploys to Kubernetes rather than a single host, document the release name, namespace, ingress host, and which values you override per environment.

### Step 5: Document Database Operations

```markdown
## Database Operations

### Migration Commands

| Operation        | Command                                                                                        | When                |
| ---------------- | ---------------------------------------------------------------------------------------------- | ------------------- |
| Create migration | `dotnet ef migrations add {Name} --project Libraries/Data --startup-project API`               | After model changes |
| Apply migration  | `dotnet ef database update --project Libraries/Data --startup-project API`                     | Before deployment   |
| Rollback         | `dotnet ef database update {PreviousMigration} --project Libraries/Data --startup-project API` | On failure          |
| Generate SQL     | `dotnet ef migrations script --project Libraries/Data --startup-project API`                   | For DBA review      |

### Backup Strategy

| Type            | Frequency          | Retention | Storage    |
| --------------- | ------------------ | --------- | ---------- |
| Full backup     | Daily              | 30 days   | [Location] |
| Transaction log | Every 15 min       | 7 days    | [Location] |
| Pre-deployment  | Before each deploy | 90 days   | [Location] |
```

### Step 6: Document Monitoring and Health

```markdown
## Health Checks

| Endpoint       | Checks                  | Expected Response |
| -------------- | ----------------------- | ----------------- |
| /health        | API is running          | 200 OK            |
| /health/db     | PostgreSQL connectivity | 200 OK or 503     |
| /health/valkey | Valkey connectivity     | 200 OK or 503     |

## Logging

| Component  | Log Location       | Format       | Retention |
| ---------- | ------------------ | ------------ | --------- |
| Main API   | /app/logs/api.log  | Serilog JSON | 30 days   |
| Auth API   | /app/logs/auth.log | Serilog JSON | 30 days   |
| nginx      | /var/log/nginx/    | Combined     | 14 days   |
| PostgreSQL | pg_log/            | Standard     | 7 days    |

## Alerts

| Condition              | Severity | Action       |
| ---------------------- | -------- | ------------ |
| API health check fails | Critical | Page on-call |
| Response time > 2s     | Warning  | Notify team  |
| Disk usage > 80%       | Warning  | Notify team  |
| Error rate > 5%        | Critical | Page on-call |
```

## Tips

1. **Start with docker-compose.yml** — It's the source of truth for your deployment
2. **Document ALL environment variables** — Missing config is the #1 deployment issue
3. **Include rollback procedures** — Every deployment should have a rollback plan
4. **Keep build/ configs version-controlled** — Never store production secrets in git
5. **Health checks are mandatory** — Every service must have a health endpoint

## Review Checklist

- [ ] Deployment architecture diagram created
- [ ] All containers documented with ports and images
- [ ] Environment variables listed for all services
- [ ] CI/CD pipeline documented
- [ ] Database migration procedures documented
- [ ] Backup strategy defined
- [ ] Health checks and monitoring configured
- [ ] Logging strategy defined
- [ ] Rollback procedure documented
