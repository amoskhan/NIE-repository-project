# Push Notifications (OneSignal) — Do and Don't

## DO ✅

1. **DO** treat OneSignal as best-effort. The BE service `IsConfigured` check returns false when keys are missing and the call is a silent no-op — by design. Push is supplementary; never make a workflow depend on push delivery.
2. **DO** call `setOneSignalExternalUserId(userId)` immediately after a successful login, BEFORE any push-eligible event. The `external_id` mapping is what `SendToUsersAsync(include_aliases.external_id)` looks up.
3. **DO** call `removeOneSignalExternalUserId()` on every logout path. `useAuth.logout()` already does this; ensure any alternative logout (e.g. session-expired auto-redirect) also clears it.
4. **DO** initialize the SDK exactly once via `initOneSignal()` in `main.ts`. The function guards against double-init via the `initialized` flag — but accidental re-runs in HMR can cause SDK quirks; prefer a single call site.
5. **DO** use `SendToUsersAsync(externalUserIds: ["alice", "shaina"])` for targeted delivery. The OneSignal payload key is `include_aliases.external_id`; the service constructs this for you.
6. **DO** include a `url` in the payload when the push should deep-link into the app (e.g. `/approvals/123`). The OneSignal click handler navigates to this URL.
7. **DO** include a `data` dictionary for in-app handling — the SDK exposes it on the `notificationClicked` event so the FE can react with custom logic instead of a hard navigation.
8. **DO** soft-fail in callers — the BE service's catch-all logs a warning but does NOT throw. Wrap your call in a try/catch only if you have a meaningful fallback action.
9. **DO** audit pushes you actually sent — `IAuditLogger.LogAsync(EAuditAction.SystemEvent, EAuditCategory.System, "PushNotification", entityId, additionalData: ...)` is the typical pattern. The shipped service does NOT audit by itself.
10. **DO** keep `RestApiKey` in environment variables / secrets, NEVER in source. The key has full send capability for your OneSignal app.

## DON'T ❌

1. **DON'T** rely on push for authentication, password reset, or any "must arrive" flow. Push is rate-limited, OS-throttled, and skippable by the user. Use email + in-app notification for those.
2. **DON'T** call `SendToAllAsync` casually — it broadcasts to every subscribed device under your OneSignal app. Include a confirm step in admin UI before invoking.
3. **DON'T** put PII in `title` or `message`. Push payloads land on lock screens; treat them as semi-public.
4. **DON'T** include sensitive identifiers in `data`. The SDK persists the data in the browser's notification log; assume it's recoverable.
5. **DON'T** set the `external_id` to a session token or any rotating value. Use the stable staff `userId`.
6. **DON'T** remove `removeOneSignalExternalUserId()` from `useAuth.logout()` "to keep notifications working after logout" — this is a privacy violation. The login on a different account on the same device must not receive the previous user's pushes.
7. **DON'T** require `oneSignalAppId` in local development. The FE service early-returns when runtime config is missing; the BE service is also a no-op. This is intentional so a developer without OneSignal access can still run the stack.
8. **DON'T** load the OneSignal SDK script in the auth FE (`src/frontend/auth/`). Push subscription belongs after login on the staff FE — pre-login the user has no `external_id` yet.
9. **DON'T** assume the user is subscribed. The OneSignal SDK shows a browser permission prompt; the user can deny. The API still accepts `SendToUsersAsync` calls but OneSignal silently filters undelivered devices.
10. **DON'T** swap to OneSignal v15 or earlier without checking the SDK loader URL. The current code targets v16 (`OneSignalSDK.page.js`); older versions used `OneSignalSDK.js`.
