# Security Model

This file is the project-specific security source of truth.

Use `docs/templates/security-model-guide.md` as the starting point.

## Minimum Contents

- Roles and access-function model
- Screen and API authorization matrix
- Sensitive data handling rules
- Audit and traceability expectations
- STRIDE or equivalent threat summary

## Authentication Model

- The template supports two permanent authentication entry paths:
  - direct username/password login through `Auth/Login`
  - portal-initiated SSO through `Auth/SsoStart`, `Auth/SsoCallback`, and `Auth/SsoFinalize`
- Both paths must end in the same Auth session contract:
  - Auth stores identity-only session data in Valkey
  - Main API authorization continues to rely on `X-Session-Id` and `UserId`
  - access-control assignments remain keyed by `LoginResponse.userId`

## Portal SSO Trust Rules

- The backend callback is trusted only after decrypting and validating the incoming JWE/JWS payload.
- Required checks for the callback payload:
  - `iss`
  - `aud`
  - `iat`
  - `nbf`
  - `exp`
  - `jti`
  - `state`
  - `nonce`
  - configured source-system identifier
  - configured source URL allowlist, when enabled
- Replay protection is enforced with one-time-use `state` and `jti` cache entries in Valkey.
- IP allowlisting is defense in depth only and cannot replace payload validation.
- The SSO exchange API response is the authoritative identity payload. The application must not invent its own canonical user identity from raw email or username claims.

## Sensitive Data Handling

- Use JOSE standards for portal callback protection:
  - JWE `RSA-OAEP-256` + `A256GCM`
  - signed inner payload validated against the configured signing key
- Provision separate signing and decryption keys per system and per environment. Do not reuse one system's SSO keys in another system.
- Do not introduce custom reversible encryption schemes for usernames or emails.
- Keep signing and decryption keys outside committed secrets; only placeholders belong in template config.

## Update When

- New access functions or privileged flows are added
- Sensitive data handling changes
- Threat boundaries, integrations, or trust assumptions change
