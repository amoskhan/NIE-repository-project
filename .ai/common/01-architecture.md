# 01 - Architecture

```
src/backend/
|-- API/                    # Main API service (port 5002)
|   |-- Controllers/        # API endpoints (extend BaseController)
|   |-- Middleware/         # Correlation, ETag, exception, session, user roles
|   |-- Mapping/            # Mapster DTO mappings (MappingProfile.cs)
|   |-- Authorization/      # RequireAccessFunction attribute + handlers
|   |-- Extensions/         # Observability, rate limiting, TickerQ, seeders
|   |-- Jobs/               # TickerQ background jobs
|   `-- Program.cs          # Service registration and startup
|-- Auth/                   # Auth API service (port 5001)
|   |-- Controllers/        # Login, logout, session verify/refresh, registration, password reset
|   |-- Models/             # Auth-local request/response types
|   `-- Services/           # AuthSessionService + local identity (credential) services
`-- Libraries/
    |-- Domain/Models/      # Entity/domain model classes only
    |-- Data/Data/          # MainDbContext
    |-- Data/Migrations/    # EF Core migrations
    |-- Services/Services/  # Business logic folders
    `-- Shared/             # DTOs, enums, security catalogs, cross-layer services, helpers, settings

src/frontend/               # pnpm monorepo
|-- main/                   # Main user application (port 8002)
|-- auth/                   # Login application (port 8001)
`-- packages/
    |-- ui/                 # @apptemplate/ui reusable Vue components + theme runtime
    `-- shared/             # @apptemplate/shared constants, utilities, types, composables

build/                      # Dockerfiles, nginx config, Docker Compose stack
deploy/                     # Helm chart
.github/workflows/          # GitHub Actions CI/CD
tests/                      # Playwright API + E2E tests
tools/                      # Versioning and template tooling
.ai/                        # AI agent instructions
```

## Auth Boundary

The template is **self-contained**: it ships its own identity provider. There is no call to an external identity service in the default configuration.

- Auth API (5001): the only service that mints sessions. Owns login, logout, session verify/refresh, self-service registration, and password reset.
  - Credentials are verified against the local `users` table using the ASP.NET Core `PasswordHasher<T>` (PBKDF2). Plaintext passwords are never stored, logged, or returned.
  - A successful login writes a session record to Valkey and returns the session token. The wire contract is `POST /api/Auth/Login { userid, pd }` → session token → `X-Session-Id` header on every subsequent request.
  - An **optional external OIDC slot** (Google / Microsoft / GitHub) exists as configuration only and ships **disabled**. When a project enables it, the OIDC callback still terminates in the Auth API and still mints the same Valkey session — the downstream contract does not change.
- Main API (5002): validates `X-Session-Id` through `SessionValidationMiddleware`, looks up user context from Valkey, and populates `BaseController.UserId`, `UserRoles`, `UserAccessFunctions`, and `IsAdmin`.

Rules: only the Auth API touches credentials or mints sessions. The Main API never authenticates a user itself — it only validates an existing session. Demo accounts are seeded for local development and must be removed or have their passwords rotated before any real deployment.

Decision record: [`../adrs/003-local-identity-provider.md`](../adrs/003-local-identity-provider.md).

## Backend Library Ownership

- `Libraries/Domain/Models/` is entity-only. Do not put DTOs, enums, security catalogs, service contracts, helpers, converters, or bundled `*Models.cs` files under Domain.
- Keep one top-level domain entity/domain object per file. If you add `PurchaseOrderLine`, create `PurchaseOrderLine.cs`.
- DTOs live under `Libraries/Shared/Dto/`.
- Enums live under `Libraries/Shared/Enum/`.
- Security catalogs live under `Libraries/Shared/Security/`.
- Cross-layer contracts required by lower layers live with their implementation in Shared, for example `Libraries/Shared/Services/UserContext/IUserContextService.cs` and `UserContextService.cs`.
- Business services live under `Libraries/Services/Services/<Feature>/`. Each service interface and implementation must be separate files in that same folder. Service-local request/result/helper types also get their own files in that folder.

## Automatic Behaviors

- Audit logging: `MainDbContext.SaveChanges` captures create/update/delete for any `TimestampedEntity` subclass. Manual events go through `IAuditLogger`.
- Timestamps: `CreatedOn`, `UpdatedOn`, `CreatedBy`, and `UpdatedBy` are set automatically. Do not set them in service or controller code.
- Session validation: every request through Main API except OpenAPI, health, and favicon is validated.
- Migrations apply on startup: `Program.cs` runs `Database.Migrate()` on boot. Do not deploy a migration that requires manual SQL.
