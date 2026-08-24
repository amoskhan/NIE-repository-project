using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contracts.Integration;

/// <summary>
/// Canonical serializer settings shared by event producers and consumers.
/// </summary>
public static class IntegrationJsonOptions
{
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };
}
