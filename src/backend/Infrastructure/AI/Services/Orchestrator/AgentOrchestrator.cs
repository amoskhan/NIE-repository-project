using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI;

/// <summary>
/// Microsoft Agent Framework orchestrator. Wraps an <see cref="IChatClient"/>
/// with function-invocation middleware so registered <see cref="IAITool"/>s
/// can be called mid-stream, while emitting SSE events to the caller.
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private const int MaxToolInvocationsPerRun = 8;
    private const int MaxOutputTokens = 2_048;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IAzureOpenAIService _aiService;
    private readonly IEnumerable<IAITool> _tools;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;

    public AgentOrchestrator(
        IAzureOpenAIService aiService,
        IEnumerable<IAITool> tools,
        IConfiguration configuration,
        ILogger<AgentOrchestrator> logger,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider)
    {
        _aiService = aiService;
        _tools = tools;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
        _ = configuration; // reserved for future per-feature toggles
    }

    public async IAsyncEnumerable<AIStreamEventDto> ExecuteAsync(
        string query,
        string userId,
        string systemPrompt,
        List<AIChatMessageDto> history,
        AccessControlContext accessContext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<AIStreamEventDto>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        var runTask = RunAgentAsync(
            query,
            userId,
            systemPrompt,
            history,
            accessContext,
            channel.Writer,
            cancellationToken);

        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }

        await runTask;
    }

    private async Task RunAgentAsync(
        string query,
        string userId,
        string systemPrompt,
        List<AIChatMessageDto> history,
        AccessControlContext accessContext,
        ChannelWriter<AIStreamEventDto> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            var messages = ConvertMessages(history, query);
            var aiFunctions = BuildAIFunctions(accessContext, writer, cancellationToken);

            var agent = new ChatClientBuilder(_aiService.CreateChatClient())
                .UseFunctionInvocation()
                .BuildAIAgent(
                    instructions: systemPrompt,
                    name: "ApplicationAssistant",
                    description: "NIE template assistant with grounded tools.",
                    tools: aiFunctions,
                    loggerFactory: _loggerFactory,
                    services: _serviceProvider);

            var streamed = new StringBuilder();
            var updates = new List<AgentResponseUpdate>();

            var runOptions = new ChatClientAgentRunOptions(new ChatOptions
            {
                Temperature = 0.2f,
                MaxOutputTokens = MaxOutputTokens,
            });

            await foreach (var update in agent.RunStreamingAsync(
                messages,
                options: runOptions,
                cancellationToken: cancellationToken))
            {
                updates.Add(update);
                if (string.IsNullOrEmpty(update.Text))
                {
                    continue;
                }

                streamed.Append(update.Text);
                await writer.WriteAsync(
                    new AIStreamEventDto { Type = AIStreamEventTypes.Message, Content = update.Text },
                    cancellationToken);
            }

            var response = updates.Count > 0 ? updates.ToAgentResponse() : null;
            if (streamed.Length == 0 && response?.Text is { Length: > 0 } finalText)
            {
                await writer.WriteAsync(
                    new AIStreamEventDto { Type = AIStreamEventTypes.Message, Content = finalText },
                    cancellationToken);
            }

            if (response?.Usage is { } usage)
            {
                await writer.WriteAsync(
                    new AIStreamEventDto
                    {
                        Type = AIStreamEventTypes.Metadata,
                        InputTokens = (int?)usage.InputTokenCount,
                        OutputTokens = (int?)usage.OutputTokenCount,
                    },
                    cancellationToken);
            }

            await writer.WriteAsync(
                new AIStreamEventDto
                {
                    Type = AIStreamEventTypes.Stop,
                    StopReason = response?.FinishReason?.ToString() ?? "end_turn",
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Agent execution cancelled for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent execution failed for user {UserId}", userId);
            await writer.WriteAsync(
                new AIStreamEventDto { Type = AIStreamEventTypes.Error, Error = "AI service error." },
                cancellationToken);
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private List<AITool> BuildAIFunctions(
        AccessControlContext accessContext,
        ChannelWriter<AIStreamEventDto> writer,
        CancellationToken cancellationToken)
    {
        var functions = new List<AITool>();
        var toolInvocationCount = 0;

        foreach (var tool in _tools)
        {
            var captured = tool;
            functions.Add(AIFunctionFactory.Create(
                async (string arguments) =>
                {
                    if (Interlocked.Increment(ref toolInvocationCount) > MaxToolInvocationsPerRun)
                    {
                        await writer.WriteAsync(
                            new AIStreamEventDto
                            {
                                Type = AIStreamEventTypes.ToolResult,
                                ToolName = captured.Name,
                                ToolOutput = "Tool invocation limit reached.",
                            },
                            cancellationToken);

                        return "Tool invocation limit reached.";
                    }

                    await writer.WriteAsync(
                        new AIStreamEventDto
                        {
                            Type = AIStreamEventTypes.ToolStart,
                            ToolName = captured.Name,
                            ToolInput = arguments,
                        },
                        cancellationToken);

                    try
                    {
                        var raw = await captured.ExecuteAsync(arguments, accessContext, cancellationToken);
                        var (text, sources) = UnwrapEnvelope(raw);

                        await writer.WriteAsync(
                            new AIStreamEventDto
                            {
                                Type = AIStreamEventTypes.ToolResult,
                                ToolName = captured.Name,
                                ToolOutput = text.Length > 500 ? text[..500] + "..." : text,
                                SourceItems = sources,
                            },
                            cancellationToken);

                        return text;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Tool {Tool} failed", captured.Name);
                        await writer.WriteAsync(
                            new AIStreamEventDto
                            {
                                Type = AIStreamEventTypes.ToolResult,
                                ToolName = captured.Name,
                                ToolOutput = "Tool failed.",
                            },
                            cancellationToken);
                        return "Tool failed.";
                    }
                },
                name: captured.Name,
                description: captured.Description));
        }

        return functions;
    }

    private static (string Text, List<AIChatSourceItemDto>? Sources) UnwrapEnvelope(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (string.Empty, null);
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<AIToolResponseEnvelope>(raw, JsonOptions);
            if (envelope != null &&
                (!string.IsNullOrWhiteSpace(envelope.AssistantText) ||
                 (envelope.SourceItems is { Count: > 0 })))
            {
                return (envelope.AssistantText, envelope.SourceItems);
            }
        }
        catch (JsonException)
        {
            // not envelope-shaped — return raw text
        }

        return (raw, null);
    }

    private static List<ChatMessage> ConvertMessages(IEnumerable<AIChatMessageDto> history, string query)
    {
        var messages = new List<ChatMessage>();

        foreach (var item in history)
        {
            if (string.IsNullOrWhiteSpace(item.Content))
            {
                continue;
            }

            messages.Add(new ChatMessage(new ChatRole(item.Role), item.Content));
        }

        messages.Add(new ChatMessage(ChatRole.User, query));
        return messages;
    }
}
