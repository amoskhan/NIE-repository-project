namespace AppTemplate.AI;

/// <summary>
/// A single chat turn passed to the model.
/// </summary>
public class AIChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// Tool definition surfaced to the model for function-calling.
/// </summary>
public class AIToolDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object InputSchema { get; set; } = new { };
}

/// <summary>
/// Non-streaming chat response.
/// </summary>
public class AIChatResponseDto
{
    public string? Content { get; set; }
    public string? StopReason { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}

/// <summary>
/// A retrieval source surfaced to the user (e.g. a CMS page, document, etc.).
/// </summary>
public class AIChatSourceItemDto
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Excerpt { get; set; }
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }
}

/// <summary>
/// A single event emitted by <see cref="IAgentOrchestrator"/> while a chat is
/// being generated. Serialized to SSE on the wire.
/// </summary>
public class AIStreamEventDto
{
    /// <summary>
    /// One of: session, message, tool_start, tool_result, metadata, stop, done, error.
    /// </summary>
    public string Type { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Error { get; set; }
    public string? ToolName { get; set; }
    public string? ToolInput { get; set; }
    public string? ToolOutput { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public string? StopReason { get; set; }
    public string? ConversationId { get; set; }
    public string? MessageId { get; set; }
    public List<AIChatSourceItemDto>? SourceItems { get; set; }
}

/// <summary>
/// Access control context passed to tools and RAG queries so they can scope
/// results to what the calling user is allowed to see.
/// </summary>
public class AccessControlContext
{
    public string UserType { get; set; } = "staff";
    public string UserId { get; set; } = string.Empty;
    public bool IsSystemAdmin { get; set; }
    public List<string>? RoleIds { get; set; }
    public List<string>? PermissionCodes { get; set; }
    public int? DepartmentId { get; set; }
    public List<int>? DepartmentIds { get; set; }
}

/// <summary>
/// Result row returned by the pgvector RAG service.
/// </summary>
public class RagSearchResult
{
    public int DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public double Score { get; set; }
    public string? Title { get; set; }
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }
}

/// <summary>
/// Optional envelope a tool can return to attach grounded source items to the
/// assistant's reply (the orchestrator unwraps this transparently).
/// </summary>
public class AIToolResponseEnvelope
{
    public string AssistantText { get; set; } = string.Empty;
    public List<AIChatSourceItemDto> SourceItems { get; set; } = new();
}
