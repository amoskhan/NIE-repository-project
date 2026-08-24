using Pgvector;

namespace AI;

/// <summary>
/// Produces embedding vectors for arbitrary text. The default implementation
/// uses Azure OpenAI; throws clearly when credentials are missing.
/// </summary>
public interface IEmbeddingService
{
    Task<Vector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    Task<List<Vector>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
