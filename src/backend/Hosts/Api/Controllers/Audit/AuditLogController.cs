using Api.Authorization;
using Application.Contracts;
using Application.Features;
using Application.Security;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Read-only audit log management endpoints.
/// </summary>
public class AuditLogController : BaseController
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.AuditRead)]
    public async Task<ActionResult<AuditLogPagedResultDto>> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
    {
        return Ok(await _auditLogService.GetAuditLogsAsync(filter));
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AuditRead)]
    public async Task<ActionResult<DataTablePageDto<AuditLogDto>>> Search(
        [FromBody] DataTableRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await _auditLogService.SearchTableAsync(request, cancellationToken));

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AuditRead)]
    public async Task<ActionResult<DataTableFilterOptionPageDto>> GetFilterOptions(
        [FromBody] DataTableFilterOptionsRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await _auditLogService.GetFilterOptionsAsync(request, cancellationToken));

    [HttpGet("Entry/{id:guid}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.AuditRead)]
    public async Task<ActionResult<AuditLogDto>> GetAuditLogById(Guid id)
    {
        var entry = await _auditLogService.GetByIdAsync(id);
        return entry == null ? NotFound("Audit log entry not found.") : Ok(entry);
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.AuditRead)]
    public async Task<ActionResult<List<AuditLogDto>>> GetEntityHistory(string entityName, string entityId)
    {
        return Ok(await _auditLogService.GetEntityHistoryAsync(entityName, entityId));
    }

    [HttpGet("User/{userId}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.AuditRead)]
    public async Task<ActionResult<List<AuditLogDto>>> GetUserActivity(string userId, int maxRecords = 100)
    {
        return Ok(await _auditLogService.GetUserActivityAsync(userId, maxRecords));
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.AuditRead)]
    public async Task<ActionResult<AuditLogSummaryDto>> GetAuditSummary()
    {
        return Ok(await _auditLogService.GetSummaryAsync());
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.AuditRead)]
    public async Task<ActionResult<List<string>>> GetAuditEntityNames()
    {
        return Ok(await _auditLogService.GetDistinctEntityNamesAsync());
    }

    [HttpGet("Category/{category}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.AuditRead)]
    public async Task<ActionResult<List<AuditLogDto>>> GetAuditLogsByCategory(EAuditCategory category, int maxRecords = 100)
    {
        return Ok(await _auditLogService.GetByCategoryAsync(category, maxRecords));
    }
}
