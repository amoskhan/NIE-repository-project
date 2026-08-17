# Ignite Workspace Compatibility — Verify

You do **not** need an Ignite workspace to verify this feature. Vite serves the app at its own `base`, so exporting the same environment variables and browsing to the mount path exercises the whole preview + HMR path on your machine.

Run the whole of this file after touching either `vite.config.ts`, `constants.ts`, or either `Program.cs` health block.

---

## 1. Unit tests

```bash
pnpm --dir src/frontend run test:unit
# or a single project:
pnpm --dir src/frontend exec vitest run packages/shared/src/config/constants.test.ts
```

Expect `constants.test.ts` to pass **22 tests**, including the two `REGRESSION` cases that pin what a deep mount resolves to _without_ the runtime-config overrides:

```
getAppBasePath("/ignite/coder/@u/w/apps/preview-main/")  ->  "/ignite"
getBackendUrl("main", "/api")                            ->  "/ignite/api-main/api"
```

Those are deliberately "wrong" answers. If a change makes them pass differently, the heuristic was altered — stop and read `constants.ts`.

## 2. Type-check, lint, build

```bash
pnpm --dir src/frontend run type-check
pnpm --dir src/frontend run lint
pnpm --dir src/frontend run build
dotnet build src/backend/AppTemplate.sln
```

These are the same four commands `ignite.manifest.json` declares under `validation`.

---

## 3. Simulate the Ignite mount locally

### 3a. Start the main app as a workspace would

From `src/frontend/main`:

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

> **Windows / Git Bash:** MSYS rewrites leading-slash values into Windows paths, and you will get a base like `/Program Files/Git/ignite/coder/...`. Export `MSYS_NO_PATHCONV=1` first, or use PowerShell (`$env:VITE_BASE_PATH = "..."`). This is a shell artifact, not a template bug.

`VITE_HMR_CLIENT_PORT=8002` / `VITE_HMR_PROTOCOL=ws` are only needed because you are on plain HTTP on localhost. A real workspace leaves both unset and gets `clientPort: 443` + inferred `wss`.

Vite should print:

```
➜  Local:   http://localhost:8002/ignite/coder/@demo/ws-1/apps/preview-main/
```

If it prints `http://localhost:8002/` instead, `base` is not tracking `VITE_BASE_PATH` — that is the blank-iframe bug.

### 3b. Assert on the served HTML

```bash
B="http://localhost:8002/ignite/coder/@demo/ws-1/apps/preview-main"

curl -s "$B/" | grep -o 'src="[^"]*"'
```

Expect **every** URL to carry the mount path:

```
src="/ignite/coder/@demo/ws-1/apps/preview-main/@vite/client"
src="/ignite/coder/@demo/ws-1/apps/preview-main/src/main.ts"
```

A bare `src="/src/main.ts"` means `base` is wrong and the preview will be blank.

### 3c. Assert on the injected runtime config

```bash
curl -s "$B/" | grep -o 'window.__APP_TEMPLATE_CONFIG__ = .*'
```

Expect a single classic `<script>` in the `<head>` containing:

```js
window.__APP_TEMPLATE_CONFIG__ = {
  mainApiBaseUrl: "/ignite/coder/@demo/ws-1/apps/main-api/",
  authApiBaseUrl: "/ignite/coder/@demo/ws-1/apps/auth-api/",
  authAppUrl: "/ignite/coder/@demo/ws-1/apps/preview-auth/",
  appBasePath: "/ignite/coder/@demo/ws-1/apps/preview-main/",
  sessionCookieName: "NieIgniteWorkspace0-SessionToken",
  userCookieName: "NieIgniteWorkspace0-User",
};
```

Check specifically that:

- `cookieDomain` is **absent** — `VITE_COOKIE_DOMAIN` was the empty string and blank values are dropped;
- the tag has no `type="module"` — it must run before the deferred module scripts;
- for the **auth** app the object carries `mainAppUrl` (from `VITE_DASHBOARD_URL`) instead of `authAppUrl`.

### 3d. Assert on the HMR client

```bash
curl -s "$B/@vite/client" | grep -E "const hmrPort|const socketHost"
```

Expect:

```js
const hmrPort = 8002;
const socketHost = `${null || importMetaUrl.hostname}:${hmrPort || importMetaUrl.port}${"/ignite/coder/@demo/ws-1/apps/preview-main/"}`;
```

The mount path must appear as `__HMR_BASE__` in `socketHost` — that is the websocket path the Ignite orchestrator has to route.

### 3e. Assert the workspace default (443)

Restart with only `VITE_BASE_PATH` and `VITE_ALLOWED_HOSTS` set (drop the two HMR variables) and repeat 3d:

```js
const hmrPort = 443;
```

That is the value a real workspace gets, and the reason the socket URL is well-formed on the HTTPS edge.

### 3f. Assert the no-environment baseline

Restart with **no** `VITE_*` variables at all and hit `http://localhost:8002/`:

```bash
curl -s "http://localhost:8002/" | grep -oE 'src="[^"]*"|__APP_TEMPLATE_CONFIG__'
curl -s "http://localhost:8002/@vite/client" | grep "const hmrPort"
```

Expect exactly:

```
src="/@vite/client"
src="/src/main.ts"
const hmrPort = null;
```

and **no** `__APP_TEMPLATE_CONFIG__` match. This proves the bridge is fully inert for plain local development.

### 3g. Browser check — HMR really connects

With the 3a environment running, open <http://localhost:8002/ignite/coder/@demo/ws-1/apps/preview-main/> and:

1. DevTools → Network → WS: expect a `101 Switching Protocols` to `…/apps/preview-main/`.
2. Console: expect `[vite] connected.` and **no** `failed to connect to websocket`.
3. Edit any `.vue` file and save: the change appears without a full reload.
4. Console: `window.__APP_TEMPLATE_CONFIG__` returns the object from 3c.

API calls will 404 in this simulation — nothing is serving `…/apps/main-api/` locally. That is expected; this recipe validates module/asset/HMR path resolution and runtime-config injection, not end-to-end API traffic.

### 3h. Repeat for the auth app

From `src/frontend/auth`, with `preview-auth`, port **8001**, and `VITE_DASHBOARD_URL="$MOUNT/preview-main/"` in place of `VITE_AUTH_SERVICE_URL`. Both configs must behave identically — the helper block is duplicated, so a fix applied to only one is the classic failure here.

---

## 4. Health-check contract

```bash
dotnet run --project src/backend/API      # 5002 locally
dotnet run --project src/backend/Auth     # 5001 locally
```

```bash
# Liveness — the endpoint the Coder coder_app healthcheck polls. Runs NO checks.
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health   # 200
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/health   # 200
curl -s http://localhost:5002/health                                    # "Healthy"

# Readiness — runs the "ready"-tagged Postgres + Valkey probes.
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health/ready   # 200
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/health/ready   # 200

# Plain-text liveness.
curl -s http://localhost:5002/health/live   # "ok"
curl -s http://localhost:5001/health/live   # "ok"
```

### The decisive test: liveness must survive a dead dependency

```bash
docker stop apptemplate-postgres    # or whatever your dev Postgres container is called

curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health         # MUST still be 200
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/health         # MUST still be 200
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health/ready   # MUST be 503
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health/live    # MUST be 200

docker start apptemplate-postgres
```

If `/health` returns 503 here, a dependency probe has leaked onto liveness and every Ignite workspace will park at _Degraded_ on the next Postgres blip. Fix it by tagging the probe `"ready"` and restoring `Predicate = _ => false` on `/health`.

Repeat with Valkey stopped.

---

## 5. Topology and manifest sanity

```bash
# All four services must exist.
test -d src/frontend/main   && test -d src/frontend/auth   && echo "frontends OK"
test -f src/backend/API/API.csproj && test -f src/backend/Auth/Auth.csproj && echo "backends OK"

# The manifest must describe reality.
python -c "import json;print(json.load(open('ignite.manifest.json'))['frontends'][0]['path'])"
```

Then eyeball `ignite.manifest.json` against the tree:

| Manifest field                      | Must match                                                                                          |
| ----------------------------------- | --------------------------------------------------------------------------------------------------- |
| `frontends[].path`                  | a real directory                                                                                    |
| `frontends[].dependencyManifests[]` | real files (they are **repo-relative**: `src/frontend/pnpm-lock.yaml`, not a bare `pnpm-lock.yaml`) |
| `backends[].projectPath`            | a real `.csproj`                                                                                    |
| `backends[].assemblyName`           | the `.csproj` base name, unless an `AssemblyName` override was added                                |
| `datastores[].database`             | `ConnectionStrings:MainDbConnection` in `src/backend/API/appsettings.json`                          |
| `validation.*`                      | commands that actually resolve — check with `pnpm --dir src/frontend run`                           |
| `toolchain.dotnetSdk`               | `global.json`                                                                                       |
| `toolchain.node` / `pnpm`           | `src/frontend/package.json` `engines` / `packageManager`                                            |

## 6. No hardcoded cookie names

```bash
grep -rn "AppTemplate-SessionToken\|AppTemplate-User" src/frontend \
  --include=*.ts --include=*.vue \
  | grep -v node_modules | grep -v /dist/ | grep -v constants.test.ts
```

Expect matches **only** in `src/frontend/packages/shared/src/config/constants.ts` (the two `DEFAULT_*_COOKIE_NAME` constants). Anything else breaks per-workspace isolation.

## 7. `inject-vite-origin.py` stays a no-op

The workspace runs that legacy patch script over both Vite configs at every frontend start. It only edits a literal `base: "/"` / `base: "/login/"`, and only injects `allowedHosts` when the text has none.

```bash
grep -n 'base:' src/frontend/main/vite.config.ts src/frontend/auth/vite.config.ts
grep -c 'allowedHosts' src/frontend/main/vite.config.ts src/frontend/auth/vite.config.ts
```

Expect `base: basePath` (never a bare string literal) and at least one `allowedHosts` occurrence in each file. If either check fails, the workspace will start rewriting your config on boot.
