# Ignite Workspace Compatibility — Do and Don't

These are hard rules. Breaking one usually produces a symptom that only appears **inside a workspace**, never on your laptop — a blank iframe, a workspace parked at _Starting_, or a session that gets clobbered by another student's preview. Verify against [`verify.md`](./verify.md) before you call a Vite or health-check change done.

## DO ✅

1. **DO** keep `base: basePath` in both `vite.config.ts` files, where `basePath = readEnv("VITE_BASE_PATH") ?? "./"`. The preview proxy forwards the **full mount path** to Vite, so `base` must equal it. `"./"` stays the default so production builds keep emitting relative asset URLs.
2. **DO** keep the `allowedHosts` spread conditional:
   ```ts
   ...(configuredAllowedHosts && configuredAllowedHosts.length > 0 ? { allowedHosts: configuredAllowedHosts } : {}),
   ```
   Assigning an empty array rejects _everything_, including local development.
3. **DO** keep `resolveHmrOptions()`'s `clientPort: 443` default whenever `VITE_BASE_PATH` is set. On an `https:` page the module URL's port is empty, so without a pinned port Vite builds a malformed `host:` authority and the socket never opens.
4. **DO** leave the HMR **protocol** to the client. Vite infers `wss` from the `https:` module URL correctly. `VITE_HMR_PROTOCOL` exists as an escape hatch, not as something to set by default.
5. **DO** deliver per-deployment application config through `window.__APP_TEMPLATE_CONFIG__` (or the `<meta name="app:key">` channel), which `getRuntimeString()` in `constants.ts` reads.
6. **DO** mirror any change to the hosted-preview helper block across **both** `src/frontend/main/vite.config.ts` and `src/frontend/auth/vite.config.ts`. It is duplicated on purpose — `main` and `auth` are separate Vite projects and a cross-project import would break `vite build`. The only difference between the two blocks is `RUNTIME_CONFIG_FROM_ENV` (main maps `VITE_AUTH_SERVICE_URL`→`authAppUrl`; auth maps `VITE_DASHBOARD_URL`→`mainAppUrl`).
7. **DO** read every cookie name from `FRONTEND_CONSTANTS.cookies.session` / `.user`. Ignite namespaces them per workspace (`NieIgniteWorkspace<projectId>-SessionToken`) because every preview shares one public hostname.
8. **DO** register any new dependency probe with the `"ready"` tag: `.AddX(..., tags: readyTags)`. `/health` must keep `Predicate = _ => false`.
9. **DO** keep both frontends (`src/frontend/main`, `src/frontend/auth`) and both backends (`src/backend/API`, `src/backend/Auth`) present. The workspace's `run-frontend.sh` runs both Vite apps under one supervisor program with a shared `EXIT` trap.
10. **DO** update `ignite.manifest.json` in the same commit whenever you change a project path, an assembly name, a port, a health path, an install command or a validation command. It is the template's declaration of its own shape and is deliberately **not** in copier's `_skip_if_exists`, so `copier update` keeps derived repos current.
11. **DO** keep new absolute-path/absolute-URL handling going through `normalizeConfiguredBaseUrl()` and `joinBaseAndPath()` in `constants.ts`. `joinPath()` alone mangles a scheme (`https://x` → `/https:/x`).
12. **DO** re-run the local mount simulation in [`verify.md`](./verify.md) after touching a Vite config, `constants.ts`, or either `Program.cs` health block.

## DON'T ❌

1. **DON'T** hardcode `base` to a literal (`"/"`, `"/login/"`, `"/ignite/coder/..."`). A literal `base: "/"` also re-arms the workspace's legacy `inject-vite-origin.py` patch script, which currently finds nothing to change and is a harmless no-op — a literal makes it start rewriting your config behind your back.
2. **DON'T** delete `src/frontend/auth` (or `src/backend/Auth`) to "simplify" the template. Deleting the auth app takes the **main** app down with it: `wait -n` returns, the shared trap kills the surviving Vite, supervisord exhausts its retries, and the workspace never leaves _Starting_. This is the trap the Ignite plan calls out in §1.5.
3. **DON'T** change any workspace port. 18002 / 18001 / 15002 / 15001 / 5432 / 6379 are declared in the Ignite `shared.sh`, mirrored in `main.tf` and in the orchestrator's `previewAppPorts`. Equally, **don't** "fix" `server.port: 8002` to 18002 — the workspace passes `--port` on the CLI, which wins, and 8002 is what local `pnpm dev` needs.
4. **DON'T** add `app.UsePathBase(...)`, a global route prefix, or any other mount-path handling to the .NET APIs. The proxy **strips** the prefix for the `main-api` / `auth-api` slugs; the API never sees it and must serve at the root.
5. **DON'T** use `import.meta.env.VITE_*` for application configuration. It bakes values into the bundle at build time and breaks build-once-promote-many. `VITE_*` variables reach the app only through the injection plugin, at request time, in dev only.
6. **DON'T** hardcode `"AppTemplate-SessionToken"` or `"AppTemplate-User"` anywhere. Two previews open in one browser will silently overwrite each other's session — a bug that is close to impossible to reproduce from a student's report.
7. **DON'T** put a Postgres, Valkey or any other dependency probe on `/health`. supervisord starts `auth-api` and `main-api` at the same priority and the application database is created by the **Main API's** `Database.Migrate()`, so on a cold boot the Auth API is healthy while its database does not exist yet. A dependency-touching liveness probe parks the whole workspace at _Degraded_. Use `/health/ready`.
8. **DON'T** point an uptime monitor, Kubernetes readiness probe or load-balancer target group at `/health` expecting it to detect a dead database — it now returns 200 with an empty check set. Repoint those at `/health/ready`.
9. **DON'T** switch either router to `createWebHistory()`. Both apps use `createWebHashHistory()`, so deep links live behind `#` and the preview never needs an SPA history fallback — which the proxy does not provide.
10. **DON'T** remove the `allowedHosts` key entirely. Besides the 403 risk, its absence lets `inject-vite-origin.py` inject its own `server` block into your config at workspace start.
11. **DON'T** introduce a new **required** environment variable for the frontend to boot. The workspace sets exactly nine `VITE_*` variables; anything else is unset there and the app will fail in a workspace while working perfectly on your laptop. New config belongs in `AppTemplateRuntimeConfig` with a sane default.
12. **DON'T** add a fifth browser-reachable service. Only `preview-main`, `preview-auth`, `main-api` and `auth-api` are whitelisted, in two independent places (nginx `location` regex and the orchestrator's `previewAppPorts`).
13. **DON'T** rename or move `src/frontend/{main,auth}`, `src/backend/API/API.csproj`, `src/backend/Auth/Auth.csproj`, or the built assembly names (`API`, `Auth`). The workspace run scripts hardcode all six, and `run_dotnet_dev_service` looks for `<assembly>.dll` under `bin/Debug`.
14. **DON'T** move `pnpm-lock.yaml` / `pnpm-workspace.yaml` out of `src/frontend/`. `run-frontend.sh` installs from there and fingerprints those files (plus `.npmrc` and every `package.json` up to depth 3) to decide when to reinstall and restart Vite.
15. **DON'T** convert the injected runtime-config `<script>` to a module, or move the config read into an inline module in `index.html`. The injected tag is a classic script at `head-prepend` specifically so it executes before any module script.
16. **DON'T** assume `getFrontendAssetUrl(path, "auth")` is correct under Ignite — it still appends the `/login` segment to the app base path, and under Ignite the auth app is a separate mount. No caller does this today; if you need one, go through `authAppUrl`.
