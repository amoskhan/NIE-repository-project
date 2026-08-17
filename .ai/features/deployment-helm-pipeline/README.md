# Deployment — GitHub Actions, Helm, Docker Compose

> **Status:** `released`
> **Removable in derived repos:** **partly** — pick the delivery path you actually use and delete the rest
> **Required by:** nothing at runtime; `health-observability` supplies the `/health` endpoint every probe depends on

This feature is the delivery layer: how the code that runs on your laptop becomes something other people can open in a browser. The template gives you three pieces and expects you to use **one or two** of them, not all three.

| Piece              | Where                       | Use it when                                                                                                                                 |
| ------------------ | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| **GitHub Actions** | `.github/workflows/`        | Always. Build and test on every push to `main` and every pull request.                                                                      |
| **Docker Compose** | `build/docker-compose.yml`  | Running the whole stack on one machine — your laptop, a course VM, a single cloud box. This is the right default for most student projects. |
| **Helm chart**     | `deploy/helm/app-template/` | Only if you have a Kubernetes cluster. If you do not know whether you have one, you do not have one — use Compose.                          |

Nothing here is mandatory. A project that ships by running `docker compose up` on a single VM and never touches Helm is a perfectly finished project.

## Quick Links

- [`files.md`](./files.md) — every file this feature owns
- [`do-dont.md`](./do-dont.md) — feature-specific rules
- [`verify.md`](./verify.md) — lint, render, and smoke checks you can run locally

## CI: GitHub Actions

Workflows live in `.github/workflows/`. **The template ships exactly one workflow — `ci.yml`.** Everything else in the table below is a gap you fill yourself; nothing in the box publishes an image or gates a PR on the template audit.

| Workflow                | Ships?                                  | Trigger                      | Does                                                                                                                                                                                                                                                        |
| ----------------------- | --------------------------------------- | ---------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ci.yml` — Build & test | **Yes — the only workflow in the repo** | push to `main`, pull request | Restores and builds `src/backend/AppTemplate.sln` in Release, runs any `*Tests.csproj` it finds (skips cleanly when there are none), then builds the API container images with `docker build`. **Build only — no `--push`, no registry login, no secrets.** |
| Audit                   | **No — add it yourself**                | pull request                 | Would run `python tools/template-audit/audit.py` so template drift fails the PR instead of being discovered months later. The tool exists; no workflow calls it.                                                                                            |
| Image publish           | **No — add it yourself**                | tag or manual dispatch       | Would build `build/Dockerfile.api`, `Dockerfile.auth`, and `Dockerfile.ui` and push them to a container registry. Add a release workflow that logs into your registry and reruns the same builds with `--push`.                                             |

`ci.yml` needs no secrets, so it works on a fork from the first push. Adding either of the missing workflows is the point where secrets start to matter — read the rules below before you write them.

Rules that matter more than the exact YAML:

- **Secrets come from GitHub repository secrets**, referenced as `${{ secrets.NAME }}`. Never commit a credential, and never `echo` a secret into the job log.
- **Pin action versions** (`ci.yml` uses `actions/checkout@v7` and `actions/setup-dotnet@v6`, never `@main`) so a third-party change cannot silently alter your build. Confirm a major tag actually exists on the action's releases page before bumping it — a tag that does not exist fails the job instantly with "Unable to resolve action".
- **Restrict `permissions:`** to what the job needs. The default of `contents: read` is right for most jobs.
- **Cache pnpm and NuGet** — it is the difference between a 90-second CI run and a 6-minute one.

## Local & single-host: Docker Compose

`build/docker-compose.yml` brings up the full stack: the UI (nginx, published on `8102`), the Auth API, the Main API, PostgreSQL (`postgres:18-alpine`), Valkey, and Mailpit — the infrastructure all on **public base images** (`postgres`, `valkey/valkey`, `axllent/mailpit`). Only the three application images come from `${DOCKER_REGISTRY_URL}`.

```bash
cd build
# Create build/.env (git-ignored; the template ships no .env.example) with the
# three variables the compose file interpolates:
#   DOCKER_REGISTRY_URL=ghcr.io/your-org
#   COMMIT_ID=<git sha or release tag>
#   POSTGRES_PASSWORD=<must match appsettings.*.json>
docker compose up -d
docker compose ps
```

Backend settings are mounted read-only from `build/appsettings.api.json` and `build/appsettings.auth.json`, so you change configuration without rebuilding an image. Uploads land in `./uploads`, database files in `./pgdata` — both are host volumes, so back them up or accept that `docker compose down -v` erases them.

## Kubernetes: the Helm chart

`deploy/helm/app-template/` is a small, neutral chart. It exists for projects that already have a cluster; skip it otherwise.

```text
deploy/
  helm/
    app-template/
      Chart.yaml
      values.yaml          # defaults
      values-dev.yaml      # per-environment overrides
      values-stg.yaml
      values-prd.yaml
      templates/
        _helpers.tpl
        hpa.yaml           # optional autoscaling
        ingress.yaml       # path- or host-based routing
        pdb.yaml           # optional disruption budgets
        workloads.yaml     # Deployments (or Rollouts) + Services
```

Key values:

| Value               | Meaning                                                                                                     |
| ------------------- | ----------------------------------------------------------------------------------------------------------- |
| `appKey`            | Stable short identifier for the app; feeds resource names                                                   |
| `environment`       | `dev` / `stg` / `prd` — used for naming and labelling                                                       |
| `hostingMode`       | `path` (share a hostname under `pathPrefix`) or `host` (own hostname)                                       |
| `hostName`          | The ingress hostname. Ships as `apps.dev.example.com` / `apps.example.com` — **placeholders, replace them** |
| `pathPrefix`        | Path segment when `hostingMode: path`                                                                       |
| `runtimeSecretName` | Name of the Kubernetes Secret mounted into backend containers via `envFrom`                                 |
| `workloads.<name>`  | Per-service map: image repo/tag, port, `healthPath`, replicas, strategy, optional `hpa` and `pdb`           |
| `rollouts.enabled`  | **Opt-in.** `false` by default                                                                              |

### Argo Rollouts is optional

When `rollouts.enabled: false`, `workloads.yaml` renders plain Kubernetes `Deployment`s and a single `Service` per workload. Nothing beyond stock Kubernetes is required. **All three environment files — `values-dev.yaml`, `values-stg.yaml`, and `values-prd.yaml` — ship `rollouts.enabled: false`**, so a fresh `helm install` works on any vanilla cluster with no extra CRDs.

Setting `rollouts.enabled: true` **and** a workload's `strategy: BlueGreen` renders an `argoproj.io/v1alpha1` `Rollout` plus active/preview Services instead. That requires the Argo Rollouts controller to be installed in the cluster. `values-prd.yaml` already sets `strategy: BlueGreen` on both API workloads as an illustration of what a production profile can look like, but with `rollouts.enabled: false` that field is inert — flip the flag to `true` only after installing the Argo Rollouts controller and CRDs, or `helm install` fails with "no matches for kind Rollout".

## Image tags

Whatever pipeline you use, resolve every image to an **immutable tag** — a commit SHA or a release version. `latest` is fine while you are iterating locally and actively harmful in a deployed environment, because "restart the pod" silently becomes "deploy whatever was pushed most recently".

Sanitize tags derived from external identifiers before pushing: Docker tags allow only `[A-Za-z0-9_.-]`, max 128 chars, and cannot start with a period or dash.
