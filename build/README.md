# build/ — container images and deployment configuration

Everything needed to package App Template into three images and run them on a
single host. Kubernetes manifests live in [`deploy/helm`](../deploy/helm)
instead.

If you scaffolded with Copier and answered `stack: Backend-only`, ignore the
`Dockerfile.ui` / `nginx.conf` / `maintenance.html` rows below and the
`apptemplate-ui` service in `docker-compose.yml` — those files were not copied
into your project.

| File                    | What it is                                                                                 |
| ----------------------- | ------------------------------------------------------------------------------------------ |
| `Dockerfile.api`        | Main API image. Debian-based because PDF/report rendering runs Playwright + Chromium.      |
| `Dockerfile.auth`       | Auth API image (local identity provider + Valkey sessions). Alpine.                        |
| `Dockerfile.ui`         | Builds the pnpm workspace, serves both SPAs from nginx.                                    |
| `nginx.conf`            | The front door: static SPAs + reverse proxy to both APIs.                                  |
| `maintenance.html`      | Static page nginx serves on 502/503/504.                                                   |
| `appsettings.api.json`  | Production configuration mounted into the Main API container.                              |
| `appsettings.auth.json` | Production configuration mounted into the Auth API container.                              |
| `docker-compose.yml`    | Single-host topology: UI, both APIs, Postgres, Valkey, Mailpit.                            |
| `.env.example`          | Documented template for the three variables `docker-compose.yml` requires. Copy to `.env`. |

All three images are built from the **repository root**, not from this folder:

```bash
docker build -f build/Dockerfile.api  -t apptemplate-main-api .
docker build -f build/Dockerfile.auth -t apptemplate-auth-api .
docker build -f build/Dockerfile.ui   -t apptemplate-ui .
```

CI builds all three on every push and pull request: the two API images in
`.github/workflows/ci.yml`, the UI image in `.github/workflows/ci-frontend.yml`
(split so a Copier `stack: Backend-only` scaffold can drop the frontend half
wholesale).

## The two placeholders

Two strings in this folder are meant to be replaced when you deploy. They are
not variables — search and replace them.

| Placeholder   | Appears in                                                           | Replace with                                                                                                       |
| ------------- | -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `MYAPP`       | `nginx.conf` (5 `location` blocks, 4 `proxy_cookie_path` directives) | Your app's base path segment, e.g. `robotics-portal`. Delete the prefix entirely if the app owns its own hostname. |
| `YOUR_DOMAIN` | `AllowedCORSOrigin` in both `appsettings.*.json`                     | The public origin the browser loads, e.g. `https://apps.example.com`.                                              |

`AllowedCORSOrigin` entries are **origins**: scheme + host + optional port, and
nothing else. `https://apps.example.com/robotics-portal/api-main` is a URL, not
an origin, and the CORS middleware will silently never match it.

Keep `MYAPP` in step with `pathPrefix` in
[`deploy/helm/app-template/values*.yaml`](../deploy/helm/app-template) and with
the frontend's runtime config (`<meta name="app:*">` tags in `index.html`).

## Running the single-host stack

`docker-compose.yml` expects three variables. Start from the documented template
and edit the copy — `build/.env` is git-ignored, `build/.env.example` is not:

```bash
cp build/.env.example build/.env
```

```dotenv
DOCKER_REGISTRY_URL=ghcr.io/your-org      # no trailing slash
COMMIT_ID=v1.0.0                          # a git SHA or release tag, not `latest`
POSTGRES_PASSWORD=<a real password>       # must match the connection strings in appsettings.*.json
```

```bash
docker compose -f build/docker-compose.yml up -d
```

Compose substitutes an empty string for an unset variable and only warns, so a
value you forget surfaces much later as an `invalid reference format` or an
authentication failure rather than a clean error.

If you enabled the AI chatbot, Postgres has to carry the `vector` extension:
switch `apptemplate-postgres` to `pgvector/pgvector:pg18` — the commented line
in `docker-compose.yml` shows exactly where.

Only the UI publishes a port (`8102`). Put TLS termination in front of it.
Postgres data lands in `build/pgdata/` and uploaded files in `build/uploads/`;
both are git-ignored bind mounts, so back them up like any other data.

Mailpit is included so outbound email works out of the box: the APIs send to
`apptemplate-mailpit:1025` and you read the messages at
`http://127.0.0.1:8025` on the host. Point `EmailSettings` at a real SMTP relay
when you have one, then drop the service.

## Configuration files

These two JSON files are mounted over `/app/appsettings.json` inside their
containers, and the image build deletes the one that was published — so each
file is the **complete** runtime configuration, not an overlay. A section you
delete here is a section the app does not get.

Keys whose name starts with `//` are documentation. They are valid JSON and the
.NET configuration binder treats them as unread leaf values, which is how these
files carry comments without breaking `jq` or any other strict JSON tool.

Secrets are marked `CHANGE_ME`. Optional integrations are left empty (`""`) and
stay switched off until you fill them in:

| Section          | Empty means                                                                    |
| ---------------- | ------------------------------------------------------------------------------ |
| `Sentry.Dsn`     | No error reporting is sent.                                                    |
| `OneSignal`      | Push notifications are not delivered.                                          |
| `AzureOpenAI`    | The AI chat endpoints return a "not configured" error.                         |
| `FileStorage.S3` | Ignored while `Provider` is `Local`.                                           |
| `ExternalIdp.*`  | Every external provider is disabled; only the local identity provider is used. |

Prefer environment variables over editing these files for anything sensitive —
ASP.NET Core maps `__` to `:`, so `ConnectionStrings__MainDbConnection` and
`Sentry__Dsn` override the JSON without putting secrets in a file.

### Local identity provider

`appsettings.auth.json` configures the built-in identity provider. Credentials
live in the `UserAccounts` table (hashed with ASP.NET Core's `PasswordHasher`)
and sessions live in Valkey — there is no external identity service to sign up
for. `LocalIdentity` controls the minimum password length, lockout thresholds,
the password-reset token lifetime and whether visitors may register themselves.

`ExternalIdp` is the optional "sign in with…" slot and ships fully disabled and
empty. To turn on, say, Google: set `ExternalIdp.Enabled` to `true`, set
`Providers.Google.Enabled` to `true`, fill in the `ClientId`/`ClientSecret` you
registered with Google, set `Authority` to `https://accounts.google.com`, and
point `RedirectUri` at this API's `/api/Auth/ExternalCallback`. GitHub has no
OIDC discovery document, so set its `AuthorizationEndpoint`, `TokenEndpoint`
and `UserInfoEndpoint` instead of `Authority`.

Keep client secrets out of git — pass them as
`ExternalIdp__Providers__Google__ClientSecret` in the environment.
