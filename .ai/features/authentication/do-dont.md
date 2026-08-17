# Authentication — Do and Don't

## DO ✅

1. **DO** read the session via `IUserContextService` (or `HttpContext.Items[Constants.KeySessionUserId]`) — never read the `X-Session-Id` header directly in a controller or service. The middleware has already validated and unpacked it.
2. **DO** use `[AllowAnonymous]` on any endpoint that must skip session validation. `SessionValidationMiddleware` checks for `endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>()` and bypasses the Valkey lookup. The skip-by-path list (`/openapi`, `/health`, `/favicon.ico`, `/tickerq`) is the only other escape hatch.
3. **DO** write to `session:{token}` ONLY through `AuthSessionService.IssueSessionAsync`. That keeps the entry shape, expiry, and key prefix in one place — and it is exactly what lets the local and external sign-in paths share everything downstream.
4. **DO** go through `ILocalIdentityService` for anything touching credentials. It already handles lockout accounting, hash upgrade-on-signin, constant-time reset-token comparison, and generic error messages. Reimplementing any of that by hand will be worse.
5. **DO** keep failure messages generic. `LoginResponse.errorMessage` is deliberately identical for unknown user, wrong password, and inactive account. Splitting them apart to "improve the UX" hands attackers an account-enumeration oracle.
6. **DO** keep `ForgotPassword` answering `200` whether or not the account exists. Same reason.
7. **DO** raise `LocalIdentity:MinPasswordLength` rather than adding character-class rules. Length is the rule that actually helps; `!` on the end of a short password is not entropy.
8. **DO** keep lockout on. `MaxFailedLoginAttempts: 0` disables it entirely and turns `Login` into an unlimited guessing endpoint.
9. **DO** set `LocalIdentity:AllowSelfRegistration = false` for an invite-only or internal app. Public `Register` on an app that was never meant to have public signup is a common and avoidable mistake.
10. **DO** change or delete the seeded demo accounts (`admin` / `alice` / `bob`) before anyone else can reach the app. Their passwords are published in this template's source.
11. **DO** keep `CreateTestSession` behind `_environment.IsDevelopment()`. Without that guard it is a session-minting endpoint that requires no credentials.
12. **DO** use `DateTimeHelper.Now` for `LastActive` so the value matches the app's configured timezone.
13. **DO** redirect through `authService.redirectToLogin()` (FE) on every 401, so the cookie is cleared before the browser re-enters the login flow. A manual `window.location.href = "/"` skips that cleanup.
14. **DO** validate any `returnUrl` against the safe-URL check before redirecting. `ExternalStart` already does this — keep it that way when you add redirect targets.
15. **DO** match external accounts on the provider's `sub` claim (`UserAccount.ExternalSubject`), never on email. Users change email addresses; `sub` is stable, and matching on email lets anyone who can register that address take over the account.

## DON'T ❌

1. **DON'T** invent your own password hashing. No MD5, no SHA-256-with-a-static-salt, no reversible "encryption so we can email it back". `PasswordHasher<UserAccount>` is registered and correct.
2. **DON'T** store, log, or return a password or a password hash. Not in a DTO, not in an audit row, not in a debug log, not in an exception message.
3. **DON'T** add credential fields to an entity without excluding them from auditing. `MainDbContext.ShouldAuditProperty` skips `PasswordHash` and `PasswordResetTokenHash`; anything similar you add must be skipped too, or the secret ends up copied into `AuditLogs`.
4. **DON'T** log the full session token. Log `userId` / `correlationId` / a hash if you must — leaking the token is equivalent to leaking a password.
5. **DON'T** store the raw password-reset token. Only its SHA-256 hash is persisted; the raw value exists once, in the response and the email. Storing it means a database read is a password reset.
6. **DON'T** put roles, permissions, or access functions inside the Valkey session blob. They are fetched by the FE from the Main API (`AccessControlController.GetCurrentAccessProfile`). In the session, they go stale the moment a role changes.
7. **DON'T** read `Request.Cookies["SessionToken"]` from a Main API controller. The cookie is FE-only; the Main API contract is the `X-Session-Id` header.
8. **DON'T** call `IDistributedCache.SetStringAsync("session:...", ...)` from anywhere outside `AuthSessionService`.
9. **DON'T** trust claims-based `IsAuthenticated` from ASP.NET in the Main API — this project uses session-based auth, not JWT bearer. `[Authorize]` will not behave as you expect; use `[RequireAccessFunction(...)]`.
10. **DON'T** rename `LoginRequest.userid` / `pd`. They are the published wire contract shared by the Vue login page and the API tests.
11. **DON'T** make `LoginRequest`'s properties non-nullable. They are nullable so a blank field returns a plain 401 instead of a 400 model-binding error that describes the request shape.
12. **DON'T** commit an `ExternalIdp` client secret. Use `dotnet user-secrets` locally and `ExternalIdp__Providers__Google__ClientSecret` (or a secret store) when deployed.
13. **DON'T** enable a provider you have not fully configured. `GetEnabledProviders` only lists providers that are both enabled and complete, so a half-configured entry silently does not appear — and you will spend an afternoon wondering why the button is missing.
14. **DON'T** set GitHub's `Authority` and expect discovery to work. GitHub publishes no OIDC discovery document; set `AuthorizationEndpoint`, `TokenEndpoint`, and `UserInfoEndpoint` explicitly.
15. **DON'T** hardcode the `session:` cache prefix in new code. If you change it, change every place that reads or writes it (and consider promoting it to a `Constants.SessionCachePrefix`).
16. **DON'T** call `/api/Auth/Verify` from a hot path. It is a debug helper; per-request validation already happens in `SessionValidationMiddleware`.
