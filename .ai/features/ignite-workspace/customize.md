# Ignite Workspace Compatibility — Customize

Read [`do-dont.md`](./do-dont.md) first: several plausible-looking customizations are the exact things that break a workspace.

## 1. Add a new runtime-config key

Application config that varies per deployment goes in `AppTemplateRuntimeConfig`, never in `import.meta.env`.

1. Add the optional field, with a doc comment, in `src/frontend/packages/shared/src/config/constants.ts`:
   ```ts
   interface AppTemplateRuntimeConfig {
     // ...
     /** Absolute URL of the reporting service; defaults to "<appBasePath>/api-reports". */
     reportsApiBaseUrl?: string;
   }
   ```
2. Read it through `getRuntimeString("reportsApiBaseUrl")`, which checks `window.__APP_TEMPLATE_CONFIG__` first and then `<meta name="app:reportsApiBaseUrl">`.
3. Normalise it with `normalizeConfiguredBaseUrl()` and join with `joinBaseAndPath()` so an absolute URL is not mangled:
   ```ts
   export function getReportsBaseUrl(): string {
     const configured = getRuntimeString("reportsApiBaseUrl");
     if (configured) return normalizeConfiguredBaseUrl(configured);
     return joinBaseAndPath(getAppBasePath(), "api-reports");
   }
   ```
4. **Give it a working default.** A key with no default becomes a required variable, and the workspace will not set it.
5. Add tests to `constants.test.ts` covering: absent (fallback), a path override, an absolute-URL override, and a trailing slash.

## 2. Expose a new key to the hosted preview

Only if Ignite actually injects a variable for it — the workspace sets exactly nine `VITE_*` variables today.

Add the mapping to `RUNTIME_CONFIG_FROM_ENV` in **both** `vite.config.ts` files (or just the relevant one, if the variable is app-specific like `VITE_AUTH_SERVICE_URL`):

```ts
const RUNTIME_CONFIG_FROM_ENV: Record<string, string> = {
  // ...
  VITE_REPORTS_API_URL: "reportsApiBaseUrl",
};
```

`collectRuntimeConfig()` drops blank values automatically, so an unset variable adds nothing to the injected object.

## 3. Override the HMR client port or protocol

The config pins `clientPort: 443` whenever `VITE_BASE_PATH` is set, because that is where TLS terminates at the Ignite edge. If a future edge terminates on a different port, set the escape hatch on the Vite process rather than editing the config:

```bash
VITE_HMR_CLIENT_PORT=8443 ...  # any positive integer wins over the 443 default
VITE_HMR_PROTOCOL=ws           # forces ws:// instead of the inferred wss://
```

`resolveHmrOptions()` returns `undefined` when neither is set and `VITE_BASE_PATH` is absent, leaving `server.hmr` entirely at Vite's default for plain local `pnpm dev`.

Note: when a `clientPort` is set, Vite's HMR client does **not** attempt its "direct websocket connection fallback". A wrong port therefore fails permanently rather than silently recovering — good for debugging, bad if you guess.

## 4. Change the default cookie names

The defaults live at the top of `constants.ts`:

```ts
const DEFAULT_SESSION_COOKIE_NAME = "AppTemplate-SessionToken";
const DEFAULT_USER_COOKIE_NAME = "AppTemplate-User";
```

`tools/template-rename/rename.py` rewrites the `AppTemplate` branding when you scaffold. For a deployment that shares a hostname with another deployment of the same app, do **not** change the default — set the runtime-config override instead, in your `index.html` / nginx / Helm config:

```html
<script>
  window.__APP_TEMPLATE_CONFIG__ = {
    sessionCookieName: "MyApp-Staging-SessionToken",
    userCookieName: "MyApp-Staging-User",
  };
</script>
```

Ignite does exactly this, with `NieIgniteWorkspace<projectId>-SessionToken`.

Because `FRONTEND_CONSTANTS` is frozen at module evaluation, a test that exercises the override must reset the module registry:

```ts
vi.resetModules();
window.__APP_TEMPLATE_CONFIG__ = { sessionCookieName: "X-Session" };
const { FRONTEND_CONSTANTS } = await import("./constants");
expect(FRONTEND_CONSTANTS.cookies.session).toBe("X-Session");
```

## 5. Add a dependency health check

Register it with the `"ready"` tag so it lands on `/health/ready` and **not** on the Coder liveness probe:

```csharp
string[] readyTags = ["ready"];
builder.Services.AddHealthChecks()
    .AddNpgSql(configuration.GetConnectionString("MainDbConnection")!, name: "postgresql", tags: readyTags)
    .AddRedis(configuration["Valkey:ConnectionString"]!, name: "valkey", tags: readyTags)
    .AddCheck<SapApiHealthCheck>("sap", tags: readyTags);
```

Leave the three mappings alone:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/health/live", () => Results.Ok("ok"));
```

An **untagged** check joins neither endpoint's predicate set (`/health` runs nothing, `/health/ready` runs only `"ready"`), so it would silently never run. Always tag.

## 6. Add a new page or asset that must load in the preview

Nothing special is required for anything Vite serves — it inherits `base`. But:

- Static files referenced from `index.html` with an absolute `/...` path (e.g. `/manifest.json`, `/app-logo-title.svg`) are rewritten by Vite against `base` for you. Absolute paths built **in code** are not — use `getFrontendAssetUrl()`.
- Both routers use `createWebHashHistory()`, so new routes live behind `#` and need no proxy change.

## 7. Update `ignite.manifest.json`

Change it whenever the shape it describes changes. Fields that mirror reality:

| Field                                       | Source of truth in this repo                                                                                                                                                    |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `workspaceRoot`                             | Ignite's `shared.sh` → `IGNITE_DEFAULT_WORKSPACE_ROOT`                                                                                                                          |
| `frontends[].path` / `port` / `previewSlug` | the app directories, Ignite's ports, the four allowed slugs                                                                                                                     |
| `frontends[].installCommand`                | `pnpm --dir src/frontend install …` — install happens at the pnpm workspace root, not in the app directory                                                                      |
| `frontends[].dependencyManifests`           | **repo-relative** paths (`src/frontend/pnpm-lock.yaml`, not a bare `pnpm-lock.yaml`), because the lockfile and workspace file live at `src/frontend/`, not in the app directory |
| `backends[].projectPath` / `assemblyName`   | `src/backend/API/API.csproj` → `API`; no `AssemblyName` override exists in `Directory.Build.props` or either `.csproj`, so the assembly name is the project-file name           |
| `backends[].healthPath`                     | `/health/live`                                                                                                                                                                  |
| `datastores[].database`                     | `src/backend/API/appsettings.json` → `ConnectionStrings:MainDbConnection`                                                                                                       |
| `validation.*`                              | scripts that actually exist — `pnpm --dir src/frontend run type-check` / `lint` / `build`, and the real solution name `src/backend/AppTemplate.sln`                             |
| `toolchain`                                 | `global.json` (dotnet SDK), `src/frontend/package.json` `engines` + `packageManager`, `.devcontainer/docker-compose.yml` (postgres image tag)                                   |

`repo` currently carries the template's placeholder (`https://github.com/your-org/app-template.git`), consistent with `.app-template-version.json`. Whoever registers this template with Ignite must supply the real URL on the platform side.

## 8. Things you cannot customize (yet)

| Want                                                 | Status                                                                                                                                                 |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Only one frontend app                                | Not possible — `run-frontend.sh` runs both under one supervisord program with a shared `EXIT` trap                                                     |
| Only one backend API                                 | Not possible — the missing API's `coder_app` healthcheck fails forever and parks the workspace at _Degraded_                                           |
| A third frontend / a fifth browser-reachable service | Not possible — four slugs, whitelisted in the Ignite nginx `location` regex and the orchestrator's `previewAppPorts`                                   |
| Different ports                                      | Not possible — declared in Ignite's `shared.sh`, `main.tf` and the orchestrator                                                                        |
| A `stack: backend` scaffold in a workspace           | Not supported — `copier.yml` removes `src/frontend/` but `ignite.manifest.json` still declares two frontends, and `preview-main` never becomes healthy |

Ignite's contract layer (plan Part 2) is intended to lift the first four. Until it ships, treat them as fixed.
