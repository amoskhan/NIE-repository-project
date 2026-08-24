using System.Text;

namespace Application.AI.Prompts;

/// <summary>
/// Composes a final system prompt from a base <see cref="PromptDefinition"/>,
/// per-user context, and the registered tool list. Mirrors the launchpad-v2
/// helper so call sites are familiar.
/// </summary>
public static class PromptBuilder
{
    /// <summary>
    /// Combine the staff system prompt with optional context blocks.
    /// </summary>
    public static string BuildStaffPrompt(
        string? userContext = null,
        string? conversationContext = null,
        string? toolDescriptions = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine(StaffPrompts.Default.GetPrompt());
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(userContext))
        {
            sb.AppendLine("User context:");
            sb.AppendLine(userContext);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(conversationContext))
        {
            sb.AppendLine("Conversation context so far:");
            sb.AppendLine(conversationContext);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(toolDescriptions))
        {
            sb.AppendLine(toolDescriptions);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Replace <c>{{TOKEN}}</c> placeholders in a template.
    /// </summary>
    public static string ReplaceTokens(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var result = template;
        foreach (var (key, value) in tokens)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }
        return result;
    }
}
