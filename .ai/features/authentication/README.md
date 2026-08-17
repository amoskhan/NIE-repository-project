# Authentication

> **Status:** `core`
> **Removable in derived repos:** **no** — every app built on this template assumes a signed-in user
> **Required by:** every authenticated controller, every authenticated FE page, `authorization-access-functions`, `audit-logging` (login/logout entries), `caching-valkey` (session store)

The Authentication feature owns the entire login surface: the dedicated `Auth` API microservice (port 5001), the Vue auth shell at `src/frontend/auth/`, the Valkey-backed session store, and the `X-Session-Id` header convention. The Main API never authenticates a user; it only consumes a session token via `SessionValidationMiddleware` and looks the session up in Valkey. This split keeps the auth surface tight: one instrumented service, one place to rotate secrets, one place to audit login events.

**The template ships a self-contained local identity provider.** There is no external identity service to sign up for and no API key to obtain — clone the repo, start the stack, and sign in with a seeded demo account. Credentials live in the `UserAccounts` table, passwords are hashed with ASP.NET Core's `PasswordHasher<UserAccount>`, and the service handles self-service registration, password reset, password change, and account lockout.

An **optional external identity provider slot** (Google / Microsoft / GitHub) is fully wired but **ships disabled and empty**. Turn it on with configuration alone when your project needs it.

Sessions are JSON-serialized `AuthSessionDto` blobs stored under `session:{sessionToken}` in Valkey with a sliding-window expiry of `ValidSessionTimeInMins` minutes. The Auth API issues; the Main API validates. Roles and access functions are NOT in the session payload — the frontend fetches them from the Main API after redirect (see `authorization-access-functions`).

## Quick links

- [`files.md`](./files.md) — every file owned and touched by this feature
- [`do-dont.md`](./do-dont.md) — feature-specific rules
- [`customize.md`](./customize.md) — password policy, demo accounts, enabling an external provider
- [`verify.md`](./verify.md) — proof the auth flow works end to end

## The wire contract (do not break this)

Everything downstream of login depends on exactly three things. Change the identity backend as much as you like; keep these stable:

| Step                | Contract                                                                                               |
| ------------------- | ------------------------------------------------------------------------------------------------------ |
| 1. Login            | `POST /api/Auth/Login` with body `{ "userid": "...", "pd": "..." }`                                    |
| 2. Session          | The Auth API writes `session:{sessionToken}` to Valkey and returns the token                           |
| 3. Every later call | The FE sends `X-Session-Id: <sessionToken>`; `SessionValidationMiddleware` in the Main API resolves it |

`userid` / `pd` are unlovely names, but they are the published contract — the Vue login page and the API tests post exactly these two fields. Both are nullable on purpose, so a blank field returns a plain `401` rather than a `400` model-binding error that would leak the request shape.

## Architectural shape

```mermaid
flowchart LR
  Browser["Browser<br/>(auth FE :8001)"] -->|POST Login| AuthApi["Auth API :5001<br/>AuthController"]
  Browser -->|Register / ForgotPassword / ResetPassword| AuthApi
  AuthApi -->|verify hash| Local["ILocalIdentityService<br/>UserAccounts table"]
  AuthApi -.->|optional, disabled by default| Ext["IExternalIdpService<br/>Google / Microsoft / GitHub"]
  Local --> Session["IAuthSessionService.IssueSessionAsync"]
  Ext -.-> Session
  Session -->|set session:{token}| Valkey[(Valkey)]
  Browser -->|redirect with session cookie| MainFe["Main FE :8002"]
  MainFe -->|every request<br/>X-Session-Id header| MainApi["Main API :5002<br/>SessionValidationMiddleware"]
  MainApi -->|GET session:{token}| Valkey
  MainApi -->|access function lookup| Db[(MainDbContext)]
```

Both sign-in paths converge on `IAuthSessionService.IssueSessionAsync`. The Main API cannot tell how a session was created, and that is deliberate.

## Endpoints

| Endpoint                           | Purpose                                                                                                                            |
| ---------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `POST /api/Auth/Login`             | Verify `{userid, pd}` against `UserAccounts`, issue a session                                                                      |
| `POST /api/Auth/Logout`            | Drop `session:{token}` from Valkey                                                                                                 |
| `GET /api/Auth/Verify`             | Debug helper — is this session token still valid?                                                                                  |
| `POST /api/Auth/Refresh`           | Rotate the session token — writes a new `session:{token}` entry, then retires the old one, and returns the replacement             |
| `POST /api/Auth/GetProfile`        | Name / email / department for a session token                                                                                      |
| `POST /api/Auth/CreateTestSession` | **Development only** — mint a session with no credentials                                                                          |
| `POST /api/Auth/Register`          | Self-service signup with `{ userId, fullName, email, password }`; only when `LocalIdentity:AllowSelfRegistration`                  |
| `POST /api/Auth/ForgotPassword`    | Issue a single-use reset token from `{ userIdOrEmail }`; **always answers 200**                                                    |
| `POST /api/Auth/ResetPassword`     | Consume a reset token and set a new password: `{ token, newPassword }`. No user identifier — the token hash identifies the account |
| `POST /api/Auth/ChangePassword`    | Change password for a signed-in user, re-checking the current one                                                                  |
| `GET /api/Auth/ExternalProviders`  | List enabled + fully configured providers; empty array when the slot is off                                                        |
| `GET /api/Auth/ExternalStart`      | Redirect to a provider's authorization endpoint (auth-code flow with PKCE)                                                         |
| `GET /api/Auth/ExternalCallback`   | Provider redirects back here; mints the same Valkey session a password sign-in would                                               |

## The local identity provider

`ILocalIdentityService` / `LocalIdentityService` owns credentials and nothing else. It does not resolve roles, and it does not create sessions — it returns a `LoginResponse` and the controller hands that to `IAuthSessionService`.

What it does for you, already:

- **PBKDF2 hashing** via `PasswordHasher<UserAccount>`, including transparent **rehash on sign-in** when the hashing parameters move on.
- **Lockout** — counts consecutive failures, locks the account for `LockoutMinutes` after `MaxFailedLoginAttempts`, clears the counter on success.
- **Generic failure messages** — `LoginResponse.errorMessage` is deliberately identical for "no such user", "wrong password", and "account inactive", so nobody can enumerate accounts.
- **Reset tokens stored as SHA-256 hashes**, compared in constant time, single-use, expiring after `PasswordResetTokenTtlMinutes`. The raw token exists only in the response and the email.
- **`ForgotPassword` always returns 200**, whether or not the account exists.

Configuration lives in the `LocalIdentity` section of `src/backend/Auth/appsettings.json`:

| Setting                        | Default | Meaning                                                                                              |
| ------------------------------ | ------- | ---------------------------------------------------------------------------------------------------- |
| `MinPasswordLength`            | `12`    | Length is the most useful password rule — raise this rather than adding character-class requirements |
| `MaxFailedLoginAttempts`       | `5`     | `0` disables lockout entirely (not recommended)                                                      |
| `LockoutMinutes`               | `15`    | How long the lockout lasts                                                                           |
| `PasswordResetTokenTtlMinutes` | `30`    | Reset-token lifetime                                                                                 |
| `AllowSelfRegistration`        | `true`  | Set `false` for an invite-only or internal app                                                       |

`UserAccount` (in `src/backend/Libraries/Domain/Models/UserAccount.cs`) carries `UserId`, `Name`, `Email`, `Department`, `PasswordHash`, `IsActive`, `MustChangePassword`, `FailedLoginCount`, `LockoutEndOn`, `LastLoginOn`, `ExternalProvider`, `ExternalSubject`, `PasswordResetTokenHash`, `PasswordResetExpiresOn`. An account can be local-only, external-only (`PasswordHash` null), or both.

`UserId` is the identity key for the whole system — sessions, audit rows, and `UserRole.UserId` all join on it.

`PasswordHash` and `PasswordResetTokenHash` are explicitly excluded from the audit trail by `MainDbContext.ShouldAuditProperty`. Do not add credential material to any entity without doing the same.

## The optional external provider slot

`IExternalIdpService` / `ExternalIdpService` implements the OAuth 2.0 authorization-code flow with PKCE. It ships **off**: `ExternalIdp:Enabled` is `false` and every provider has empty credentials, so `GET /api/Auth/ExternalProviders` returns `[]` and the login page renders only the password form.

Each provider under `ExternalIdp:Providers` has `Enabled`, `DisplayName`, `ClientId`, `ClientSecret`, `Authority`, `Scopes`, `RedirectUri`, and three endpoint overrides (`AuthorizationEndpoint`, `TokenEndpoint`, `UserInfoEndpoint`). Google and Microsoft publish a discovery document, so setting `Authority` is enough; **GitHub does not**, so it needs the three endpoint overrides instead.

On callback, `ILocalIdentityService.ResolveExternalUserAsync` matches or provisions a `UserAccount` by `(ExternalProvider, ExternalSubject)` — the provider's stable `sub` claim, never the email address, which users can change.

## Demo accounts

Seeded by `MainDbContextSeeder` in **Development only**, so a fresh clone is usable immediately:

| User    | Password      | Role          |
| ------- | ------------- | ------------- |
| `admin` | `Admin@12345` | Administrator |
| `alice` | `Alice@12345` | Administrator |
| `bob`   | `Bob@12345`   | User          |

The seeder refreshes profile fields on every start but **only sets a password when the account has none**, so changing a demo password locally survives a restart.

**These are development fixtures, and the passwords are printed in a public template.** Before anything other people can reach: change them or delete the accounts, and keep the seeder guarded to Development.

## Key entry points

| Layer                  | Path                                                                      | Purpose                                                                                                                                               |
| ---------------------- | ------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| Auth API host          | `src/backend/Auth/Program.cs`                                             | Boots the service: `MainDbContext`, Valkey, CORS, OpenAPI, Sentry+OTel, `PasswordHasher<UserAccount>`, `ILocalIdentityService`, `IExternalIdpService` |
| Auth API controller    | `src/backend/Auth/Controllers/AuthController.cs`                          | Every endpoint in the table above                                                                                                                     |
| Local IDP              | `src/backend/Auth/Services/LocalIdentityService.cs`                       | Credential verification, registration, reset, change, external-account resolution                                                                     |
| External IDP           | `src/backend/Auth/Services/ExternalIdpService.cs`                         | Authorization-code + PKCE flow, provider discovery, claim mapping                                                                                     |
| Session issuer         | `src/backend/Auth/Services/AuthSessionService.cs`                         | `IssueSessionAsync` — the only writer of `session:{token}`                                                                                            |
| Account entity         | `src/backend/Libraries/Domain/Models/UserAccount.cs`                      | The credential record                                                                                                                                 |
| Demo seeding           | `src/backend/Libraries/Data/Data/MainDbContextSeeder.cs`                  | Development-only demo accounts and their role assignments                                                                                             |
| Main API session check | `src/backend/API/Middleware/SessionValidationMiddleware.cs`               | Reads `X-Session-Id`, validates against Valkey, populates `HttpContext.Items`                                                                         |
| User context           | `src/backend/Libraries/Shared/Services/UserContext/UserContextService.cs` | Typed wrapper over the `HttpContext.Items` session keys (contract: `IUserContextService.cs` alongside it)                                             |
| FE login page          | `src/frontend/auth/src/components/LoginPage.vue`                          | Password form, register / forgot-password entry points, provider buttons                                                                              |
| FE auth service        | `src/frontend/main/src/services/authService.ts`                           | `ensureAuthenticated`, `redirectToLogin`, `getAuthLoginUrl`                                                                                           |
| FE auth composable     | `src/frontend/main/src/composables/useAuth.ts`                            | `currentUser`, `isAuthenticated`, `logout`, `hasRole`                                                                                                 |
| Session DTO            | `src/backend/Auth/Models/AuthSessionDto.cs`                               | Shape of the JSON in Valkey: `UserId`, `Name`, `Email`, `Department`, `LastActive`                                                                    |
| Auth config            | `src/backend/Auth/appsettings.json`                                       | `ConnectionStrings`, `Valkey`, `LocalIdentity`, `ExternalIdp`, `ValidSessionTimeInMins`, `AllowedCORSOrigin`, `Sentry`                                |
