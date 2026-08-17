# Authentication — Verify

Use this when you change anything in `Auth/`, `UserAccount`, `SessionValidationMiddleware`, or any session-touching code.

## Backend

```bash
dotnet build src/backend/AppTemplate.sln
dotnet run --project src/backend/Auth   # port 5001
dotnet run --project src/backend/API    # port 5002
```

- Auth API should log `Now listening on: http://localhost:5001`.
- Main API should log `Now listening on: http://localhost:5002`.
- The Auth API needs `ConnectionStrings:MainDbConnection` to reach the same database as the Main API — a connection error at boot usually means those two drifted apart.

## API smoke — local sign-in

Development seeds three accounts: `admin` / `Admin@12345`, `alice` / `Alice@12345`, `bob` / `Bob@12345`.

```bash
# 1. Sign in
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/Login \
  -H "Content-Type: application/json" \
  -d '{"userid":"alice","pd":"Alice@12345"}' | jq -r .sessionToken)

echo "Session: $SESSION"
# Expect: a hex token, not "null"

# 2. Wrong password → 401
curl -s -X POST http://localhost:5001/api/Auth/Login \
  -H "Content-Type: application/json" \
  -d '{"userid":"alice","pd":"wrong"}' -w "\n%{http_code}\n"

# 3. Unknown user → the SAME status and the SAME body as step 2
curl -s -X POST http://localhost:5001/api/Auth/Login \
  -H "Content-Type: application/json" \
  -d '{"userid":"no-such-person","pd":"wrong"}' -w "\n%{http_code}\n"
# Expect: byte-identical to step 2. Any difference is an account-enumeration oracle.

# 4. Missing fields → 401, not 400
curl -s -X POST http://localhost:5001/api/Auth/Login \
  -H "Content-Type: application/json" -d '{}' -w "\n%{http_code}\n"
# Expect: 401
```

## Lockout

```bash
# With MaxFailedLoginAttempts = 5, six wrong guesses should lock the account
for i in $(seq 1 6); do
  curl -s -o /dev/null -w "attempt $i: %{http_code}\n" -X POST http://localhost:5001/api/Auth/Login \
    -H "Content-Type: application/json" -d '{"userid":"bob","pd":"wrong"}'
done

# Now the CORRECT password must also be refused until the lockout expires
curl -s -o /dev/null -w "correct password while locked: %{http_code}\n" -X POST http://localhost:5001/api/Auth/Login \
  -H "Content-Type: application/json" -d '{"userid":"bob","pd":"Bob@12345"}'
# Expect: 401
```

```sql
SELECT "UserId", "FailedLoginCount", "LockoutEndOn" FROM "UserAccounts" WHERE "UserId" = 'bob';
-- Expect: FailedLoginCount at the cap and LockoutEndOn in the future.
-- After a successful sign-in, FailedLoginCount must return to 0.
```

## Session plumbing

```bash
# Mint a dev session with no credentials (Development only)
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"alice","Name":"Alice Tan","Email":"alice@example.edu","Department":"Digital Services"}' \
  | jq -r .sessionToken)

curl -s http://localhost:5001/api/Auth/Verify -H "X-Session-Id: $SESSION" | jq

# The Main API accepts the same session
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile -H "X-Session-Id: $SESSION"
# Expect: 200

# Missing session → 401
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile
# Expect: 401

# Bogus session → 401
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile -H "X-Session-Id: not-a-real-token"
# Expect: 401

# Logout removes the session
curl -s -X POST http://localhost:5001/api/Auth/Logout -H "X-Session-Id: $SESSION"
curl -s -o /dev/null -w "after logout: %{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile -H "X-Session-Id: $SESSION"
# Expect: 401
```

`CreateTestSession` must be unreachable outside Development:

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/backend/Auth
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" -d '{"UserId":"alice"}'
# Expect: 404
```

## Registration, reset, change

```bash
# Register (only when LocalIdentity:AllowSelfRegistration is true)
curl -s -X POST http://localhost:5001/api/Auth/Register \
  -H "Content-Type: application/json" \
  -d '{"UserId":"carol","FullName":"Carol Ng","Email":"carol@example.edu","Password":"CorrectHorseBattery1"}' \
  -w "\n%{http_code}\n"

# Too-short password must be refused by the SERVER, not just the form
curl -s -X POST http://localhost:5001/api/Auth/Register \
  -H "Content-Type: application/json" \
  -d '{"UserId":"dave","Password":"short"}' -w "\n%{http_code}\n"
# Expect: 400 citing MinPasswordLength

# With AllowSelfRegistration = false, Register must refuse outright
```

```bash
# ForgotPassword answers 200 for a known AND an unknown account, identically
curl -s -X POST http://localhost:5001/api/Auth/ForgotPassword \
  -H "Content-Type: application/json" -d '{"UserIdOrEmail":"carol@example.edu"}' -w "\n%{http_code}\n"
curl -s -X POST http://localhost:5001/api/Auth/ForgotPassword \
  -H "Content-Type: application/json" -d '{"UserIdOrEmail":"nobody@example.edu"}' -w "\n%{http_code}\n"
# Expect: same status, same body

# Take the token from the reset email in Mailpit (http://localhost:8025).
# The reset token is self-identifying: ResetPassword takes NO UserId — the
# account is resolved from the single-use token hash alone.
curl -s -X POST http://localhost:5001/api/Auth/ResetPassword \
  -H "Content-Type: application/json" \
  -d '{"Token":"<token>","NewPassword":"AnotherLongPassphrase2"}' -w "\n%{http_code}\n"
# Expect: 200

# Replaying the same token must fail — tokens are single-use
curl -s -X POST http://localhost:5001/api/Auth/ResetPassword \
  -H "Content-Type: application/json" \
  -d '{"Token":"<same-token>","NewPassword":"YetAnotherPassphrase3"}' -w "\n%{http_code}\n"
# Expect: 400/401

# ChangePassword requires a session AND the current password
curl -s -X POST http://localhost:5001/api/Auth/ChangePassword \
  -H "Content-Type: application/json" -H "X-Session-Id: $SESSION" \
  -d '{"CurrentPassword":"wrong","NewPassword":"WillNotBeAccepted4"}' -w "\n%{http_code}\n"
# Expect: 400/401
```

## Credential storage (run once, and after any change to the hashing path)

```sql
-- Hashes must be opaque, and two users with the SAME password must have DIFFERENT hashes.
SELECT "UserId", left("PasswordHash", 20) AS hash_prefix, "PasswordResetTokenHash" IS NOT NULL AS has_reset_token
FROM "UserAccounts";

-- The raw reset token must NEVER appear in the table — only its SHA-256 hash.
```

Credential material must not reach the audit trail:

```sql
SELECT count(*) FROM "AuditLogs"
WHERE "AdditionalData" ILIKE '%PasswordHash%'
   OR "AdditionalData" ILIKE '%PasswordResetTokenHash%';
-- Expect: 0. MainDbContext.ShouldAuditProperty excludes both.

-- UserAccount changes should be categorised as AccessControl, not Data.
SELECT DISTINCT "Category" FROM "AuditLogs" WHERE "EntityName" = 'UserAccount';
```

And nothing should leak into the logs:

```bash
grep -rniE "\"pd\"|PasswordHash|Alice@12345" ./logs 2>/dev/null
# Expect: no hits containing an actual credential value
```

## External provider

With the shipped defaults (`ExternalIdp:Enabled = false`):

```bash
curl -s http://localhost:5001/api/Auth/ExternalProviders | jq
# Expect: []  — the login page then renders only the password form

curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:5001/api/Auth/ExternalStart?provider=Google"
# Expect: 503 — never a redirect to a half-configured provider

curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:5001/api/Auth/ExternalCallback?code=x&state=y"
# Expect: 503
```

Once enabled and configured:

```bash
curl -s http://localhost:5001/api/Auth/ExternalProviders | jq
# Expect: one entry per fully-configured provider, with name / displayName / startUrl.
# An empty array means a required field is still blank.

curl -s -o /dev/null -w "%{http_code}\n" -D - \
  "http://localhost:5001/api/Auth/ExternalStart?provider=Google" | grep -i "^location:"
# Expect: 302 to the provider's authorize endpoint, with client_id, redirect_uri,
# code_challenge, and code_challenge_method=S256 in the query string.

# An unsafe returnUrl must be rejected, not followed
curl -s -o /dev/null -w "%{http_code}\n" \
  "http://localhost:5001/api/Auth/ExternalStart?provider=Google&returnUrl=https://evil.example.com/"
# Expect: 400
```

Manual click-path once enabled: click the provider button → authenticate at the provider → land on the callback → get redirected to the main FE with a session cookie. The Main API must accept that session exactly like a password-issued one, and a `UserAccount` row should exist with `ExternalProvider` and `ExternalSubject` populated.

## Session refresh (token rotation)

`Refresh` does not extend the existing token — it mints a replacement, then retires the old one.

```bash
NEW=$(curl -s -X POST http://localhost:5001/api/Auth/Refresh \
  -H "Content-Type: application/json" -H "X-Session-Id: $SESSION" | tr -d '"')
echo "old=$SESSION new=$NEW"
# Expect: a different token, or 401 if the session had already expired.

# The new token works
curl -s -o /dev/null -w "new: %{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile -H "X-Session-Id: $NEW"
# Expect: 200

# The old token no longer does
curl -s -o /dev/null -w "old: %{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile -H "X-Session-Id: $SESSION"
# Expect: 401
```

## Frontend (manual click-path)

1. Start the full stack via the `🚀 All Services (Hot Reload)` task.
2. Open `http://localhost:8001` (Auth FE) — the login form renders.
3. Sign in as `alice` / `Alice@12345` — the page sets the session and user cookies named by `FRONTEND_CONSTANTS.cookies` and redirects to the main FE.
4. DevTools → Application → Cookies — the session and user cookies exist with `Path=/` and the right domain.
5. Network tab — every request to `:5002` carries `X-Session-Id: <token>`.
6. Profile menu → Logout — cookies cleared, browser back at the Auth FE.
7. Delete the session cookie and click a sidebar link — the FE redirects to the login page.
8. Walk "forgot password" end to end: request a reset, open Mailpit, follow the link, set a new password, sign in with it.

## Audit and observability

- `AuditLog`: a `Login` row (`Action = 10`, `Category = Authentication`) for the user who just signed in.
- Sentry: with `Sentry:Dsn` configured, a forced exception in `AuthController.Login` appears within 60 seconds.
- Valkey (never paste real tokens): `redis-cli -p 6379 KEYS "session:*"` shows one entry per active session.

## Permissions

A user with no roles still gets a valid session — the Main API returns `403` from any `[RequireAccessFunction(...)]` endpoint until a role is assigned. `GetCurrentAccessProfile` is intentionally NOT gated, so the FE can render the empty state. Seeded `admin` and `alice` are Administrators; `bob` is a plain User, which makes him the right account for testing that a permission is actually enforced.
