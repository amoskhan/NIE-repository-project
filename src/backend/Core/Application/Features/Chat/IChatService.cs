using Application.AI;
using Domain.Models;

namespace Application.Features.Chat;

public interface IChatService
{
    // Conversations
    Task<List<ChatConversation>> GetConversationsAsync(string userId, string? source = null, CancellationToken cancellationToken = default);
    Task<ChatConversation?> GetConversationAsync(Guid conversationId, string userId, CancellationToken cancellationToken = default);
    Task<ChatConversation> CreateConversationAsync(string userId, string title, string source, CancellationToken cancellationToken = default);
    Task<bool> DeleteConversationAsync(Guid conversationId, string userId, CancellationToken cancellationToken = default);
    Task<bool> RenameConversationAsync(Guid conversationId, string userId, string newTitle, CancellationToken cancellationToken = default);

    // Messages
    Task<List<ChatMessage>> GetMessagesAsync(Guid conversationId, string userId, CancellationToken cancellationToken = default);
    Task<ChatMessage> SendMessageAsync(Guid conversationId, string userId, string content, CancellationToken cancellationToken = default);

    // Feedback
    Task SubmitFeedbackAsync(Guid messageId, string userId, string type, string? comment = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the assistant reply as <see cref="AIStreamEventDto"/> events
    /// (message / tool_start / tool_result / metadata / stop / error) so the
    /// controller can emit them as SSE.
    /// </summary>
    IAsyncEnumerable<AIStreamEventDto> StreamResponseAsync(
        Guid conversationId,
        string userId,
        string content,
        CancellationToken cancellationToken = default);
}
