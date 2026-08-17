# Caching (Valkey) — Customize

## 1. Run Valkey locally

```bash
docker run -d --name apptemplate-valkey -p 6379:6379 valkey/valkey:latest
```

Default config in `src/backend/API/appsettings.json` (the template ships no `appsettings.Development.json` — create one yourself if you want a dev-only override):

```json
"Valkey": {
  "ConnectionString": "localhost:6379",
  "InstanceName": "AppTemplate-auth-session-instance"
}
```

## 2. Use a managed Valkey / Redis

Edit env-specific `appsettings.{Env}.json` (or env vars):

```json
"Valkey": {
  "ConnectionString": "your-host.amazonaws.com:6379,password=...,ssl=true",
  "InstanceName": "myapp:"
}
```

`ConfigurationOptions.AbortOnConnectFail = false` is preset by the bootstrap code so transient cluster handshakes don't crash the app.

## 3. Add a new cache prefix for a feature

Pick a prefix like `report:cache:`. In your service:

```csharp
public class ReportCacheService
{
    private const string Prefix = "report:cache:";
    private readonly IDistributedCache _cache;
    public ReportCacheService(IDistributedCache cache) { _cache = cache; }

    public async Task<MonthlyReport?> GetAsync(int month, int year)
    {
        var key = $"{Prefix}{year}:{month}";
        var json = await _cache.GetStringAsync(key);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<MonthlyReport>(json);
    }

    public async Task SetAsync(int month, int year, MonthlyReport report)
    {
        var key = $"{Prefix}{year}:{month}";
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(report), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        });
    }
}
```

Document the new prefix in `files.md` § "Key prefix registry".

## 4. Use direct Redis commands (sets, sorted sets, pub/sub)

Inject `IConnectionMultiplexer`:

```csharp
public class ActiveUsersService
{
    private readonly IDatabase _db;
    public ActiveUsersService(IConnectionMultiplexer mux) { _db = mux.GetDatabase(); }

    public Task TouchAsync(string userId) =>
        _db.SortedSetAddAsync("active_users", userId, DateTimeHelper.Now.Ticks);

    public async Task<List<string>> GetActiveLastNMinutesAsync(int minutes)
    {
        var floor = DateTimeHelper.Now.AddMinutes(-minutes).Ticks;
        var raw = await _db.SortedSetRangeByScoreAsync("active_users", min: floor);
        return raw.Select(x => x.ToString()).ToList();
    }
}
```

Use `_db.KeyExpireAsync` to add TTLs to keys created via direct commands (sorted sets do not auto-expire individual members).

## 5. Distributed lock for "run-once" jobs

```csharp
var locked = await _db.StringSetAsync(
    "lock:report-rebuild",
    "1",
    expiry: TimeSpan.FromMinutes(5),
    when: When.NotExists);

if (!locked) return; // someone else owns the lock

try { await DoExpensiveWorkAsync(); }
finally { await _db.KeyDeleteAsync("lock:report-rebuild"); }
```

Use sparingly; for scheduled jobs prefer TickerQ which handles distributed scheduling itself.

## 6. Switch the cache namespace per environment

Setting `Valkey:InstanceName = "myapp-staging:"` for staging and `"myapp-production:"` for production prevents cross-env collisions when multiple environments share a Valkey instance. The library prepends this prefix automatically.

## 7. Tune the connection options

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var options = ConfigurationOptions.Parse(builder.Configuration["Valkey:ConnectionString"]!);
    options.AbortOnConnectFail = false;
    options.ConnectRetry = 5;
    options.ConnectTimeout = 5000;
    options.SyncTimeout = 5000;
    options.ResponseTimeout = 5000;
    options.KeepAlive = 30;
    return ConnectionMultiplexer.Connect(options);
});
```

## 8. Inspect cache contents in dev

```bash
# Connect with redis-cli
redis-cli -p 6379

# List all session keys (be careful in production — KEYS is O(N))
> KEYS "apptemplate:session:*"

# Inspect a session
> GET "apptemplate:session:abc123..."

# Check TTL
> TTL "apptemplate:session:abc123..."

# Bulk delete a feature's keys
> EVAL "for _, k in ipairs(redis.call('KEYS', ARGV[1])) do redis.call('DEL', k) end" 0 "apptemplate:user_access_functions_*"
```

## 9. Watch keyspace events (debugging stale entries)

```bash
> CONFIG SET notify-keyspace-events "KEA"
> PSUBSCRIBE "__keyevent@0__:*"
# Watch which keys are written / expired during a flow
```

Re-disable the events when done; they add CPU overhead.
