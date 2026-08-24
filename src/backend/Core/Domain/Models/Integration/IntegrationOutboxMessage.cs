namespace Domain.Models;

/// <summary>
/// Durable transactional outbox row awaiting transport publication.
/// </summary>
public sealed class IntegrationOutboxMessage : BaseEntity
{
    public Guid MessageId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public int EventVersion { get; set; }

    public string Producer { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string? CausationId { get; set; }

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset AvailableAtUtc { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; set; }

    public DateTimeOffset? DeadLetteredAtUtc { get; set; }

    public int AttemptCount { get; set; }

    public Guid? LockToken { get; set; }

    public DateTimeOffset? LockExpiresAtUtc { get; set; }

    public string? LastFailureCode { get; set; }
}
