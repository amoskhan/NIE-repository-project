# Workflow Engine — Files

## Backend (Create)

- `src/backend/Libraries/Shared/Enum/EWorkflowState.cs`
- `src/backend/Libraries/Domain/Models/WorkflowTransition.cs`
- `src/backend/Libraries/Domain/Models/WorkflowStateLog.cs`
- `src/backend/Libraries/Services/Services/Workflow/IWorkflowService.cs`
- `src/backend/Libraries/Services/Services/Workflow/WorkflowService.cs`
- `src/backend/API/Controllers/WorkflowController.cs`

## Backend (Edit)

- `src/backend/Libraries/Domain/Models/Samples/Procurement/PurchaseOrder.cs` — add `WorkflowState`
- `src/backend/Libraries/Data/Data/MainDbContext.cs` — add DbSets, configs, seed data
- `src/backend/API/Program.cs` — register `IWorkflowService`

## Frontend (Create)

- `src/frontend/main/src/types/workflow.ts`
- `src/frontend/main/src/services/workflowService.ts`
- `src/frontend/main/src/components/workflow/WorkflowTimeline.vue`
- `src/frontend/main/src/components/workflow/WorkflowActionBar.vue`
