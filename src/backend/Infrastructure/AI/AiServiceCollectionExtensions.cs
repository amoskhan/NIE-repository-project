using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AI;

/// <summary>
/// DI helpers for the AI library. Call <see cref="AddAiInfrastructure"/> from
/// <c>Program.cs</c> to register the orchestrator, OpenAI client, prompts,
/// rate limiter, and an empty <see cref="IAITool"/> set.
/// </summary>
public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
        services.AddSingleton<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        services.AddSingleton<IChatbotRateLimitService, InMemoryChatbotRateLimitService>();
        services.AddScoped<IPgVectorRagService, PgVectorRagService>();

        // Default tool set — knowledge base + vendor search. Add more via AddAiTool<T>().
        services.AddAiTool<RagSearchTool>();
        services.AddAiTool<VendorSearchTool>();

        return services;
    }

    /// <summary>
    /// Register a custom <see cref="IAITool"/> implementation. Tools are
    /// resolved as an <c>IEnumerable&lt;IAITool&gt;</c> by the orchestrator.
    /// </summary>
    public static IServiceCollection AddAiTool<TTool>(this IServiceCollection services)
        where TTool : class, IAITool
    {
        services.AddScoped<IAITool, TTool>();
        return services;
    }
}
