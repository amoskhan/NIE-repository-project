namespace Domain.Models;

/// <summary>
/// Durable inbox receipt used to suppress repeated at-least-once deliveries.
/// </summary>
public sealed class IntegrationInboxMessage : BaseEntity
{
    public Guid MessageId { get; set; }

    public string Consumer { get; set; } = string.Empty;

    public string EventName { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAtUtc { get; set; }
}
