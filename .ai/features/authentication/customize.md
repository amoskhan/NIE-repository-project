# Authentication — Customize

Common customizations and the exact files to edit. Every change must keep the **wire contract** intact: `POST /api/Auth/Login {userid, pd}` → a Valkey-backed session token → `X-Session-Id` on every later request. Breaking that is a redesign, not a customization.

## 1. Change the password policy

All of it is configuration — no code change:

```jsonc
// src/backend/Auth/appsettings.json
"LocalIdentity": {
  "MinPasswordLength": 12,          // raise this; length beats character classes
  "MaxFailedLoginAttempts": 5,      // 0 disables lockout — don't
  "LockoutMinutes": 15,
  "PasswordResetTokenTtlMinutes": 30,
  "AllowSelfRegistration": true
}
```

`MinPasswordLength` is enforced server-side by `LocalIdentityService` on `Register`, `ResetPassword`, and `ChangePassword` alike. Mirror the number in the FE form text (`src/frontend/auth/src/components/LoginPage.vue`) so users see the rule before submitting — but the server check stays authoritative.

## 2. Turn off self-service registration

Set `LocalIdentity:AllowSelfRegistration = false`. `POST /api/Auth/Register` then refuses, and you provision accounts yourself (a seeder, an admin screen, or a migration). Do this for any app that was not meant to have public signup.

## 3. Change the seeded demo accounts

`MainDbContextSeeder` seeds `admin` / `alice` / `bob` in **Development only**.

1. Edit `GetDevelopmentUserAccountSeeds()` in `src/backend/Libraries/Data/Data/MainDbContextSeeder.cs` — the `CreateDevelopmentUserAccount(id, userId, name, email, department, password)` calls.
2. Keep the `UserId` values in sync with `GetDevelopmentUserRoleSeeds()`, or the accounts will exist with no roles.
3. The seeder only sets a password when the account has none, so a locally-changed demo password survives a restart. To force a reset, delete the row and let it re-seed.
4. **Before any deployment other people can reach:** change these passwords or delete the accounts. They are published in this template's source.
5. For a first-run administrator instead of fixed demo users, read the initial credentials from configuration (e.g. `ADMIN_PASSWORD`) and set `MustChangePassword = true` so the account cannot stay on the bootstrap password.

## 4. Add a field to the session payload (e.g. `OfficeLocation`)

1. `src/backend/Auth/Models/AuthSessionDto.cs` — add `public string? OfficeLocation { get; set; }`.
2. `src/backend/Libraries/Shared/Dto/AuthDto.cs` — add the same property; the Main API deserializes through this DTO.
3. `src/backend/Auth/Services/AuthSessionService.cs` — populate it in the `sessionDto` initializer inside `IssueSessionAsync`.
4. `src/backend/Auth/Controllers/AuthController.cs` — populate it in `CreateTestSession` too, so dev sessions don't silently differ.
5. `src/backend/Libraries/Shared/Globals/Constants.cs` — add `KeySessionOfficeLocation`.
6. `src/backend/API/Middleware/SessionValidationMiddleware.cs` — set `context.Items[Constants.KeySessionOfficeLocation]` in the validation happy path.
7. `src/backend/Libraries/Shared/Services/UserContext/UserContextService.cs` (and its `IUserContextService.cs`) — add the typed accessor.
8. If the value comes from the account record, add the column to `UserAccount` and generate a migration. Existing sessions return `null` until the user signs in again.

## 5. Change the session expiry window

1. `src/backend/Auth/appsettings.json` — set `ValidSessionTimeInMins`.
2. `src/backend/API/appsettings.json` — set the **same** value. `SessionValidationMiddleware` re-checks `lastActive + ValidSessionTimeInMins` on every request, so a mismatch produces sessions that one service considers valid and the other does not.
3. Restart both. No migration; existing Valkey entries pick up the new window on their next write.

## 6. Enable an external provider (Google / Microsoft / GitHub)

Ships off. Turn it on only when the project needs it.

1. Register an OAuth application with the provider and collect the **client ID** and **client secret**.
2. Register the redirect URI with the provider — it points at this API's callback:
   `http://localhost:5001/api/Auth/ExternalCallback` in dev, the deployed equivalent otherwise.
3. Edit `src/backend/Auth/appsettings.json`:
   ```jsonc
   "ExternalIdp": {
     "Enabled": true,                       // the master switch
     "Providers": {
       "Google": {
         "Enabled": true,
         "DisplayName": "Google",
         "ClientId": "...",
         "ClientSecret": "",                // leave blank here — see step 4
         "Authority": "https://accounts.google.com",
         "Scopes": "openid profile email",
         "RedirectUri": "http://localhost:5001/api/Auth/ExternalCallback"
       }
     }
   }
   ```
   - **Google:** `Authority` = `https://accounts.google.com`
   - **Microsoft:** `Authority` = `https://login.microsoftonline.com/{tenant}/v2.0`
   - **GitHub:** publishes no discovery document. Leave `Authority` empty and set `AuthorizationEndpoint`, `TokenEndpoint`, and `UserInfoEndpoint` explicitly.
4. Supply the secret out of band — `dotnet user-secrets set "ExternalIdp:Providers:Google:ClientSecret" "..."` locally, or the `ExternalIdp__Providers__Google__ClientSecret` environment variable when deployed. **Never commit it.**
5. Restart the Auth API and check `GET /api/Auth/ExternalProviders` — the provider must appear. An empty array means the master switch is off, the provider is disabled, or a required field is blank; `GetEnabledProviders` only lists providers that are both enabled and complete.
6. Decide the account-linking policy explicitly. `ResolveExternalUserAsync` takes an `allowAutoProvision` flag: `true` creates a `UserAccount` on first sight (friendlier), `false` requires the account to exist already (safer). Pick one deliberately.
7. Nothing downstream changes — the external path ends in `IssueSessionAsync`, exactly like the password path.

## 7. Add a provider that isn't Google / Microsoft / GitHub

Any standards-compliant OIDC issuer works. Add an entry under `ExternalIdp:Providers` keyed by your provider's name, set `Authority` if it publishes a discovery document, or the three endpoint overrides if it doesn't. `ExternalIdpService` is provider-agnostic; there is no per-provider branch to extend.

## 8. Replace the identity backend entirely (LDAP, a campus SSO, a hosted IDP)

1. For a **credential POST** style backend: implement `ILocalIdentityService` against it and swap the registration in `src/backend/Auth/Program.cs`. `AuthController` and `AuthSessionService` need no changes — they only see `LoginResponse`.
2. For a **redirect-based OIDC** backend: use the `ExternalIdp` slot (§6) rather than rewriting `Login`. You keep both paths and the audit trail stays separable.
3. Keep the `UserAccounts` table even if credentials move elsewhere — `UserId` is what `UserRole`, the audit log, and every session join on. Use it as a profile/shadow record.
4. Update the FE login form if the provider needs extra fields (a tenant or campus code).
5. Keep `POST /api/Auth/Login {userid, pd}` responding, or change the FE and the tests in the same commit.

## 9. Redirect to a different post-login page

1. `src/frontend/auth/src/components/LoginPage.vue` — the success handler that sets `window.location.href`.
2. For deep-linking back to the originally requested page, pass it as `returnUrl` and validate it before redirecting. `AuthController.IsSafeReturnUrl` already guards `ExternalStart`; reuse that check rather than trusting the caller. An unvalidated `returnUrl` is an open redirect.
