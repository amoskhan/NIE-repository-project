using System.Text.Json;
using Contracts.Integration;

namespace Application.Integration;

/// <summary>
/// Stable JSON settings for integration contracts.
/// </summary>
public static class IntegrationJsonSerializer
{
    public static JsonSerializerOptions Options => IntegrationJsonOptions.Default;
}
