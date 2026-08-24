using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI;

/// <summary>
/// Default <see cref="IAzureOpenAIService"/> built on the Azure OpenAI SDK and
/// <c>Microsoft.Extensions.AI.OpenAI</c>. Throws clearly when not configured —
/// no silent fallback to a mock.
/// </summary>
public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAIService> _logger;

    public AzureOpenAIService(
        IConfiguration configuration,
        ILogger<AzureOpenAIService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public IChatClient CreateChatClient()
    {
        if (!AzureOpenAIConfiguration.IsConfigured(_configuration))
        {
            throw new InvalidOperationException(
                AzureOpenAIConfiguration.MissingChatCredentialsMessage);
        }

        var endpoint = AzureOpenAIConfiguration.GetEndpoint(_configuration)!;
        var apiKey = AzureOpenAIConfiguration.GetApiKey(_configuration)!;
        var deployment = AzureOpenAIConfiguration.GetChatDeployment(_configuration);

        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        return azureClient.GetChatClient(deployment).AsIChatClient();
    }

    public async Task<AIChatResponseDto> ChatAsync(
        List<AIChatMessageDto> messages,
        List<AIToolDefinitionDto>? tools = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        var client = CreateChatClient();
        var converted = ConvertMessages(messages, systemPrompt);

        var response = await client.GetResponseAsync(converted, cancellationToken: cancellationToken);

        return new AIChatResponseDto
        {
            Content = response.Text,
            StopReason = response.FinishReason?.ToString(),
            InputTokens = (int)(response.Usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(response.Usage?.OutputTokenCount ?? 0),
        };
    }

    private static List<ChatMessage> ConvertMessages(
        IEnumerable<AIChatMessageDto> history,
        string? systemPrompt)
    {
        var converted = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            converted.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }

        foreach (var item in history)
        {
            if (string.IsNullOrWhiteSpace(item.Content))
            {
                continue;
            }

            converted.Add(new ChatMessage(new ChatRole(item.Role), item.Content));
        }

        return converted;
    }
}
