# Deployment — File Map

## Owned Files

### Helm chart (optional — Kubernetes only)

| Path                                                | Role           | Purpose                                                                                                                                                                                                                                                                                 |
| --------------------------------------------------- | -------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `deploy/helm/app-template/Chart.yaml`               | chart metadata | Names and versions the application chart.                                                                                                                                                                                                                                               |
| `deploy/helm/app-template/values.yaml`              | default values | `appKey`, `environment`, `hostingMode`, `hostName`, `pathPrefix`, `runtimeSecretName`, ingress tuning, `rollouts.enabled`, and the `workloads` map.                                                                                                                                     |
| `deploy/helm/app-template/values-dev.yaml`          | env override   | Development hosts/paths; `rollouts.enabled: false`; single-replica workloads.                                                                                                                                                                                                           |
| `deploy/helm/app-template/values-stg.yaml`          | env override   | Staging hosts/paths; `rollouts.enabled: false`.                                                                                                                                                                                                                                         |
| `deploy/helm/app-template/values-prd.yaml`          | env override   | Production hosts/paths; multi-replica workloads with `hpa`/`pdb` and `strategy: BlueGreen` set as an illustration. Ships `rollouts.enabled: false`, so those workloads still render as plain Deployments — flip the flag to `true` only once the Argo Rollouts controller is installed. |
| `deploy/helm/app-template/templates/_helpers.tpl`   | Helm helper    | Normalizes chart and service names.                                                                                                                                                                                                                                                     |
| `deploy/helm/app-template/templates/workloads.yaml` | Helm template  | Renders Deployments + Services by default; Rollouts + active/preview Services when `rollouts.enabled` and `strategy: BlueGreen`. Wires `envFrom` the runtime secret and health probes from `healthPath`.                                                                                |
| `deploy/helm/app-template/templates/ingress.yaml`   | Helm template  | Path- or host-based ingress driven by `hostingMode`.                                                                                                                                                                                                                                    |
| `deploy/helm/app-template/templates/hpa.yaml`       | Helm template  | Optional autoscaling; rendered per workload that defines `hpa`.                                                                                                                                                                                                                         |
| `deploy/helm/app-template/templates/pdb.yaml`       | Helm template  | Optional disruption budgets; rendered per workload that defines `pdb`.                                                                                                                                                                                                                  |

### Container build & single-host run

| Path                          | Role          | Purpose                                                                                                                                                                                                    |
| ----------------------------- | ------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `build/docker-compose.yml`    | compose stack | UI (published on `8102`) + Auth API + Main API + PostgreSQL (`postgres:18-alpine`) + Valkey + Mailpit. Infrastructure runs on public base images; the three app images come from `${DOCKER_REGISTRY_URL}`. |
| `build/Dockerfile.api`        | image         | Main API container image.                                                                                                                                                                                  |
| `build/Dockerfile.auth`       | image         | Auth API container image.                                                                                                                                                                                  |
| `build/Dockerfile.ui`         | image         | nginx image serving both built SPAs.                                                                                                                                                                       |
| `build/nginx.conf`            | config        | Routing for the UI container, including the SPA fallback and API proxying.                                                                                                                                 |
| `build/appsettings.api.json`  | config        | Main API settings mounted read-only into the container.                                                                                                                                                    |
| `build/appsettings.auth.json` | config        | Auth API settings mounted read-only into the container.                                                                                                                                                    |
| `build/maintenance.html`      | static        | Maintenance page served when the app is intentionally down.                                                                                                                                                |

### CI/CD

| Path                       | Role           | Purpose                                                                                                                                                                                                                                                          |
| -------------------------- | -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `.github/workflows/ci.yml` | GitHub Actions | The only workflow shipped. Builds and tests the .NET solution, then builds the API container images — build only, nothing is pushed and no secrets are used. Audit and image-publish workflows are **not** in the box; add them yourself (see `README.md` § CI). |

## Inputs

Configure these as GitHub repository secrets/variables, Compose `.env` entries, or Helm values — never as committed literals.

| Input                                        | Used by                                                         | Source                                                                                                                      |
| -------------------------------------------- | --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `POSTGRES_PASSWORD`                          | Compose                                                         | `build/.env` (git-ignored; create it with `cp build/.env.example build/.env` — `build/.env.example` ships and is committed) |
| `DOCKER_REGISTRY_URL`                        | Compose, CI publish                                             | `build/.env` / repository variable                                                                                          |
| `COMMIT_ID`                                  | Compose, CI publish                                             | Commit SHA or release tag — the immutable image tag                                                                         |
| Registry credentials                         | CI publish (the publish workflow you add — `ci.yml` needs none) | GitHub repository secrets                                                                                                   |
| `appKey` / `pathPrefix` / `hostName`         | Helm                                                            | `values-<env>.yaml`                                                                                                         |
| Runtime secret contents                      | Helm                                                            | A Kubernetes Secret named by `runtimeSecretName`, created out of band                                                       |
| `workloads.<name>.image.repository` / `.tag` | Helm                                                            | Set at `helm upgrade` time from the CI job's resolved image tag                                                             |

## Migrations

None — this feature ships no database schema. Applying EF Core migrations at deploy time is a decision your pipeline makes (a job step or an init container); the chart does not do it for you.
