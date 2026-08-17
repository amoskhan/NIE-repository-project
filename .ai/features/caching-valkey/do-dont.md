# Caching (Valkey) — Do and Don't

## DO ✅

1. **DO** add new keys with a clear `prefix:identifier` pattern. The shipped prefixes (`session:` and `user_access_functions_`) are listed in the key registry in `files.md` — add a row there for every prefix you introduce.
2. **DO** use `IDistributedCache` for typed get/set with TTL. Use `IConnectionMultiplexer` only when you need pub/sub, atomic increments, sets, or other Redis-specific commands.
3. **DO** set an `AbsoluteExpirationRelativeToNow` on every `SetStringAsync` / `SetAsync`. Valkey is memory-bound; entries without TTLs accumulate and never expire.
4. **DO** keep `AbortOnConnectFail = false`. Valkey hiccups should not crash the process — the `IDistributedCache` returns null and the caller can degrade gracefully.
5. **DO** point Auth and Main at the SAME Valkey instance + the SAME `InstanceName` prefix in production. The session contract relies on Auth writing and Main reading the same key.
6. **DO** set `Valkey:InstanceName` (e.g. `myapp:`) so that several apps sharing one Valkey instance don't collide. The cache library prepends this prefix to every key automatically — handy when a whole class shares one cache server.
7. **DO** check `await _cache.GetStringAsync(key)` for null/empty before deserializing. Stale callers that assume hits cause null-deref bugs.
8. **DO** invalidate explicitly when source-of-truth data changes. The `user_access_functions_{userId}` cache is invalidated when the user's roles or a role's access functions change — see `AccessControlController`.
9. **DO** cache only what's expensive AND read-hot. Caching every DB query reverses cause and effect — use Valkey for cross-request, cross-instance state and recompute results that are already cheap.
10. **DO** include the Valkey health check in `/health`. The `AddRedis(..., name: "valkey")` call surfaces Valkey outages as a 503 that a load balancer or Kubernetes probe can act on.

## DON'T ❌

1. **DON'T** write to `session:{token}` from anywhere except `AuthSessionService.IssueSessionAsync`. Spreading writes across the codebase fragments the contract; any future change (adding a field, changing TTL) must touch all writers.
2. **DON'T** use Valkey as a database. Treat every entry as ephemeral. If you need durability, write to PostgreSQL and cache the read.
3. **DON'T** put PII or large blobs in Valkey. Key sizes balloon in production; budget the working set so it fits in RAM with headroom.
4. **DON'T** cache without a TTL. Even "static" data deserves an expiration so a misbehaving writer can self-heal in N minutes instead of forever.
5. **DON'T** assume cache writes are atomic across N replicas. Use `IDatabase.StringSetAsync(..., when: When.NotExists)` with a TTL for distributed locks; do NOT rely on `IDistributedCache` for that pattern.
6. **DON'T** disable the Valkey health check on `/health`. A silent Valkey outage breaks login (no session writes) and authorization (no cached codes) — the load balancer should pull the instance.
7. **DON'T** mix `RedisDatabase` numbers across features. Both apps default to DB 0; using different numbers fragments the keyspace and hides the fact that two features actually share state.
8. **DON'T** rely on Valkey pub/sub for critical events. Use TickerQ + DB rows + outbound events for "must deliver" workflows; pub/sub is best-effort.
9. **DON'T** put session expiry logic in two places. The Auth API writes with TTL; the Main middleware re-checks `lastActiveSgt + ValidSessionTimeInMins` for sliding-window. Both must read the same TTL config (`ValidSessionTimeInMins`).
10. **DON'T** use `ConnectionMultiplexer.Connect(...)` per request — the singleton registration in `Program.cs` is correct. The multiplexer is thread-safe and meant to be long-lived.
