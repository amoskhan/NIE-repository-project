# Authentication — File Map

## Owned files

### Auth API (microservice)

| Path                                                 | Layer      | Purpose                                                                                                                                                                                                                                                                 |
| ---------------------------------------------------- | ---------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Auth/Auth.csproj`                       | Project    | Auth service project file                                                                                                                                                                                                                                               |
| `src/backend/Auth/Program.cs`                        | Host       | Registers `MainDbContext`, Valkey (`IConnectionMultiplexer` + `AddStackExchangeRedisCache`), CORS, built-in OpenAPI, Sentry+OpenTelemetry, `IPasswordHasher<UserAccount>`, `LocalIdentityOptions`, `ILocalIdentityService`, `ExternalIdpOptions`, `IExternalIdpService` |
| `src/backend/Auth/Controllers/AuthController.cs`     | Controller | `Login`, `Logout`, `Verify`, `Refresh`, `GetProfile`, `CreateTestSession`, `Register`, `ForgotPassword`, `ResetPassword`, `ChangePassword`, `ExternalProviders`, `ExternalStart`, `ExternalCallback`                                                                    |
| `src/backend/Auth/Services/ILocalIdentityService.cs` | Interface  | The local IDP contract: `VerifyCredentialsAsync`, `RegisterAsync`, `CreatePasswordResetTokenAsync`, `ResetPasswordAsync`, `ChangePasswordAsync`, `ResolveExternalUserAsync`                                                                                             |
| `src/backend/Auth/Services/LocalIdentityService.cs`  | Service    | Password verification against `UserAccounts`, lockout accounting, hash upgrade-on-signin, registration, SHA-256 reset tokens with constant-time comparison, external-account resolution                                                                                 |
| `src/backend/Auth/Services/IExternalIdpService.cs`   | Interface  | `IsEnabled`, `GetEnabledProviders`, `BuildAuthorizationUrlAsync`, `HandleCallbackAsync`                                                                                                                                                                                 |
| `src/backend/Auth/Services/ExternalIdpService.cs`    | Service    | OAuth 2.0 authorization-code flow with PKCE: discovery, state handling, token exchange, userinfo, claim mapping                                                                                                                                                         |
| `src/backend/Auth/Services/IAuthSessionService.cs`   | Interface  | `IssueSessionAsync`                                                                                                                                                                                                                                                     |
| `src/backend/Auth/Services/AuthSessionService.cs`    | Service    | The single source of truth for writing `session:{token}` in Valkey                                                                                                                                                                                                      |
| `src/backend/Auth/appsettings.json`                  | Config     | `ConnectionStrings:MainDbConnection`, `Valkey`, `LocalIdentity`, `ExternalIdp`, `ValidSessionTimeInMins`, `AllowedCORSOrigin`, `Sentry`                                                                                                                                 |

### Auth API models

| Path                                                   | Layer   | Purpose                                                                                                                                                                                                                                                              |
| ------------------------------------------------------ | ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Auth/Models/AuthSessionDto.cs`            | DTO     | Shape of the value at `session:{token}` in Valkey                                                                                                                                                                                                                    |
| `src/backend/Auth/Models/LoginRequest.cs`              | DTO     | `{ userid, pd }` — the wire contract. Both nullable so a blank field yields 401, not 400                                                                                                                                                                             |
| `src/backend/Auth/Models/LoginResponse.cs`             | DTO     | Internal login result (`isAuthenticated`, `userId`, `fullName`, `email`, `department`, `errorMessage`) — produced by both the local and external paths                                                                                                               |
| `src/backend/Auth/Models/IssuedLoginResponse.cs`       | DTO     | What the Auth API returns to the FE after issuing the Valkey session                                                                                                                                                                                                 |
| `src/backend/Auth/Models/LocalIdentityOptions.cs`      | Options | Binds `LocalIdentity`: `MinPasswordLength`, `MaxFailedLoginAttempts`, `LockoutMinutes`, `PasswordResetTokenTtlMinutes`, `AllowSelfRegistration`                                                                                                                      |
| `src/backend/Auth/Models/LocalIdentityRequests.cs`     | DTOs    | `RegisterRequest` (`UserId`, `FullName`, `Email`, `Password`), `ForgotPasswordRequest` (`UserIdOrEmail`), `ResetPasswordRequest` (`Token`, `NewPassword` — **no `UserId`**; the reset token is self-identifying), `ChangePasswordRequest`, `ExternalProviderSummary` |
| `src/backend/Auth/Models/ExternalIdpOptions.cs`        | Options | Binds `ExternalIdp`: `Enabled` plus a `Providers` map of `Enabled`, `DisplayName`, `ClientId`, `ClientSecret`, `Authority`, `Scopes`, `RedirectUri`, `AuthorizationEndpoint`, `TokenEndpoint`, `UserInfoEndpoint`                                                    |
| `src/backend/Auth/Models/ExternalLoginState.cs`        | Model   | Short-lived state for an in-flight external sign-in (PKCE verifier, provider, return URL)                                                                                                                                                                            |
| `src/backend/Auth/Models/CreateTestSessionRequest.cs`  | DTO     | Dev-only request body for `CreateTestSession`                                                                                                                                                                                                                        |
| `src/backend/Auth/Models/CreateTestSessionResponse.cs` | DTO     | Dev-only response carrying the freshly issued session token                                                                                                                                                                                                          |

### The account store

| Path                                                     | Layer     | Purpose                                                                                                                                                                                                                                                                                                                                                                                     |
| -------------------------------------------------------- | --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Domain/Models/UserAccount.cs`     | Entity    | The credential record backing the `UserAccounts` table. `UserId` (unique login name, the identity key everywhere else), `Name`, `Email`, `Department`, `PasswordHash` (nullable — null means external-only), `IsActive`, `MustChangePassword`, `FailedLoginCount`, `LockoutEndOn`, `LastLoginOn`, `ExternalProvider`, `ExternalSubject`, `PasswordResetTokenHash`, `PasswordResetExpiresOn` |
| `src/backend/Libraries/Data/Data/MainDbContext.cs`       | DbContext | `DbSet<UserAccount> UserAccounts`, its entity configuration, and the audit rules below                                                                                                                                                                                                                                                                                                      |
| `src/backend/Libraries/Data/Data/MainDbContextSeeder.cs` | Seeder    | Development-only demo accounts (`admin`, `alice`, `bob`) hashed with the same `PasswordHasher<UserAccount>` the Auth API verifies with, plus their `UserRole` assignments                                                                                                                                                                                                                   |

Two things in `MainDbContext` matter and are easy to break:

- `ShouldAuditProperty` explicitly **excludes `PasswordHash` and `PasswordResetTokenHash`** from the audit trail. Credential material must never be copied into audit rows.
- `ResolveAuditCategory` classifies `UserAccount` changes as `EAuditCategory.AccessControl`, alongside roles and access functions.

Roles and permissions are **not** in `UserAccount`. They live in `UserRole` / `Role` and are resolved by the Main API — the join is `UserAccount.UserId == UserRole.UserId`. See `authorization-access-functions`.

## Main API session enforcement

| Path                                                                      | Layer      | Purpose                                                                                                                                                  |
| ------------------------------------------------------------------------- | ---------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/API/Middleware/SessionValidationMiddleware.cs`               | Middleware | The only consumer of session state in the Main API. Reads `X-Session-Id`, validates against Valkey, populates `HttpContext.Items[Constants.KeySession*]` |
| `src/backend/API/Middleware/UserRolesMiddleware.cs`                       | Middleware | Hydrates `KeySessionUserAccessFunctions` after `SessionValidationMiddleware` (used by `[RequireAccessFunction]`)                                         |
| `src/backend/Libraries/Shared/Services/UserContext/UserContextService.cs` | Service    | Adapts `HttpContext.Items` to a typed `IUserContextService.UserId/UserName/Email/SessionId` (contract in the sibling `IUserContextService.cs`)           |
| `src/backend/Libraries/Shared/Globals/Constants.cs`                       | Constants  | `KeySessionUserId`, `KeySessionUserName`, `KeySessionUserEmail`, `KeySessionSessionId`, `KeySessionUserDept`, `KeySessionUserAccessFunctions`            |

## Frontend

| Path                                             | Layer      | Purpose                                                                                                                             |
| ------------------------------------------------ | ---------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/auth/src/App.vue`                  | Page shell | Auth FE root                                                                                                                        |
| `src/frontend/auth/src/components/LoginPage.vue` | Page       | Password form, register + forgot-password entry points, one button per entry from `GET /api/Auth/ExternalProviders`, redirect logic |
| `src/frontend/auth/src/main.ts`                  | Bootstrap  | Mounts the auth FE app                                                                                                              |
| `src/frontend/main/src/services/authService.ts`  | Service    | `ensureAuthenticated()`, `redirectToLogin()`, `getAuthLoginUrl()` — cookie-driven session gate                                      |
| `src/frontend/main/src/composables/useAuth.ts`   | Composable | Reactive `currentUser`, `isAuthenticated`, `isAdmin`, `logout`, `hasRole`                                                           |
| `src/frontend/main/src/services/api.ts`          | HTTP       | Axios instance that injects `X-Session-Id` from cookie on every Main API call and redirects to login on 401                         |

## Touched files (line-level edits required when changing the auth flow)

| Path                                                   | What it contains                                                                        | Why must be touched                                                                                                                                 |
| ------------------------------------------------------ | --------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/API/Program.cs`                           | `builder.Services.AddSessionValidation(configuration)` and `app.UseSessionValidation()` | Required so the middleware actually runs in the Main API pipeline                                                                                   |
| `src/backend/API/Middleware/MiddlewareExtensions.cs`   | Extensions registering `SessionValidationMiddleware` and `UserRolesMiddleware`          | Both must be added in the right order                                                                                                               |
| `src/backend/API/appsettings.json`                     | `Valkey:ConnectionString`, `ValidSessionTimeInMins`, `AllowedCORSOrigin`                | Main API consumes the same Valkey instance the Auth API writes to; must match                                                                       |
| `src/backend/Libraries/Shared/Dto/AuthDto.cs`          | DTO used by `SessionValidationMiddleware` to deserialize the session blob               | Must stay schema-compatible with `Auth.Models.AuthSessionDto`, field by field                                                                       |
| `src/frontend/main/src/services/api.ts`                | Adds `X-Session-Id` from cookie, intercepts 401                                         | Must call `authService.redirectToLogin()` on 401                                                                                                    |
| `src/frontend/main/src/router/index.ts`                | Navigation guard calling `authService.ensureAuthenticated()`                            | Without this, deep links bypass the cookie check                                                                                                    |
| `src/frontend/packages/shared/src/config/constants.ts` | Runtime constants: auth/main frontend URLs, backend URLs, cookie names and domain       | Both FE apps derive URLs from the app base path and optional `window.__APP_TEMPLATE_CONFIG__` / `app:*` meta tags; do not add frontend `.env` files |
| `src/frontend/main/src/main.ts`                        | Bootstraps the Main FE and consumes `FRONTEND_CONSTANTS`                                | Auth redirects resolve through shared constants, not Vite env variables                                                                             |

## Migrations

Sessions live in Valkey — no migration.

`UserAccounts` is a real table in the Main database, so **changing `UserAccount` requires a migration**. Both the Auth API and the Main API resolve `MainDbContext` against the same database; `src/backend/Auth/appsettings.json` carries its own `ConnectionStrings:MainDbConnection` and it must point at the same place as the Main API's.

## External dependencies

| Package                                                                   | Project     | Purpose                                                                                        |
| ------------------------------------------------------------------------- | ----------- | ---------------------------------------------------------------------------------------------- |
| `Microsoft.AspNetCore.Identity`                                           | Auth + Data | `PasswordHasher<UserAccount>` — PBKDF2 hashing, used identically by the service and the seeder |
| `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL` | Auth        | `MainDbContext` access to `UserAccounts`                                                       |
| `StackExchange.Redis`                                                     | Auth + API  | Valkey client for `IConnectionMultiplexer` and `IDistributedCache`                             |
| `Microsoft.Extensions.Caching.StackExchangeRedis`                         | Auth + API  | `AddStackExchangeRedisCache(options)`                                                          |
| `Sentry.AspNetCore` + `Sentry.OpenTelemetry`                              | Auth + API  | Error capture and distributed tracing                                                          |
| `js-cookie`                                                               | FE          | Cookie read/write for session and user                                                         |
