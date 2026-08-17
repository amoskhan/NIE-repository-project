using Domain.Models;
using AppTemplate.AI;

namespace Services.Services.Chat;

/// <summary>
/// Embedding service for semantic search using pgvector.
/// </summary>
public interface IChatEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<List<ChatEmbedding>> SearchSimilarAsync(string query, int topK = 5, string? sourceType = null);
    Task StoreEmbeddingsAsync(string sourceType, int sourceId, string content, int chunkSize = 500);
}
