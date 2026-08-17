# Deployment — Verify

Run from the project root.

## Docker Compose (the path most projects use)

```bash
cd build

# Bring the stack up
docker compose up -d

# Every service should be running or healthy — none restarting
docker compose ps

# Postgres and Valkey report healthy via their compose healthchecks
docker compose ps --format "table {{.Service}}\t{{.Status}}"

# The Main API's own health endpoint exercises Postgres + Valkey
docker compose exec apptemplate-api-service curl -sf http://localhost:8080/health
# Expect: exit 0 and a healthy JSON payload

# The UI is served
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8102/
# Expect: 200

# Tear down (add -v only if you mean to delete pgdata and uploads)
docker compose down
```

Confirm no secret leaked into the committed files:

```bash
git -C .. check-ignore build/.env && echo ".env is ignored — good"
grep -rnE "password\s*[:=]\s*[\"'][^\"']+" build/*.json build/docker-compose.yml
# Expect: only ${VARIABLE} placeholders, never a literal
```

## GitHub Actions

```bash
# Validate every workflow parses as YAML before you push
python - <<'PY'
import pathlib, yaml, sys
found = False
for path in sorted(pathlib.Path(".github/workflows").glob("*.y*ml")):
    yaml.safe_load(path.read_text(encoding="utf-8"))
    print("ok:", path)
    found = True
sys.exit(0 if found else "no workflows found under .github/workflows")
PY
```

```bash
# No floating action refs
grep -rnE "uses:\s+\S+@(main|master)\b" .github/workflows/
# Expect: no output — pin to a released tag

# No inline secrets
grep -rnE "(password|secret|token|api[_-]?key)\s*[:=]\s*[\"'][^\"'$]" .github/workflows/
# Expect: no output — everything should go through ${{ secrets.* }}
```

Reproduce the build locally before pushing. The first two lines are what `ci.yml` actually runs; the pnpm lines are the frontend equivalent, which no shipped workflow covers yet — run them by hand:

```bash
dotnet build src/backend/AppTemplate.sln
dotnet test  src/backend/AppTemplate.sln
pnpm -C src/frontend install --frozen-lockfile
pnpm -C src/frontend --filter main type-check
pnpm -C src/frontend --filter main build:production
```

Then watch the real run: `gh run list --limit 5` and `gh run view --log-failed`.

## Helm chart (only if you deploy to Kubernetes)

```bash
helm lint deploy/helm/app-template \
  -f deploy/helm/app-template/values.yaml \
  -f deploy/helm/app-template/values-dev.yaml
```

Render with the dev profile and confirm you get **plain Deployments** (no Argo CRDs) when rollouts are off:

```bash
helm template apptemplate deploy/helm/app-template \
  -f deploy/helm/app-template/values.yaml \
  -f deploy/helm/app-template/values-dev.yaml | grep -E "^kind:" | sort | uniq -c
# Expect: Deployment / Service / Ingress (and HorizontalPodAutoscaler if configured).
# Expect NO "kind: Rollout" — that only appears when rollouts.enabled is true.
```

Render an ad-hoc workload to check the value plumbing:

```bash
helm template apptemplate deploy/helm/app-template \
  -f deploy/helm/app-template/values.yaml \
  --set workloads.api-main.replicas=1 \
  --set workloads.api-main.image.repository=example/api-main \
  --set workloads.api-main.image.tag=local \
  --set workloads.api-main.containerPort=8080 \
  --set workloads.api-main.servicePort=80 \
  --set workloads.api-main.healthPath=/health
```

Confirm the placeholders were replaced before a real deploy:

```bash
grep -rnE "example\.com|example\.invalid" deploy/helm/app-template/values-*.yaml
# Expect in a real project: no output. In the pristine template these are the
# intentional non-resolvable placeholders.
```

Validate the rendered manifests against the cluster's schema without applying them:

```bash
helm template apptemplate deploy/helm/app-template \
  -f deploy/helm/app-template/values-dev.yaml | kubectl apply --dry-run=server -f -
```

## Post-deploy smoke (whichever path you used)

```bash
BASE=https://<your-host>/<pathPrefix>

curl -s -o /dev/null -w "%{http_code}\n" "$BASE/api-main/health"   # Expect 200
curl -s -o /dev/null -w "%{http_code}\n" "$BASE/api-auth/health"   # Expect 200
curl -s -o /dev/null -w "%{http_code}\n" "$BASE/"                  # Expect 200 (SPA shell)

# An unauthenticated API call must still be rejected
curl -s -o /dev/null -w "%{http_code}\n" "$BASE/api-main/api/AccessControl/GetCurrentAccessProfile"
# Expect: 401
```
