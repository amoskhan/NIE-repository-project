global using System.Text.Json;

namespace BuildingBlocks.Extensions;

//NOTE: Please use this JsonExtensions throughout 
//for json serialization and deserialization for easier maintenance
public static class JsonExtensions
{
    private static readonly JsonSerializerOptions options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, options);
    }

    public static string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, options);
    }
}
