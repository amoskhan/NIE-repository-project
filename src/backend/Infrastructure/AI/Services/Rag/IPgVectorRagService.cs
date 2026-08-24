namespace AI;

/// <summary>
/// Retrieval-augmented generation backed by PostgreSQL + pgvector.
///
/// The default <see cref="NullPgVectorRagService"/> implementation returns no
/// results so the app builds without a configured embedding model. Replace
/// the registration with a real implementation when wiring up RAG.
/// </summary>
public interface IPgVectorRagService
{
    Task<List<RagSearchResult>> SearchAsync(
        string query,
        int topK,
        AccessControlContext context,
        string? sourceType = null,
        CancellationToken cancellationToken = default);

    Task IndexDocumentAsync(
        string sourceType,
        Guid sourceId,
        string content,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}
