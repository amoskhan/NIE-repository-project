using Application.Abstractions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Workflow;

public class WorkflowService : IWorkflowService
{
    private readonly IApplicationDbContext _context;

    public WorkflowService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowStateLog?> GetCurrentStateAsync(string ownerType, Guid ownerId)
    {
        return await _context.WorkflowStateLogs
            .Where(w => w.OwnerType == ownerType && w.OwnerId == ownerId)
            .OrderByDescending(w => w.TransitionedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IList<WorkflowStateLog>> GetStateHistoryAsync(string ownerType, Guid ownerId)
    {
        return await _context.WorkflowStateLogs
            .Where(w => w.OwnerType == ownerType && w.OwnerId == ownerId)
            .OrderBy(w => w.TransitionedAt)
            .ToListAsync();
    }

    public async Task<WorkflowStateLog> TransitionStateAsync(
        string ownerType,
        Guid ownerId,
        string fromState,
        string toState,
        string? performedByUserId,
        string? performedByName,
        string performedByRole,
        string? remarks = null,
        string? ipAddress = null)
    {
        var canTransition = await CanTransitionAsync(ownerType, ownerId, toState, performedByRole);
        if (!canTransition)
            throw new InvalidOperationException(
                $"Transition from '{fromState}' to '{toState}' is not allowed for role '{performedByRole}'.");

        var log = new WorkflowStateLog
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            FromState = fromState,
            ToState = toState,
            Remarks = remarks,
            PerformedByUserId = performedByUserId,
            PerformedByName = performedByName,
            PerformedByRole = performedByRole,
            TransitionedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.WorkflowStateLogs.Add(log);
        await _context.SaveChangesAsync();

        return log;
    }

    public async Task<IList<WorkflowTransition>> GetAvailableTransitionsAsync(string currentState, string userRole)
    {
        return await _context.WorkflowTransitions
            .Where(t => t.IsActive && t.FromState == currentState && t.RequiredRole == userRole)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Id)
            .ToListAsync();
    }

    public async Task<bool> CanTransitionAsync(string ownerType, Guid ownerId, string toState, string userRole)
    {
        var currentState = await GetCurrentStateAsync(ownerType, ownerId);
        if (currentState == null)
        {
            // No state yet — allow first transition (usually Draft → Submitted)
            return true;
        }

        return await _context.WorkflowTransitions
            .AnyAsync(t =>
                t.IsActive &&
                t.FromState == currentState.ToState &&
                t.ToState == toState &&
                t.RequiredRole == userRole);
    }
}
