using Data.Data;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Services.Services.Workflow;

public class WorkflowService : IWorkflowService
{
    private readonly MainDbContext _context;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public WorkflowService(
        MainDbContext context,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkflowStateLog?> GetCurrentStateAsync(string ownerType, int ownerId)
    {
        return await _context.WorkflowStateLogs
            .Where(w => w.OwnerType == ownerType && w.OwnerId == ownerId)
            .OrderByDescending(w => w.TransitionedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IList<WorkflowStateLog>> GetStateHistoryAsync(string ownerType, int ownerId)
    {
        return await _context.WorkflowStateLogs
            .Where(w => w.OwnerType == ownerType && w.OwnerId == ownerId)
            .OrderBy(w => w.TransitionedAt)
            .ToListAsync();
    }

    public async Task<WorkflowStateLog> TransitionStateAsync(
        string ownerType,
        int ownerId,
        string fromState,
        string toState,
        string? performedByUserId,
        string? performedByName,
        string performedByRole,
        string? remarks = null)
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
            IpAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString()
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

    public async Task<bool> CanTransitionAsync(string ownerType, int ownerId, string toState, string userRole)
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
