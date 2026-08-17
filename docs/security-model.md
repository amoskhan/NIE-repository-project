# Security Model

This file is the project-specific security source of truth.

The **baseline** below describes what the template already gives you and the threats it already handles. Read it, then extend it with your own roles, screens, and data rules — use `docs/templates/security-model-guide.md` for the structure of the parts you add.

## Minimum Contents (what you add for your project)

- Roles and access-function model
- Screen and API authorization matrix
- Sensitive data handling rules
- Audit and traceability expectations
- STRIDE or equivalent threat summary

---

## Baseline: Authentication Model

The template ships a **self-contained local identity provider**. There is no external identity service to register with, no VPN, no partner keys to exchange. Everything needed to authenticate a user lives in this repository and in your own database.

| Concern                             | Where it lives                                                                 |
| ----------------------------------- | ------------------------------------------------------------------------------ |
| User records                        | Users table in the application database                                        |
| Password hashing                    | ASP.NET Core `PasswordHasher<TUser>` (PBKDF2, per-user salt, versioned format) |
| Login, registration, password reset | `src/backend/Auth/` — the Auth API, port 5001                                  |
| Session store                       | Valkey, key `session:{sessionToken}`                                           |
| Session validation                  | `src/backend/API/Middleware/SessionValidationMiddleware.cs`                    |
| Authorization                       | `[RequireAccessFunction]` on every Main API endpoint                           |

### The wire contract

1. `POST /api/Auth/Login` with `{ userid, pd }`. The Auth API looks the user up, verifies the password hash, and — only on success — issues an opaque session token.
2. The session token is stored in Valkey as a JSON payload (`UserId`, `Name`, `Email`, `Department`, `LastActive`) under `session:{token}`, with an expiry derived from `ValidSessionTimeInMins`.
3. The browser sends that token on every Main API request as the `X-Session-Id` header.
4. `SessionValidationMiddleware` reads the header, loads the session from Valkey, checks the idle window against `LastActive`, and populates `HttpContext.Items` with the user identity. No session, expired session, or missing header means `401`.
5. Roles and access functions are **not** in the session payload. The frontend fetches them from the Main API after login, and the Main API re-derives them from the database on every authorization check.

### Non-negotiable properties of the contract

- The Auth API is the only service that mints sessions. The Main API only reads and validates them.
- The Auth API stays out of the application database except for the identity tables. Role and permission resolution belongs to the Main API.
- Access-control assignments are keyed by the login response's `userId`. Do not key them on email, display name, or anything else the user can change.
- A session token is an opaque random value. It carries no claims, so nothing about it can be forged or replayed into elevated access without the server-side record.

### Optional external OIDC

A config-driven slot exists for one external provider (Google, Microsoft, or GitHub). **It ships disabled.** When you enable it:

- Store the client secret in user secrets or an environment variable. Never in `appsettings.json`, never in git.
- Use the authorization-code flow with PKCE. Do not use the implicit flow.
- Validate `state` on the callback (CSRF on the OAuth handshake) and `nonce` in the ID token (token-injection).
- Pin the issuer and audience, and fetch signing keys from the provider's JWKS endpoint rather than pinning a key by hand.
- Register the exact redirect URI with the provider. Wildcards are an open redirector.
- Map the provider's stable subject identifier (`sub`) to a local user record. Do **not** trust `email` as the join key — email is re-assignable at most providers.
- An external login still ends in the same local session: same Valkey record, same `X-Session-Id` header, same access functions. There is no second authorization path.

---

## Baseline: Threat Model

### Password storage

| Threat                                 | Control                                                                                                                                                                 |
| -------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Database dump reveals usable passwords | Passwords are stored only as `PasswordHasher` output — PBKDF2 with a per-user random salt and a version prefix. Plaintext and reversible encryption are both forbidden. |
| Hash format ages out                   | The `PasswordHasher` format is versioned. When it reports a rehash is needed on a successful login, rehash and persist.                                                 |
| Weak passwords                         | Enforce a minimum length server-side in the registration and reset handlers. Length beats composition rules; do not add rules the user will defeat with `Password1!`.   |
| Password leaks through logs or errors  | Never log the `pd` field, never echo it in an error, never put it in a Sentry breadcrumb. `SendDefaultPii` stays disabled.                                              |
| Password leaks through the API surface | No endpoint returns a password hash. Profile responses expose display fields only.                                                                                      |

### Credential stuffing and brute force

| Threat                                        | Control                                                                                                                                                                  |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Online password guessing                      | Rate-limit `POST /api/Auth/Login` per user id and per source IP. Apply a lockout or exponential backoff after repeated failures on the same account.                     |
| Account enumeration on login                  | Return one generic failure message for "no such user" and "wrong password". Never distinguish them.                                                                      |
| Account enumeration on registration and reset | Both endpoints respond identically whether or not the address is known. The reset email is the only channel that reveals existence.                                      |
| Timing-based enumeration                      | Verify a dummy hash when the user is not found, so the response time does not betray existence.                                                                          |
| Automated signup abuse                        | Registration is the most exposed endpoint in the template. Rate-limit it, and gate it behind email verification or an invite code if your project is publicly reachable. |

### Session security

| Threat                                      | Control                                                                                                                                                                                                                                                          |
| ------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Session fixation                            | A **new** token is generated on every successful login. Never accept a caller-supplied session id and never reuse the pre-login token. Any privilege change — a successful login, a password change, an elevation — must issue a new token and drop the old one. |
| Session hijacking in transit                | HTTPS everywhere off localhost. HSTS in production. The token is a bearer credential; anyone holding it is the user.                                                                                                                                             |
| Session theft via XSS                       | Vue escapes interpolated content by default. Do not use `v-html` with anything a user can influence. Keep security headers on (`SecurityHeadersMiddleware`) and keep the CSP tight.                                                                              |
| Cross-site request forgery                  | The session travels in the `X-Session-Id` header, not as an ambient cookie, so a cross-site form post cannot carry it. If you introduce cookie-borne auth, you also introduce a CSRF-token requirement.                                                          |
| Stolen token used forever                   | Sessions expire on an idle window (`ValidSessionTimeInMins`, checked against `LastActive` on every request). Expired records are deleted from Valkey when they are found.                                                                                        |
| Session survives logout                     | `POST /api/Auth/Logout` deletes `session:{token}` from Valkey. Revocation is immediate and server-side because the token carries no claims — this is the main reason the template does not hand out JWTs to browsers.                                            |
| Session survives a password change          | Delete every session belonging to that user on password change or reset. This is the control that actually evicts an attacker.                                                                                                                                   |
| Stale permissions                           | Access functions are resolved from the database per request, not cached in the token. A revoked role takes effect on the user's next request.                                                                                                                    |
| Dev-only session minting reaches production | The test-session endpoint is guarded by `IsDevelopment()`. Do not remove that guard, and do not add a similar one.                                                                                                                                               |

### Password reset

| Threat                                  | Control                                                                                                          |
| --------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| Guessable reset token                   | Use a cryptographically random token of at least 128 bits. Never a counter, a timestamp, or a hash of the email. |
| Reset token reuse                       | Single use. Consume it on success and on the first failed attempt to spend it.                                   |
| Reset token lives too long              | Short TTL — minutes to an hour, not days.                                                                        |
| Reset token stolen from the database    | Store a hash of the token, compare hashes. The plaintext only ever exists in the email.                          |
| Reset link used to take over an account | Reset invalidates all existing sessions for that user and does not itself log the user in.                       |
| Host-header poisoning in the reset link | Build the link from configuration, never from the inbound `Host` header.                                         |

### Authorization

| Threat                                        | Control                                                                                                                                                                                             |
| --------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Missing authorization on a new endpoint       | Every Main API endpoint carries `[RequireAccessFunction(...)]`. An endpoint with no attribute is reachable by any authenticated user — that is the single most common defect to look for in review. |
| Client-side-only permission checks            | Hiding a menu item is a usability feature, not a control. `app-config/navigation.ts` and the API attribute must always agree, and the API is the one that decides.                                  |
| Broken object-level authorization (IDOR/BOLA) | For records with per-user or per-department ownership, use `EnsureOwnedAsync` on `BaseController` or `[RequireOwnership]`. Owning a valid session does not entitle you to record `id=1`.            |
| Privilege escalation through mass assignment  | Controllers load the entity and assign fields explicitly. Never map a request DTO wholesale onto a loaded entity, and never let a request set an owner id, a role, or an audit column.              |
| Frontend and backend permission codes drift   | `src/frontend/main/src/app-config/accessFunctions.ts` mirrors `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs`. The backend is authoritative; the mirror exists for typing.         |

### Data and platform

| Threat                      | Control                                                                                                                                                                              |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| SQL injection               | EF Core parameterizes everything. If you must write raw SQL, parameterize it — never interpolate user input.                                                                         |
| Server-side request forgery | Any outbound HTTP call built from user-supplied input goes through the SSRF allowlist guard.                                                                                         |
| Unbounded queries           | Paginated list endpoints use the shared page-size cap. An uncapped `take` is a denial-of-service primitive.                                                                          |
| Malicious file upload       | Validate content type and size, store with a generated name, and serve downloads with an explicit content type and `Content-Disposition`. Never execute or include an uploaded file. |
| Secrets in the repository   | Config files carry empty placeholders only. Real values come from user secrets, environment variables, or your deployment's secret store.                                            |
| No trail after an incident  | Audit logging records who did what and when, with a retention purge job. Do not disable it to make a test pass.                                                                      |
| Errors leak internals       | `ExceptionHandlingMiddleware` returns a generic message; details appear only in Development.                                                                                         |

---

## Sensitive Data Handling

- Classify what your project stores before you store it. Anything personal raises the bar on retention, logging, and access.
- Do not put personal data in log messages, exception text, URLs, or Sentry events.
- Do not invent your own encryption. If a field genuinely needs encryption at rest, use a vetted library and keep the key outside the repository.
- Keep secrets out of the frontend bundle. Anything the browser downloads is public — the runtime configuration slots are for non-secret values only.
- Prefer deleting data you no longer need over securing it forever.

## Update When

- New access functions or privileged flows are added
- Sensitive data handling changes
- Threat boundaries, integrations, or trust assumptions change
- An external identity provider is enabled
