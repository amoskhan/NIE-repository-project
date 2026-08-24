using Api.Authorization;
using Application.Contracts;
using Application.Features.DataTablePreferences;
using Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public sealed class UserDataTablePreferenceController : BaseController
{
    private readonly IUserDataTablePreferenceService _service;

    public UserDataTablePreferenceController(IUserDataTablePreferenceService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.DataTablePreferenceManage)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        return Ok(await _service.GetAllAsync(
            SystemApplicationIds.Core,
            UserId,
            cancellationToken));
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.DataTablePreferenceManage)]
    public async Task<IActionResult> Get(
        [FromQuery] string tableKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        if (!DataTablePreferenceTableKey.TryNormalize(tableKey, out var normalizedKey))
        {
            return InvalidTableKey();
        }

        var preference = await _service.GetAsync(
            SystemApplicationIds.Core,
            UserId,
            normalizedKey,
            cancellationToken);
        return preference is null ? NoContent() : Ok(preference);
    }

    [HttpPut]
    [RequireAccessFunction(AccessFunctionCodes.Api.DataTablePreferenceManage)]
    public async Task<IActionResult> Upsert(
        [FromQuery] string tableKey,
        [FromBody] UpsertUserDataTablePreferenceDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        if (!DataTablePreferenceTableKey.TryNormalize(tableKey, out var normalizedKey))
        {
            return InvalidTableKey();
        }

        try
        {
            return Ok(await _service.UpsertAsync(
                SystemApplicationIds.Core,
                UserId,
                normalizedKey,
                request,
                cancellationToken));
        }
        catch (DataTablePreferenceConflictException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Table preference conflict",
                Detail = exception.Message,
                Instance = HttpContext.Request.Path,
            });
        }
    }

    [HttpDelete]
    [RequireAccessFunction(AccessFunctionCodes.Api.DataTablePreferenceManage)]
    public async Task<IActionResult> Delete(
        [FromQuery] string tableKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        if (!DataTablePreferenceTableKey.TryNormalize(tableKey, out var normalizedKey))
        {
            return InvalidTableKey();
        }

        await _service.DeleteAsync(SystemApplicationIds.Core, UserId, normalizedKey, cancellationToken);
        return NoContent();
    }

    private BadRequestObjectResult InvalidTableKey() => BadRequest(new ValidationProblemDetails(
        new Dictionary<string, string[]>
        {
            ["tableKey"] =
            [
                "A table key must be 3 to 160 lowercase letters, numbers, dots, or hyphens.",
            ],
        })
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Validation failed",
        Instance = HttpContext.Request.Path,
    });
}
