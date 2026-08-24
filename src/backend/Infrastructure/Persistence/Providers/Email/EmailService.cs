
using System.Net;
using System.Text.RegularExpressions;
using Application.Abstractions.Identity;
using Application.Features.Email;
using Application.Features.PushNotification;
using BuildingBlocks.Helpers;
using Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Providers.Email;

public class EmailService : IEmailService
{
    private const string LogoContentId = "nie-template-nie-logo";
    private const string LogoResourceName = "Infrastructure.Providers.Reports.ReportLogo.svg";

    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly string _contentRootPath;
    private readonly byte[]? _nieLogoPng;

    public EmailService(
        IOptions<EmailSettings> settings,
        ILogger<EmailService> logger,
        string contentRootPath)
    {
        _settings = settings.Value;
        _logger = logger;
        _contentRootPath = contentRootPath;
        _nieLogoPng = LoadNieLogoPng();

        // Derive SenderEmail from AppName if not explicitly set
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail) && !string.IsNullOrWhiteSpace(_settings.AppName))
            _settings.SenderEmail = $"{_settings.AppName.ToLowerInvariant().Replace(" ", "")}@nie.edu.sg";
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

        using var message = new MimeMessage();
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

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = ConvertHtmlToPlainText(htmlBody),
        };
        if (_nieLogoPng is { Length: > 0 } &&
            htmlBody.Contains($"cid:{LogoContentId}", StringComparison.Ordinal))
        {
            var logo = new MimePart("image", "png")
            {
                Content = new MimeContent(
                    new MemoryStream(_nieLogoPng, writable: false)),
                ContentDisposition =
                    new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64,
                ContentId = LogoContentId,
                FileName = "nie-logo.png",
            };
            bodyBuilder.LinkedResources.Add(logo);
        }

        message.Body = bodyBuilder.ToMessageBody();

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

    private byte[]? LoadNieLogoPng()
    {
        try
        {
            using var stream = typeof(EmailService).Assembly
                .GetManifestResourceStream(LogoResourceName);
            if (stream is null)
            {
                _logger.LogWarning(
                    "Embedded NIE email logo resource {ResourceName} was not found.",
                    LogoResourceName);
                return null;
            }

            using var reader = new StreamReader(stream);
            var svg = reader.ReadToEnd();
            var marker = new[]
                {
                    "data:image/png;base64,",
                    "data:img/png;base64,",
                }
                .FirstOrDefault(candidate =>
                    svg.Contains(candidate, StringComparison.OrdinalIgnoreCase));
            var start = marker is null
                ? -1
                : svg.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                _logger.LogWarning(
                    "Embedded NIE email logo does not contain a PNG payload.");
                return null;
            }

            start += marker!.Length;
            var end = svg.IndexOfAny(['"', '\''], start);
            if (end <= start)
            {
                _logger.LogWarning(
                    "Embedded NIE email logo PNG payload is malformed.");
                return null;
            }

            return Convert.FromBase64String(svg[start..end]);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Embedded NIE email logo could not be decoded.");
            return null;
        }
    }

    private static string ConvertHtmlToPlainText(string html)
    {
        var text = Regex.Replace(
            html,
            @"<\s*br\s*/?\s*>|</\s*(p|div|tr|h[1-6])\s*>",
            Environment.NewLine,
            RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", " ");
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(
            text,
            @"(\r?\n\s*){3,}",
            Environment.NewLine + Environment.NewLine);
        return text.Trim();
    }

    #endregion
}
