namespace Application.AI;

/// <summary>
/// Enforces per-user conversation/token quotas and exposes the remaining
/// allowance to the UI. Implementations may back this with a database or
/// distributed cache.
/// </summary>
public interface IChatbotRateLimitService
{
    Task<ChatbotQuotaStatus> GetStatusAsync(string userId, CancellationToken cancellationToken = default);

    Task EnsureCanStartConversationAsync(string userId, CancellationToken cancellationToken = default);

    Task EnsureCanSendMessageAsync(string userId, CancellationToken cancellationToken = default);

    Task RecordUsageAsync(
        string userId,
        int inputTokens,
        int outputTokens,
        CancellationToken cancellationToken = default);
}
