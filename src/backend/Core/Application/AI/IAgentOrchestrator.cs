namespace Application.AI;

/// <summary>
/// Drives the chat-with-tools loop and emits SSE-compatible events as they
/// happen (token deltas, tool calls, completion metadata, errors).
/// </summary>
public interface IAgentOrchestrator
{
    IAsyncEnumerable<AIStreamEventDto> ExecuteAsync(
        string query,
        string userId,
        string systemPrompt,
        List<AIChatMessageDto> history,
        AccessControlContext accessContext,
        CancellationToken cancellationToken = default);
}
