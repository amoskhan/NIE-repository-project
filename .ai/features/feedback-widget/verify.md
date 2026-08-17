# Feedback Widget — Verify

## Frontend (manual click-path)

1. Login as any user (`http://localhost:8001` → submit credentials → land on `:8002`).
2. Confirm the floating button appears in the bottom-right of every authenticated page (Dashboard, Vendors, Catalog, etc.). Open the page, scroll, navigate — the button stays anchored.
3. Click the button. The popover opens with the configured `questionText` (default "How is your experience with this page?"), two rating buttons (thumbs-down `1`, thumbs-up `5`), and a free-text area.
4. Without selecting a rating the Submit button is disabled (`canSubmit = !!selectedRating.value && !isSubmitting`).
5. Click the negative rating, type a comment, click Submit. Network tab should show `POST /api/Feedback/Submit` with body:
   ```json
   {
     "function_id": "<namespace>.<routeName>",
     "rating": "1",
     "feedback": "...",
     "page": "<full url>"
   }
   ```
6. Confirm a success toast appears and the popover closes.
7. Refresh the page — the button is now hidden for THIS `function_id` for 24 hours. Confirm:
   ```js
   localStorage.getItem("<project-slug>.feedback.submittedAt.<routeName>");
   // returns a numeric millisecond timestamp
   ```
8. Navigate to a different route. The button reappears (different `function_id`).
9. Manually clear the localStorage key and refresh — the button reappears.

## Failure path

1. Stop the Main API.
2. Open the popover, pick a rating, submit. Confirm:
   - The toast does NOT appear
   - `submitError` renders inside the popover
   - The popover stays open so the user can retry
3. Restart the API and click Submit again. Confirm success.

## Network contract

```bash
SESSION=$(...) # any authenticated session

curl -s -X POST http://localhost:5002/api/Feedback/Submit \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{
    "function_id": "smoke.test",
    "rating": "5",
    "feedback": "Nice",
    "page": "http://localhost:8002/"
  }' | jq

# Expect: { "acknowledged": true } from the controller you wrote.
# A 404 here means the receiver does not exist yet — the template ships no
# FeedbackController. Implement it per customize.md § 6, or remove the widget.

# Unauthenticated submissions must be rejected (attribution is the point)
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5002/api/Feedback/Submit \
  -H "Content-Type: application/json" \
  -d '{"function_id":"smoke.test","rating":"5","feedback":"x","page":"/"}'
# Expect: 401
```

## Layout integration

```bash
# Confirm exactly one mount point
grep -n "FloatingFeedbackButton" src/frontend/main/src/staff/layouts/*.vue
# Expect: exactly one import line and one render line in StaffLayout.vue

# Confirm no stray mounts on individual pages
grep -rn "FloatingFeedbackButton" src/frontend/main/src/staff/pages/
# Expect: no matches
```

## Namespace check

```bash
# Once you have replaced the procurement reference sample with your own domain,
# the feedback namespace must be your project slug, not "procurement."
grep -n "procurement\." src/frontend/main/src/staff/layouts/StaffLayout.vue
grep -n "procurement\." src/frontend/main/src/components/feedback/FloatingFeedbackButton.vue
# Expect in your project: no matches. Both should reference your own slug.
```

## Accessibility

- Tab to the floating button — confirm it has a focus ring.
- Press Enter to open the popover.
- Tab through the rating buttons — both should be reachable and have a visible focus state.
- Tab into the textarea, type, Tab to Submit, Enter to submit. Whole flow should be keyboard-only operable.
- Close button should be reachable via keyboard (Esc OR a focusable close icon).
