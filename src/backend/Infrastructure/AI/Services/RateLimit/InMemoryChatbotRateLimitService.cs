using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace AI;

/// <summary>
/// Default in-memory <see cref="IChatbotRateLimitService"/>. Limits reset each
/// UTC day. Swap for a Redis/DB-backed implementation when running multiple
/// API instances.
/// </summary>
public class InMemoryChatbotRateLimitService : IChatbotRateLimitService
{
    private readonly int _conversationsPerDay;
    private readonly int _tokensPerDay;
    private readonly int _retentionDays;
    private readonly ConcurrentDictionary<string, UsageRecord> _usage = new();

    public InMemoryChatbotRateLimitService(IConfiguration configuration)
    {
        _conversationsPerDay = configuration.GetValue("AI:Quota:ConversationsPerDay", 25);
        _tokensPerDay = configuration.GetValue("AI:Quota:TokensPerDay", 50_000);
        _retentionDays = configuration.GetValue("AI:Quota:RetentionDays", 30);
    }

    public Task<ChatbotQuotaStatus> GetStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        var record = GetRecord(userId);
        var status = new ChatbotQuotaStatus
        {
            ConversationsToday = record.Conversations,
            ConversationsDailyLimit = _conversationsPerDay,
            TokensToday = record.Tokens,
            TokensDailyLimit = _tokensPerDay,
            RetentionDays = _retentionDays,
        };

        if (status.ConversationsToday >= _conversationsPerDay * 0.8)
        {
            status.Warnings.Add(
                $"You've used {status.ConversationsToday}/{_conversationsPerDay} conversations today.");
        }
        if (status.TokensToday >= _tokensPerDay * 0.8)
        {
            status.Warnings.Add(
                $"You've used {status.TokensToday}/{_tokensPerDay} tokens today.");
        }

        return Task.FromResult(status);
    }

    public Task EnsureCanStartConversationAsync(string userId, CancellationToken cancellationToken = default)
    {
        var record = GetRecord(userId);
        if (record.Conversations >= _conversationsPerDay)
        {
            throw new InvalidOperationException(
                $"Daily conversation limit reached ({_conversationsPerDay}). Try again tomorrow.");
        }

        record.Conversations += 1;
        return Task.CompletedTask;
    }

    public Task EnsureCanSendMessageAsync(string userId, CancellationToken cancellationToken = default)
    {
        var record = GetRecord(userId);
        if (record.Tokens >= _tokensPerDay)
        {
            throw new InvalidOperationException(
                $"Daily token limit reached ({_tokensPerDay}). Try again tomorrow.");
        }

        return Task.CompletedTask;
    }

    public Task RecordUsageAsync(
        string userId,
        int inputTokens,
        int outputTokens,
        CancellationToken cancellationToken = default)
    {
        var record = GetRecord(userId);
        record.Tokens += inputTokens + outputTokens;
        return Task.CompletedTask;
    }

    private UsageRecord GetRecord(string userId)
    {
        var today = DateTime.UtcNow.Date;
        return _usage.AddOrUpdate(
            userId,
            _ => new UsageRecord { Day = today },
            (_, existing) =>
            {
                if (existing.Day != today)
                {
                    existing.Day = today;
                    existing.Conversations = 0;
                    existing.Tokens = 0;
                }

                return existing;
            });
    }

    private class UsageRecord
    {
        public DateTime Day { get; set; }
        public int Conversations { get; set; }
        public int Tokens { get; set; }
    }
}
