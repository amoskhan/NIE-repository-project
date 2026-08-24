namespace Application.Features.Email;

public interface IEmailService
{
    /// <summary>
    /// Send an email using an HTML template file with placeholder replacements.
    /// The template is loaded from the Templates folder relative to the app's content root.
    /// </summary>
    Task SendTemplatedEmailAsync(
        string toEmail,
        string subject,
        string templateFileName,
        Dictionary<string, string> placeholders,
        string? toName = null);

    /// <summary>
    /// Send an email using an HTML template file to multiple recipients.
    /// </summary>
    Task SendTemplatedEmailAsync(
        List<string> toEmails,
        string subject,
        string templateFileName,
        Dictionary<string, string> placeholders);

    /// <summary>
    /// Send an email using the unified base template (BaseTemplate.html).
    /// Only requires the inner content HTML — header, footer, and app name are handled automatically.
    /// </summary>
    Task SendBaseTemplatedEmailAsync(
        string toEmail,
        string subject,
        string contentHtml,
        string? toName = null);

    /// <summary>
    /// Send an email using the unified base template to multiple recipients.
    /// </summary>
    Task SendBaseTemplatedEmailAsync(
        List<string> toEmails,
        string subject,
        string contentHtml);

    /// <summary>
    /// Send a raw HTML email to a single recipient.
    /// </summary>
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? toName = null);

    /// <summary>
    /// Send a raw HTML email to multiple recipients.
    /// </summary>
    Task SendEmailAsync(List<string> toEmails, string subject, string htmlBody);

    /// <summary>
    /// Send a raw HTML email with CC recipients.
    /// </summary>
    Task SendEmailWithCCAsync(List<string> toEmails, List<string> ccEmails, string subject, string htmlBody);
}
