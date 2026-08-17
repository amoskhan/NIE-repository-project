# Feedback Widget — File Map

## Owned files

| Path                                                                                | Layer        | Purpose                                                                                                                                                                                                                       |
| ----------------------------------------------------------------------------------- | ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/main/src/components/feedback/FloatingFeedbackButton.vue`              | Page widget  | The mounted floating button. Owns popover open/close, rating selection (`FeedbackRating` from the service), submit lifecycle (`isSubmitting`, `submitError`), success toast, and the 24-hour `localStorage`-based hide window |
| `src/frontend/main/src/services/feedbackService.ts`                                 | Service      | Typed axios client. Exports `FeedbackRating = "1" \| "5"`, `FeedbackSubmitRequest`, `FeedbackSubmitResponse`, and a single `submit` function calling `POST /api/Feedback/Submit`                                              |
| `src/frontend/packages/ui/src/components/composite/app-feedback/AppFeedbackHub.vue` | UI primitive | The reusable visual: rating + textarea + submit button. Used by `FloatingFeedbackButton.vue` so the same look-and-feel can be embedded inline elsewhere if needed                                                             |
| `src/frontend/packages/ui/src/components/composite/app-feedback/index.ts`           | Barrel       | Re-exports `AppFeedbackHub` from the `@apptemplate/ui` package so consumers can `import { AppFeedbackHub } from "@apptemplate/ui"`                                                                                            |

## Touched files

| Path                                                  | What it contains                                                                                                                            | Why must be touched                                                                                                                                                                                                                                                                                                                                                       |
| ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/main/src/staff/layouts/StaffLayout.vue` | Imports `FloatingFeedbackButton`, defines `feedbackFunctionId`, renders `<FloatingFeedbackButton :function-id="feedbackFunctionId" />` once | Template-owned shell mount. Do not edit for feature work; move feedback-specific knobs into feedback-owned config/components first                                                                                                                                                                                                                                        |
| `src/frontend/packages/ui/src/index.ts`               | Exports `./components/composite/app-feedback` (line 13)                                                                                     | Required to expose `AppFeedbackHub` from the UI library barrel                                                                                                                                                                                                                                                                                                            |
| Backend endpoint `/api/Feedback/Submit`               | **Not shipped — you write it.** There is no `FeedbackController.cs` in the template                                                         | The widget assumes a working endpoint at this path. Add `src/backend/API/Controllers/FeedbackController.cs` that stores submissions in your own table or forwards them to a collector you run. Leave it under the default session-validation gate so submissions are attributable; do not add a `[RequireAccessFunction]` — every signed-in user should be able to submit |

## Known issue — the `function_id` namespace

The `function_id` prefix ships as `procurement.`, inherited from the bundled
procurement reference sample. Rename it to your own project slug.

If the namespace is still hardcoded in `StaffLayout.vue`, do not patch it there
as part of feature work — the layout is template-owned shell. Move the namespace
into feedback-owned config or a feedback composable first, then change that
feature-owned code.

## Migrations

None — the FE widget does not own any backend table.

## External dependencies

| Package                         | Where | Purpose                               |
| ------------------------------- | ----- | ------------------------------------- |
| `vue`                           | FE    | Reactive primitives, lifecycle        |
| `@apptemplate/ui`               | FE    | Source of `AppFeedbackHub` (peer dep) |
| `axios` (via `@/services/api`)  | FE    | HTTP client                           |
| `localStorage` (browser-native) | FE    | 24-hour hide window                   |
