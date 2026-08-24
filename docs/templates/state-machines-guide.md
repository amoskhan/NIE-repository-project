# Guide: Creating State Machine Documentation

> **This is a GUIDE.** Each project creates its own `docs/state-machines.md` when entities have lifecycle states. This document explains HOW to create it.

---

## Purpose

State machine documentation defines valid state transitions for entities that have lifecycle workflows (e.g., Pending → Approved → Completed). AI agents use this to generate validation logic that prevents illegal state transitions.

## When to Create

- When an entity has a `Status` field with multiple values
- When business rules govern transitions between states
- When different user roles can trigger different transitions
- During Phase 1.2 (Technical Design) of AIDLC

## Format

Use **Mermaid.js stateDiagram-v2** syntax. Accompany with transition tables.

## How to Create

### Step 1: Identify Stateful Entities

Look at your requirements and entity model. Any entity with a `Status` or `State` field likely needs a state machine.

Common examples:

- Application/Request workflows (Draft → Submitted → Approved → Rejected)
- Order processing (Created → Processing → Shipped → Delivered)
- Document management (Draft → Review → Published → Archived)
- Task management (Open → In Progress → Done)

### Step 2: Create the State Diagram

```markdown
# State Machines

## Entity: [EntityName] States

\`\`\`mermaid
stateDiagram-v2
[*] --> Draft : Created

    Draft --> Submitted : Submit
    Draft --> Cancelled : Cancel

    Submitted --> UnderReview : Assign Reviewer
    Submitted --> Draft : Return to Draft

    UnderReview --> Approved : Approve
    UnderReview --> Rejected : Reject
    UnderReview --> Submitted : Request Changes

    Approved --> Completed : Complete
    Approved --> Cancelled : Cancel

    Rejected --> Draft : Revise
    Rejected --> Cancelled : Cancel

    Completed --> [*]
    Cancelled --> [*]

\`\`\`
```

### Step 3: Create the Transition Table

```markdown
## Transition Rules

| Current State | Action          | Next State  | Allowed Roles     | Conditions                 | Side Effects                    |
| ------------- | --------------- | ----------- | ----------------- | -------------------------- | ------------------------------- |
| -             | Create          | Draft       | Any authenticated | -                          | Set CreatedBy                   |
| Draft         | Submit          | Submitted   | Owner             | All required fields filled | Set SubmittedAt                 |
| Draft         | Cancel          | Cancelled   | Owner, Admin      | -                          | Set CancelledAt, CancelReason   |
| Submitted     | Assign          | UnderReview | Admin             | Reviewer assigned          | Set ReviewerId                  |
| Submitted     | Return          | Draft       | Admin             | Must provide reason        | Clear SubmittedAt               |
| UnderReview   | Approve         | Approved    | Reviewer          | All criteria met           | Set ApprovedAt, ApprovedBy      |
| UnderReview   | Reject          | Rejected    | Reviewer          | Must provide reason        | Set RejectedAt, RejectionReason |
| UnderReview   | Request Changes | Submitted   | Reviewer          | Must provide feedback      | Add comment                     |
| Approved      | Complete        | Completed   | Admin             | Completion criteria met    | Set CompletedAt                 |
| Approved      | Cancel          | Cancelled   | Admin             | -                          | Set CancelledAt                 |
| Rejected      | Revise          | Draft       | Owner             | -                          | Increment revision number       |
| Rejected      | Cancel          | Cancelled   | Owner, Admin      | -                          | Set CancelledAt                 |
```

### Step 4: Define Code Table Values

Since NIE Template uses Code tables for statuses:

```markdown
## Code Table Configuration

| ECodeType    | ECodeName               | Value        | Sort Order | Notes          |
| ------------ | ----------------------- | ------------ | ---------- | -------------- |
| EntityStatus | EntityStatusDraft       | Draft        | 1          | Initial state  |
| EntityStatus | EntityStatusSubmitted   | Submitted    | 2          |                |
| EntityStatus | EntityStatusUnderReview | Under Review | 3          |                |
| EntityStatus | EntityStatusApproved    | Approved     | 4          |                |
| EntityStatus | EntityStatusRejected    | Rejected     | 5          |                |
| EntityStatus | EntityStatusCompleted   | Completed    | 6          | Terminal state |
| EntityStatus | EntityStatusCancelled   | Cancelled    | 7          | Terminal state |
```

### Step 5: Document the Service-Level Validation

```markdown
## Implementation Pattern

State transitions are enforced in the Service layer:

\`\`\`csharp
// In YourEntityService.cs
public async Task<ApiResponse<YourEntityDto>> TransitionStateAsync(
Guid id, string newStatus, string? reason = null)
{
var entity = await \_dbContext.YourEntities.FindAsync(id);
if (entity == null) return ApiResponse<YourEntityDto>.NotFound();

    // Validate transition
    if (!IsValidTransition(entity.Status, newStatus))
        return ApiResponse<YourEntityDto>.Error(
            $"Cannot transition from {entity.Status} to {newStatus}");

    // Apply transition
    entity.Status = newStatus;
    entity.UpdatedAt = DateTime.UtcNow;
    entity.UpdatedBy = _currentUser;

    // Side effects based on new status
    switch (newStatus)
    {
        case "Submitted":
            entity.SubmittedAt = DateTime.UtcNow;
            break;
        case "Approved":
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedBy = _currentUser;
            break;
        // ... other side effects
    }

    await _dbContext.SaveChangesAsync();
    return ApiResponse<YourEntityDto>.Success(entity.Adapt<YourEntityDto>());

}

private static bool IsValidTransition(string current, string next) =>
ValidTransitions.Contains((current, next));

private static readonly HashSet<(string, string)> ValidTransitions = new()
{
("Draft", "Submitted"),
("Draft", "Cancelled"),
("Submitted", "UnderReview"),
("Submitted", "Draft"),
// ... all valid transitions from the table above
};
\`\`\`
```

### Step 6: Document Frontend State Handling

```markdown
## Frontend State Display

| State        | Badge Color | Available Actions                | Icon |
| ------------ | ----------- | -------------------------------- | ---- |
| Draft        | Gray        | Submit, Cancel, Edit             | ✏️   |
| Submitted    | Blue        | Return (admin), Assign (admin)   | 📤   |
| Under Review | Yellow      | Approve, Reject, Request Changes | 🔍   |
| Approved     | Green       | Complete, Cancel                 | ✅   |
| Rejected     | Red         | Revise, Cancel                   | ❌   |
| Completed    | Dark Green  | None                             | 🏁   |
| Cancelled    | Dark Gray   | None                             | 🚫   |
```

## Tips

1. **Keep states minimal** — Only add states that represent meaningful business milestones
2. **Terminal states are final** — Once an entity reaches Completed or Cancelled, it shouldn't transition further
3. **Document side effects** — State transitions often trigger emails, logs, or timestamp updates
4. **Role-based transitions** — Not all users can trigger all transitions
5. **Separate concerns** — State validation in Service layer, display logic in Frontend
6. **Code tables, not enums** — Use NIE Template's Code table pattern for status values

## Review Checklist

- [ ] State diagram in Mermaid.js shows all states and transitions
- [ ] Transition table lists all valid transitions with roles and conditions
- [ ] Code table values defined for ECodeType/ECodeName
- [ ] Side effects documented for each transition
- [ ] Invalid transitions explicitly handled
- [ ] Frontend display (colors, actions) mapped to states
- [ ] Terminal states identified
