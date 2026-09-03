# Ignite workspace runtime

Staff applications declare independently running processes in
`ignite.services.json`. The workspace watches this file, updates Supervisor,
and publishes every frontend through the Live Build service picker. The same
contract is used in local Docker workspaces and AWS EKS workspaces.

Supported service kinds are `frontend`, `backend`, and `worker`. A frontend or
backend needs a unique TCP port. Prefer `18100`–`18199` for additional
frontends and `15100`–`15199` for additional backends; `5432`, `6379`, and
`19000` are reserved by the workspace. Exactly one frontend is primary.
The service ids `postgres`, `valkey`, `runtime-gateway`, and
`runtime-services-watcher` are also reserved for platform processes.

Frontend, command-backed backend, and worker commands are JSON string arrays,
so no shell parsing is required. A .NET backend identifies a
repository-relative `.csproj` and assembly; a backend implemented with another
runtime identifies a working directory and command instead. Both backend forms
declare a port and health path and are published through the same semantic
service route. Ignite provides the resilient supervision/reload loop for each
form.
Use `environment` only for non-secret configuration names and values. Secrets
remain deployment/runtime configuration and must never be committed here.

## Per-service environment overrides

Ignite's **Live Build → Workspace Health → Environment variables** editor sets
per-service overrides without touching the repository. They are stored in
`~/.ignite/service-environment.json` (mode `0600`), which is outside the project
tree — so nothing typed there is committed, backed up with the project, or
readable through the CLI agent's file tools:

```json
{
  "version": 1,
  "services": { "preview-main": { "VITE_FEATURE_FLAGS": "beta" } }
}
```

Precedence, lowest to highest: the agent process environment, the service's
`environment` block in `ignite.services.json`, the values Ignite computes for
every service (`PORT`, `VITE_BASE_PATH`, `VITE_API_URL`, cookie names, …), then
these overrides. The workspace keeps `PORT`, `PATH`, `HOME`, `USER`, `SHELL`,
`PWD`, `VITE_BASE_PATH`, `VITE_ALLOWED_HOSTS`, and every `IGNITE_*` name: an
override there would take the service off its preview mount or break the
container, so it is rejected rather than ignored.

.NET backends additionally reserve `ASPNETCORE_URLS` and every name defined in
`.ignite/runtime.env` and `.ignite/application-runtime.env` — including the
`ExternalAPIs__*` and `NIEAuthApi__*` allow-list. `run-dotnet-runtime-service.sh`
re-sources those files through `load_runtime_env` after inheriting the service
environment, so an override of one of those names could never take effect;
`env-describe` reports them as locked. Frontends, workers, and command-backed
backends are launched directly and do not reserve them.
Names accept `[A-Za-z_][A-Za-z0-9_]*` so .NET `Section__Key` configuration works;
at most 32 variables per service, 1000 characters per value.

Saving from Ignite restarts that one service. A hand edit of the overlay is not
watched — apply it yourself:

```bash
node /opt/ignite/bin/runtime-services.mjs env-describe <service-id> | jq
supervisorctl -c /opt/ignite/etc/supervisord.conf restart <service-id>
```

Ignite delivers its small application-runtime allow-list (currently NIE Auth
and StaffStore settings) through an ephemeral Coder parameter and materializes
it as `.ignite/application-runtime.env` with mode `0600`. Application services
source that file; CLI agents source the separate `agent-runtime.env` and do not
inherit application credentials. Rotation is applied by a preserving workspace
restart when the allow-listed configuration fingerprint changes.

Browser code must address services by their stable manifest id, never by
constructing Coder owner, workspace, application, port, ingress, or EKS paths.
Every frontend mount exposes a same-origin runtime map at
`./~ignite/runtime-config` and same-mount service routes at
`./~ignite/services/<service-id>/`. Ignite injects the equivalent typed routing
values before the application starts. The shared resolver validates this
external input, exposes generic semantic service-id lookup, and retains
ordinary `/api-main`, `/api-auth`, `/`, and `/login` fallbacks for standalone
Docker Compose and Kubernetes deployments. Preview session cookies receive a
workspace-derived name and the common workspace-app path so two previews on
the shared Coder origin cannot reuse each other's browser session. Cookies set
directly by preview backends are also made host-only and rewritten to that
workspace path; `__Host-` cookies are omitted because their mandatory root path
cannot be isolated on a shared preview origin.
Older copied templates that still call `/api-main` or `/api-auth` are bridged
to the same manifest routes at runtime. The bridge is limited to same-origin
Fetch, XHR, WebSocket, and EventSource traffic for those two legacy prefixes;
new code must use the typed semantic resolver instead.

When a service is not running, begin with:

```bash
curl -fsS http://127.0.0.1:19000/__ignite/status | jq
supervisorctl -c /opt/ignite/etc/supervisord.conf status
tail -n 200 ~/.ignite/logs/<service-id>.err.log
```

Fix the underlying build/runtime error and confirm every required service is
healthy. Changes to `ignite.services.json` and frontend dependency manifests
are detected automatically.
