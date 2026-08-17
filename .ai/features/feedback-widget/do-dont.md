# Feedback Widget - Do and Don't

## DO

1. **DO** keep the widget mounted exactly once, inside `StaffLayout.vue`. Adding a second mount on a child page double-renders the floating button.
2. **DO** namespace the `function_id` to `<project-slug>.<routeName>`, using your own slug rather than the `procurement.` prefix inherited from the reference sample. The namespace is the aggregation key, so a stale prefix makes your own data hard to read.
3. **DO** use `route.name`, not `route.path`, for the `<routeName>` segment. Paths can change between deployments; route names are stable in `src/frontend/main/src/router/index.ts`.
4. **DO** keep the rating values as the literal strings `"1"` and `"5"` unless you change the receiver to match. The FE type is `FeedbackRating = "1" | "5"`; both ends must agree.
5. **DO** reuse `AppFeedbackHub` from `@apptemplate/ui` for inline feedback surfaces, for example a "rate this report" panel inside a report page.
6. **DO** keep a hide window of roughly a day (`FEEDBACK_HIDE_TTL_MS = 24 * 60 * 60 * 1000`). Shorten it and the widget nags; lengthen it a lot and you stop hearing about problems.
7. **DO** implement the `/api/Feedback/Submit` receiver before shipping the widget, or remove the widget. A button that silently fails teaches users their feedback is ignored.
8. **DO** call `feedbackService.submit({ function_id, rating, feedback, page })` with `page = window.location.href`.
9. **DO** show a brief toast and close the popover on success. Stay open and surface `submitError` on failure so the user can retry.
10. **DO** clear `reappearTimer` on `onUnmounted` to prevent setTimeout leaks.
11. **DO** test the widget on every route.
12. **DO** treat the existing `StaffLayout.vue` mount as template-owned. Feature changes belong in `FloatingFeedbackButton.vue`, `feedbackService.ts`, feedback-owned config, or page-local inline feedback.

## DON'T

1. **DON'T** half-remove the widget. Either keep it (and implement the receiver) or delete it cleanly — component, service, and layout mount together. Leaving a button wired to a 404 is the worst of both.
2. **DON'T** disable it by commenting out the mount in `StaffLayout.vue`. If you want it off for one deployment, gate it through `FRONTEND_CONSTANTS.features` backed by runtime config so the shell file stays inheritable.
3. **DON'T** put PII or sensitive identifiers in `function_id`. It ends up in whatever store or dashboard aggregates the submissions.
4. **DON'T** submit on every keystroke or rating click. Only the explicit Submit button triggers `feedbackService.submit`.
5. **DON'T** push feedback into the shared session. Sessions are for auth state only.
6. **DON'T** wrap `feedbackService.submit` in a global retry loop.
7. **DON'T** auto-open the popover on page load.
8. **DON'T** bind the widget to a Pinia store. The state is local and short-lived.
9. **DON'T** swallow exceptions from `feedbackService.submit`; the popover renders `submitError`.
10. **DON'T** style the widget with arbitrary Tailwind classes that compete with `AppFeedbackHub`. Override colors only via theme tokens.
11. **DON'T** mount the widget inside the auth layout (`src/frontend/auth/`). Feedback is for signed-in pages, where you know who is talking to you.
12. **DON'T** edit `StaffLayout.vue` to change feedback namespace, placement, feature flags, or question text. Move those knobs into feedback-owned config/components first.
