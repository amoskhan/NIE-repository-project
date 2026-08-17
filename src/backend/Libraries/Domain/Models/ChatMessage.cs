using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models;
using Pgvector;

namespace Domain.Models;

/// <summary>
/// Individual chat message within a conversation.
/// </summary>
public class ChatMessage : TimestampedEntity
{
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = default!; // "user", "assistant", "system"

    [Required]
    public string Content { get; set; } = default!;

    public int? TokenCount { get; set; }

    /// <summary>
    /// JSON metadata: model used, latency, source documents cited, etc.
    /// </summary>
    public string? Metadata { get; set; }

    public int ConversationId { get; set; }
    public virtual ChatConversation Conversation { get; set; } = default!;
}
