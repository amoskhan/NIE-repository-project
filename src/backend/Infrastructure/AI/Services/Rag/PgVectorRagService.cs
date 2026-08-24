using AI.Models;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AI;

/// <summary>
/// pgvector-backed RAG service. Embeds the query with
/// <see cref="IEmbeddingService"/>, then orders <see cref="ChatEmbedding"/>
/// rows by cosine distance to return the closest matches.
/// </summary>
public class PgVectorRagService : IPgVectorRagService
{
    private const int DefaultChunkSize = 800;
    private const int ChunkOverlap = 100;

    private readonly DbContext _db;
    private readonly IEmbeddingService _embeddings;
    private readonly ILogger<PgVectorRagService> _logger;

    public PgVectorRagService(
        DbContext db,
        IEmbeddingService embeddings,
        ILogger<PgVectorRagService> logger)
    {
        _db = db;
        _embeddings = embeddings;
        _logger = logger;
    }

    public async Task<List<RagSearchResult>> SearchAsync(
        string query,
        int topK,
        AccessControlContext context,
        string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || topK <= 0)
        {
            return new List<RagSearchResult>();
        }

        var queryVector = await _embeddings.GenerateEmbeddingAsync(query, cancellationToken);

        var rows = _db.Set<ChatEmbedding>().AsNoTracking()
            .Where(e => e.Embedding != null);

        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            rows = rows.Where(e => e.SourceType == sourceType);
        }

        var results = await rows
            .OrderBy(e => e.Embedding!.CosineDistance(queryVector))
            .Take(topK)
            .Select(e => new
            {
                e.Id,
                e.Content,
                e.SourceType,
                e.SourceId,
                e.SourceMetadata,
                Distance = e.Embedding!.CosineDistance(queryVector),
            })
            .ToListAsync(cancellationToken);

        return results.Select(r => new RagSearchResult
        {
            DocumentId = r.Id,
            Content = r.Content,
            ChunkIndex = 0,
            Score = 1.0 - r.Distance,
            Title = r.SourceMetadata,
            SourceType = r.SourceType,
            SourceId = r.SourceId,
        }).ToList();
    }

    public async Task IndexDocumentAsync(
        string sourceType,
        Guid sourceId,
        string content,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var chunks = ChunkText(content, DefaultChunkSize, ChunkOverlap);
        if (chunks.Count == 0)
        {
            return;
        }

        var vectors = await _embeddings.GenerateEmbeddingsAsync(chunks, cancellationToken);

        var existing = await _db.Set<ChatEmbedding>()
            .Where(e => e.SourceType == sourceType && e.SourceId == sourceId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _db.Set<ChatEmbedding>().RemoveRange(existing);
        }

        var metadataLabel = metadata != null && metadata.TryGetValue("title", out var title)
            ? title
            : null;

        for (var i = 0; i < chunks.Count; i++)
        {
            _db.Set<ChatEmbedding>().Add(new ChatEmbedding
            {
                Content = chunks[i],
                Embedding = vectors[i],
                SourceType = sourceType,
                SourceId = sourceId,
                SourceMetadata = metadataLabel,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var trimmed = text.Trim();
        if (trimmed.Length <= chunkSize)
        {
            chunks.Add(trimmed);
            return chunks;
        }

        var step = Math.Max(1, chunkSize - overlap);
        for (var start = 0; start < trimmed.Length; start += step)
        {
            var len = Math.Min(chunkSize, trimmed.Length - start);
            chunks.Add(trimmed.Substring(start, len));
            if (start + len >= trimmed.Length)
            {
                break;
            }
        }

        return chunks;
    }
}
