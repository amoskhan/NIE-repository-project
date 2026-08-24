namespace Application.AI;

public class ChatbotQuotaStatus
{
    public int ConversationsToday { get; set; }
    public int ConversationsDailyLimit { get; set; }
    public int TokensToday { get; set; }
    public int TokensDailyLimit { get; set; }
    public int RetentionDays { get; set; }
    public List<string> Warnings { get; set; } = new();

    public bool ConversationsExceeded => ConversationsToday >= ConversationsDailyLimit;
    public bool TokensExceeded => TokensToday >= TokensDailyLimit;
}
