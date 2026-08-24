using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace AI;

/// <summary>
/// Azure OpenAI-backed embedding generator. The client is constructed lazily
/// so DI resolution succeeds even when credentials are missing — calls then
/// throw a clear <see cref="InvalidOperationException"/> the tool layer can
/// catch and surface as a graceful empty result.
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private const int BatchSize = 16;

    private readonly IConfiguration _configuration;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly Lazy<IEmbeddingGenerator<string, Embedding<float>>> _generator;

    public EmbeddingService(
        IConfiguration configuration,
        ILogger<EmbeddingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _generator = new Lazy<IEmbeddingGenerator<string, Embedding<float>>>(BuildGenerator);
    }

    public async Task<Vector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embedding = await _generator.Value.GenerateAsync(text, options: null, cancellationToken);
        return new Vector(embedding.Vector.ToArray());
    }

    public async Task<List<Vector>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var results = new List<Vector>(texts.Count);

        for (int i = 0; i < texts.Count; i += BatchSize)
        {
            var batch = texts.Skip(i).Take(BatchSize).ToList();
            var response = await _generator.Value.GenerateAsync(batch, options: null, cancellationToken);

            foreach (var embedding in response)
            {
                results.Add(new Vector(embedding.Vector.ToArray()));
            }

            _logger.LogDebug(
                "Embedding batch {Done}/{Total}",
                Math.Min(i + BatchSize, texts.Count),
                texts.Count);
        }

        return results;
    }

    private IEmbeddingGenerator<string, Embedding<float>> BuildGenerator()
    {
        if (!AzureOpenAIConfiguration.IsConfigured(_configuration))
        {
            throw new InvalidOperationException(
                AzureOpenAIConfiguration.MissingEmbeddingCredentialsMessage);
        }

        var endpoint = AzureOpenAIConfiguration.GetEndpoint(_configuration)!;
        var apiKey = AzureOpenAIConfiguration.GetApiKey(_configuration)!;
        var deployment = AzureOpenAIConfiguration.GetEmbeddingDeployment(_configuration);

        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        return azureClient.GetEmbeddingClient(deployment).AsIEmbeddingGenerator();
    }
}
