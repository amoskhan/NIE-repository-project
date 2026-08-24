# Portal SSO Integration Guide

Detailed onboarding steps for enabling permanent dual-path authentication in a project created from the NIE Template.

This guide covers:

- what an application team must do after creating a new project from the template
- what the portal/platform team must do to integrate that same project into the portal
- key management rules for keeping SSO isolated per system and per environment
- the repository boundary between template consumers and internal portal implementation code

---

## Scope

The NIE Template supports two permanent login paths:

- direct username/password login through `Auth/Login`
- portal-initiated SSO through `Auth/SsoStart`, `Auth/SsoCallback`, and `Auth/SsoFinalize`

Both paths must end the same way:

- Auth creates the same Valkey-backed session
- the auth frontend writes the same cookie/session token
- the Main API continues to authorize by `UserId` from the Auth session

The SSO path replaces only the upstream credential-validation step. It does not replace downstream session creation, role lookup, or authorization behavior.

---

## Audience Split

There are two distinct audiences for this process.

### Application Team

The project team that creates a new system from the NIE Template is responsible for:

- enabling and configuring the Auth-side SSO endpoints
- registering system-specific values such as audience, source-system ID, and allowed source URL
- validating that manual login and SSO both work
- keeping project-specific secrets out of source control

### Portal / Platform Team

The portal or platform team is responsible for:

- holding the portal-side SSO sender implementation
- minting the callback artifact
- signing the inner token
- encrypting the outer token
- calling the application callback endpoint
- redirecting the browser back to the application

The application team should not need the portal's full implementation details to consume SSO safely. They only need the contract and configuration values described in this document.

---

## Repository Boundary

The `tools/` folder currently acts as local reference material for SSO development and verification. It includes:

- local key generation helpers
- a local sender example
- a local mock exchange API
- a local end-to-end verification script

That tooling is **not** intended to remain in every project derived from the NIE Template.

### Required policy

- move the portal sender, key generation helpers, and mock exchange utilities into an internal portal/platform repository or internal secure distribution point
- do **not** commit those portal implementation tools into derived application repositories
- derived application repositories should keep only:
  - Auth-side SSO code
  - Auth-side configuration
  - app-team-facing documentation
  - the public integration contract for the portal handoff

### Practical intent

Application developers should know:

- which endpoints exist
- which claims are required
- which keys their system owns
- which values must be registered per environment

Application developers do **not** need the full portal-side implementation code in their project repository.

---

## Per-System Key Policy

Each integrated system must have its **own** SSO keys.

Do not share the same keys across multiple NIE-derived systems.

### Minimum rule set

- each system gets its own portal signing key pair
- each system gets its own Auth decryption key pair
- each environment gets its own key pairs
- production keys must be different from staging keys
- one project's keys must never be reused by another project

### Result

For every `(system, environment)` pair, provision:

1. a portal signing key pair
2. an Auth decryption key pair

Example:

- System A staging: unique signing pair + unique decryption pair
- System A production: different signing pair + different decryption pair
- System B staging: different again

### Why this matters

This prevents:

- one compromised project key from affecting another project
- accidental cross-system token acceptance
- environment crossover between staging and production

---

## Key Ownership Model

Each side should store only the keys it actually needs.

### Portal side stores

- portal signing **private** key
- Auth decryption **public** key

### Auth side stores

- portal signing **public** key
- Auth decryption **private** key

### Meaning

- portal signs, Auth verifies
- portal encrypts, Auth decrypts

---

## Claims and Values

The portal callback payload must include these core values.

| Claim / Value | Required | Source | Purpose |
| --- | --- | --- | --- |
| `iss` | Yes | Portal config | Identifies trusted portal issuer |
| `aud` | Yes | App/Auth config | Identifies the target application |
| `iat` | Yes | Portal runtime | Issued-at timestamp |
| `nbf` | Yes | Portal runtime | Not-before timestamp |
| `exp` | Yes | Portal runtime | Expiration timestamp |
| `jti` | Yes | Portal runtime | One-time token ID for replay protection |
| `state` | Yes | `Auth/SsoStart` | Binds callback to pending login |
| `nonce` | Yes | `Auth/SsoStart` | Prevents mismatched login handoffs |
| `source_system` | Yes | Portal registration | Identifies the registered portal integration |
| `source_url` | Recommended / required when allowlist enabled | Portal registration | Identifies the registered source application URL |
| `exchange_token` | Yes | Portal runtime | One-time exchange token for the upstream identity lookup |
| `preferred_username` | Optional | Portal runtime | Hint only, not authoritative |
| `email` | Optional | Portal runtime | Hint only, not authoritative |
| `sub` | Recommended | Portal runtime | Subject identifier |

The application does **not** trust `Origin` or `Referer` as the main proof of sender.

The authoritative trust chain is:

- JWE decryption
- JWS signature validation
- issuer / audience validation
- `state` / `nonce` / `jti`
- registered `source_system`
- registered `source_url` when allowlisting is enabled
- optional callback IP allowlisting as a secondary control

---

## Application Team Runbook

Use these steps whenever a new project is created from the NIE Template and needs portal SSO.

### Step 1: Decide that the project supports both login modes

Confirm that the project will keep both:

- username/password login
- portal SSO login

Do not remove direct login when enabling SSO unless there is a separate approved requirement to do so.

### Step 2: Register a project-specific SSO identity

For the new system, define:

- system name
- environment name
- `PortalSso:Issuer`
- `PortalSso:Audience`
- `PortalSso:SourceSystemId`
- `PortalSso:AllowedSourceUrls`

These values must be unique and agreed between the application team and the portal team.

### Step 3: Provision per-system keys

Generate or request:

1. portal signing key pair for the specific system/environment
2. Auth decryption key pair for the specific system/environment

Store them outside source control.

Recommended storage:

- vault / KMS / HSM
- protected file paths with environment-level secret injection

### Step 4: Configure Auth

Populate the `PortalSso` section in Auth configuration:

- `Enabled`
- `LaunchUrlTemplate`
- `DefaultReturnUrl`
- `CallbackUrl`
- `Issuer`
- `Audience`
- `SourceSystemId`
- `AllowedSourceUrls`
- `Crypto:SigningPublicKeyPath` or `Crypto:SigningPublicKeyPem`
- `Crypto:DecryptionPrivateKeyPath` or `Crypto:DecryptionPrivateKeyPem`
- `ExchangeApi:BaseUrl`
- `ExchangeApi:Path`

If possible, prefer key file paths or secret-provider injection over raw PEM values in config.

### Step 5: Confirm the exchange API contract

The SSO exchange API must return the same `LoginResponse` contract already used by manual login.

That is the key compatibility rule that keeps the downstream session and Main API behavior unchanged.

The exchange API response must remain the authoritative source for:

- `userId`
- `userName`
- `email`
- `sessionToken` when supplied
- `isAuthenticated`

### Step 6: Enable the auth frontend SSO entry point

For the auth frontend, enable the runtime SSO flag through the deployed page
configuration instead of a Vite `.env` file:

```html
<meta name="nie:portalSsoEnabled" content="true">
```

or:

```html
<script>
  window.__NIE_APPLICATION_CONFIG__ = {
    ...(window.__NIE_APPLICATION_CONFIG__ ?? {}),
    portalSsoEnabled: true,
  };
</script>
```

Validate that:

- the direct login form still works
- the portal SSO button appears only when intended
- SSO success leads to the same redirect/cookie behavior as manual login

### Step 7: Validate application-side security rules

Confirm that the Auth service rejects:

- bad issuer
- bad audience
- bad signature
- bad decryption
- expired token
- reused `jti`
- reused `state`
- bad `nonce`
- wrong `source_system`
- wrong `source_url` when allowlisting is enabled

### Step 8: Validate both login paths before release

At minimum:

- manual login success
- manual login failure
- SSO success
- SSO replay rejection
- SSO invalid-source rejection
- Auth `Verify` success for sessions issued by either path

### Step 9: Handoff only the required contract to project developers

Developers consuming the template project should receive:

- endpoint names
- claim contract
- environment values they must request
- where keys come from
- how to validate the Auth flow

They should not need the portal implementation code in the application repo.

---

## Portal / Platform Team Runbook

Use these steps to onboard the same project into the portal.

### Step 1: Create a portal registration for the system

For each application/environment pair, create a portal registration that stores:

- system identifier
- issuer
- audience
- allowed source URL
- portal signing private key
- Auth encryption public key

### Step 2: Keep keys isolated per system

Do not create one shared signing key for all projects.

The portal must maintain separate key material for:

- each project
- each environment

### Step 3: Launch the application SSO flow

The browser should begin with the application's:

- `GET Auth/SsoStart`

The returned values contain the one-time values required for the handoff:

- `state`
- `nonce`
- `launchUrl`
- `callbackUrl`
- `returnUrl`

### Step 4: Build the portal callback artifact

Using the portal-side implementation:

1. take `state` and `nonce` from `SsoStart`
2. create the payload claims
3. mint a short-lived `exchange_token`
4. sign the inner token with `PS256`
5. encrypt the signed token with `RSA-OAEP-256` + `A256GCM`

### Step 5: Call the application callback

The portal backend performs a server-to-server POST to:

- `POST Auth/SsoCallback`

with:

```json
{
  "state": "value from SsoStart",
  "encryptedPayload": "nested-jwe"
}
```

### Step 6: Redirect the browser back to the application

After the callback succeeds, redirect the browser to the application return URL with the same `state`.

The auth frontend then completes:

- `GET Auth/SsoFinalize?state=...`

### Step 7: Keep the exchange token one-time and short-lived

The exchange token must be:

- single use
- short lived
- tied to the portal-authenticated user
- usable only by the registered target application

### Step 8: Plan for rotation

For each system/environment pair, define:

- who rotates keys
- how public keys are redistributed
- how long old keys remain valid during transition
- how rotation is tested before production cutover

---

## Recommended Handoff Checklist Between Teams

Before go-live, both sides should agree on the following values.

| Item | Owner |
| --- | --- |
| Application name | Joint |
| Environment name | Joint |
| `Issuer` | Portal team |
| `Audience` | Joint |
| `SourceSystemId` | Portal team |
| `AllowedSourceUrls` | Joint |
| Portal signing public key delivery | Portal team |
| Auth decryption public key delivery | Application team |
| Exchange API endpoint | Application/upstream identity owner |
| Callback URL | Application team |
| Return URL | Application team |

---

## Key Generation and Local Verification

Use organization-approved secure tooling for production keys. Development key generation, portal callback simulation, and mock exchange utilities are maintained outside application repositories by the portal/platform team.

For local verification, request the approved external Portal SSO test harness or manually replay a signed/encrypted callback produced by that harness. Application repositories should validate only the application-side contract, configuration, and callback handling.

---

## What Must Not Be Committed To Derived Projects

When a new application repository is created from the NIE Template, do not carry forward:

- internal mock exchange utilities
- internal portal sender code
- internal key generation helpers
- generated development key files

These must be moved out of the template repository and maintained separately by the portal/platform team.

Derived project repositories should retain only the application-side contract and configuration needed to consume SSO.
