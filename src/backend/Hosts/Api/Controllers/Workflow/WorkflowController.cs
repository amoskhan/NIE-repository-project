using Api.Authorization;
using Application.Features.Workflow;
using Application.Security;
using Domain.Enums;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : BaseController
{
    private readonly IWorkflowService _workflowService;

    public WorkflowController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    /// <summary>
    /// Get the current workflow state for an entity.
    /// </summary>
    [HttpGet("{ownerType}/{ownerId}/state")]
    [RequireAccessFunction(AccessFunctionCodes.Api.WorkflowRead)]
    public async Task<IActionResult> GetCurrentState(string ownerType, Guid ownerId)
    {
        var state = await _workflowService.GetCurrentStateAsync(ownerType, ownerId);
        if (state == null)
            return Ok(new { state = EWorkflowState.Draft.ToString(), history = Array.Empty<object>() });

        return Ok(state);
    }

    /// <summary>
    /// Get the full state transition history for an entity.
    /// </summary>
    [HttpGet("{ownerType}/{ownerId}/history")]
    [RequireAccessFunction(AccessFunctionCodes.Api.WorkflowRead)]
    public async Task<IActionResult> GetStateHistory(string ownerType, Guid ownerId)
    {
        var history = await _workflowService.GetStateHistoryAsync(ownerType, ownerId);
        return Ok(history);
    }

    /// <summary>
    /// Transition an entity to a new state.
    /// </summary>
    [HttpPost("{ownerType}/{ownerId}/transition")]
    [RequireAccessFunction(AccessFunctionCodes.Api.WorkflowTransition)]
    public async Task<IActionResult> TransitionState(
        string ownerType,
        Guid ownerId,
        [FromBody] TransitionRequest request)
    {
        var currentState = await _workflowService.GetCurrentStateAsync(ownerType, ownerId);
        var fromState = currentState?.ToState ?? EWorkflowState.Draft.ToString();

        var log = await _workflowService.TransitionStateAsync(
            ownerType,
            ownerId,
            fromState,
            request.ToState,
            UserId?.ToString(),
            UserName,
            UserRoles?.FirstOrDefault() ?? "User",
            request.Remarks,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(log);
    }

    /// <summary>
    /// Get available transitions from the current state for the current user's role.
    /// </summary>
    [HttpGet("{ownerType}/{ownerId}/available-transitions")]
    [RequireAccessFunction(AccessFunctionCodes.Api.WorkflowRead)]
    public async Task<IActionResult> GetAvailableTransitions(string ownerType, Guid ownerId)
    {
        var currentState = await _workflowService.GetCurrentStateAsync(ownerType, ownerId);
        var state = currentState?.ToState ?? EWorkflowState.Draft.ToString();
        var role = UserRoles?.FirstOrDefault() ?? "User";

        var transitions = await _workflowService.GetAvailableTransitionsAsync(state, role);
        return Ok(transitions);
    }
}

public class TransitionRequest
{
    public string ToState { get; set; } = default!;
    public string? Remarks { get; set; }
}
