# Feedback Widget - Customize

## 1. Replace the procurement namespace with your project namespace

Pick a short slug, for example `myapp`, `studyplanner`, or `teamtracker`. This is
the key everything aggregates on, so do this early — renaming it later orphans
the submissions you have already collected.

Do not edit `StaffLayout.vue` for this. The layout is template-owned shell
infrastructure. If the namespace still lives in the layout, first create an
explicit template task to move it into feedback-owned config or a feedback
composable, then change that feature-owned code.

1. Keep the layout mount unchanged.
2. Put the namespace in feedback-owned config/composable code.
3. Edit `src/frontend/main/src/components/feedback/FloatingFeedbackButton.vue`:
   ```ts
   const FEEDBACK_STORAGE_PREFIX = "myapp.feedback.submittedAt.";
   ```
4. Existing entries in `localStorage` under the previous prefix become stale.
   Either accept that the widget will reappear on every page once after the
   rename, or add a one-time migration in `FloatingFeedbackButton.vue.onMounted`
   that copies previous keys to the project namespace.

## 2. Move the widget to the bottom-left

Do not edit `StaffLayout.vue` for placement changes. Add a feedback-owned
runtime/config value and let `FloatingFeedbackButton.vue` choose its placement
from that value.

## 3. Disable the widget for a specific deployment

1. Add a public runtime config key before the frontend bundle loads:
   ```html
   <script>
     window.__APP_TEMPLATE_CONFIG__ = {
       ...(window.__APP_TEMPLATE_CONFIG__ ?? {}),
       feedbackEnabled: false,
     };
   </script>
   ```
2. Add `feedbackEnabled?: boolean` to `AppTemplateRuntimeConfig` in
   `src/frontend/packages/shared/src/config/constants.ts`, expose it under
   `FRONTEND_CONSTANTS.features`, and default it to `true`.
3. Gate rendering inside `FloatingFeedbackButton.vue`, not in `StaffLayout.vue`.
4. Default the flag to `true` so a deployment that sets nothing keeps the widget.

## 4. Replace the question text per page

The component accepts a `questionText` prop:

```html
<FloatingFeedbackButton
  :function-id="feedbackFunctionId"
  question-text="How was the approval flow?"
/>
```

Do not pass this prop by editing `StaffLayout.vue`. Use feedback-owned config or
an inline `AppFeedbackHub` on a feature page.

## 5. Lengthen or shorten the 24-hour hide

1. Edit `FloatingFeedbackButton.vue`:
   ```ts
   const FEEDBACK_HIDE_TTL_MS = 7 * 24 * 60 * 60 * 1000; // 7 days
   ```
2. Restart the dev server. Existing localStorage entries continue to use their
   stored timestamp; only the comparison TTL changed.

## 6. Implement the `/api/Feedback/Submit` receiver (required)

The widget posts to `/api/Feedback/Submit` (relative). **The template does not
ship a controller for this route** — there is no `FeedbackController.cs`. Until
you add one, every submission fails. Pick one of the two shapes below.

### Option A — store it yourself (recommended)

The straightforward choice: you can read your own feedback with a SQL query.

1. Add an entity `Feedback` with `FunctionId`, `Rating`, `Comment`, `PageUrl`,
   `SubmittedBy`, `SubmittedOn`, and a migration for it.
2. Create `src/backend/API/Controllers/FeedbackController.cs` with a single
   `Submit` action that receives `{ function_id, rating, feedback, page }`,
   stamps `IUserContextService.UserId`, inserts the row, and returns
   `{ acknowledged: true }` to match `FeedbackSubmitResponse`.
3. Leave it under the default session-validation gate so submissions are
   attributable — `[AllowAnonymous]` would throw away the attribution the widget
   exists to provide.
4. Do not add a `[RequireAccessFunction]`. Every signed-in user should be able to
   submit.
5. Validate `rating` against the two accepted values and cap `feedback` length
   server-side. It is a free-text field reachable by every user.

### Option B — forward to an external collector

If you already run a feedback service, make the controller a thin proxy: same
action, but it posts to the collector via `IHttpClientFactory` instead of writing
a row. Keep the outbound URL in configuration, and put the collector's host on
the SSRF outbound allowlist rather than letting the URL come from the request.

Either way, audit-log it if you want a trail:
`IAuditLogger.LogAsync(EAuditAction.SystemEvent, EAuditCategory.System, "Feedback", function_id)`.

## 7. Embed the inline `AppFeedbackHub` on a specific page

```vue
<script setup lang="ts">
import { AppFeedbackHub } from "@apptemplate/ui";
import feedbackService from "@/services/feedbackService";

async function onSubmit(payload: { rating: "1" | "5"; feedback: string }) {
  await feedbackService.submit({
    function_id: "myapp.reports.inline",
    rating: payload.rating,
    feedback: payload.feedback,
    page: window.location.href,
  });
}
</script>

<template>
  <AppFeedbackHub @submit="onSubmit" />
</template>
```

The inline hub does not carry the 24-hour hide logic; that lives in
`FloatingFeedbackButton`. Add feature-local state if you want a thanks message
after submit.
