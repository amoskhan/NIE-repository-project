using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AI;

/// <summary>
/// Generic retrieval tool. The model invokes this when it needs to ground
/// an answer in indexed content. Returns an
/// <see cref="AIToolResponseEnvelope"/> so source items render in the UI.
/// </summary>
public class RagSearchTool : IAITool
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPgVectorRagService _rag;
    private readonly ILogger<RagSearchTool> _logger;

    public RagSearchTool(IPgVectorRagService rag, ILogger<RagSearchTool> logger)
    {
        _rag = rag;
        _logger = logger;
    }

    public string Name => "knowledge_search";

    public string Description =>
        "Search the system's indexed knowledge base (documents, policies, FAQs) " +
        "for content relevant to the user's question. Use this whenever the user " +
        "asks a factual question that may be answered by stored content.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            query = new
            {
                type = "string",
                description = "Natural-language search query. Be specific.",
            },
            topK = new
            {
                type = "integer",
                description = "Number of results to return (1-10, default 5).",
            },
            sourceType = new
            {
                type = "string",
                description = "Optional source-type filter (e.g. \"PurchaseOrder\", \"Vendor\").",
            },
        },
        required = new[] { "query" },
    };

    public async Task<string> ExecuteAsync(
        string arguments,
        AccessControlContext context,
        CancellationToken cancellationToken = default)
    {
        var input = ParseArgs(arguments);
        if (string.IsNullOrWhiteSpace(input.Query))
        {
            return BuildEmpty("Query is required.");
        }

        var topK = Math.Clamp(input.TopK ?? 5, 1, 10);

        try
        {
            var results = await _rag.SearchAsync(
                input.Query,
                topK,
                context,
                input.SourceType,
                cancellationToken);

            if (results.Count == 0)
            {
                return BuildEmpty("No matching content was found in the knowledge base.");
            }

            var assistantText = new StringBuilder();
            assistantText.AppendLine("Knowledge base results:");
            foreach (var r in results)
            {
                var snippet = r.Content.Length > 320 ? r.Content[..320] + "..." : r.Content;
                assistantText.AppendLine(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"- [{r.SourceType ?? "doc"}#{r.SourceId}] (score {r.Score:0.00}) {snippet}");
            }

            var envelope = new AIToolResponseEnvelope
            {
                AssistantText = assistantText.ToString(),
                SourceItems = results.Select(r => new AIChatSourceItemDto
                {
                    Title = r.Title ?? $"{r.SourceType} #{r.SourceId}",
                    Excerpt = r.Content.Length > 200 ? r.Content[..200] + "..." : r.Content,
                    SourceType = r.SourceType,
                    SourceId = r.SourceId,
                }).ToList(),
            };

            return JsonSerializer.Serialize(envelope, JsonOptions);
        }
        catch (InvalidOperationException ex)
        {
            // Embedding service not configured → degrade gracefully.
            _logger.LogWarning(ex, "RAG search unavailable");
            return BuildEmpty(
                "The knowledge base is not available right now (embedding service is not configured).");
        }
    }

    private static SearchArgs ParseArgs(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return new SearchArgs();
        }

        try
        {
            return JsonSerializer.Deserialize<SearchArgs>(arguments, JsonOptions) ?? new SearchArgs();
        }
        catch (JsonException)
        {
            return new SearchArgs { Query = arguments };
        }
    }

    private static string BuildEmpty(string message) =>
        JsonSerializer.Serialize(
            new AIToolResponseEnvelope { AssistantText = message },
            JsonOptions);

    private class SearchArgs
    {
        public string? Query { get; set; }
        public int? TopK { get; set; }
        public string? SourceType { get; set; }
    }
}
