# Push Notifications (OneSignal) — File Map

## Owned files

### Backend

| Path                                                                                           | Layer        | Purpose                                                                                                                                                                                                                                                                          |
| ---------------------------------------------------------------------------------------------- | ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Services/Services/PushNotification/IPushNotificationService.cs`         | Interface    | `SendToUsersAsync(externalUserIds, title, message, url?, data?)`, `SendToAllAsync(...)`, `SendToSegmentAsync(segment, ...)`                                                                                                                                                      |
| `src/backend/Libraries/Services/Services/PushNotification/OneSignalPushNotificationService.cs` | Service      | OneSignal REST implementation. POSTs to `https://api.onesignal.com/notifications` with `Authorization: Key <RestApiKey>`. `IsConfigured()` short-circuits when `AppId` or `RestApiKey` is empty. Builds payload with `app_id`, `headings`, `contents`, optional `url` and `data` |
| `src/backend/Libraries/Shared/Models/OneSignalSettings.cs`                                     | Options POCO | Two strings: `AppId`, `RestApiKey`                                                                                                                                                                                                                                               |

### Frontend

| Path                                                 | Layer   | Purpose                                                                                                                                                                                                                                                                      |
| ---------------------------------------------------- | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/main/src/services/oneSignalService.ts` | Service | Lazy SDK loader: pushes config into `window.OneSignalDeferred`, injects `OneSignalSDK.page.js` script tag. Exposes `initOneSignal`, `setOneSignalExternalUserId`, `removeOneSignalExternalUserId`. All three are no-ops when `FRONTEND_CONSTANTS.oneSignal.appId` is not set |

## Touched files

| Path                                                                    | What it contains                                                                                                                                                                          | Why must be touched                                                                                                                           |
| ----------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/API/Program.cs` lines 105-106                              | `builder.Services.Configure<OneSignalSettings>(configuration.GetSection("OneSignal"))` and `builder.Services.AddHttpClient<IPushNotificationService, OneSignalPushNotificationService>()` | DI wiring                                                                                                                                     |
| `src/backend/API/appsettings.json`                                      | `"OneSignal": { "AppId": "", "RestApiKey": "" }`                                                                                                                                          | Empty in template, populated per environment                                                                                                  |
| `src/frontend/main/src/main.ts`                                         | `initOneSignal()` call on app boot                                                                                                                                                        | Required so the SDK loads (the call is a no-op when runtime config is missing)                                                                |
| `src/frontend/main/src/composables/useAuth.ts` line 53                  | `removeOneSignalExternalUserId()` inside `logout()`                                                                                                                                       | Cleans the user/device mapping on logout                                                                                                      |
| Login success handler (e.g. `LoginPage.vue` or post-redirect bootstrap) | `setOneSignalExternalUserId(userId)` after successful login                                                                                                                               | Links the OneSignal device record to the staff user id; required for `SendToUsersAsync` to find the recipient                                 |
| `src/frontend/packages/shared/src/config/constants.ts`                  | Reads `oneSignalAppId` from `window.__APP_TEMPLATE_CONFIG__` or `<meta name="app:oneSignalAppId">`                                                                                        | Required to enable push without rebuilding the frontend                                                                                       |
| `public/OneSignalSDKWorker.js`                                          | The OneSignal service worker file                                                                                                                                                         | Must be served at site root for the SDK to register the worker. Place under `src/frontend/main/public/` so Vite copies it to the build output |

## Migrations

None — push state lives in OneSignal cloud, not in our DB.

## External dependencies

| Side | Package                                 | Purpose                                    |
| ---- | --------------------------------------- | ------------------------------------------ |
| BE   | (uses `HttpClient` only)                | No package — pure REST                     |
| FE   | (loads `OneSignalSDK.page.js` from CDN) | No npm dep — script is injected at runtime |

## Removal note

This feature is `optional-core`. A removal task should:

1. Delete `OneSignalPushNotificationService.cs`, `IPushNotificationService.cs`, `OneSignalSettings.cs`.
2. Delete `oneSignalService.ts`.
3. Remove the `OneSignal` section from `appsettings.json`.
4. Remove the `Configure<OneSignalSettings>` + `AddHttpClient<IPushNotificationService, ...>` lines from `Program.cs`.
5. Remove the `initOneSignal()` call from `main.ts`.
6. Remove the `removeOneSignalExternalUserId` call from `useAuth.ts` (line 53) — which currently imports from `oneSignalService.ts`.
7. Remove `oneSignalAppId` from runtime config and deployment meta tags.
8. Delete `public/OneSignalSDKWorker.js`.
