# Push Notifications (OneSignal) — Verify

This file does NOT include a real OneSignal App ID or REST API Key.

## Backend boot (no credentials)

```bash
dotnet build src/backend/AppTemplate.sln
dotnet run --project src/backend/API
```

With empty `OneSignal:AppId` / `OneSignal:RestApiKey`, the service registers but `IsConfigured()` returns false. Calls become no-ops.

## API smoke

There is no controller for push notifications by default — sends are triggered from BE services. To smoke test directly, add a temporary dev-only endpoint:

```csharp
app.MapGet("/dev/test-push", async (IPushNotificationService push) =>
{
    await push.SendToUsersAsync(
        externalUserIds: new[] { "alice" },
        title: "Smoke",
        message: "Hello from the smoke test.",
        url: "http://localhost:8002/");
    return Results.Ok();
}).RequireHost("localhost"); // dev only
```

```bash
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"alice"}' | jq -r .sessionToken)

curl -s http://localhost:5002/dev/test-push -H "X-Session-Id: $SESSION"
```

Without configured keys, the BE logs:

```
OneSignal is not configured (AppId or RestApiKey missing). Skipping push notification.
```

With configured keys, the BE logs the OneSignal API response status; the recipient device (if subscribed) receives the push.

Remove the dev endpoint after verification.

## FE subscription smoke

1. Set `oneSignalAppId` to a valid app ID through `window.__APP_TEMPLATE_CONFIG__` or `<meta name="app:oneSignalAppId">`.
2. Drop `OneSignalSDKWorker.js` into `src/frontend/main/public/`.
3. Restart the FE dev server. Open `http://localhost:8002` in Chrome.
4. Login. After login, confirm:
   - The browser shows a "Allow notifications" prompt (first time only).
   - DevTools → Application → Service Workers → `OneSignalSDKWorker.js` is registered.
   - DevTools → Network → a request to `https://onesignal.com/api/v1/players` registers the device.
5. Open the OneSignal console → Audience → confirm a new player appears with the `external_id = "alice"`.

## Logout cleanup

1. While logged in, click logout. The FE calls `removeOneSignalExternalUserId()` (via `useAuth.logout`) which translates to `oneSignal.logout()`.
2. In the OneSignal console, confirm the device's `external_id` is cleared (the device subscription remains, but is unmapped from any user).

## Negative path

```bash
# Stop OneSignal API access (e.g. invalid REST key)
# Edit src/backend/API/appsettings.json with a fake key
# OneSignal: { "AppId": "real", "RestApiKey": "fake" }

# Trigger a send. The BE logs:
#   "OneSignal API returned 401: <body>"
# The call does NOT throw (the service catches all exceptions in SendNotificationAsync).
```

## Permissions / payload

OneSignal payloads support up to 256 chars in `headings` and 1024 in `contents`. Long messages are truncated by browsers. Verify your title fits.

## Audit verification

If you wired the `IAuditLogger` hook (per `customize.md` § 8):

```sql
SELECT "EntityName", "NewValues", "Outcome"
FROM "AuditLogs"
WHERE "EntityName" = 'PushNotification'
ORDER BY "Timestamp" DESC LIMIT 5;
```

## Frontend check (no real config)

Without runtime `oneSignalAppId`:

```js
// In DevTools console after login
window.OneSignalDeferred;
// Expect: undefined or never set (the service early-returns)
```

Confirms the service truly no-ops when not configured.
