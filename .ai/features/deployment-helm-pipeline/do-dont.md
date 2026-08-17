# Deployment — Do and Don't

## DO ✅

1. **DO** pick the smallest delivery path that meets your requirement. Docker Compose on one host is a complete, defensible answer for a course project; reach for Kubernetes only when you actually have a cluster.
2. **DO** run build, test, and type-check in GitHub Actions on every pull request. CI that only runs on `main` finds problems after they are merged, which is the wrong time.
3. **DO** pin third-party actions to a released version (`actions/checkout@v4`). Floating refs mean someone else's push can change your build.
4. **DO** keep `permissions:` in each workflow as narrow as the job needs — `contents: read` unless it must write.
5. **DO** put every credential in GitHub repository secrets, a `.env` file that is git-ignored, or a Kubernetes Secret. Configuration goes in files; secrets never do.
6. **DO** tag images with an immutable identifier — the commit SHA or a release version — and deploy that exact tag.
7. **DO** sanitize any tag derived from an external identifier before pushing. Docker tags accept only `[A-Za-z0-9_.-]`, cap at 128 characters, and may not start with `.` or `-`.
8. **DO** keep `appKey` and `pathPrefix` explicit and stable per project — resource names and ingress paths are derived from them, so churn there means churn everywhere.
9. **DO** keep backend health probes pointed at `/health` unless a workload documents a different `healthPath`. That endpoint exercises Postgres and Valkey; `/health/live` only proves the process is running.
10. **DO** keep environment differences in `values-dev.yaml` / `values-stg.yaml` / `values-prd.yaml` rather than forking templates.
11. **DO** replace the placeholder hostnames (`apps.dev.example.com`, `apps.example.com`) and the placeholder image repositories (`example.invalid/...`) before your first real deploy. They are deliberately non-resolvable so a half-configured chart fails loudly.
12. **DO** decide explicitly how database migrations get applied — a CI step, an init container, or a manual `dotnet ef database update`. Silent auto-migration on startup is convenient and occasionally catastrophic.

## DON'T ❌

1. **DON'T** commit secrets: no connection strings with passwords, no Sentry DSNs with auth tokens, no registry credentials, no API keys — not in `values*.yaml`, not in `appsettings*.json`, not in a workflow file.
2. **DON'T** echo secrets into CI logs. `echo "${{ secrets.X }}"` publishes them to anyone who can read the run.
3. **DON'T** deploy `:latest` to anything other people use. A restart then silently pulls whatever was pushed most recently.
4. **DON'T** treat Argo Rollouts as required. `rollouts.enabled: false` renders plain Deployments and needs nothing beyond stock Kubernetes. Only turn it on if the controller is actually installed in your cluster.
5. **DON'T** scatter ad-hoc deploy scripts across the repo. Deployment logic belongs in `.github/workflows/` or in the chart, where it is reviewable.
6. **DON'T** fork the Helm templates per service. If two workloads differ, express the difference in the `workloads` map, not in a copied template.
7. **DON'T** point uptime checks at `/health/ready` or `/health/live` and assume they cover dependencies. `/health` is the endpoint that actually exercises Postgres and Valkey.
8. **DON'T** bake environment configuration into an image. The same image must be promotable from dev to production with only its configuration changing.
9. **DON'T** rely on Compose host volumes (`./pgdata`, `./uploads`) as a backup strategy. `docker compose down -v` erases them, and so does a rebuilt VM.
10. **DON'T** grant a CI job cluster-admin because a narrower role was inconvenient to work out. Scope the deploy credential to the namespace it deploys into.
