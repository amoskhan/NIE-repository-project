using Application.AI;

namespace Application.Tests;

public sealed class ChatbotQuotaStatusTests
{
    [Theory]
    [InlineData(0, 10, false)]
    [InlineData(9, 10, false)]
    [InlineData(10, 10, true)]
    [InlineData(11, 10, true)]
    [InlineData(0, 0, true)]
    public void Treats_the_conversation_limit_as_reached_once_it_is_met(
        int used,
        int limit,
        bool expected)
    {
        var status = new ChatbotQuotaStatus
        {
            ConversationsToday = used,
            ConversationsDailyLimit = limit,
        };

        Assert.Equal(expected, status.ConversationsExceeded);
    }

    [Theory]
    [InlineData(0, 5000, false)]
    [InlineData(4999, 5000, false)]
    [InlineData(5000, 5000, true)]
    [InlineData(5001, 5000, true)]
    public void Treats_the_token_limit_as_reached_once_it_is_met(int used, int limit, bool expected)
    {
        var status = new ChatbotQuotaStatus
        {
            TokensToday = used,
            TokensDailyLimit = limit,
        };

        Assert.Equal(expected, status.TokensExceeded);
    }

    [Fact]
    public void Reports_each_quota_independently()
    {
        var status = new ChatbotQuotaStatus
        {
            ConversationsToday = 1,
            ConversationsDailyLimit = 10,
            TokensToday = 9000,
            TokensDailyLimit = 5000,
        };

        Assert.False(status.ConversationsExceeded);
        Assert.True(status.TokensExceeded);
    }

    [Fact]
    public void Blocks_a_brand_new_status_because_no_allowance_has_been_configured()
    {
        var status = new ChatbotQuotaStatus();

        Assert.True(status.ConversationsExceeded);
        Assert.True(status.TokensExceeded);
        Assert.Empty(status.Warnings);
    }
}
