namespace Application.AI.Prompts;

/// <summary>
/// Versioned prompt definition. All system prompts in this codebase use this
/// shape so changes can be tracked and audited over time.
///
/// Versioning:
///   - Major (X.0.0): breaking changes to structure or expected output format
///   - Minor (0.X.0): improvements that don't change expected output
///   - Patch (0.0.X): typo/clarity fixes
/// </summary>
public class PromptDefinition
{
    /// <summary>Unique identifier (e.g. "staff_chatbot_v1").</summary>
    public required string Name { get; init; }

    /// <summary>Semantic version string.</summary>
    public required string Version { get; init; }

    /// <summary>ISO date of last update (YYYY-MM-DD).</summary>
    public required string LastUpdated { get; init; }

    /// <summary>Team or person responsible for this prompt.</summary>
    public required string Author { get; init; }

    /// <summary>System prompt body. Supports the <c>{{current_datetime}}</c> token.</summary>
    public required string SystemPrompt { get; init; }

    /// <summary>Optional notes on purpose, known issues, or change history.</summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Returns the trimmed prompt with runtime tokens (e.g. <c>{{current_datetime}}</c>) substituted.
    /// </summary>
    public string GetPrompt()
    {
        var prompt = SystemPrompt.Trim();
        if (prompt.Contains("{{current_datetime}}"))
        {
            var now = DateTime.UtcNow.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt");
            prompt = prompt.Replace("{{current_datetime}}", now);
        }
        return prompt;
    }

    public override string ToString() => $"{Name}@v{Version}";
}
