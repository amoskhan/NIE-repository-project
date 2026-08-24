using System.Text.Json;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AI;

/// <summary>
/// Concrete tool demo: lets the model search active vendors by name, code,
/// or category. Useful as a template when adding more system-data tools.
/// </summary>
public class VendorSearchTool : IAITool
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DbContext _db;

    public VendorSearchTool(DbContext db)
    {
        _db = db;
    }

    public string Name => "search_vendors";

    public string Description =>
        "Search the procurement system's active vendor list by name, code, or category. " +
        "Returns up to 10 matching vendors with contact details.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            keyword = new
            {
                type = "string",
                description = "Free-text search across vendor name, code, and category.",
            },
            includeInactive = new
            {
                type = "boolean",
                description = "Include inactive vendors. Defaults to false.",
            },
        },
        required = new[] { "keyword" },
    };

    public async Task<string> ExecuteAsync(
        string arguments,
        AccessControlContext context,
        CancellationToken cancellationToken = default)
    {
        var input = ParseArgs(arguments);
        if (string.IsNullOrWhiteSpace(input.Keyword))
        {
            return BuildEmpty("Please specify a keyword.");
        }

        var keyword = input.Keyword.Trim();
        var includeInactive = input.IncludeInactive ?? false;

        var query = _db.Set<Vendor>().AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(v => v.IsActive);
        }

        var lowered = keyword.ToLowerInvariant();
        query = query.Where(v =>
            v.Name.ToLower().Contains(lowered) ||
            v.Code.ToLower().Contains(lowered) ||
            (v.Category != null && v.Category.ToLower().Contains(lowered)));

        var matches = await query
            .OrderBy(v => v.Name)
            .Take(10)
            .Select(v => new
            {
                v.Id,
                v.Name,
                v.Code,
                v.Category,
                v.ContactPerson,
                v.Email,
                v.IsActive,
            })
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
        {
            return BuildEmpty($"No vendors matched '{keyword}'.");
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"Found {matches.Count} vendor(s) for '{keyword}':");
        foreach (var v in matches)
        {
            sb.AppendLine(
                System.Globalization.CultureInfo.InvariantCulture,
                $"- {v.Name} ({v.Code}){(v.Category is null ? "" : $" — {v.Category}")}{(v.IsActive ? "" : " [INACTIVE]")}");
        }

        var envelope = new AIToolResponseEnvelope
        {
            AssistantText = sb.ToString(),
            SourceItems = matches.Select(v => new AIChatSourceItemDto
            {
                Title = $"{v.Name} ({v.Code})",
                Excerpt = $"{v.Category ?? "Vendor"} — {v.ContactPerson ?? "no contact"} — {v.Email ?? "no email"}",
                SourceType = "Vendor",
                SourceId = v.Id,
            }).ToList(),
        };

        return JsonSerializer.Serialize(envelope, JsonOptions);
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
            return new SearchArgs { Keyword = arguments };
        }
    }

    private static string BuildEmpty(string message) =>
        JsonSerializer.Serialize(
            new AIToolResponseEnvelope { AssistantText = message },
            JsonOptions);

    private class SearchArgs
    {
        public string? Keyword { get; set; }
        public bool? IncludeInactive { get; set; }
    }
}
