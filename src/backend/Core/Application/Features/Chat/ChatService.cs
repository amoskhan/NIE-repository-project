using System.Runtime.CompilerServices;
using Application.Abstractions;
using Application.AI;
using Application.AI.Prompts;
using BuildingBlocks.Helpers;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Chat;

/// <summary>
/// Persists conversation state and bridges to <see cref="IAgentOrchestrator"/>
/// for streaming replies.
/// </summary>
public class ChatService : IChatService
{
    private const int HistoryWindow = 20;
    private const int MaxMessageLength = 4_000;
    private const int MaxTitleLength = 200;
    private const int MaxSourceLength = 50;

    private readonly IApplicationDbContext _context;
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IChatbotRateLimitService _rateLimit;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IApplicationDbContext context,
        IAgentOrchestrator orchestrator,
        IChatbotRateLimitService rateLimit,
        ILogger<ChatService> logger)
    {
        _context = context;
        _orchestrator = orchestrator;
        _rateLimit = rateLimit;
        _logger = logger;
    }

    public async Task<List<ChatConversation>> GetConversationsAsync(
        string userId,
        string? source = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ChatConversation>()
            .Where(c => c.UserId == userId);

        if (!string.IsNullOrWhiteSpace(source))
        {
            var normalizedSource = NormalizeSource(source);
            query = query.Where(c => c.Source == normalizedSource);
        }

        return await query
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatConversation?> GetConversationAsync(
        Guid conversationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ChatConversation>()
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.UserId == userId,
                cancellationToken);
    }

    public async Task<ChatConversation> CreateConversationAsync(
        string userId,
        string title,
        string source,
        CancellationToken cancellationToken = default)
    {
        await _rateLimit.EnsureCanStartConversationAsync(userId, cancellationToken);

        var conversation = new ChatConversation
        {
            UserId = userId,
            Title = NormalizeTitle(title),
            Source = NormalizeSource(source),
            LastMessageAt = DateTimeHelper.Now,
            MessageCount = 0,
        };

        _context.Set<ChatConversation>().Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<bool> DeleteConversationAsync(
        Guid conversationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Set<ChatConversation>()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.UserId == userId,
                cancellationToken);

        if (conversation is null)
        {
            return false;
        }

        _context.Set<ChatMessage>().RemoveRange(conversation.Messages);
        _context.Set<ChatConversation>().Remove(conversation);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RenameConversationAsync(
        Guid conversationId,
        string userId,
        string newTitle,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetConversationAsync(conversationId, userId, cancellationToken);
        if (conversation is null)
        {
            return false;
        }

        conversation.Title = NormalizeTitle(newTitle);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(
        Guid conversationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ChatMessage>()
            .Where(m => m.ConversationId == conversationId && m.Conversation.UserId == userId)
            .OrderBy(m => m.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessage> SendMessageAsync(
        Guid conversationId,
        string userId,
        string content,
        CancellationToken cancellationToken = default)
    {
        await foreach (var _ in StreamResponseAsync(conversationId, userId, content, cancellationToken))
        {
            // Drain the stream so StreamResponseAsync can persist the assistant reply.
        }

        return await _context.Set<ChatMessage>()
            .Where(m =>
                m.ConversationId == conversationId &&
                m.Conversation.UserId == userId &&
                m.Role == "assistant")
            .OrderByDescending(m => m.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The assistant did not produce a response.");
    }

    public async Task SubmitFeedbackAsync(
        Guid messageId,
        string userId,
        string type,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var messageExists = await _context.Set<ChatMessage>()
            .AnyAsync(
                message => message.Id == messageId &&
                    message.Conversation.UserId == userId &&
                    message.Role == "assistant",
                cancellationToken);

        if (!messageExists)
        {
            return;
        }

        // Hook point: wire to a MessageFeedback table when the schema is added.
        _logger.LogInformation(
            "Chat feedback received: messageId={MessageId} type={Type} hasComment={HasComment}",
            messageId,
            type,
            !string.IsNullOrWhiteSpace(comment));
    }

    public async IAsyncEnumerable<AIStreamEventDto> StreamResponseAsync(
        Guid conversationId,
        string userId,
        string content,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var normalizedContent = NormalizeContent(content);
        await _rateLimit.EnsureCanSendMessageAsync(userId, cancellationToken);

        var conversation = await GetConversationAsync(conversationId, userId, cancellationToken)
            ?? throw new KeyNotFoundException("Conversation not found.");

        var userMessage = new ChatMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = normalizedContent,
            TokenCount = normalizedContent.Length / 4,
        };
        _context.Set<ChatMessage>().Add(userMessage);
        await _context.SaveChangesAsync(cancellationToken);

        var history = await _context.Set<ChatMessage>()
            .Where(m => m.ConversationId == conversationId && m.Id != userMessage.Id)
            .OrderByDescending(m => m.CreatedOn)
            .Take(HistoryWindow)
            .OrderBy(m => m.CreatedOn)
            .Select(m => new AIChatMessageDto { Role = m.Role, Content = m.Content })
            .ToListAsync(cancellationToken);

        var systemPrompt = PromptBuilder.BuildStaffPrompt(
            userContext: $"User ID: {userId}",
            conversationContext: $"Source: {conversation.Source}");

        var accessContext = new AccessControlContext
        {
            UserId = userId,
            UserType = "staff",
        };

        var assistantText = new System.Text.StringBuilder();
        var inputTokens = 0;
        var outputTokens = 0;

        await foreach (var evt in _orchestrator.ExecuteAsync(
            normalizedContent,
            userId,
            systemPrompt,
            history,
            accessContext,
            cancellationToken))
        {
            if (evt.Type == AIStreamEventTypes.Message && !string.IsNullOrEmpty(evt.Content))
            {
                assistantText.Append(evt.Content);
            }
            else if (evt.Type == AIStreamEventTypes.Metadata)
            {
                inputTokens += evt.InputTokens ?? 0;
                outputTokens += evt.OutputTokens ?? 0;
            }

            yield return evt;
        }

        if (assistantText.Length > 0)
        {
            _context.Set<ChatMessage>().Add(new ChatMessage
            {
                ConversationId = conversationId,
                Role = "assistant",
                Content = assistantText.ToString(),
                TokenCount = outputTokens > 0 ? outputTokens : assistantText.Length / 4,
            });
        }

        conversation.LastMessageAt = DateTimeHelper.Now;
        conversation.MessageCount += assistantText.Length > 0 ? 2 : 1;

        await _context.SaveChangesAsync(cancellationToken);
        await _rateLimit.RecordUsageAsync(userId, inputTokens, outputTokens, cancellationToken);
    }

    private static string NormalizeTitle(string? value)
    {
        var title = CollapseWhitespace(value);
        if (string.IsNullOrWhiteSpace(title))
        {
            return "New Conversation";
        }

        return title.Length > MaxTitleLength ? title[..MaxTitleLength] : title;
    }

    private static string NormalizeSource(string? value)
    {
        var source = CollapseWhitespace(value);
        if (string.IsNullOrWhiteSpace(source))
        {
            return "General";
        }

        return source.Length > MaxSourceLength ? source[..MaxSourceLength] : source;
    }

    private static string NormalizeContent(string? value)
    {
        var content = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(value));
        }

        if (content.Length > MaxMessageLength)
        {
            throw new ArgumentException($"Message exceeds maximum length of {MaxMessageLength} characters.", nameof(value));
        }

        return content;
    }

    private static string CollapseWhitespace(string? value)
    {
        return string.Join(
            " ",
            (value ?? string.Empty).Split(
                Array.Empty<char>(),
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
