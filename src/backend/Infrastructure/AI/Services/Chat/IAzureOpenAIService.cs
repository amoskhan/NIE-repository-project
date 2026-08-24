using Microsoft.Extensions.AI;

namespace AI;

/// <summary>
/// Thin wrapper over the Azure OpenAI SDK so the orchestrator can request a
/// configured <see cref="IChatClient"/> without owning credential details.
/// </summary>
public interface IAzureOpenAIService
{
    /// <summary>
    /// Returns a configured <see cref="IChatClient"/> for the chat deployment.
    /// Throws if Azure OpenAI is not configured.
    /// </summary>
    IChatClient CreateChatClient();

    /// <summary>Non-streaming chat completion.</summary>
    Task<AIChatResponseDto> ChatAsync(
        List<AIChatMessageDto> messages,
        List<AIToolDefinitionDto>? tools = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);
}
