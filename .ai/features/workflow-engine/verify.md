# Workflow Engine — Verify

## Manual Smoke Tests

1. Create a Purchase Order → verify state is "Draft"
2. Click workflow actions → verify only "Submit" and "Cancel" are available
3. Submit → verify transition to "Submitted" with log entry
4. View timeline → verify all states shown with timestamps and remarks
5. Attempt invalid transition via API → verify 400 error returned

## API Smoke

```bash
# Get current state
curl -X GET "http://localhost:5002/api/Workflow/PurchaseOrder/1/state" -H "X-Session-Id: ..."

# Get history
curl -X GET "http://localhost:5002/api/Workflow/PurchaseOrder/1/history" -H "X-Session-Id: ..."

# Transition state
curl -X POST "http://localhost:5002/api/Workflow/PurchaseOrder/1/transition" \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: ..." \
  -d '{"toState":"Submitted","remarks":"Ready for review"}'
```

## DB Verification

```sql
SELECT * FROM "WorkflowTransitions" WHERE "IsActive" = true;
SELECT * FROM "WorkflowStateLogs" WHERE "OwnerType" = 'PurchaseOrder' ORDER BY "TransitionedAt" DESC;
```
