namespace Infrastructure.Options;

public class EmailSettings
{
    /// <summary>
    /// Short application identifier used to build the sender email (e.g. "i3g" → i3g@nie.edu.sg)
    /// and displayed in the unified email template header/footer.
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 25;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Derived from AppName by default (appname@nie.edu.sg). Override in appsettings if needed.
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }
    public List<string> BccEmails { get; set; } = [];
}
