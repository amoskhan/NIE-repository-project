using Application.AI.Prompts;

namespace Application.Tests;

public sealed class PromptBuilderTests
{
    [Fact]
    public void Starts_every_staff_prompt_with_the_shared_system_prompt()
    {
        var prompt = PromptBuilder.BuildStaffPrompt();

        Assert.StartsWith("You are the NIE Template assistant", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Omits_every_optional_block_when_nothing_is_supplied()
    {
        var prompt = PromptBuilder.BuildStaffPrompt();

        Assert.DoesNotContain("User context:", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Conversation context so far:", prompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Treats_a_blank_context_block_as_absent(string? context)
    {
        var prompt = PromptBuilder.BuildStaffPrompt(context, context, context);

        Assert.DoesNotContain("User context:", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Conversation context so far:", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Labels_the_user_context_block_when_it_is_supplied()
    {
        var prompt = PromptBuilder.BuildStaffPrompt(userContext: "Role: Finance approver");

        Assert.Contains("User context:", prompt, StringComparison.Ordinal);
        Assert.Contains("Role: Finance approver", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Conversation context so far:", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Orders_the_context_blocks_so_tools_come_last()
    {
        var prompt = PromptBuilder.BuildStaffPrompt(
            "Role: Finance approver",
            "The user asked about PO-1.",
            "Tools: get_purchase_order");

        var userIndex = prompt.IndexOf("User context:", StringComparison.Ordinal);
        var conversationIndex = prompt.IndexOf("Conversation context so far:", StringComparison.Ordinal);
        var toolIndex = prompt.IndexOf("Tools: get_purchase_order", StringComparison.Ordinal);

        Assert.True(userIndex > 0);
        Assert.True(conversationIndex > userIndex);
        Assert.True(toolIndex > conversationIndex);
    }

    [Fact]
    public void Appends_tool_descriptions_without_a_label()
    {
        var prompt = PromptBuilder.BuildStaffPrompt(toolDescriptions: "Tools: get_purchase_order");

        Assert.Contains("Tools: get_purchase_order", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("User context:", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Replaces_every_occurrence_of_a_known_token()
    {
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["NAME"] = "Ada",
            ["ROLE"] = "approver",
        };

        var result = PromptBuilder.ReplaceTokens("Hi {{NAME}}, you are an {{ROLE}}. Bye {{NAME}}.", tokens);

        Assert.Equal("Hi Ada, you are an approver. Bye Ada.", result);
    }

    [Fact]
    public void Leaves_a_token_untouched_when_no_replacement_is_supplied()
    {
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal) { ["NAME"] = "Ada" };

        Assert.Equal("Hi Ada, {{UNKNOWN}}.", PromptBuilder.ReplaceTokens("Hi {{NAME}}, {{UNKNOWN}}.", tokens));
    }

    [Fact]
    public void Returns_the_template_unchanged_when_there_are_no_tokens()
    {
        const string template = "Nothing to replace {{HERE}}.";

        Assert.Equal(
            template,
            PromptBuilder.ReplaceTokens(template, new Dictionary<string, string>(StringComparer.Ordinal)));
    }

    [Fact]
    public void Ignores_a_token_name_that_is_not_wrapped_in_double_braces()
    {
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal) { ["NAME"] = "Ada" };

        Assert.Equal("Hi {NAME} NAME.", PromptBuilder.ReplaceTokens("Hi {NAME} NAME.", tokens));
    }
}

public sealed class PromptDefinitionTests
{
    private static PromptDefinition Definition(string systemPrompt) =>
        new()
        {
            Name = "test_prompt",
            Version = "1.2.3",
            LastUpdated = "2026-01-01",
            Author = "Tests",
            SystemPrompt = systemPrompt,
        };

    [Fact]
    public void Trims_surrounding_whitespace_from_the_system_prompt()
    {
        Assert.Equal("Body", Definition("\n   Body   \n").GetPrompt());
    }

    [Fact]
    public void Substitutes_the_current_date_time_token()
    {
        var prompt = Definition("Today is {{current_datetime}}.").GetPrompt();

        Assert.DoesNotContain("{{current_datetime}}", prompt, StringComparison.Ordinal);
        Assert.StartsWith("Today is ", prompt, StringComparison.Ordinal);
        Assert.EndsWith(".", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Leaves_a_prompt_without_the_token_unchanged()
    {
        Assert.Equal("No tokens here.", Definition("No tokens here.").GetPrompt());
    }

    [Fact]
    public void Identifies_itself_by_name_and_version()
    {
        Assert.Equal("test_prompt@v1.2.3", Definition("Body").ToString());
    }

    [Fact]
    public void Resolves_the_shipped_staff_prompt_without_leaving_tokens_behind()
    {
        var prompt = StaffPrompts.Default.GetPrompt();

        Assert.DoesNotContain("{{current_datetime}}", prompt, StringComparison.Ordinal);
        Assert.Contains("Only act on the calling user's behalf", prompt, StringComparison.Ordinal);
        Assert.Equal(prompt.Trim(), prompt);
    }
}
