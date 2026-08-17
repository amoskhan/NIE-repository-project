# Push Notifications (OneSignal) — Customize

## 1. Onboard OneSignal for the first time

1. Create a OneSignal app at https://app.onesignal.com → Web Push → register your origin (e.g. `https://app.example.com`).
2. Copy the App ID and the REST API Key from the OneSignal console.
3. Download the OneSignal service worker file (`OneSignalSDKWorker.js`) and place it under `src/frontend/main/public/` so it is served at `/OneSignalSDKWorker.js` after build.
4. Add the public OneSignal App ID to runtime frontend config:
   ```html
   <meta
     name="app:oneSignalAppId"
     content="00000000-0000-0000-0000-000000000000"
   />
   ```
   Or inject it before the frontend bundle loads:
   ```html
   <script>
     window.__APP_TEMPLATE_CONFIG__ = {
       ...(window.__APP_TEMPLATE_CONFIG__ ?? {}),
       oneSignalAppId: "00000000-0000-0000-0000-000000000000",
     };
   </script>
   ```
5. Edit `src/backend/API/appsettings.json` (or env-specific):
   ```json
   "OneSignal": {
     "AppId": "00000000-0000-0000-0000-000000000000",
     "RestApiKey": "<keep in secrets, NOT here>"
   }
   ```
6. Restart both apps. On the staff FE, after login, the browser should show a OneSignal subscription prompt.

## 2. Send a targeted push from a service

```csharp
public class ApprovalService
{
    private readonly IPushNotificationService _push;
    public ApprovalService(IPushNotificationService push) { _push = push; }

    public async Task NotifyApprovalAsync(string approverUserId, int orderId)
    {
        await _push.SendToUsersAsync(
            externalUserIds: new[] { approverUserId },
            title: "Approval Required",
            message: $"Order #{orderId} is awaiting your decision.",
            url: $"https://app.example.com/approvals/{orderId}",
            data: new Dictionary<string, string> { ["orderId"] = orderId.ToString() });
    }
}
```

The service is registered as `IPushNotificationService`; inject it like any other dependency.

## 3. Send to a OneSignal segment

OneSignal segments are defined in the OneSignal console (e.g. "Subscribed Users", "Active Last 7 Days"). To send:

```csharp
await _push.SendToSegmentAsync(
    segment: "Subscribed Users",
    title: "Maintenance window",
    message: "The portal will be down for 30 minutes at 11 PM tonight.");
```

## 4. Send to ALL subscribed devices (broadcast)

```csharp
await _push.SendToAllAsync(
    title: "System Notice",
    message: "Please refresh your browser to apply the latest update.");
```

Wire this behind an admin-only endpoint with a confirm step. Audit-log every call.

## 5. Disable push for a specific environment without removing code

In `appsettings.{Env}.json`:

```json
"OneSignal": { "AppId": "", "RestApiKey": "" }
```

Empty strings cause `IsConfigured()` to return false, the BE service silently no-ops, and the FE service no-ops because runtime `oneSignalAppId` is unset.

## 6. Add deep-link handling on the FE

OneSignal's default behavior on click is `window.open(url)`. To intercept:

1. Edit `oneSignalService.ts` — add a click listener inside `OneSignalDeferred.push`:
   ```ts
   oneSignal.Notifications.addEventListener("click", (event) => {
     const data = event.notification?.additionalData as
       Record<string, string> | undefined;
     if (data?.orderId) {
       window.location.href = `/orders/${data.orderId}`;
       event.preventDefault?.();
     }
   });
   ```
2. The `data` dictionary populated by `SendToUsersAsync(data: ...)` arrives here.

## 7. Replace OneSignal with a different push provider (e.g. Firebase Cloud Messaging)

1. Create `FirebasePushNotificationService` implementing `IPushNotificationService`. Use the FCM HTTP v1 API.
2. Replace the `AddHttpClient<IPushNotificationService, OneSignalPushNotificationService>()` line in `Program.cs:106` with the new service.
3. Replace `oneSignalService.ts` with `firebaseMessagingService.ts` (FCM has its own JS SDK).
4. Replace the `removeOneSignalExternalUserId()` call in `useAuth.ts:53` with the FCM equivalent.
5. The interface contract is preserved — caller services that inject `IPushNotificationService` work unchanged.

## 8. Add an `IAuditLogger` hook for every push

Edit `OneSignalPushNotificationService.SendNotificationAsync`:

```csharp
private async Task SendNotificationAsync(Dictionary<string, object> payload)
{
    using var scope = _scopeFactory.CreateScope(); // requires IServiceScopeFactory in ctor
    var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();

    try
    {
        // ... existing code ...
        await auditLogger.LogAsync(
            EAuditAction.SystemEvent, EAuditCategory.System,
            "PushNotification", null,
            newValues: JsonSerializer.Serialize(new { title, recipients }));
    }
    catch (Exception ex)
    {
        await auditLogger.LogAsync(
            EAuditAction.SystemEvent, EAuditCategory.System,
            "PushNotification", null,
            outcome: "Failed",
            additionalData: ex.Message);
    }
}
```

Note: `OneSignalPushNotificationService` is currently typed-`HttpClient` only — adding `IServiceScopeFactory` is a small constructor change.
