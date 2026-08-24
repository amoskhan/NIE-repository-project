
using Api.Authorization;
using Application.Contracts;
using Application.Security;
using BuildingBlocks.Helpers;
using Infrastructure.Options;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Controllers;

/// <summary>Signed-in user's durable in-app notifications.</summary>
public class NotificationController : BaseController
{
    private readonly MainDbContext _context;
    private readonly OneSignalSettings _pushNotificationSettings;

    public NotificationController(
        MainDbContext context,
        IOptions<OneSignalSettings> pushNotificationSettings)
    {
        _context = context;
        _pushNotificationSettings = pushNotificationSettings.Value;
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationRead)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        var rows = await VisibleNotifications(UserId)
            .OrderByDescending(item => item.CreatedOn)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
        return Ok(rows.Select(ToDto));
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationRead)]
    public async Task<IActionResult> GetUnread(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        var rows = await VisibleNotifications(UserId)
            .Where(item => !item.IsRead)
            .OrderByDescending(item => item.CreatedOn)
            .Take(100)
            .ToListAsync(cancellationToken);
        return Ok(rows.Select(ToDto));
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationRead)]
    public async Task<IActionResult> GetUnreadCount(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        return Ok(await VisibleNotifications(UserId)
            .CountAsync(item => !item.IsRead, cancellationToken));
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationPreferenceManage)]
    public async Task<IActionResult> MarkAsRead(
        [FromQuery] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        var item = await _context.Notifications
            .FirstOrDefaultAsync(
                row => row.Id == id && row.RecipientUserId == UserId,
                cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!item.IsRead)
        {
            item.IsRead = true;
            item.ReadAt = DateTimeHelper.Now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationPreferenceManage)]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        var now = DateTimeHelper.Now;
        await _context.Notifications
            .Where(item => item.RecipientUserId == UserId && !item.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.IsRead, true)
                    .SetProperty(item => item.ReadAt, now),
                cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationRead)]
    public IActionResult GetPushConfiguration()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        return Ok(new
        {
            enabled = !string.IsNullOrWhiteSpace(_pushNotificationSettings.AppId),
            appId = _pushNotificationSettings.AppId,
        });
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationRead)]
    public async Task<IActionResult> GetPreferences(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        var preferences = await _context.UserNotificationPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == UserId, cancellationToken);
        return Ok(ToPreferencesDto(preferences));
    }

    [HttpPut]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationPreferenceManage)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UserNotificationPreferencesDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        var preferences = await _context.UserNotificationPreferences
            .SingleOrDefaultAsync(item => item.UserId == UserId, cancellationToken);
        if (preferences is null)
        {
            preferences = new Domain.Models.UserNotificationPreference
            {
                UserId = UserId,
            };
            _context.UserNotificationPreferences.Add(preferences);
        }

        preferences.DesktopAlerts = dto.DesktopAlerts;
        preferences.ApprovalTasksPush = dto.ApprovalTasksPush;
        preferences.ApprovalDecisionsPush = dto.ApprovalDecisionsPush;
        preferences.OrderUpdatesPush = dto.OrderUpdatesPush;
        preferences.SystemAlertsPush = dto.SystemAlertsPush;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ToPreferencesDto(preferences));
    }

    private IQueryable<Domain.Models.Notification> VisibleNotifications(
        string userId) =>
        _context.Notifications
            .AsNoTracking()
            .Where(item => item.RecipientUserId == userId);

    private static NotificationDto ToDto(Domain.Models.Notification item) =>
        new()
        {
            Id = item.Id,
            RecipientUserId = item.RecipientUserId,
            RecipientName = item.RecipientName,
            Title = item.Title,
            Message = item.Message,
            Type = item.Type,
            IsRead = item.IsRead,
            ReadAt = item.ReadAt,
            Link = item.Link,
            SourceEntityType = item.SourceEntityType,
            SourceEntityId = item.SourceEntityId,
            EventKey = item.EventKey,
            CorrelationKey = item.CorrelationKey,
            IsActionRequired = item.IsActionRequired,
            CreatedOn = item.CreatedOn.GetValueOrDefault(DateTimeHelper.Now),
        };

    private static UserNotificationPreferencesDto ToPreferencesDto(
        Domain.Models.UserNotificationPreference? item) =>
        item is null
            ? new UserNotificationPreferencesDto()
            : new UserNotificationPreferencesDto
            {
                DesktopAlerts = item.DesktopAlerts,
                ApprovalTasksPush = item.ApprovalTasksPush,
                ApprovalDecisionsPush = item.ApprovalDecisionsPush,
                OrderUpdatesPush = item.OrderUpdatesPush,
                SystemAlertsPush = item.SystemAlertsPush,
            };

    private sealed class NotificationDto
    {
        public Guid Id { get; set; }
        public string RecipientType { get; set; } = "User";
        public string RecipientUserId { get; set; } = default!;
        public string? RecipientEmail { get; set; }
        public string? RecipientName { get; set; }
        public string Title { get; set; } = default!;
        public string Message { get; set; } = default!;
        public string Type { get; set; } = default!;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? Link { get; set; }
        public string? SourceEntityType { get; set; }
        public Guid? SourceEntityId { get; set; }
        public string? EventKey { get; set; }
        public string? CorrelationKey { get; set; }
        public bool IsActionRequired { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
