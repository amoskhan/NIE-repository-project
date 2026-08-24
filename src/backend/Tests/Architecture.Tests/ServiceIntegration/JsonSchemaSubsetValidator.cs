using System.Text.Json;
using System.Text.RegularExpressions;

namespace Architecture.Tests;

/// <summary>
/// Dependency-free evaluator for the deliberately small JSON Schema vocabulary used by
/// integration-manifest.schema.json. Unknown schema keywords fail closed so the test cannot
/// silently under-validate if the published schema evolves.
/// </summary>
internal static class JsonSchemaSubsetValidator
{
    private static readonly HashSet<string> SupportedKeywords = new(StringComparer.Ordinal)
    {
        "$defs",
        "$id",
        "$ref",
        "$schema",
        "additionalProperties",
        "const",
        "items",
        "minimum",
        "minItems",
        "minLength",
        "pattern",
        "properties",
        "required",
        "title",
        "type",
        "uniqueItems",
    };

    public static IReadOnlyList<string> Validate(JsonElement rootSchema, JsonElement instance)
    {
        var errors = new List<string>();
        ValidateNode(rootSchema, rootSchema, instance, "$", errors);
        return errors;
    }

    private static void ValidateNode(
        JsonElement rootSchema,
        JsonElement schema,
        JsonElement instance,
        string instancePath,
        ICollection<string> errors)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"The schema at {instancePath} must be an object.");
        }

        foreach (var keyword in schema.EnumerateObject())
        {
            if (!SupportedKeywords.Contains(keyword.Name))
            {
                throw new InvalidDataException(
                    $"Unsupported JSON Schema keyword '{keyword.Name}' at {instancePath}; extend the fail-closed test validator before using it.");
            }
        }

        if (schema.TryGetProperty("$ref", out var reference))
        {
            ValidateNode(
                rootSchema,
                ResolveLocalReference(rootSchema, reference.GetString()),
                instance,
                instancePath,
                errors);
        }

        if (schema.TryGetProperty("type", out var expectedType)
            && !HasExpectedType(instance, expectedType.GetString()))
        {
            errors.Add($"{instancePath}: expected type '{expectedType.GetString()}'.");
            return;
        }

        if (schema.TryGetProperty("const", out var constant)
            && !JsonElement.DeepEquals(constant, instance))
        {
            errors.Add($"{instancePath}: value does not equal the declared constant.");
        }

        switch (instance.ValueKind)
        {
            case JsonValueKind.Object:
                ValidateObject(rootSchema, schema, instance, instancePath, errors);
                break;
            case JsonValueKind.Array:
                ValidateArray(rootSchema, schema, instance, instancePath, errors);
                break;
            case JsonValueKind.String:
                ValidateString(schema, instance, instancePath, errors);
                break;
            case JsonValueKind.Number:
                ValidateNumber(schema, instance, instancePath, errors);
                break;
        }
    }

    private static void ValidateObject(
        JsonElement rootSchema,
        JsonElement schema,
        JsonElement instance,
        string instancePath,
        ICollection<string> errors)
    {
        if (schema.TryGetProperty("required", out var required))
        {
            foreach (var requiredProperty in required.EnumerateArray())
            {
                var propertyName = requiredProperty.GetString()
                    ?? throw new InvalidDataException("A required property name cannot be null.");
                if (!instance.TryGetProperty(propertyName, out _))
                {
                    errors.Add($"{instancePath}: required property '{propertyName}' is missing.");
                }
            }
        }

        var hasProperties = schema.TryGetProperty("properties", out var properties);
        foreach (var property in instance.EnumerateObject())
        {
            if (hasProperties && properties.TryGetProperty(property.Name, out var propertySchema))
            {
                ValidateNode(
                    rootSchema,
                    propertySchema,
                    property.Value,
                    $"{instancePath}/{property.Name}",
                    errors);
                continue;
            }

            if (schema.TryGetProperty("additionalProperties", out var additionalProperties)
                && additionalProperties.ValueKind == JsonValueKind.False)
            {
                errors.Add($"{instancePath}: additional property '{property.Name}' is not allowed.");
            }
        }
    }

    private static void ValidateArray(
        JsonElement rootSchema,
        JsonElement schema,
        JsonElement instance,
        string instancePath,
        ICollection<string> errors)
    {
        var items = instance.EnumerateArray().ToArray();
        if (schema.TryGetProperty("minItems", out var minimumItems)
            && items.Length < minimumItems.GetInt32())
        {
            errors.Add($"{instancePath}: requires at least {minimumItems.GetInt32()} items.");
        }

        if (schema.TryGetProperty("uniqueItems", out var uniqueItems)
            && uniqueItems.ValueKind == JsonValueKind.True)
        {
            for (var left = 0; left < items.Length; left++)
            {
                for (var right = left + 1; right < items.Length; right++)
                {
                    if (JsonElement.DeepEquals(items[left], items[right]))
                    {
                        errors.Add($"{instancePath}: items {left} and {right} must be unique.");
                    }
                }
            }
        }

        if (schema.TryGetProperty("items", out var itemSchema))
        {
            for (var index = 0; index < items.Length; index++)
            {
                ValidateNode(rootSchema, itemSchema, items[index], $"{instancePath}/{index}", errors);
            }
        }
    }

    private static void ValidateString(
        JsonElement schema,
        JsonElement instance,
        string instancePath,
        ICollection<string> errors)
    {
        var value = instance.GetString() ?? string.Empty;
        if (schema.TryGetProperty("minLength", out var minimumLength)
            && value.Length < minimumLength.GetInt32())
        {
            errors.Add($"{instancePath}: string is shorter than {minimumLength.GetInt32()} characters.");
        }

        if (schema.TryGetProperty("pattern", out var pattern))
        {
            var expression = new Regex(
                pattern.GetString() ?? throw new InvalidDataException("A schema pattern cannot be null."),
                RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
                TimeSpan.FromSeconds(1));
            if (!expression.IsMatch(value))
            {
                errors.Add($"{instancePath}: string does not match the declared pattern.");
            }
        }
    }

    private static void ValidateNumber(
        JsonElement schema,
        JsonElement instance,
        string instancePath,
        ICollection<string> errors)
    {
        if (schema.TryGetProperty("minimum", out var minimum)
            && instance.GetDecimal() < minimum.GetDecimal())
        {
            errors.Add($"{instancePath}: number is less than {minimum.GetDecimal()}.");
        }
    }

    private static bool HasExpectedType(JsonElement instance, string? expectedType) => expectedType switch
    {
        "array" => instance.ValueKind == JsonValueKind.Array,
        "integer" => instance.ValueKind == JsonValueKind.Number && instance.TryGetInt64(out _),
        "object" => instance.ValueKind == JsonValueKind.Object,
        "string" => instance.ValueKind == JsonValueKind.String,
        _ => throw new InvalidDataException($"Unsupported JSON Schema type '{expectedType}'."),
    };

    private static JsonElement ResolveLocalReference(JsonElement rootSchema, string? reference)
    {
        if (reference is null || !reference.StartsWith("#/", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Only local JSON Schema references are supported; found '{reference}'.");
        }

        var current = rootSchema;
        foreach (var encodedSegment in reference[2..].Split('/'))
        {
            var segment = encodedSegment.Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
            {
                throw new InvalidDataException($"JSON Schema reference '{reference}' cannot be resolved.");
            }
        }

        return current;
    }
}
