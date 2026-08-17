# Workflow Engine — Customize

## Adding a New State

1. Add enum value to `Shared/Enum/EWorkflowState.cs`
2. Add corresponding transitions in `MainDbContext.cs` seed data
3. Create a migration: `dotnet ef migrations add AddNewWorkflowState`

## Adding Workflow to a New Entity

1. Add `public string WorkflowState { get; set; } = EWorkflowState.Draft.ToString()` to the entity
2. Seed default transitions for the entity's workflow in `OnModelCreating`
3. Call `WorkflowService.TransitionStateAsync(ownerType: "YourEntity", ...)` in your service
4. Add `WorkflowTimeline` + `WorkflowActionBar` components to the entity's detail page

## Changing Transition Permissions

- Update `WorkflowTransition.RequiredRole` in the seed data (or via admin UI)
- No code changes needed — the transition table is read at runtime
