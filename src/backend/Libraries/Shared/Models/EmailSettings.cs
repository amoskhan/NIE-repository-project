namespace Shared.Models;

/// <summary>
/// Outgoing mail configuration, bound from the "EmailSettings" section.
/// The development defaults point at the Mailpit catcher in docker compose
/// (SMTP localhost:1025, web UI http://localhost:8025) so no real mail is ever sent.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Display name of the application, shown in the shared email template's
    /// header and footer (the <c>{AppName}</c> placeholder).
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 25;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Address outgoing mail is sent from. <b>Required</b> — it is never derived from
    /// anything else, because a guessed domain fails SPF/DKIM and the mail bounces.
    /// <c>EmailService</c> throws an <see cref="InvalidOperationException"/> at construction
    /// when this is blank.
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Friendly "From" name shown next to <see cref="SenderEmail"/> in mail clients.
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Use implicit SSL/TLS for the SMTP connection. Leave false for local dev catchers
    /// such as Mailpit; turn it on for a real relay.
    /// </summary>
    public bool EnableSsl { get; set; }

    /// <summary>
    /// Optional addresses blind-copied on every message (e.g. an archive mailbox).
    /// </summary>
    public List<string> BccEmails { get; set; } = [];
}
