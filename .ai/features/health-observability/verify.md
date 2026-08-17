# Health & Observability — Verify

## Backend

```bash
dotnet build src/backend/AppTemplate.sln
dotnet run --project src/backend/API
```

## Health endpoints

```bash
# Canonical uptime endpoint for Sentry Uptime, load balancers and the hosted-workspace
# liveness probe. Dependency-free by design — it answers 200 whenever the process is up,
# even with Postgres and Valkey down. Dependency status lives on /health/ready.
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health
# Expect: 200

# Readiness — this is the one that exercises Postgres + Valkey (checks tagged "ready").
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health/ready
# Expect: 200 when both are up, 503 when either is down.

# Inspect the body
curl -s http://localhost:5002/health
# Default body: "Healthy" (or richer JSON if you customized ResponseWriter)

# Liveness — never touches deps
curl -s http://localhost:5002/health/live
# Expect: "ok"

# Readiness. Uses the same HealthChecks pipeline unless a project customizes it.
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health/ready
# Expect: 200
```

## Probe-time independence from session

```bash
# All three endpoints must succeed WITHOUT a session header
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health/ready
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health/live
# Expect: 200 / 200 / 200
```

## Failure path — Postgres down

```bash
# Stop the DB
docker stop apptemplate-postgres   # or your dev DB container

curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health
# Expect: 503 (Service Unavailable)
curl -s http://localhost:5002/health
# Expect: "Unhealthy"

curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health/live
# Expect: 200 (live is independent)

docker start apptemplate-postgres
```

## Failure path — Valkey down

```bash
docker stop apptemplate-valkey
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health
# Expect: 503

docker start apptemplate-valkey
```

## Sentry capture — without DSN

```bash
# With an empty Sentry:Dsn, no Sentry SDK is initialized.
# A forced exception is just logged via ILogger.LogError.
# Confirm by adding a temporary endpoint that throws and observing the logs.
```

## Sentry capture — with DSN (development)

```bash
# Set Sentry:Dsn to a real DSN in src/backend/API/appsettings.json (or the env var below)
ASPNETCORE_ENVIRONMENT=Development \
  Sentry__Dsn="https://...@sentry.io/..." \
  Sentry__Environment="dev-personal" \
  dotnet run --project src/backend/API

# Trigger a deliberate error (e.g. /api/AuditLog/GetAuditLogs?pageSize=foo to cause a binding fault)
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" -d '{"UserId":"alice"}' | jq -r .sessionToken)
curl -s "http://localhost:5002/api/AuditLog/GetAuditLogs?pageSize=foo" -H "X-Session-Id: $SESSION"

# Within ~30 seconds, confirm the exception appears in the Sentry dashboard
# under Environment: dev-personal
```

## OpenTelemetry tracing

```bash
# With Sentry DSN set, every traced request appears under the Sentry → Performance tab.
# Look for spans:
#   GET /api/AuditLog/GetAuditLogs
#   ↳ EF Core: SELECT * FROM "AuditLogs" WHERE ...
#   ↳ HttpClient call (if any)

# Sample rate is TracesSampleRate (default 0.2 = 1 in 5 requests sampled).
# To force sampling for verification, temporarily set TracesSampleRate=1.0.
```

## Correlation ID

```bash
# Send a request with a custom correlation id
curl -s -i http://localhost:5002/health/ready -H "X-Correlation-Id: smoke-12345"
# Expect: response includes X-Correlation-Id: smoke-12345 header

# Send a request without one — server generates one
curl -s -i http://localhost:5002/health/ready
# Expect: response includes X-Correlation-Id: <some uuid>

# Confirm the correlation id appears in audit log entries created during this request
psql "$DATABASE_URL" -c \
  "SELECT \"CorrelationId\", \"RequestUrl\" FROM \"AuditLogs\"
   ORDER BY \"Timestamp\" DESC LIMIT 1;"
# Expect: CorrelationId matches the one returned in the response header
```

## Auth API observability mirror

```bash
# Sentry is also wired in the Auth API
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/openapi/v1.json
# A traced request appears in the Sentry "apptemplate-auth" service (if Sentry:ServiceName is set)
```
