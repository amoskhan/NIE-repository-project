# Workflow Engine — Do & Don't

## DO ✅

1. Use the `WorkflowService.TransitionStateAsync` method — never update `WorkflowState` directly.
2. Seed transitions via `OnModelCreating` in `MainDbContext.cs` (table-driven).
3. Use `WorkflowStateLog` for the full audit trail — it tracks who, when, and from what IP.
4. Extend `EWorkflowState` when adding new states — keep the enum as single source of truth.
5. Use `OwnerType` + `OwnerId` pattern for polymorphic workflow binding.

## DON'T ❌

1. Don't hardcode state transitions in controllers or services — use the `WorkflowTransitions` table.
2. Don't skip creating a `WorkflowStateLog` entry on every transition.
3. Don't bypass role-based validation — `WorkflowTransition.RequiredRole` is mandatory.
4. Don't use workflow for simple boolean statuses — it's for multi-step processes.
