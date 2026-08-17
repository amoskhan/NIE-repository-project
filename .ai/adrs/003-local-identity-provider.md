# Local Identity Provider with an Optional External OIDC Slot

## Metadata

- **Date:** 2026-07-31
- **Status:** Accepted
- **Deciders:** App Template maintainers
- **AI Model Used:** Claude Opus 5

## Context

The template previously delegated authentication to an external, institution-specific identity provider reached through an API gateway. Login credentials were posted to the Auth API, forwarded to that provider, and a Valkey-backed session was minted from the provider's answer.

That coupling is unusable outside the issuing institution:

- The provider requires credentials, network allowlisting, and an account that a student team cannot obtain.
- Nothing runs offline — a laptop with no access to that network cannot log in at all, so the whole template is unbootable.
- The provider is a real third party. Pointing a course project at it is both a support burden and a privacy problem.

The template still needs the rest of its stack to work unchanged: role and access-function authorization, audit logging, session validation on the Main API, and the login UI. So whatever replaces the external provider must keep the same wire contract.

## Options Considered

### Option A: Keep an external identity provider, make its base URL configurable

**Description:** Leave the federation code in place and let each project point it at their own OIDC/SAML provider.

- **Pros:** No new authentication code to own or secure; matches what a production deployment would eventually do.
- **Cons:** The template does not run out of the box. Every team must register an application with some provider before their first login, which is the single worst possible first-run experience for a student project.

### Option B: Local identity provider inside the Auth API

**Description:** Add a `users` table and verify credentials in the Auth API using the ASP.NET Core `PasswordHasher<T>` (PBKDF2 with per-user salt). Add self-service registration and password reset, and seed demo accounts for local development. The Auth API keeps minting the same Valkey session it always did.

- **Pros:** Clones and runs offline with zero external accounts; the entire auth surface is readable code that students can study and extend; keeps the existing session contract, so nothing downstream changes.
- **Cons:** The template now owns credential-handling code, which is security-sensitive and must be reviewed (hashing, throttling, enumeration resistance, reset-token lifetime). Seeded demo accounts are a deployment hazard if they are not removed.

### Option C: Local identity provider **plus** a disabled external OIDC slot

**Description:** Option B, and in addition a configuration-driven external OIDC provider (Google / Microsoft / GitHub) that ships **disabled**. When a project enables it, the OIDC callback still terminates in the Auth API and still mints the same Valkey session.

- **Pros:** Everything in Option B, and a project that later wants "Sign in with Google" has a defined, already-wired place to put it instead of inventing a parallel auth path.
- **Cons:** A second code path to keep correct even while it is switched off; a misconfigured slot could be enabled accidentally.

## Decision

Adopt **Option C**.

- The Auth API (port 5001) is the only service that touches credentials or mints sessions. Passwords are hashed with `PasswordHasher<T>` and are never stored, logged, or returned in plaintext.
- Self-service registration and password reset ship with the template. Demo accounts are seeded for local development and must be removed or rotated before any real deployment.
- The wire contract is unchanged: `POST /api/Auth/Login { userid, pd }` → Valkey-backed session token → `X-Session-Id` header on every subsequent request → `SessionValidationMiddleware` on the Main API. Existing sessions, role resolution, and access functions are untouched.
- The external OIDC slot is configuration-only and defaults to **disabled**. Enabling it does not change the downstream contract: the callback terminates in the Auth API, which mints the same session.
- Removed together with the old provider: the API-gateway integration, the national-identity data lookup it enabled, and the institutional portal single-sign-on bridge. None of them have a replacement, and no code, config, or docs should reference them.

## Consequences

- **Positive:** The template clones and runs with no external accounts, no gateway, and no network dependency. First login works offline.
- **Positive:** Authentication becomes teaching material — hashing, sessions, and reset flows are visible, testable code rather than an opaque remote call.
- **Positive:** Downstream surfaces (Main API session validation, RBAC, audit) required no change, because the session contract was preserved.
- **Negative:** The template now carries security-sensitive credential code and must keep it hardened (rate limiting on login/registration/reset, no user enumeration, short-lived reset tokens).
- **Negative:** Two authentication paths exist even though one is off by default.
- **Risks:** Seeded demo accounts reaching a deployed environment. A project enabling the OIDC slot without allowlisting the issuer URL (route it through `SsrfGuard.Validate`). Password-reset tokens that are too long-lived or not single-use.

## AI Reasoning Chain

> The template's value is that it runs immediately and shows a complete, honest implementation of every cross-cutting concern. An external identity provider breaks the first property (nothing works without an account nobody can get) and hides the second (auth becomes a remote call you cannot read). A local provider restores both, and the ASP.NET Core `PasswordHasher<T>` means the risky part — the hashing itself — is a well-reviewed framework primitive rather than hand-rolled crypto. Preserving the existing `{userid, pd}` → session-token → `X-Session-Id` contract was the constraint that made this a contained change: session validation, RBAC, and audit logging never learn that the identity source changed. The optional OIDC slot is included because "let us sign in with a real provider later" is the most predictable next request, and a defined disabled slot is far safer than a team inventing a second auth path under deadline.
