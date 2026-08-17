# 08 — OWASP Top 10 (2025) + API Security Top 10 Audit Checklist

Source of truth: <https://owasp.org/Top10/2025/> and OWASP API Security Top 10 2023 (refreshed for 2025 reading).

This checklist is the security gate for every PR that touches authentication, authorization, input handling, output, file uploads, external calls, or configuration.

## Web Application Top 10 (2025)

| #     | Risk                                     | Status (template baseline 2026.07.31.1)                                                                                                                                                                             | Where it's enforced                                                                              | Open gaps                                                                                                                                                                                            |
| ----- | ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| W-A01 | Broken Access Control                    | pass — `RequireAccessFunction` + access-function catalog + access-control admin UI                                                                                                                                  | `BaseController`, `AccessControlController`, `AccessFunctionCatalog`                             | Per-record ownership checks (BOLA) — add to feature dossiers                                                                                                                                         |
| W-A02 | Cryptographic Failures                   | partial — sessions in Valkey via TLS; passwords hashed with ASP.NET Core `PasswordHasher<T>` (PBKDF2, per-user salt) in the Auth API. **Verify** TLS-only cookies, `Secure`/`HttpOnly`/`SameSite=Strict` everywhere | Auth API credential service; `Program.cs` antiforgery cookie config; Auth API session cookies    | Audit cookie attributes; document TLS enforcement; confirm no plaintext password is ever logged                                                                                                      |
| W-A03 | Injection                                | pass — EF Core parameterizes; no `FromSqlRaw` allowed                                                                                                                                                               | `02-coding-standards-csharp.md` rule N-20                                                        | —                                                                                                                                                                                                    |
| W-A04 | Insecure Design                          | partial — STRIDE doc exists but not all features run threat model                                                                                                                                                   | `docs/security-model.md`                                                                         | Run STRIDE per feature dossier                                                                                                                                                                       |
| W-A05 | Security Misconfiguration                | pass — `SecurityHeadersMiddleware` emits CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, and Permissions-Policy from bindable `SecurityHeaders` options                                        | `API/Middleware/SecurityHeadersMiddleware.cs`, `SecurityHeadersOptions.cs`, `build/nginx.conf`   | Keep the CSP in sync with any new CDN/asset origin; verify nginx does not weaken the headers                                                                                                         |
| W-A06 | Vulnerable / outdated components         | partial — Sentry/MailKit warnings present in build (NU1902)                                                                                                                                                         | `dotnet list package --vulnerable` + `pnpm audit`                                                | Schedule monthly upgrade task                                                                                                                                                                        |
| W-A07 | Identification & Authentication Failures | partial — local credential check + Valkey session validation with configurable timeout; optional external OIDC ships disabled                                                                                       | `SessionValidationMiddleware`, `AuthSessionService`, Auth API login/registration/reset endpoints | Verify session rotation on login and fixation guard; throttle login, registration, and password-reset endpoints; remove or rotate seeded demo accounts before deploying                              |
| W-A08 | Software & Data Integrity Failures       | partial — no SBOM publish in CI yet                                                                                                                                                                                 | `.github/workflows/`                                                                             | Add SBOM (`dotnet pack --include-source`, `pnpm sbom`) to the CI workflow                                                                                                                            |
| W-A09 | Security Logging & Monitoring Failures   | pass for entity changes (`AuditLog`); manual events via `IAuditLogger`                                                                                                                                              | `MainDbContext.SaveChanges`, `IAuditLogger`                                                      | Verify login failure / access-denied / role-change events are wired                                                                                                                                  |
| W-A10 | Server-Side Request Forgery (SSRF)       | partial — `SsrfGuard.Validate` (HTTPS-only + host allowlist) exists, but nothing forces you to call it                                                                                                              | `Libraries/Shared/Helpers/SsrfGuard.cs`                                                          | Route **every** config-driven or user-influenced outbound URL through `SsrfGuard.Validate` before sending — e.g. a third-party webhook endpoint, an OIDC issuer, or a push-notification API base URL |

## API Security Top 10

| #     | Risk                                            | Status                                                                                                                      | Open gaps                                                                                                |
| ----- | ----------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| API1  | Broken Object Level Authorization (BOLA)        | partial — `RequireAccessFunction` is function-level only                                                                    | Per-record ownership checks need a standard pattern (e.g. `IOwnedEntity` interface + filter) — open task |
| API2  | Broken Authentication                           | pass — `X-Session-Id` validated against Valkey on every Main API request; credentials only ever verified in the Auth API    | Confirm session expiry + rotation on login; rate-limit login and password-reset                          |
| API3  | Broken Object Property Level Authorization      | partial — DTOs flatten what UI needs, but mass-assignment via `Edit` accepts whole DTO                                      | Audit each `Edit` to ensure server-only fields aren't writable from request                              |
| API4  | Unrestricted Resource Consumption               | partial — rate limiting registered globally; pagination caps not enforced                                                   | Cap PageSize at 100 (rule N-17)                                                                          |
| API5  | Broken Function Level Authorization             | pass                                                                                                                        | —                                                                                                        |
| API6  | Unrestricted Access to Sensitive Business Flows | gap — anti-automation patterns not in template (e.g. captcha, throttle on self-service registration or approval submission) | Document per-flow; add per-user throttling on account-creation and approval flows                        |
| API7  | Server-Side Request Forgery                     | partial (see W-A10)                                                                                                         | Call `SsrfGuard.Validate` on every outbound URI                                                          |
| API8  | Security Misconfiguration                       | partial (see W-A05)                                                                                                         | Headers shipped; still audit CORS origins per environment                                                |
| API9  | Improper Inventory Management                   | pass - built-in OpenAPI is mapped in development only                                                                       | Keep production API inventory private                                                                    |
| API10 | Unsafe Consumption of APIs                      | partial — third-party responses (push provider, AI provider, any project webhook) are not strictly schema-validated         | Validate all 3rd-party JSON via a typed DTO + explicit checks; never trust a status code alone           |

## Outbound call pattern (W-A10 / API7)

Any URL that comes from configuration, a database row, or user input can be repointed at an internal address. Validate before you send — never after:

```csharp
// Settings: WebhookSettings { string Url; string[] AllowedHosts; }
var uri = SsrfGuard.Validate(
    _settings.Url,
    _settings.AllowedHosts,      // e.g. ["hooks.example.com", "*.partner.example.net"]
    "Project webhook");          // label shown to operators if the guard trips

var response = await _httpClient.PostAsJsonAsync(uri, payload);
```

`SsrfGuard.Validate` requires an absolute HTTPS URL whose host matches the allowlist, and throws otherwise. An empty allowlist is a hard failure, not an open door. Apply the same pattern to any OIDC issuer URL if a project enables the optional external identity slot.

## How to run a full audit

```bash
# .NET vulnerability scan
dotnet list src/backend/AppTemplate.sln package --vulnerable

# Frontend audit
cd src/frontend && pnpm audit

# Header smoke test
curl -I http://localhost:5002/api/Code/GetAll | grep -iE "strict-transport|content-security|x-content-type|x-frame|referrer-policy|permissions-policy"
```

## Open follow-up tasks (template baseline 2026.07.31.1)

- `[x] W-A05 / API8 — SecurityHeadersMiddleware (CSP, HSTS, X-Frame-Options, etc.) shipped; keep nginx config aligned`
- `[ ] W-A10 / API7 — route every outbound integration through `SsrfGuard.Validate``
- `[ ] W-A07 — rate-limit login, self-service registration, and password reset; document demo-account removal`
- `[ ] API1 — standardize per-record ownership pattern (`IOwnedEntity` + auth filter)`
- `[ ] API3 — audit every controller's `Edit` for accepted server-side fields`
- `[ ] API4 / N-17 — enforce max PageSize`
- `[x] API9 - remove production Swagger UI; expose built-in OpenAPI only in development`
- `[ ] API10 — typed validation of all external API responses`
- `[ ] W-A08 — SBOM generation in the GitHub Actions build workflow`
- `[ ] W-A06 — monthly dependency upgrade task (recurring)`
