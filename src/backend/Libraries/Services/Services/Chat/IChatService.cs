using Domain.Models;
using AppTemplate.AI;

namespace Services.Services.Chat;

public interface IChatService
{
    // Conversations
    Task<List<ChatConversation>> GetConversationsAsync(string userId, string? source = null, CancellationToken cancellationToken = default);
    Task<ChatConversation?> GetConversationAsync(int conversationId, string userId, CancellationToken cancellationToken = default);
    Task<ChatConversation> CreateConversationAsync(string userId, string title, string source, CancellationToken cancellationToken = default);
    Task<bool> DeleteConversationAsync(int conversationId, string userId, CancellationToken cancellationToken = default);
    Task<bool> RenameConversationAsync(int conversationId, string userId, string newTitle, CancellationToken cancellationToken = default);

    // Messages
    Task<List<ChatMessage>> GetMessagesAsync(int conversationId, string userId, CancellationToken cancellationToken = default);
    Task<ChatMessage> SendMessageAsync(int conversationId, string userId, string content, CancellationToken cancellationToken = default);

    // Feedback
    Task SubmitFeedbackAsync(int messageId, string userId, string type, string? comment = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the assistant reply as <see cref="AIStreamEventDto"/> events
    /// (message / tool_start / tool_result / metadata / stop / error) so the
    /// controller can emit them as SSE.
    /// </summary>
    IAsyncEnumerable<AIStreamEventDto> StreamResponseAsync(
        int conversationId,
        string userId,
        string content,
        CancellationToken cancellationToken = default);
}
