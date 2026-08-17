# Running this template inside NIE Ignite

> **Audience:** anyone maintaining this template (or a project scaffolded from it) who needs it to
> keep working inside an NIE Ignite workspace. You do not need to have seen Ignite before.
>
> **TL;DR — the one rule you must not break:** the two Vite dev servers are served behind a
> **path-based** proxy that forwards the **full mount path**, so Vite's `base` must equal that mount
> path. The two .NET APIs are served behind the same proxy with the prefix **stripped**, so they must
> keep answering at `/…` as if they were at the root. Everything else in this document follows from
> that asymmetry.

---

## 1. What Ignite is, and how your project actually runs

**NIE Ignite** is the platform NIE uses to give each student project a live, always-on development
environment plus an AI coding agent. When a project is created, Ignite provisions a
[Coder](https://coder.com) workspace: one Linux container per project, with your repository checked
out at `/home/coder/workspace/project`.

Inside that container, **supervisord** keeps five programs alive. They are defined in the Ignite
repo at `build/coder/template/v1/etc/supervisord.conf`:

| supervisord program | priority | what it runs      | what it starts                                                                                |
| ------------------- | -------- | ----------------- | --------------------------------------------------------------------------------------------- |
| `postgres`          | 10       | `run-postgres.sh` | PostgreSQL bound to `127.0.0.1` only, `trust` auth, superuser `postgres`                      |
| `valkey`            | 10       | `run-valkey.sh`   | Valkey (Redis-compatible), no password, no key prefix                                         |
| `auth-api`          | 20       | `run-auth-api.sh` | `dotnet` on `src/backend/Auth/Auth.csproj`                                                    |
| `main-api`          | 20       | `run-main-api.sh` | `dotnet` on `src/backend/API/API.csproj`                                                      |
| `frontend`          | 30       | `run-frontend.sh` | **both** Vite dev servers (`src/frontend/auth` and `src/frontend/main`) under **one** program |

Two things about that table matter later:

- `auth-api` and `main-api` share **priority 20**, so supervisord starts them at the same time. Neither
  waits for the other, and neither waits for a database to exist.
- The `frontend` program runs **two** Vite servers. There is no separate supervisord program per app.
  See [§8](#8-the-2-frontend--2-backend-topology-is-currently-required).

The `.NET` services are _not_ run with `dotnet watch`. `run_dotnet_dev_service` in
`build/coder/template/v1/bin/shared.sh` fingerprints the source tree every second, waits for the
change burst to settle, runs `dotnet build`, and only then swaps the running DLL. A failed build
leaves the previous process running. So a C# edit takes a few seconds to appear, and a compile error
does not take the API down.

The Ignite web UI embeds your running frontend in an **iframe**, served through a reverse proxy — you
never get a raw container port. That proxy is the entire reason this document exists.

---

## 2. Ports

These ports are fixed. They are declared once in `build/coder/template/v1/bin/shared.sh` and
mirrored in `build/coder/template/v1/main.tf` (`locals.ports`) and in the orchestrator's
`previewAppPorts` map (`build/coder/orchestrator/src/server.mjs`). Changing any of them in your repo
breaks the workspace.

| Service              | Port in the workspace | Local `pnpm dev` port (this repo) |
| -------------------- | --------------------- | --------------------------------- |
| Main frontend (Vite) | **18002**             | 8002                              |
| Auth frontend (Vite) | **18001**             | 8001                              |
| Main API (.NET)      | **15002**             | 5002                              |
| Auth API (.NET)      | **15001**             | 5001                              |
| PostgreSQL           | **5432**              | 5432                              |
| Valkey               | **6379**              | 6379                              |

The workspace ports differ from the local ports on purpose — `run-frontend.sh` and `run-main-api.sh`
override them at launch (`--port ${IGNITE_MAIN_FRONTEND_PORT}` for Vite, `ASPNETCORE_URLS` for .NET),
so `server.port: 8002` in `src/frontend/main/vite.config.ts` is simply ignored in a workspace. Do not
"fix" it to 18002; that would break local development for no gain.

Postgres in the workspace listens on `127.0.0.1` with `trust` authentication and the `postgres`
superuser (password also set to `postgres`), which is exactly what
`src/backend/API/appsettings.json` → `ConnectionStrings:MainDbConnection` already expects. The
workspace's `run-postgres.sh` pre-creates only the `NieIgnite` database and the per-project
`$IGNITE_DATABASE_NAME`; the `AppTemplate` database this repo uses is created by the Main API's
`context.Database.Migrate()` call in `src/backend/API/Program.cs` on first boot (the `postgres`
superuser has `CREATEDB`).

---

## 3. The preview mount: URL shape and a worked example

Every previewable service is reachable at:

```
<edge origin><coder access path>/@<owner>/<workspace>/apps/<slug>/<rest>
```

There are exactly **four** allowed slugs. They are whitelisted in two independent places —
`build/nginx.conf` (the `location ~ ^/ignite/coder/@[^/]+/[^/]+/apps/(preview-main|preview-auth|main-api|auth-api)(/|$)`
regex) and `previewAppPorts` in the orchestrator — and both must agree:

| Slug           | Upstream port | What it is                    |
| -------------- | ------------- | ----------------------------- |
| `preview-main` | 18002         | Main frontend Vite dev server |
| `preview-auth` | 18001         | Auth frontend Vite dev server |
| `main-api`     | 15002         | Main .NET API                 |
| `auth-api`     | 15001         | Auth .NET API                 |

### Worked example

For user `jane`, workspace `ws-42`, on the compose deployment where Coder is mounted at
`https://ai.nie.edu.sg/ignite/coder`:

```
Main frontend   https://ai.nie.edu.sg/ignite/coder/@jane/ws-42/apps/preview-main/
Auth frontend   https://ai.nie.edu.sg/ignite/coder/@jane/ws-42/apps/preview-auth/
Main API        https://ai.nie.edu.sg/ignite/coder/@jane/ws-42/apps/main-api/
Auth API        https://ai.nie.edu.sg/ignite/coder/@jane/ws-42/apps/auth-api/
```

A request for the main app's Vue router page, an asset and an API call therefore look like:

```
GET /ignite/coder/@jane/ws-42/apps/preview-main/                     -> Vite index.html
GET /ignite/coder/@jane/ws-42/apps/preview-main/@vite/client         -> Vite HMR client
GET /ignite/coder/@jane/ws-42/apps/preview-main/src/main.ts          -> Vite module
GET /ignite/coder/@jane/ws-42/apps/main-api/api/Vendor/GetAll        -> Main API
```

The prefix is _deployment-supplied_, not hardcoded: the same image serves the compose mount
(`/ignite/coder`) and the AWS apex mount (`/coder`). Never hardcode `/ignite/coder` in this repo.

Note also that **everything is one origin**. The apps and the APIs are all served from
`https://ai.nie.edu.sg`, so browser requests from the preview to the API are same-origin and never
trigger a CORS preflight. The `AllowedCORSOrigin` arrays in `src/backend/API/appsettings.json` and
`src/backend/Auth/appsettings.json` are therefore not exercised inside a workspace — but they still
matter for local development and for real deployments, so leave them alone.

---

## 4. ⚠️ THE CRITICAL RULE: the proxy strips the prefix for APIs and **not** for the frontends

This lives in `resolvePreviewProxyTarget()` in `build/coder/orchestrator/src/server.mjs`:

```js
const appBase = previewMount.appPath(owner, workspaceName, appSlug);
let upstreamPath = url.pathname;
if (appSlug === "main-api" || appSlug === "auth-api") {
  upstreamPath = url.pathname.slice(appBase.length) || "/"; // prefix STRIPPED
  upstreamPath = `/${upstreamPath.replace(/^\/+/, "")}`;
}
// else: the FULL path is forwarded UNCHANGED
```

So:

| Slug           | Browser requests                                            | Upstream (in the container) receives                      |
| -------------- | ----------------------------------------------------------- | --------------------------------------------------------- |
| `preview-main` | `/ignite/coder/@jane/ws-42/apps/preview-main/src/main.ts`   | `/ignite/coder/@jane/ws-42/apps/preview-main/src/main.ts` |
| `main-api`     | `/ignite/coder/@jane/ws-42/apps/main-api/api/Vendor/GetAll` | `/api/Vendor/GetAll`                                      |

### Consequences for the frontends

Vite must be told it is mounted at that path, or **every single URL it emits is wrong**. Vite derives
the URL of every module, every static asset, the `/@vite/client` script and the HMR websocket path
from its `base` option. If `base` is `"/"` or `"./"` while the browser is at
`…/apps/preview-main/`, then:

- `index.html` comes back (the proxy asked for the mount root, Vite serves its root) — but
- every `<script type="module" src="/src/main.ts">` resolves to `https://ai.nie.edu.sg/src/main.ts`,
  which the edge does not route to anything → **404**;
- `@vite/client` never loads → **no HMR, no error overlay**;
- the iframe renders **blank/white** with a console full of 404s and a MIME-type error.

That is why `src/frontend/main/vite.config.ts` and `src/frontend/auth/vite.config.ts` both do:

```ts
const basePath = readEnv("VITE_BASE_PATH") ?? "./";
// ...
base: basePath,
```

`"./"` remains the default so production builds keep emitting relative asset URLs (they are served
under an nginx path prefix). `VITE_BASE_PATH` is only ever set by the workspace.

### Consequences for the APIs

Because the prefix is stripped, the .NET apps must keep serving at the root: routes stay
`/api/Vendor/GetAll` and `/health`. **Do not** add `app.UsePathBase(...)` or a route prefix to make
the mount path "work" — the API never sees the mount path. Conversely, the API must never emit an
absolute redirect or `Location` header built from its own request path, because the browser's path is
the _unstripped_ one.

### Consequence for routing

Both apps use `createWebHashHistory()` (`src/frontend/main/src/router/index.ts`,
`src/frontend/auth/src/router/index.ts`). Routes are `#/vendors`, not `/vendors`, so the preview never
needs an SPA history fallback from the proxy. Switching to `createWebHistory()` would require the
proxy to rewrite deep links back to `index.html`, which it does not do. Don't.

---

## 5. How hot reload works end to end

Vite 8's HMR client (`vite/dist/client/client.mjs`, verified against the `vite@8.0.2` copy installed
in `src/frontend/node_modules`) builds its socket URL like this:

```js
const importMetaUrl = new URL(import.meta.url);
const socketProtocol =
  __HMR_PROTOCOL__ || (importMetaUrl.protocol === "https:" ? "wss" : "ws");
const hmrPort = __HMR_PORT__; // server.hmr.clientPort || server.hmr.port || null
const socketHost = `${__HMR_HOSTNAME__ || importMetaUrl.hostname}:${hmrPort || importMetaUrl.port}${__HMR_BASE__}`;
new WebSocket(`${socketProtocol}://${socketHost}?token=${wsToken}`, "vite-hmr");
```

Walk it through for the worked example:

1. **`__HMR_BASE__` is the dev `base`.** With `base` set correctly, the socket path is
   `/ignite/coder/@jane/ws-42/apps/preview-main/`, which the nginx `location` regex matches and the
   orchestrator routes. With the wrong `base`, the websocket URL points somewhere the edge does not
   route — HMR never connects. **This is the same rule as §4, applied to the socket.**
2. **The protocol is inferred, correctly.** `import.meta.url` for `@vite/client` is an `https:` URL
   (TLS terminates at `ai.nie.edu.sg` / `ignite.nie.edu.sg`), so the client picks `wss`. Do **not**
   hardcode `server.hmr.protocol`; leave the inference alone unless you have a specific reason, in
   which case `VITE_HMR_PROTOCOL` overrides it.
3. **The port must be pinned.** On an `https:` page `importMetaUrl.port` is the empty string. With no
   `clientPort`, `socketHost` becomes `ai.nie.edu.sg:` + the base — a malformed authority, and the
   socket never opens. So `resolveHmrOptions()` in both `vite.config.ts` files sets
   `clientPort: 443` whenever `VITE_BASE_PATH` is set, unless `VITE_HMR_CLIENT_PORT` overrides it.
   443 is where TLS terminates at the edge.
4. **The orchestrator proxies the upgrade.** `server.on("upgrade", …)` in
   `build/coder/orchestrator/src/server.mjs` resolves the same preview target and calls
   `proxyUpgradeRequest(...)`, and nginx sets `Upgrade` / `Connection` on that location. So a
   correctly-addressed websocket does get through.

One subtlety worth knowing when debugging: when `hmrPort` is set, Vite's client does **not** attempt
its "direct websocket connection fallback". A wrong `clientPort` therefore fails loudly and finally
rather than silently recovering.

HMR failing does **not** break the app — the page still loads and works, you just lose live reload.
That makes it easy to miss, so check it explicitly after touching a Vite config.

---

## 6. The environment Ignite injects, and where this repo consumes it

`build/coder/template/v1/bin/run-frontend.sh` launches each Vite process with an inline environment.
Nothing else in the workspace sets `VITE_*`. Verbatim from that script:

| Variable                  | Value in a workspace                                                             | Set for       | Consumed in this repo by                                                                         |
| ------------------------- | -------------------------------------------------------------------------------- | ------------- | ------------------------------------------------------------------------------------------------ |
| `VITE_ALLOWED_HOSTS`      | `ai.nie.edu.sg,ignite.nie.edu.sg,localhost,127.0.0.1,host.docker.internal`       | both          | `configuredAllowedHosts` → `server.allowedHosts` in both `vite.config.ts`                        |
| `VITE_BASE_PATH`          | `<prefix>/@jane/ws-42/apps/preview-main/` (main) / `…/apps/preview-auth/` (auth) | both          | `basePath` → `base` in both `vite.config.ts`; also mapped to `appBasePath` in the runtime config |
| `VITE_API_URL`            | `<prefix>/@jane/ws-42/apps/main-api/`                                            | both          | runtime config `mainApiBaseUrl` → `getBackendBaseUrl("main")`                                    |
| `VITE_AUTH_API_URL`       | `<prefix>/@jane/ws-42/apps/auth-api/`                                            | both          | runtime config `authApiBaseUrl` → `getBackendBaseUrl("auth")`                                    |
| `VITE_AUTH_SERVICE_URL`   | the auth app's base path                                                         | **main only** | runtime config `authAppUrl` → `getFrontendUrl("auth")`                                           |
| `VITE_DASHBOARD_URL`      | the main app's base path                                                         | **auth only** | runtime config `mainAppUrl` → `getFrontendUrl("main")`                                           |
| `VITE_COOKIE_DOMAIN`      | `""` (empty)                                                                     | both          | runtime config `cookieDomain`; empty values are dropped, so nothing is set                       |
| `VITE_COOKIE_SESSION_KEY` | `NieIgniteWorkspace<projectId>-SessionToken`                                     | both          | runtime config `sessionCookieName` → `FRONTEND_CONSTANTS.cookies.session`                        |
| `VITE_COOKIE_USER_KEY`    | `NieIgniteWorkspace<projectId>-User`                                             | both          | runtime config `userCookieName` → `FRONTEND_CONSTANTS.cookies.user`                              |

Two extra variables are **ours**, not Ignite's — escape hatches this repo added, unset by default:

| Variable               | Effect                                                                                                                                       |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `VITE_HMR_CLIENT_PORT` | Overrides the `clientPort: 443` default. Needed if a future edge terminates TLS on another port, and useful for the local simulation in §10. |
| `VITE_HMR_PROTOCOL`    | Overrides `server.hmr.protocol`. Leave unset in a workspace.                                                                                 |

Vite itself is launched as:

```
node ./node_modules/vite/bin/vite.js --mode development --host 0.0.0.0 --port <port> --strictPort
```

`--host 0.0.0.0` is required (the proxy reaches Vite from outside loopback) and `--strictPort` stops
Vite from silently picking a different port when the configured one is busy. The CLI `--port` wins
over `server.port` in the config, which is why §2 says not to change it.

### 6.1 `server.allowedHosts`

Vite 6+ answers **403 "This host is not allowed"** for any `Host` header not in
`server.allowedHosts`, and the orchestrator forwards its own **public** host upstream
(`config.previewUpstreamHost`, derived from the deployment's preview base — `ai.nie.edu.sg` on the
compose mount, `ignite.nie.edu.sg` at the AWS apex), not the container name. Both configs parse
`VITE_ALLOWED_HOSTS` and **spread the result in only when non-empty**:

```ts
...(configuredAllowedHosts && configuredAllowedHosts.length > 0
  ? { allowedHosts: configuredAllowedHosts }
  : {}),
```

The spread matters: passing an empty array would reject _everything_, including local development.
With no env, Vite keeps its own default and `pnpm dev` behaves exactly as before.

### 6.2 The runtime-config injection plugin

The `VITE_*` URLs above are **not** read with `import.meta.env.VITE_*` anywhere in this repo, and they
must not be. `import.meta.env` bakes values into the bundle at build time, which breaks
build-once-promote-many — the rule the whole `AppTemplateRuntimeConfig` design in
`src/frontend/packages/shared/src/config/constants.ts` exists to enforce.

Instead, each `vite.config.ts` registers a small **dev-server-only** plugin,
`hostedPreviewRuntimeConfigPlugin()`:

- `apply: "serve"` — it is completely absent from `vite build`;
- `transformIndexHtml` with `order: "pre"` and `injectTo: "head-prepend"` — the tag lands before the
  app's module script, so `window.__APP_TEMPLATE_CONFIG__` exists before any module reads it;
- it emits a classic (non-module) inline `<script>`:
  `window.__APP_TEMPLATE_CONFIG__ = {…};`
- the JSON is escaped for `<` and `>` (to `<` / `>`) so it cannot terminate the script tag;
- blank/whitespace-only env values are treated as unset, so the empty `VITE_COOKIE_DOMAIN` never
  becomes an empty `domain` cookie attribute;
- if **no** mapped variable is set, nothing at all is injected.

This is the same `window.__APP_TEMPLATE_CONFIG__` channel that nginx and Helm populate in a real
deployment (`docs/RUNTIME-OBSERVABILITY-DOSSIER.md`), so the preview and production take identical
code paths.

The helper block (`readEnv`, `resolveHmrOptions`, `RUNTIME_CONFIG_FROM_ENV`, `collectRuntimeConfig`,
`serializeRuntimeConfig`, `hostedPreviewRuntimeConfigPlugin`) is **duplicated verbatim** in both
config files rather than imported. That is deliberate and commented in the source: `main` and `auth`
are separate Vite projects with separate dependency graphs, and a cross-project import would break
`vite build`. If you change one, change the other.

### 6.3 The runtime-config keys the plugin drives

All of these are optional fields on `AppTemplateRuntimeConfig` in
`src/frontend/packages/shared/src/config/constants.ts`:

| Key                 | Read by                                                                         | Effect when set                                    |
| ------------------- | ------------------------------------------------------------------------------- | -------------------------------------------------- |
| `appBasePath`       | `getAppBasePath()`                                                              | Replaces the first-path-segment heuristic outright |
| `mainApiBaseUrl`    | `getBackendBaseUrl("main")` → `getBackendUrl()` → `FRONTEND_CONSTANTS.api.main` | Absolute base for main API calls                   |
| `authApiBaseUrl`    | `getBackendBaseUrl("auth")` → `FRONTEND_CONSTANTS.api.auth`                     | Absolute base for auth API calls                   |
| `mainAppUrl`        | `getFrontendUrl("main")` → `FRONTEND_CONSTANTS.apps.main`                       | Where the auth app sends a signed-in user          |
| `authAppUrl`        | `getFrontendUrl("auth")` → `FRONTEND_CONSTANTS.apps.auth`                       | Where the main app sends an unauthenticated user   |
| `sessionCookieName` | `FRONTEND_CONSTANTS.cookies.session`                                            | Session cookie name                                |
| `userCookieName`    | `FRONTEND_CONSTANTS.cookies.user`                                               | Cached-user cookie name                            |
| `cookieDomain`      | `FRONTEND_CONSTANTS.cookies.domain`, `getCookieAttributes()`                    | Cookie `domain` attribute                          |

Values may be an absolute path (`/ignite/coder/…/apps/main-api/`) or an absolute URL
(`https://api.example.edu/main`). `isAbsoluteUrl()` / `normalizeConfiguredBaseUrl()` /
`joinBaseAndPath()` in the same file keep a scheme from being mangled by the path joiner, and strip
a trailing slash so `…/main-api/` and `…/main-api` behave identically.

**Why the overrides are needed at all:** `getAppBasePath()`'s fallback reads only the **first** path
segment of `window.location.pathname`. Under `/ignite/coder/@jane/ws-42/apps/preview-main/` it would
answer `"/ignite"`, and the derived main-API base would come out as `"/ignite/api-main"` — a URL that
routes to nothing. Two tests in
`src/frontend/packages/shared/src/config/constants.test.ts` are explicitly marked `REGRESSION` and
pin exactly that wrong-without-the-override behaviour, so nobody "fixes" the heuristic by guessing.

**Precedence caveat:** `getAppBasePath()` and `getFrontendUrl()` give the runtime-config override
precedence over _everything_, including the explicit `pathname` argument and the localhost shortcut.
That is intentional — the override is a statement of deployment fact, the heuristic is a guess — but
it means a wrong `appBasePath` in a production config now breaks every derived URL instead of being
silently ignored.

**Known gap:** `getFrontendAssetUrl(path, "auth")` still appends the `/login` segment to the app base
path. Under Ignite the auth app is a _separate mount_, so a cross-app asset URL built that way would
be wrong. No caller does this today — the only caller is
`navigator.serviceWorker.register(getFrontendAssetUrl("sw.js"))` in `src/frontend/main/src/main.ts`,
which uses the default `"main"` app. If you ever need auth assets from the main app, route it through
`authAppUrl` instead.

---

## 7. Cookie names are namespaced per workspace — do not hardcode them

`run-frontend.sh` computes `cookie_prefix="NieIgniteWorkspace${IGNITE_PROJECT_ID}"` and passes
`VITE_COOKIE_SESSION_KEY` / `VITE_COOKIE_USER_KEY` to both apps.

The reason: **every student workspace preview is served from the same public hostname**
(`ai.nie.edu.sg`). Cookies are scoped by host, not by path prefix, unless you go out of your way. If
every workspace used the template's default `AppTemplate-SessionToken`, then a student with two
project previews open in one browser would have each project silently overwrite the other's session —
random logouts, wrong user, an impossible bug report.

In this repo the names are resolved once, at module evaluation, in `FRONTEND_CONSTANTS.cookies`
(`constants.ts`), and read from there by `src/frontend/main/src/services/api.ts`,
`src/frontend/main/src/services/authService.ts`, `src/frontend/main/src/composables/useAuth.ts`,
`src/frontend/main/src/composables/useSignalR.ts`, `src/frontend/main/src/router/index.ts` and
`src/frontend/auth/src/services/session.ts`.

**If you write `Cookie.get("AppTemplate-SessionToken")` anywhere, you break workspace isolation** —
and only for the students who have two previews open, which is the worst kind of bug to reproduce.
Always go through `FRONTEND_CONSTANTS.cookies`.

Because `FRONTEND_CONSTANTS` is frozen at module evaluation, tests that exercise a cookie-name
override must `vi.resetModules()` and re-import the module. `constants.test.ts` shows the pattern.

---

## 8. The 2-frontend / 2-backend topology is currently REQUIRED

`build/coder/template/v1/bin/run-frontend.sh` starts **both** Vite apps as children of a **single**
supervisord program and ends with:

```bash
trap terminate_children EXIT INT TERM
bash -lc "$auth_command" & auth_pid=$!
bash -lc "$main_command" & main_pid=$!
# ...
wait -n "$auth_pid" "$main_pid" "$dependency_watch_pid"
```

`wait -n` returns as soon as **any one** child exits, and the shared `EXIT` trap
(`terminate_children`) then kills the others. So:

> **Deleting `src/frontend/auth` does not just remove the login app — it takes the main app down
> with it.** The auth child fails instantly (`cd '<root>/auth'` fails), `wait -n` returns, the trap
> kills the main Vite, the script exits well inside `startsecs=10`, supervisord counts a failed
> start, retries, and finally parks the `frontend` program at FATAL. `preview-main` never becomes
> healthy and the workspace never leaves **Starting**.

The same shape applies to the backends: the `auth-api` and `main-api` `coder_app` healthchecks in
`main.tf` are independent, so removing `src/backend/Auth` leaves the `auth-api` probe failing forever
and parks the workspace at **Degraded**, with a dead `/login/` tab in the Ignite UI.

This is the exact trap the Ignite plan calls out in
`docs/plans/2026-07-17-nie-template-std-and-template-contract-layer.md` §1.5 ("Sequencing constraint
(verified, critical)"). **This template keeps both frontends and both backends precisely so it does
not hit that trap.** Do not "simplify" the template by deleting one.

The plan's Part 2 contract layer is meant to remove this constraint (generic per-service run scripts,
supervisord programs generated from the manifest, per-service `wait` isolation, an nginx wildcard for
slugs). Until that ships, the four-service topology is a hard requirement. `ignite.manifest.json` at
the repo root is this template's half of that contract — it declares the two frontends, the two
backends, the datastores, the toolchain and the validation commands.

> **Note for `stack: backend` scaffolds.** `copier.yml` excludes `src/frontend/` entirely when
> `stack == 'backend'`, but `ignite.manifest.json` is not conditional and still declares two
> frontends. Such a scaffold is not Ignite-ready as-is. (`run-frontend.sh` degrades gracefully when
> `src/frontend` is missing entirely — it logs and sleeps rather than crash-looping — but the
> `preview-main` healthcheck still never passes.)

---

## 9. The health-check contract

`main.tf` defines a `coder_app` healthcheck per slug, every **10 s**, with a threshold of **30**
consecutive failures (~5 minutes of grace):

| `coder_app`    | Probe                               |
| -------------- | ----------------------------------- |
| `preview-main` | `GET http://localhost:18002/`       |
| `preview-auth` | `GET http://localhost:18001/`       |
| `main-api`     | `GET http://localhost:15002/health` |
| `auth-api`     | `GET http://localhost:15001/health` |

A persistently failing probe parks the workspace at **Degraded** and the Ignite UI shows a dead
preview tab.

### `/health` is LIVENESS and must not touch Postgres or Valkey

Both APIs implement the same three-endpoint convention (`src/backend/API/Program.cs` and
`src/backend/Auth/Program.cs`):

```csharp
string[] readyTags = ["ready"];
builder.Services.AddHealthChecks()
    .AddNpgSql(configuration.GetConnectionString("MainDbConnection")!, name: "postgresql", tags: readyTags)
    .AddRedis(configuration["Valkey:ConnectionString"]!, name: "valkey", tags: readyTags);
// ...
app.MapHealthChecks("/health",       new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/health/live", () => Results.Ok("ok"));
```

| Endpoint        | Runs                                                   | Use for                                               |
| --------------- | ------------------------------------------------------ | ----------------------------------------------------- |
| `/health`       | **nothing** — 200 means "this process is serving HTTP" | The Coder probe. Process liveness.                    |
| `/health/ready` | the `"ready"`-tagged checks (Postgres + Valkey)        | Load-balancer / rollout gating, dependency monitoring |
| `/health/live`  | nothing; returns the string `"ok"`                     | Uptime monitors that want a body                      |

Why liveness must stay dependency-free, concretely:

1. **The cold-boot race.** supervisord starts `auth-api` and `main-api` at the _same_ priority, and
   the `AppTemplate` database is created by the **Main API's** `Database.Migrate()`. On a first boot
   the Auth API is up and correct while its database does not exist yet. A Postgres-touching
   liveness probe would report the Auth API as dead — and if the Main API's first build is slow or
   broken, it stays "dead" past the 30-failure threshold and the whole workspace goes Degraded.
2. **Dependency blips.** If Postgres or Valkey restarts under supervisord, a dependency-touching
   `/health` flips the workspace to Degraded even though both API processes are perfectly fine.

**Anything you add to `AddHealthChecks()` must carry the `"ready"` tag**, or it silently joins
`/health/ready` only — which is what you want — but if you ever remove the `Predicate` on `/health`
you re-arm both failure modes at once.

> **Migration note for existing deployments.** `/health` now returns 200 with an empty check set.
> Any uptime monitor, Kubernetes readiness probe, load-balancer target group or Sentry Cron that was
> using `/health` to detect a dead database must be repointed at `/health/ready`. Check `deploy/`,
> `build/nginx.conf` and any Compose healthchecks before you release. (Some prose in
> `.ai/features/health-observability/` still describes the older "`/health` runs the full pipeline"
> behaviour; the code above is authoritative.)

---

## 10. Reproducing the Ignite mount locally

You do **not** need a workspace to test preview + HMR. Vite serves the app at its own `base`, so
setting the same environment variables and browsing to the mount path exercises the entire §4/§5 path
logic on your laptop.

From `src/frontend/main` (bash / Git Bash):

```bash
MOUNT="/ignite/coder/@demo/ws-1/apps"

VITE_BASE_PATH="$MOUNT/preview-main/" \
VITE_ALLOWED_HOSTS="localhost,127.0.0.1" \
VITE_API_URL="$MOUNT/main-api/" \
VITE_AUTH_API_URL="$MOUNT/auth-api/" \
VITE_AUTH_SERVICE_URL="$MOUNT/preview-auth/" \
VITE_COOKIE_SESSION_KEY="NieIgniteWorkspace0-SessionToken" \
VITE_COOKIE_USER_KEY="NieIgniteWorkspace0-User" \
VITE_COOKIE_DOMAIN="" \
VITE_HMR_CLIENT_PORT=8002 \
VITE_HMR_PROTOCOL=ws \
pnpm dev
```

> **Windows / Git Bash:** MSYS rewrites leading-slash values into Windows paths, so you will get a
> base like `/Program Files/Git/ignite/coder/...`. Export `MSYS_NO_PATHCONV=1` before the command, or
> use PowerShell. This is a shell artifact, not a template bug.

PowerShell equivalent (set each with `$env:NAME = "..."` first, then `pnpm dev`; remember to clear
them afterwards, or use a fresh shell).

`VITE_HMR_CLIENT_PORT=8002` / `VITE_HMR_PROTOCOL=ws` exist only because you are on plain HTTP on
`localhost`. In a real workspace both are unset and the config's `clientPort: 443` + inferred `wss`
are correct.

Then open **<http://localhost:8002/ignite/coder/@demo/ws-1/apps/preview-main/>** and check:

```bash
BASE="http://localhost:8002/ignite/coder/@demo/ws-1/apps/preview-main"

# 1. The runtime config is injected, head-prepended, before the module script.
curl -s "$BASE/" | grep -o "window.__APP_TEMPLATE_CONFIG__ = .*"

# 2. Every asset/module URL carries the mount path (no bare "/src/main.ts").
curl -s "$BASE/" | grep -o 'src="[^"]*"'

# 3. The HMR client got the right port and base.
curl -s "$BASE/@vite/client" | grep -E "const hmrPort|socketHost"
```

Expected: `hmrPort = 8002` and the socket host ending in the mount path. Drop
`VITE_HMR_CLIENT_PORT` and re-check — it becomes `const hmrPort = 443`, which is the workspace
behaviour. Remove **all** the variables and re-check — `hmrPort = null`, `base` back to `"./"`, and
no injected script: the plain local-dev path is untouched.

In the browser, confirm the DevTools Network tab shows a `101 Switching Protocols` websocket to
`…/apps/preview-main/`, then edit a `.vue` file and watch it hot-update.

Repeat from `src/frontend/auth` with `preview-auth`, port 8001, and `VITE_DASHBOARD_URL` in place of
`VITE_AUTH_SERVICE_URL`.

API calls will 404 in this simulation — nothing is serving `…/apps/main-api/` locally. That is
expected; the simulation validates asset/module/HMR path resolution and runtime-config injection, not
end-to-end API traffic.

---

## 11. Things that will break Ignite compatibility

Treat every item here as requiring a coordinated change in the Ignite repo, not just this one.

| Change                                                                                | What breaks                                                                                                                                           |
| ------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| Renaming or moving `src/frontend/main` or `src/frontend/auth`                         | `run-frontend.sh` hardcodes `<workspace>/src/frontend/{main,auth}`; the whole `frontend` program dies (§8)                                            |
| Renaming or moving `src/backend/API/API.csproj` or `src/backend/Auth/Auth.csproj`     | `run-main-api.sh` / `run-auth-api.sh` cannot find the project; the API sleeps and its probe fails forever                                             |
| Renaming the built assemblies (`AssemblyName`)                                        | `run_dotnet_dev_service` looks for `<name>.dll` under `bin/Debug`; the build succeeds and nothing ever starts                                         |
| Changing any port in §2                                                               | `shared.sh`, `main.tf` and `previewAppPorts` all disagree; the probe hits nothing                                                                     |
| **Deleting either frontend app or either backend API**                                | Workspace never becomes healthy (§8)                                                                                                                  |
| Setting `base` to a literal string instead of the `VITE_BASE_PATH`-derived `basePath` | Blank iframe, 404 on every module, no HMR (§4)                                                                                                        |
| Removing the `allowedHosts` spread, or hardcoding an array                            | 403 "This host is not allowed" on one or both edges (§6.1)                                                                                            |
| Hardcoding a cookie name instead of `FRONTEND_CONSTANTS.cookies.*`                    | Cross-workspace session clobbering (§7)                                                                                                               |
| Adding a dependency check to `/health` (or removing the `Predicate`)                  | Degraded workspace on a cold boot or a dependency blip (§9)                                                                                           |
| Switching to `createWebHistory()`                                                     | Deep links 404 — the proxy does not do SPA fallback (§4)                                                                                              |
| Reading config via `import.meta.env.VITE_*`                                           | Baked into the bundle; the preview gets build-time values, and promotion breaks (§6.2)                                                                |
| Requiring a **new** environment variable to boot                                      | `run-frontend.sh` only sets the nine listed in §6; the app fails to start in a workspace and works fine on your laptop                                |
| Introducing a service that must be reachable from the browser                         | Only four slugs exist, whitelisted in `nginx.conf` **and** the orchestrator; a fifth is unreachable                                                   |
| Changing `pnpm-workspace.yaml` layout or moving the lockfile                          | `run-frontend.sh` installs from `src/frontend` and fingerprints `pnpm-lock.yaml` / `pnpm-workspace.yaml` / `.npmrc` there to decide when to reinstall |
| Editing `ignite.manifest.json` without matching reality                               | The contract layer will provision the wrong paths/ports/commands                                                                                      |

A note on `inject-vite-origin.py`: the workspace runs this idempotent patch script over the Vite
configs on every frontend start. It only rewrites the _literal_ strings `base: "/"` (main) and
`base: "/login/"` (auth), and only injects `allowedHosts` when the text contains no `allowedHosts:`
at all. Because this repo already writes `base: basePath` (computed from `VITE_BASE_PATH`)
and already has an `allowedHosts:` key, the script finds nothing to change and is a **harmless
no-op**. Keep it that way: do not introduce a bare `base: "/"` literal, and do not remove the
`allowedHosts` key, or the script will start editing your config behind your back.

---

## 12. Troubleshooting

| Symptom                                                                                                                   | Likely cause                                                                                                                                                                                                                 | Fix / check                                                                                                                                                                                                                                |
| ------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Blank / white iframe**, console shows 404s for `/src/main.ts`, `/@vite/client`, or a "MIME type text/html" module error | `base` does not equal the mount path — `VITE_BASE_PATH` not honoured, or overwritten with a literal                                                                                                                          | Confirm `base: basePath` and `basePath = readEnv("VITE_BASE_PATH") ?? "./"` in the app's `vite.config.ts`. Then `curl` the mount root inside the workspace and check the emitted `src=` attributes (§10)                                   |
| **403 "This host is not allowed"** (Vite's own error page)                                                                | The forwarded public host is not in `server.allowedHosts`                                                                                                                                                                    | `VITE_ALLOWED_HOSTS` must contain **both** `ai.nie.edu.sg` and `ignite.nie.edu.sg`. Check the spread is not producing an empty array, and that nothing hardcoded a different list                                                          |
| **App loads, HMR never connects** ("[vite] connecting..." then a websocket error)                                         | `clientPort` missing/wrong → malformed `host:` authority; or `base` wrong so the socket path is unroutable                                                                                                                   | Fetch `<mount>/@vite/client` and read `const hmrPort` and `socketHost`. Behind the TLS edge it should be `443` and the host should end in the mount path. Override with `VITE_HMR_CLIENT_PORT` if the edge is not on 443                   |
| **API calls 404** from the preview                                                                                        | The app is deriving API URLs from the base-path heuristic instead of the injected override (`/ignite/api-main/api` is the tell)                                                                                              | Check `window.__APP_TEMPLATE_CONFIG__` in the console. It must carry `mainApiBaseUrl` / `authApiBaseUrl` ending in `…/apps/main-api/` and `…/apps/auth-api/`. If it is missing, the injection plugin is not registered in `plugins: [...]` |
| **API calls 404 with a doubled path** (`…/apps/main-api/ignite/coder/…/api/...`)                                          | Something added a `UsePathBase` or a route prefix to the .NET app                                                                                                                                                            | The proxy strips the prefix for API slugs; the API must serve at the root (§4)                                                                                                                                                             |
| **Workspace stuck at `Starting`**                                                                                         | `preview-main` never healthy — usually the `frontend` supervisord program crash-looping because one Vite app is missing or its config throws                                                                                 | Read `/home/coder/.ignite/logs/frontend.err.log` in the workspace. Verify **both** `src/frontend/main` and `src/frontend/auth` exist with a valid `vite.config.ts` (§8)                                                                    |
| **Workspace `Degraded`, one API tab dead**                                                                                | That API's `/health` is failing for ~5 minutes                                                                                                                                                                               | Read `/home/coder/.ignite/logs/{main,auth}-api.err.log`. If `/health` was made to touch Postgres/Valkey, revert to `Predicate = _ => false` (§9). On a cold boot also check the Main API actually completed `Database.Migrate()`           |
| **Preview goes blank right after `pnpm add`**                                                                             | Expected: the dependency watcher in `run-frontend.sh` detects the changed lockfile/manifest set, reinstalls, and intentionally ends the supervisor run so supervisord restarts both Vite servers with a fresh resolver cache | Wait ~15 s and reload                                                                                                                                                                                                                      |
| **Random logouts / logged in as someone else** across two open previews                                                   | Hardcoded cookie name instead of `FRONTEND_CONSTANTS.cookies.*` (§7)                                                                                                                                                         | Grep for `"AppTemplate-SessionToken"` / `"AppTemplate-User"` string literals outside `constants.ts`                                                                                                                                        |
| **Preview 401s on every request**                                                                                         | Preview ticket expired or missing (the orchestrator's `ignite_preview_ticket` cookie, 1 h max-age)                                                                                                                           | Reload the preview from the Ignite UI so a fresh ticket is minted; this is platform-side, not a template bug                                                                                                                               |

---

## 13. Reference: the Ignite files that define this contract

Read these when this document is not enough. Paths are relative to the Ignite repository, not this
one.

| File                                                                    | Defines                                                                                                           |
| ----------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `build/coder/template/v1/bin/shared.sh`                                 | Ports, workspace root, the .NET dev-service loop                                                                  |
| `build/coder/template/v1/bin/run-frontend.sh`                           | The frontend program: install, both Vite launches, the injected env, the dependency watcher, the shared EXIT trap |
| `build/coder/template/v1/bin/run-main-api.sh`, `run-auth-api.sh`        | `ASPNETCORE_URLS`, project paths, assembly names                                                                  |
| `build/coder/template/v1/bin/run-postgres.sh`                           | Postgres bootstrap, `trust` auth, pre-created databases                                                           |
| `build/coder/template/v1/bin/inject-vite-origin.py`                     | The legacy idempotent Vite patch (a no-op for this repo)                                                          |
| `build/coder/template/v1/etc/supervisord.conf`                          | The five programs, priorities, `startsecs`                                                                        |
| `build/coder/template/v1/main.tf`                                       | `coder_app` slugs, healthchecks, and the `IGNITE_*` agent env                                                     |
| `build/coder/orchestrator/src/server.mjs`                               | `previewAppPorts`, `resolvePreviewProxyTarget` (the strip/forward rule), the websocket upgrade proxy              |
| `build/coder/orchestrator/src/preview-mount.mjs`                        | The mount path regex, app URLs, the ticket cookie                                                                 |
| `build/nginx.conf`                                                      | The four-slug `location` whitelist and the upgrade headers                                                        |
| `docs/plans/2026-07-17-nie-template-std-and-template-contract-layer.md` | §1.5 topology trap; Part 2 manifest contract                                                                      |

And in **this** repository:

| File                                                                   | Role                                                                |
| ---------------------------------------------------------------------- | ------------------------------------------------------------------- |
| `ignite.manifest.json`                                                 | This template's declaration of its own shape for the contract layer |
| `src/frontend/main/vite.config.ts`, `src/frontend/auth/vite.config.ts` | `base`, `allowedHosts`, `hmr`, the runtime-config injection plugin  |
| `src/frontend/packages/shared/src/config/constants.ts`                 | `AppTemplateRuntimeConfig` and every URL/cookie resolver            |
| `src/frontend/packages/shared/src/config/constants.test.ts`            | The override and REGRESSION tests                                   |
| `src/backend/API/Program.cs`, `src/backend/Auth/Program.cs`            | The `/health` · `/health/ready` · `/health/live` split              |
| `.ai/features/ignite-workspace/`                                       | The agent-facing dossier: hard rules, customization, verification   |
