using API.Authorization;
using Shared.Dto;
using Shared.Enum;
using Shared.Security;
using Services.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Read-only audit log management endpoints.
/// </summary>
[RequireAccessFunction(AccessFunctionCodes.Api.AuditRead)]
public class AuditLogController : BaseController
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogPagedResultDto>> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
    {
        return Ok(await _auditLogService.GetAuditLogsAsync(filter));
    }

    [HttpGet("Entry/{id:long}")]
    public async Task<ActionResult<AuditLogDto>> GetAuditLogById(long id)
    {
        var entry = await _auditLogService.GetByIdAsync(id);
        return entry == null ? NotFound("Audit log entry not found.") : Ok(entry);
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditLogDto>>> GetEntityHistory(string entityName, string entityId)
    {
        return Ok(await _auditLogService.GetEntityHistoryAsync(entityName, entityId));
    }

    [HttpGet("User/{userId}")]
    public async Task<ActionResult<List<AuditLogDto>>> GetUserActivity(string userId, int maxRecords = 100)
    {
        return Ok(await _auditLogService.GetUserActivityAsync(userId, maxRecords));
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogSummaryDto>> GetAuditSummary()
    {
        return Ok(await _auditLogService.GetSummaryAsync());
    }

    [HttpGet]
    public async Task<ActionResult<List<string>>> GetAuditEntityNames()
    {
        return Ok(await _auditLogService.GetDistinctEntityNamesAsync());
    }

    [HttpGet("Category/{category}")]
    public async Task<ActionResult<List<AuditLogDto>>> GetAuditLogsByCategory(EAuditCategory category, int maxRecords = 100)
    {
        return Ok(await _auditLogService.GetByCategoryAsync(category, maxRecords));
    }
}
