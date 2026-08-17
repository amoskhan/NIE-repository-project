# TickerQ Background Jobs — Verify

## Backend

```bash
dotnet build src/backend/AppTemplate.sln
dotnet run --project src/backend/API
```

The API logs should include a TickerQ scheduler startup line; if `Sentry:Dsn` is configured you should see `OpenTelemetry` instrumentation as well.

## Dashboard reachability

```bash
# Development (no auth)
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/tickerq/dashboard
# Expect 200

# Confirm the dashboard is NOT session-gated
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/tickerq/dashboard
# Expect 200 even WITHOUT X-Session-Id (the path is in skipPaths)
```

Open `http://localhost:5002/tickerq/dashboard` in a browser. You should see:

- Scheduler status: Running
- Node identifier: `<application>-<environment>-<machine>` per `schedulerOptions.NodeIdentifier`
- A list of registered functions including `AuditLogPurge`

## Production-mode dashboard

```bash
# Set a fake API key locally and restart with ASPNETCORE_ENVIRONMENT=Production to test the gate
ASPNETCORE_ENVIRONMENT=Production \
  TickerQ__DashboardApiKey=test-key-12345 \
  dotnet run --project src/backend/API

# Without the key — should be denied (401 / forbidden)
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/tickerq/dashboard

# With the key — should be allowed
curl -s -o /dev/null -w "%{http_code}\n" \
  -H "Authorization: Bearer test-key-12345" \
  http://localhost:5002/tickerq/dashboard
```

If the API throws `InvalidOperationException: TickerQ:DashboardApiKey configuration is required outside Development.` on boot, the gate is correctly enforced.

## Audit log purge — trigger and observe

```bash
# 1. Open the dashboard at http://localhost:5002/tickerq/dashboard
# 2. Find AuditLogPurge → "Trigger Now"
# 3. Watch the API logs:
#    "Starting audit log purge job. Retention period: 6 months"
#    "Audit log purge completed. Total records deleted: <N>. Cutoff date: <YYYY-MM-DD>"
#
# 4. Confirm the JobExecuted self-audit row:
psql "$DATABASE_URL" -c \
  "SELECT \"EntityId\", \"DurationMs\", \"Outcome\", \"AdditionalData\"
   FROM \"AuditLogs\"
   WHERE \"Action\" = 52 AND \"EntityId\" = 'AuditLogPurge'
   ORDER BY \"Timestamp\" DESC LIMIT 1;"
```

## Cron correctness

```bash
# Show the next 5 fire times for the AuditLogPurge cron in SGT
docker run --rm coreyti/cronvis "0 0 2 * * *"
# Expect 02:00:00 daily

# Confirm timezone — log into psql, look at the TickerQ schedule table
psql "$DATABASE_URL" -c \
  "SELECT \"Function\", \"Expression\", \"NextFire\", \"LastFire\"
   FROM \"CronTickers\"
   WHERE \"Function\" = 'AuditLogPurge';"
```

## Adding a new job — smoke test

```bash
# After adding a new [TickerFunction("MyJob", cronExpression: "*/30 * * * * *")]
# (every 30 seconds, for testing only)

# Confirm it appears in the dashboard
# Confirm the JobExecuted audit row appears every 30 seconds:
psql "$DATABASE_URL" -c \
  "SELECT count(*) FROM \"AuditLogs\"
   WHERE \"Action\" = 52 AND \"EntityId\" = 'MyJob' AND \"Timestamp\" > now() - interval '5 minutes';"
# Expect ~10 over 5 minutes
```

## Frontend

There is no FE for TickerQ; the dashboard is its own SPA mounted at `/tickerq/dashboard`.

## Failure observation

```bash
# Inject a deliberate exception in your test job to confirm retry behavior:
# 1. throw new Exception("test") inside ExecuteAsync
# 2. Trigger from dashboard
# 3. Observe the failure in the dashboard with stack trace
# 4. Confirm a JobExecuted audit row with Outcome = "Failed" or that the row is missing
#    (depending on whether the auditLogger.LogJobExecutedAsync call is in a try/finally)
```
