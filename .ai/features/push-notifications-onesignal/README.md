# Push Notifications (OneSignal)

> **Status:** `optional-core`
> **Removable in derived repos:** **yes** — projects without push requirements can remove via a future task
> **Required by:** any service / job that wants to send a browser / mobile push to a user

The push feature wraps OneSignal's Web Push SDK (FE) and REST API (BE):

- **FE** — `oneSignalService.ts` lazily loads `OneSignalSDK.page.js` on app boot when `FRONTEND_CONSTANTS.oneSignal.appId` is supplied through runtime config, defers the SDK init via `window.OneSignalDeferred`, and exposes two helpers: `setOneSignalExternalUserId(userId)` after login (called by `useAuth`-adjacent code) and `removeOneSignalExternalUserId()` on logout (called by `useAuth.logout`).
- **BE** — `OneSignalPushNotificationService` implements `IPushNotificationService` with three send paths: `SendToUsersAsync(externalUserIds)`, `SendToAllAsync()`, `SendToSegmentAsync(segment)`. It hits `https://api.onesignal.com/notifications` with `Authorization: Key <RestApiKey>` and an `app_id`-keyed payload.

The `IPushNotificationService.IsConfigured` short-circuit (in the BE service) returns false when `AppId` or `RestApiKey` is missing — so the service is safe to register unconditionally; calls become no-ops when not configured.

## Quick links

- [`files.md`](./files.md) — every file owned and touched by this feature
- [`do-dont.md`](./do-dont.md) — feature-specific rules
- [`customize.md`](./customize.md) — segments, deep-link payloads, web-only vs hybrid
- [`verify.md`](./verify.md) — manual subscribe + send smoke

## Architectural shape

```mermaid
flowchart LR
  Boot["main.ts → initOneSignal()"] -->|runtime oneSignalAppId set?| LoadSdk[Load OneSignalSDK.page.js]
  LoadSdk --> Init["OneSignal.init({ appId })"]
  Login["useAuth onLogin"] -->|setOneSignalExternalUserId(userId)| Init
  Logout["useAuth.logout"] -->|removeOneSignalExternalUserId| Init
  Init -->|prompts user| Browser[(browser push subscription)]
  Browser -->|registers external_id mapping| OneSignal[(OneSignal cloud)]
  Svc["BE: IPushNotificationService<br/>OneSignalPushNotificationService"] -->|POST notifications<br/>include_aliases.external_id| OneSignal
  OneSignal -->|push| Browser
```

## Key entry points

| Layer             | Path                                                                                           | Purpose                                                                                                                                  |
| ----------------- | ---------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| BE service        | `src/backend/Libraries/Services/Services/PushNotification/OneSignalPushNotificationService.cs` | `SendToUsersAsync`, `SendToAllAsync`, `SendToSegmentAsync`. Soft-fail when not configured (`IsConfigured` checks `AppId` + `RestApiKey`) |
| BE interface      | `src/backend/Libraries/Services/Services/PushNotification/IPushNotificationService.cs`         | Contract                                                                                                                                 |
| BE settings       | `src/backend/Libraries/Shared/Models/OneSignalSettings.cs`                                     | `AppId` and `RestApiKey`                                                                                                                 |
| BE DI             | `src/backend/API/Program.cs` lines 105-106                                                     | `Configure<OneSignalSettings>` and `AddHttpClient<IPushNotificationService, OneSignalPushNotificationService>()`                         |
| FE service        | `src/frontend/main/src/services/oneSignalService.ts`                                           | `initOneSignal()`, `setOneSignalExternalUserId(userId)`, `removeOneSignalExternalUserId()`                                               |
| FE config         | `window.__APP_TEMPLATE_CONFIG__.oneSignalAppId` or `<meta name="app:oneSignalAppId">`          | Without this the FE service is a no-op                                                                                                   |
| FE hook into auth | `src/frontend/main/src/composables/useAuth.ts` (line 53)                                       | `removeOneSignalExternalUserId()` is invoked on logout                                                                                   |
