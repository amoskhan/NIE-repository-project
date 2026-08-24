using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Models;

public class ChatConversation : TimestampedEntity
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "New Conversation";

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = default!;

    /// <summary>
    /// Source context (e.g., "Procurement", "General", "Documents").
    /// </summary>
    [MaxLength(50)]
    public string Source { get; set; } = "General";

    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public int MessageCount { get; set; }

    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
