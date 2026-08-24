
using Api.Authorization;
using Application.Contracts;
using Application.Features.DataTable;
using Application.Features.Email;
using Application.Features.Notifications;
using Application.Features.PushNotification;
using Application.Security;
using BuildingBlocks.Helpers;
using Domain.Models;
using Infrastructure.Options;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Controllers;

/// <summary>
/// System-administrator notification policies, templates, health and delivery
/// operations. Provider secrets are intentionally never returned.
/// </summary>
public sealed class NotificationAdministrationController : BaseController
{
    private readonly MainDbContext _context;
    private readonly INotificationTemplateRenderer _renderer;
    private readonly IEmailService _email;
    private readonly IPushNotificationService _push;
    private readonly EmailSettings _emailSettings;
    private readonly OneSignalSettings _pushNotificationSettings;

    public NotificationAdministrationController(
        MainDbContext context,
        INotificationTemplateRenderer renderer,
        IEmailService email,
        IPushNotificationService push,
        IOptions<EmailSettings> emailSettings,
        IOptions<OneSignalSettings> pushNotificationSettings)
    {
        _context = context;
        _renderer = renderer;
        _email = email;
        _push = push;
        _emailSettings = emailSettings.Value;
        _pushNotificationSettings = pushNotificationSettings.Value;
    }

    [HttpGet]
    [RequireAccessFunction(
        AccessFunctionCodes.Api.NotificationConfigurationRead,
        AccessFunctionCodes.Api.NotificationDeliveryRead)]
    public async Task<ActionResult<NotificationAdministrationOverviewDto>> GetOverview(
        CancellationToken cancellationToken)
    {
        var policyRows = await _context.NotificationPolicies
            .AsNoTracking()
            .OrderBy(item => item.Category)
            .ThenBy(item => item.DisplayName)
            .ToListAsync(cancellationToken);
        var policies = policyRows
            .Select(item =>
            {
                var definition = NotificationEventCatalog.Find(item.EventKey);
                return new NotificationPolicyDto
                {
                    Id = item.Id,
                    EventKey = item.EventKey,
                    DisplayName = item.DisplayName,
                    Description = item.Description,
                    Category = item.Category,
                    InAppEnabled = item.InAppEnabled,
                    EmailEnabled = item.EmailEnabled,
                    PushEnabled = item.PushEnabled,
                    IsActive = item.IsActive,
                    SupportsReminderConfiguration =
                        definition?.SupportsReminderConfiguration == true,
                    ReminderAfterHours = definition?.SupportsReminderConfiguration == true
                        ? item.ReminderAfterHours
                        : null,
                    EscalationAfterHours = definition?.SupportsReminderConfiguration == true
                        ? item.EscalationAfterHours
                        : null,
                };
            })
            .ToList();

        var templateRows = await _context.NotificationTemplates
            .AsNoTracking()
            .OrderBy(item => item.EventKey)
            .ThenByDescending(item => item.Version)
            .ToListAsync(cancellationToken);
        var deliveryStatusCounts = await _context.NotificationDeliveries
            .AsNoTracking()
            .GroupBy(item => item.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        return Ok(new NotificationAdministrationOverviewDto
        {
            Policies = policies,
            Templates = templateRows.Select(ToTemplateDto).ToList(),
            RecentDeliveries = [],
            DeliveryStatusCounts = deliveryStatusCounts,
            ChannelHealth = new NotificationChannelHealthDto
            {
                EmailConfigured = IsEmailConfigured(),
                PushNotificationsConfigured = ArePushNotificationsConfigured(),
                RealtimeConfigured = true,
            },
            AllowedPlaceholders = NotificationEventCatalog.AllowedPlaceholders,
        });
    }

    [HttpPut("{eventKey}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationConfigurationManage)]
    public async Task<IActionResult> UpdatePolicy(
        string eventKey,
        [FromBody] UpdateNotificationPolicyDto dto,
        CancellationToken cancellationToken)
    {
        var policy = await _context.NotificationPolicies
            .SingleOrDefaultAsync(item => item.EventKey == eventKey, cancellationToken);
        if (policy is null)
        {
            return NotFound(new { message = "Notification event was not found." });
        }

        var definition = NotificationEventCatalog.Find(eventKey);
        var timingErrors = NotificationPolicyTimingRules.Validate(
            definition,
            dto.ReminderAfterHours,
            dto.EscalationAfterHours);
        if (timingErrors.Count > 0)
        {
            return BadRequest(new
            {
                message = timingErrors[0],
            });
        }

        if (!dto.InAppEnabled ||
            (definition?.Category == "Approval tasks" &&
             (!dto.EmailEnabled || !dto.IsActive)))
        {
            return BadRequest(new
            {
                message = "In-app delivery is mandatory for workflow events, and approval-task policies must remain active with email enabled.",
            });
        }

        policy.InAppEnabled = dto.InAppEnabled;
        policy.EmailEnabled = dto.EmailEnabled;
        policy.PushEnabled = dto.PushEnabled;
        policy.IsActive = dto.IsActive;
        policy.ReminderAfterHours = definition?.SupportsReminderConfiguration == true
            ? dto.ReminderAfterHours
            : null;
        policy.EscalationAfterHours = definition?.SupportsReminderConfiguration == true
            ? dto.EscalationAfterHours
            : null;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationConfigurationManage)]
    public async Task<ActionResult<NotificationTemplateDto>> SaveTemplate(
        [FromBody] SaveNotificationTemplateDto dto,
        CancellationToken cancellationToken)
    {
        if (NotificationEventCatalog.Find(dto.EventKey) is null)
        {
            return BadRequest(new { message = "Unknown notification event." });
        }

        var errors = _renderer.Validate(dto.Subject, dto.Content);
        if (errors.Count > 0)
        {
            return BadRequest(new { message = "Template validation failed.", errors });
        }

        var currentVersions = await _context.NotificationTemplates
            .Where(item =>
                item.EventKey == dto.EventKey &&
                item.Channel == NotificationChannels.Email)
            .ToListAsync(cancellationToken);
        var nextVersion = currentVersions.Count == 0
            ? 1
            : currentVersions.Max(item => item.Version) + 1;

        if (dto.Publish)
        {
            foreach (var current in currentVersions.Where(item => item.IsPublished))
            {
                current.IsPublished = false;
            }
        }

        var template = new NotificationTemplate
        {
            EventKey = dto.EventKey,
            Channel = NotificationChannels.Email,
            Version = nextVersion,
            Subject = dto.Subject.Trim(),
            Content = dto.Content.Trim(),
            IsPublished = dto.Publish,
            PublishedBy = dto.Publish ? UserId : null,
            PublishedOn = dto.Publish ? DateTimeHelper.Now : null,
        };
        _context.NotificationTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ToTemplateDto(template));
    }

    [HttpPost("{id:guid}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationConfigurationManage)]
    public async Task<IActionResult> PublishTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var targetIdentity = await _context.NotificationTemplates
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.EventKey, item.Channel })
            .SingleOrDefaultAsync(cancellationToken);
        if (targetIdentity is null)
        {
            return NotFound();
        }

        var versions = await _context.NotificationTemplates
            .Where(item =>
                item.EventKey == targetIdentity.EventKey &&
                item.Channel == targetIdentity.Channel)
            .ToListAsync(cancellationToken);
        var template = versions.Single(item => item.Id == id);
        foreach (var version in versions.Where(item => item.IsPublished))
        {
            version.IsPublished = false;
        }

        template.IsPublished = true;
        template.PublishedBy = UserId;
        template.PublishedOn = DateTimeHelper.Now;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationDeliveryRead)]
    public async Task<ActionResult<List<NotificationDeliveryDto>>> GetDeliveries(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default) =>
        Ok(await BuildDeliveriesQuery()
            .Take(Math.Clamp(limit, 1, 500))
            .ToListAsync(cancellationToken));

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationDeliveryRead)]
    public async Task<ActionResult<DataTablePageDto<NotificationDeliveryDto>>> SearchDeliveries(
        [FromBody] DataTableRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = ApplyDeliveryQuery(_context.NotificationDeliveries.AsNoTracking(), request);
        var ordered = new DataTableSortMap<NotificationDelivery>()
            .Add("eventkey", item => item.NotificationOutbox.EventKey)
            .Add("recipientname", item => item.RecipientName)
            .Add("channel", item => item.Channel)
            .Add("status", item => item.Status)
            .Add("attempts", item => item.Attempts)
            .Add("createdon", item => item.CreatedOn)
            .Apply(query, request, items => items.OrderByDescending(item => item.CreatedOn), item => item.Id);

        var page = await ordered
            .Select(item => new NotificationDeliveryDto
            {
                Id = item.Id,
                EventKey = item.NotificationOutbox.EventKey,
                CorrelationKey = item.NotificationOutbox.CorrelationKey,
                RecipientUserId = item.RecipientUserId,
                RecipientName = item.RecipientName,
                RecipientEmail = item.RecipientEmail,
                Channel = item.Channel,
                Status = item.Status,
                Attempts = item.Attempts,
                SentOn = item.SentOn,
                NextAttemptOn = item.NextAttemptOn,
                LastError = item.LastError,
                CreatedOn = item.CreatedOn,
            })
            .ToDataTablePageAsync(request, cancellationToken);
        return Ok(page);
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationDeliveryRead)]
    public async Task<ActionResult<DataTableFilterOptionPageDto>> GetDeliveryFilterOptions(
        [FromBody] DataTableFilterOptionsRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = ApplyDeliveryQuery(_context.NotificationDeliveries.AsNoTracking(), request, request.ColumnKey);
        var values = request.ColumnKey.ToLowerInvariant() switch
        {
            "eventkey" => query.Select(item => item.NotificationOutbox.EventKey),
            "recipientname" => query.Select(item => item.RecipientName ?? item.RecipientUserId),
            "channel" => query.Select(item => item.Channel),
            "status" => query.Select(item => item.Status),
            "attempts" => query.Select(item => item.Attempts.ToString()),
            "createdon" => query.Select(item => item.CreatedOn.HasValue ? item.CreatedOn.Value.ToString("yyyy-MM-dd") : string.Empty),
            _ => query.Where(_ => false).Select(item => item.Status),
        };
        return Ok(await values.ToFilterOptionPageAsync(request, cancellationToken: cancellationToken));
    }

    [HttpPost("{id:guid}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationDeliveryRetry)]
    public async Task<IActionResult> RetryDelivery(
        Guid id,
        CancellationToken cancellationToken)
    {
        var delivery = await _context.NotificationDeliveries
            .Include(item => item.NotificationOutbox)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (delivery is null)
        {
            return NotFound();
        }

        if (delivery.Status is not (
            NotificationDeliveryStatuses.Failed or
            NotificationDeliveryStatuses.Skipped))
        {
            return Conflict(new
            {
                message = "Only failed or skipped deliveries can be retried.",
            });
        }

        delivery.Status = NotificationDeliveryStatuses.Retry;
        delivery.NextAttemptOn = DateTimeHelper.Now;
        delivery.LastError = null;
        delivery.NotificationOutbox.Status = NotificationOutboxStatuses.Retry;
        delivery.NotificationOutbox.NextAttemptOn = DateTimeHelper.Now;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.NotificationConfigurationManage)]
    public async Task<IActionResult> SendTest(
        [FromBody] TestNotificationDto dto)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        if (dto.Channel.Equals(NotificationChannels.InApp, StringComparison.OrdinalIgnoreCase))
        {
            _context.Notifications.Add(new Notification
            {
                RecipientUserId = UserId,
                RecipientName = UserName,
                Title = "NIE Template notification test",
                Message = "In-app notifications are working.",
                Type = "SystemAlert",
                Link = "/notification-administration",
                EventKey = "system.test",
                CorrelationKey = Guid.NewGuid().ToString(),
                CreatedOn = DateTimeHelper.Now,
            });
            await _context.SaveChangesAsync();
            return NoContent();
        }

        if (dto.Channel.Equals(NotificationChannels.Email, StringComparison.OrdinalIgnoreCase))
        {
            var address = string.IsNullOrWhiteSpace(dto.Email) ? UserEmail : dto.Email.Trim();
            if (!IsEmailConfigured() || string.IsNullOrWhiteSpace(address))
            {
                return BadRequest(new { message = "SMTP or a test email address is not configured." });
            }

            await _email.SendBaseTemplatedEmailAsync(
                address,
                "NIE Template notification test",
                "<p>This confirms that NIE Template email delivery is working.</p>",
                UserName);
            return NoContent();
        }

        if (dto.Channel.Equals(NotificationChannels.Push, StringComparison.OrdinalIgnoreCase))
        {
            if (!ArePushNotificationsConfigured())
            {
                return BadRequest(new { message = "Push notifications are not configured." });
            }

            await _push.SendToUsersAsync(
                new[] { UserId },
                "NIE Template notification test",
                "Push notifications are working.",
                "/notification-administration");
            return NoContent();
        }

        return BadRequest(new { message = "Unsupported notification channel." });
    }

    private IQueryable<NotificationDeliveryDto> BuildDeliveriesQuery() =>
        _context.NotificationDeliveries
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedOn)
            .Select(item => new NotificationDeliveryDto
            {
                Id = item.Id,
                EventKey = item.NotificationOutbox.EventKey,
                CorrelationKey = item.NotificationOutbox.CorrelationKey,
                RecipientUserId = item.RecipientUserId,
                RecipientName = item.RecipientName,
                RecipientEmail = item.RecipientEmail,
                Channel = item.Channel,
                Status = item.Status,
                Attempts = item.Attempts,
                SentOn = item.SentOn,
                NextAttemptOn = item.NextAttemptOn,
                LastError = item.LastError,
                CreatedOn = item.CreatedOn,
            });

    private static IQueryable<NotificationDelivery> ApplyDeliveryQuery(
        IQueryable<NotificationDelivery> query,
        DataTableRequestDto request,
        string? excludedFilter = null)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.NotificationOutbox.EventKey, pattern) ||
                EF.Functions.ILike(item.RecipientUserId, pattern) ||
                (item.RecipientName != null && EF.Functions.ILike(item.RecipientName, pattern)) ||
                (item.RecipientEmail != null && EF.Functions.ILike(item.RecipientEmail, pattern)) ||
                EF.Functions.ILike(item.Channel, pattern) ||
                EF.Functions.ILike(item.Status, pattern));
        }

        foreach (var filter in request.Filters.Where(filter => !filter.Key.Equals(excludedFilter, StringComparison.OrdinalIgnoreCase)))
        {
            var values = filter.Values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            if (values.Count == 0) continue;
            query = filter.Key.ToLowerInvariant() switch
            {
                "eventkey" => query.Where(item => values.Contains(item.NotificationOutbox.EventKey)),
                "recipientname" => query.Where(item => values.Contains(item.RecipientName ?? item.RecipientUserId)),
                "channel" => query.Where(item => values.Contains(item.Channel)),
                "status" => query.Where(item => values.Contains(item.Status)),
                "attempts" => query.Where(item => values.Contains(item.Attempts.ToString())),
                "createdon" => query.Where(item => item.CreatedOn.HasValue && values.Contains(item.CreatedOn.Value.ToString("yyyy-MM-dd"))),
                _ => query,
            };
        }
        return query;
    }

    private bool IsEmailConfigured() =>
        !string.IsNullOrWhiteSpace(_emailSettings.SmtpHost) &&
        (!string.IsNullOrWhiteSpace(_emailSettings.SenderEmail) ||
         !string.IsNullOrWhiteSpace(_emailSettings.AppName));

    private bool ArePushNotificationsConfigured() =>
        !string.IsNullOrWhiteSpace(_pushNotificationSettings.AppId) &&
        !string.IsNullOrWhiteSpace(_pushNotificationSettings.RestApiKey);

    private static NotificationTemplateDto ToTemplateDto(NotificationTemplate item) =>
        new()
        {
            Id = item.Id,
            EventKey = item.EventKey,
            Channel = item.Channel,
            Version = item.Version,
            Subject = item.Subject,
            Content = item.Content,
            IsPublished = item.IsPublished,
            PublishedBy = item.PublishedBy,
            PublishedOn = item.PublishedOn,
        };
}
