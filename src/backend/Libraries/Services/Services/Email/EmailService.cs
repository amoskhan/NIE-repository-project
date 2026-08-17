using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Helpers;
using Shared.Services.UserContext;
using Services.Services.Email;
using Services.Services.PushNotification;
using Shared.Models;

namespace Services.Services.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly string _contentRootPath;

    public EmailService(
        IOptions<EmailSettings> settings,
        ILogger<EmailService> logger,
        string contentRootPath)
    {
        _settings = settings.Value;
        _logger = logger;
        _contentRootPath = contentRootPath;

        // The sender address is deliberately NOT derived from anything: guessing a
        // domain produces mail that fails SPF/DKIM and bounces. Configure it
        // explicitly (EmailSettings:SenderEmail) and fail fast if it is missing.
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
            throw new InvalidOperationException(
                "EmailSettings:SenderEmail is required but was not configured. " +
                "Set it in appsettings.json (or user-secrets / environment variables) to the address " +
                "outgoing mail should come from, e.g. \"no-reply@example.com\".");
    }

    public async Task SendTemplatedEmailAsync(
        string toEmail, string subject, string templateFileName,
        Dictionary<string, string> placeholders, string? toName = null)
    {
        var htmlBody = await LoadAndReplaceTemplateAsync(templateFileName, placeholders);
        await SendEmailAsync(toEmail, subject, htmlBody, toName);
    }

    public async Task SendTemplatedEmailAsync(
        List<string> toEmails, string subject, string templateFileName,
        Dictionary<string, string> placeholders)
    {
        var htmlBody = await LoadAndReplaceTemplateAsync(templateFileName, placeholders);
        await SendEmailAsync(toEmails, subject, htmlBody);
    }

    public async Task SendBaseTemplatedEmailAsync(
        string toEmail, string subject, string contentHtml, string? toName = null)
    {
        var htmlBody = await BuildBaseTemplateAsync(contentHtml);
        await SendEmailAsync(toEmail, subject, htmlBody, toName);
    }

    public async Task SendBaseTemplatedEmailAsync(
        List<string> toEmails, string subject, string contentHtml)
    {
        var htmlBody = await BuildBaseTemplateAsync(contentHtml);
        await SendEmailAsync(toEmails, subject, htmlBody);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? toName = null)
    {
        await SendEmailAsync([toEmail], subject, htmlBody);
    }

    public async Task SendEmailAsync(List<string> toEmails, string subject, string htmlBody)
    {
        await SendCoreAsync(toEmails, [], subject, htmlBody);
    }

    public async Task SendEmailWithCCAsync(List<string> toEmails, List<string> ccEmails, string subject, string htmlBody)
    {
        await SendCoreAsync(toEmails, ccEmails, subject, htmlBody);
    }

    #region Private helpers

    private async Task SendCoreAsync(List<string> toEmails, List<string> ccEmails, string subject, string htmlBody)
    {
        if (toEmails.Count == 0)
        {
            _logger.LogWarning("No recipient emails provided — skipping send.");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));

        foreach (var email in toEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
            message.To.Add(MailboxAddress.Parse(email));

        foreach (var email in ccEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
            message.Cc.Add(MailboxAddress.Parse(email));

        foreach (var bcc in _settings.BccEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
            message.Bcc.Add(MailboxAddress.Parse(bcc));

        if (message.To.Count == 0)
        {
            _logger.LogWarning("No valid recipient emails after parsing — skipping send.");
            return;
        }

        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            var socketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, socketOptions);

            if (!string.IsNullOrEmpty(_settings.SmtpUsername) && !string.IsNullOrEmpty(_settings.SmtpPassword))
                await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Recipients} — Subject: {Subject}",
                string.Join(", ", toEmails), subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients} — Subject: {Subject}",
                string.Join(", ", toEmails), subject);
            throw;
        }
    }

    private async Task<string> LoadAndReplaceTemplateAsync(string templateFileName, Dictionary<string, string> placeholders)
    {
        var fullPath = Path.Combine(_contentRootPath, "Templates", templateFileName);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Email template not found at {Path}", fullPath);
            throw new FileNotFoundException($"Email template not found: {templateFileName}", fullPath);
        }

        var template = await File.ReadAllTextAsync(fullPath);

        foreach (var (key, value) in placeholders)
            template = template.Replace($"{{{key}}}", value);

        return template;
    }

    private async Task<string> BuildBaseTemplateAsync(string contentHtml)
    {
        var now = DateTimeHelper.Now;
        var placeholders = new Dictionary<string, string>
        {
            { "AppName", _settings.AppName },
            { "Content", contentHtml },
            { "DateTime", now.ToString("dd MMM yyyy, hh:mm tt") },
            { "Year", now.Year.ToString() }
        };

        return await LoadAndReplaceTemplateAsync("BaseTemplate.html", placeholders);
    }

    #endregion
}
