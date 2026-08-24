using Microsoft.Extensions.Configuration;

namespace AI;

/// <summary>
/// Reads Azure OpenAI settings from configuration without ever logging values.
/// All keys are looked up from <see cref="IConfiguration"/> — never hard-coded.
/// Configure via environment variables, user-secrets, Key Vault, or appsettings.
/// </summary>
public static class AzureOpenAIConfiguration
{
    public const string MissingChatCredentialsMessage =
        "AzureOpenAI:Endpoint and AzureOpenAI:ApiKey must be configured. " +
        "Set them via environment, user-secrets, or appsettings — do not commit them.";

    public const string MissingEmbeddingCredentialsMessage =
        "AzureOpenAI:Endpoint and AzureOpenAI:ApiKey (or DefaultAzureCredential) " +
        "must be configured for embeddings.";

    public static string? GetEndpoint(IConfiguration configuration) => GetSetting(
        configuration,
        "AzureOpenAI:Endpoint",
        "AiSettings:Endpoint");

    public static string? GetApiKey(IConfiguration configuration) => GetSetting(
        configuration,
        "AzureOpenAI:ApiKey",
        "AiSettings:ApiKey");

    public static string GetChatDeployment(IConfiguration configuration) => GetSetting(
        configuration,
        "AzureOpenAI:ChatDeployment",
        "AzureOpenAI:DeploymentName",
        "AiSettings:ChatDeploymentName")
        ?? "gpt-4o";

    public static string GetEmbeddingDeployment(IConfiguration configuration) => GetSetting(
        configuration,
        "AzureOpenAI:EmbeddingDeployment",
        "AzureOpenAI:EmbeddingDeploymentName",
        "AiSettings:EmbeddingDeploymentName")
        ?? "text-embedding-3-small";

    public static bool IsConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(GetEndpoint(configuration)) &&
        !string.IsNullOrWhiteSpace(GetApiKey(configuration));

    private static string? GetSetting(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key]?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
