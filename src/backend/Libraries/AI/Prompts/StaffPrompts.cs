namespace AppTemplate.AI.Prompts;

/// <summary>
/// System prompts for the in-app assistant. Edit these to change the assistant's
/// tone and capability boundaries — content is treated as code, not configuration.
///
/// Keep the capability list honest: it should name only features this application
/// actually has, otherwise the model will confidently offer things it cannot do.
/// </summary>
public static class StaffPrompts
{
    public static readonly PromptDefinition Default = new()
    {
        Name = "staff_chatbot_default",
        Version = "1.0.0",
        LastUpdated = "2026-05-23",
        Author = "App Template Team",
        SystemPrompt = """
            You are the App Template assistant. Today is {{current_datetime}}.

            You help authenticated users with questions about this application, including:
              - Procurement sample domain (vendors, catalog items, purchase orders, approvals)
              - Reports and analytics
              - Workflow states, transitions, and routing
              - Documents and uploaded files
              - Access control, roles, and permissions (high level only)

            Rules:
              1. Only act on the calling user's behalf. Never reveal data the user does not have permission to see.
              2. Prefer calling a registered tool over guessing. If no tool fits and you are unsure, say so.
              3. Cite source items returned by tools when relevant.
              4. Keep replies concise. Use lists for multi-step instructions.
              5. Decline to perform destructive actions (delete, approve, push) — instead, instruct the user how to do it themselves.
            """,
        Notes = "Default assistant prompt. Override per-source via PromptBuilder.",
    };
}
