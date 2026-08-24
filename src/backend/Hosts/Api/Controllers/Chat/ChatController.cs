using System.Text;
using System.Text.Json;
using Api.Authorization;
using Application.AI;
using Application.Features.Chat;
using Application.Security;
using BuildingBlocks.Helpers;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : BaseController
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatService _chatService;
    private readonly IChatbotRateLimitService _rateLimit;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IChatbotRateLimitService rateLimit,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _rateLimit = rateLimit;
        _logger = logger;
    }

    /// <summary>List all conversations for the current user.</summary>
    [HttpGet("conversations")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> GetConversations(
        [FromQuery] string? source,
        CancellationToken cancellationToken)
    {
        var conversations = await _chatService.GetConversationsAsync(CurrentUserId, source, cancellationToken);
        return Ok(conversations.Select(ToConversationResponse));
    }

    /// <summary>Get a conversation owned by the current user.</summary>
    [HttpGet("conversations/{id:guid}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _chatService.GetConversationAsync(id, CurrentUserId, cancellationToken);
        return conversation is null ? NotFound() : Ok(ToConversationResponse(conversation));
    }

    /// <summary>Get messages for a conversation owned by the current user.</summary>
    [HttpGet("conversations/{id:guid}/messages")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _chatService.GetConversationAsync(id, CurrentUserId, cancellationToken);
        if (conversation is null)
        {
            return NotFound();
        }

        var messages = await _chatService.GetMessagesAsync(id, CurrentUserId, cancellationToken);
        return Ok(messages.Select(ToMessageResponse));
    }

    /// <summary>Create a new conversation.</summary>
    [HttpPost("conversations")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var conv = await _chatService.CreateConversationAsync(
            CurrentUserId,
            request.Title ?? "New Conversation",
            request.Source ?? "General",
            cancellationToken);

        return CreatedAtAction(nameof(GetConversation), new { id = conv.Id }, ToConversationResponse(conv));
    }

    /// <summary>Rename a conversation owned by the current user.</summary>
    [HttpPost("conversations/{id:guid}/rename")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> RenameConversation(
        Guid id,
        [FromBody] RenameConversationRequest request,
        CancellationToken cancellationToken)
    {
        var renamed = await _chatService.RenameConversationAsync(id, CurrentUserId, request.Title, cancellationToken);
        return renamed ? NoContent() : NotFound();
    }

    /// <summary>Delete a conversation owned by the current user.</summary>
    [HttpDelete("conversations/{id:guid}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _chatService.DeleteConversationAsync(id, CurrentUserId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Submit thumbs-up / thumbs-down feedback on an assistant message.</summary>
    [HttpPost("messages/{id:guid}/feedback")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> SubmitFeedback(
        Guid id,
        [FromBody] FeedbackRequest request,
        CancellationToken cancellationToken)
    {
        await _chatService.SubmitFeedbackAsync(id, CurrentUserId, request.Type, request.Comment, cancellationToken);
        return NoContent();
    }

    /// <summary>Read the current user's quota status (conversations/tokens/retention).</summary>
    [HttpGet("quota")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> GetQuota(CancellationToken cancellationToken)
    {
        var status = await _rateLimit.GetStatusAsync(CurrentUserId, cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Stream the assistant reply as Server-Sent Events. Emits named events
    /// (message / tool_start / tool_result / metadata / stop / error / done).
    /// </summary>
    [HttpPost("conversations/{id:guid}/send")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    [Produces("text/event-stream")]
    public async Task SendMessage(
        Guid id,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var evt in _chatService.StreamResponseAsync(
                id,
                CurrentUserId,
                request.Content,
                cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested) break;
                await WriteSseAsync(evt, cancellationToken);
            }

            await WriteSseAsync(
                new AIStreamEventDto { Type = AIStreamEventTypes.Done },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Chat stream cancelled for user {UserId}, conversation {ConversationId}", CurrentUserId, id);
        }
        catch (ArgumentException ex)
        {
            await WriteSseAsync(
                new AIStreamEventDto { Type = AIStreamEventTypes.Error, Error = ex.Message },
                cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            await WriteSseAsync(
                new AIStreamEventDto { Type = AIStreamEventTypes.Error, Error = "Conversation not found." },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat stream failed for user {UserId}, conversation {ConversationId}", CurrentUserId, id);
            await WriteSseAsync(
                new AIStreamEventDto { Type = AIStreamEventTypes.Error, Error = "The assistant could not respond right now." },
                cancellationToken);
        }
    }

    /// <summary>Semantic search via embeddings (placeholder).</summary>
    [HttpGet("search")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public IActionResult Search([FromQuery] string q)
    {
        return Ok(new
        {
            query = q,
            results = Array.Empty<object>(),
            message = "pgvector semantic search endpoint",
        });
    }

    private string CurrentUserId => UserId ?? "unknown";

    private async Task WriteSseAsync(AIStreamEventDto evt, CancellationToken cancellationToken)
    {
        var payload = EncodeSseData(SerializePayload(evt));
        var frame = $"event: {evt.Type}\ndata: {payload}\n\n";
        await Response.WriteAsync(frame, Encoding.UTF8, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static string SerializePayload(AIStreamEventDto evt)
    {
        return evt.Type switch
        {
            "message" => evt.Content ?? string.Empty,
            "error" => evt.Error ?? string.Empty,
            _ => JsonSerializer.Serialize(evt, JsonOptions),
        };
    }

    private static string EncodeSseData(string value)
    {
        return value.Replace("\r", "\\r").Replace("\n", "\\n");
    }

    private static ChatConversationResponse ToConversationResponse(ChatConversation conversation)
    {
        return new ChatConversationResponse(
            conversation.Id,
            conversation.Title,
            conversation.Source,
            ToResponseDateTime(conversation.LastMessageAt),
            conversation.MessageCount);
    }

    private static ChatMessageResponse ToMessageResponse(ChatMessage message)
    {
        return new ChatMessageResponse(
            message.Id,
            message.Role,
            message.Content,
            ToResponseDateTime(message.CreatedOn ?? message.UpdatedOn ?? DateTimeHelper.Now),
            message.ConversationId,
            message.TokenCount);
    }

    private static DateTimeOffset ToResponseDateTime(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return new DateTimeOffset(value);
        }

        var localValue = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        return new DateTimeOffset(localValue, DateTimeHelper.SingaporeTimeZone.GetUtcOffset(localValue));
    }
}

/// <summary>Conversation summary returned to the chat UI.</summary>
public sealed record ChatConversationResponse(
    Guid Id,
    string Title,
    string Source,
    DateTimeOffset LastMessageAt,
    int MessageCount);

/// <summary>Chat message returned to the chat UI.</summary>
public sealed record ChatMessageResponse(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    Guid ConversationId,
    int? TokenCount);

public sealed record CreateConversationRequest
{
    public string? Title { get; init; } = "New Conversation";

    public string? Source { get; init; } = "General";
}

public sealed record RenameConversationRequest
{
    public string Title { get; init; } = string.Empty;
}

public sealed record FeedbackRequest
{
    public string Type { get; init; } = string.Empty;

    public string? Comment { get; init; }
}

public sealed record SendMessageRequest
{
    public string Content { get; init; } = string.Empty;
}
