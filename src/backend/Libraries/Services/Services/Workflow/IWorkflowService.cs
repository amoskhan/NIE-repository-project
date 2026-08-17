using Domain.Models;

namespace Services.Services.Workflow;

public interface IWorkflowService
{
    /// <summary>
    /// Get the current state of an entity.
    /// </summary>
    Task<WorkflowStateLog?> GetCurrentStateAsync(string ownerType, int ownerId);

    /// <summary>
    /// Get the full state transition history for an entity.
    /// </summary>
    Task<IList<WorkflowStateLog>> GetStateHistoryAsync(string ownerType, int ownerId);

    /// <summary>
    /// Transition an entity to a new state.
    /// </summary>
    Task<WorkflowStateLog> TransitionStateAsync(
        string ownerType,
        int ownerId,
        string fromState,
        string toState,
        string? performedByUserId,
        string? performedByName,
        string performedByRole,
        string? remarks = null);

    /// <summary>
    /// Get all available transitions from a given state for a role.
    /// </summary>
    Task<IList<WorkflowTransition>> GetAvailableTransitionsAsync(string currentState, string userRole);

    /// <summary>
    /// Check if a transition is allowed.
    /// </summary>
    Task<bool> CanTransitionAsync(string ownerType, int ownerId, string toState, string userRole);
}
