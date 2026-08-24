using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace AI.Models;

/// <summary>
/// Vector embedding for semantic search / RAG.
/// Requires pgvector extension enabled on PostgreSQL.
/// </summary>
public class ChatEmbedding : Domain.Models.TimestampedEntity
{
    /// <summary>
    /// Source document chunk text.
    /// </summary>
    [Required]
    public string Content { get; set; } = default!;

    /// <summary>
    /// Embedding vector backed by pgvector. Dimension matches the configured
    /// embedding model (1536 for text-embedding-3-small, 3072 for -3-large).
    /// </summary>
    [Column(TypeName = "vector(1536)")]
    public Vector? Embedding { get; set; }

    /// <summary>
    /// Source metadata (document name, page, chunk index).
    /// </summary>
    [MaxLength(500)]
    public string? SourceMetadata { get; set; }

    /// <summary>
    /// Source type (e.g., "PurchaseOrder", "Vendor", "CatalogItem").
    /// </summary>
    [MaxLength(100)]
    public string SourceType { get; set; } = default!;

    public Guid SourceId { get; set; }
}
