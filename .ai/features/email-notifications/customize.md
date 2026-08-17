# Email Notifications — Customize

## 1. Configure the local SMTP catcher for development (do this first)

A mail catcher is a fake SMTP server: it accepts every message and shows it in a browser instead of
delivering it. This is the dev default, and it means you can build and test the password-reset flow
without emailing a single real person.

1. Run Mailpit (or mailhog):
   ```bash
   docker run -d --name mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit
   ```
2. Edit `src/backend/API/appsettings.json` — the only settings file the template ships, and it already carries these Mailpit defaults. (For a dev-only override, create `src/backend/API/appsettings.Development.json` yourself; nothing generates it.)
   ```json
   "EmailSettings": {
     "AppName": "apptemplate-local",
     "SmtpHost": "localhost",
     "SmtpPort": 1025,
     "SmtpUsername": "",
     "SmtpPassword": "",
     "SenderEmail": "noreply@localhost",
     "SenderName": "App Template (Local)",
     "EnableSsl": false,
     "BccEmails": []
   }
   ```
   No username, no password, no TLS — a catcher needs none of it.
3. Open `http://localhost:8025` to read whatever your app sent.

## 2. Configure a real SMTP relay for a deployed environment

**`SenderEmail` is required.** There is no value the template can guess: it depends on which domain
you control and which mailbox your relay is authorised to send as. Leaving it blank makes
`EmailService` fall back to `<appname>@example.edu`, a placeholder domain nobody owns — a real relay
will reject it.

1. Set the block via environment variables or `appsettings.{Environment}.json`:
   ```json
   "EmailSettings": {
     "AppName": "myapp",
     "SmtpHost": "smtp.your-provider.example",
     "SmtpPort": 587,
     "SmtpUsername": "",
     "SmtpPassword": "",
     "SenderEmail": "noreply@your-domain.example",
     "SenderName": "MyApp Notifications",
     "EnableSsl": true,
     "BccEmails": []
   }
   ```
2. Supply `SmtpUsername` / `SmtpPassword` out of band — `dotnet user-secrets` locally, or
   `EmailSettings__SmtpUsername` / `EmailSettings__SmtpPassword` environment variables when deployed.
   Never commit them.
3. `EnableSsl: true` selects STARTTLS. Port 587 with STARTTLS is the normal combination; port 465 is
   implicit TLS and works differently — check what your provider expects.
4. Make sure the sending domain has SPF (and ideally DKIM) records that authorise your relay,
   otherwise recipients' servers will file your mail as spam or refuse it. If you cannot add DNS
   records for the domain, use a transactional provider's own sending domain instead of pretending
   to be one you do not control.

## 3. Add a new templated email (e.g. "Approval Request")

1. Create `src/backend/API/Templates/ApprovalRequest.html`:
   ```html
   <html>
     <body>
       <h2>Approval Required: {EntityName}</h2>
       <p>Hello {ApproverName},</p>
       <p>{RequesterName} has submitted "{EntityName}" for your approval.</p>
       <p><a href="{ApprovalUrl}">Open in portal</a></p>
     </body>
   </html>
   ```
2. Mark the file as "Copy to Output Directory: PreserveNewest" in `API.csproj` (or place under a `<Content>` group with `CopyToOutputDirectory`):
   ```xml
   <ItemGroup>
     <Content Include="Templates\**\*.html" CopyToOutputDirectory="PreserveNewest" />
   </ItemGroup>
   ```
3. Call from your service:
   ```csharp
   await _email.SendTemplatedEmailAsync(
       toEmail: approver.Email,
       subject: $"[Approval] {entity.Name}",
       templateFileName: "ApprovalRequest.html",
       placeholders: new Dictionary<string, string>
       {
           ["EntityName"] = entity.Name,
           ["ApproverName"] = approver.FullName,
           ["RequesterName"] = requester.FullName,
           ["ApprovalUrl"] = $"https://app.example.com/approvals/{entity.Id}"
       },
       toName: approver.FullName);
   ```
4. Audit-log the send:
   ```csharp
   await _auditLogger.LogEmailSentAsync(approver.Email, $"[Approval] {entity.Name}", "Success");
   ```

## 4. Rebrand the base template (header / footer / logo)

1. Open `src/backend/API/Templates/BaseTemplate.html`. It ships with a placeholder logo and accent
   colour — replace both with your project's. Keep the table-based layout: email clients are still
   bad at CSS, and this shell is deliberately conservative.
2. Inline the logo as an SVG or a `data:` URI, or host it at a stable HTTPS URL. Many clients block
   remote images by default, so do not put anything load-bearing in an image.
3. The placeholders are `{AppName}`, `{Content}`, `{DateTime}`, `{Year}`. They are replaced verbatim
   by `EmailService.BuildBaseTemplateAsync`; do not introduce new ones without adding them to that
   `placeholders` dictionary.
4. To add a new placeholder (e.g. `{LogoUrl}`), edit `BuildBaseTemplateAsync` and add the entry. Keep
   token names short and unambiguous.

## 5. Send to multiple recipients in one connection

```csharp
await _email.SendEmailAsync(
    toEmails: approvers.Select(a => a.Email).ToList(),
    subject: "Quarterly review",
    htmlBody: htmlContent);
```

Single SMTP connection, single message with multiple `To` addresses. For per-recipient personalization (different `{ApproverName}` per email), call `SendTemplatedEmailAsync` per recipient — or use a queue.

## 6. Add a retry/queue layer for critical emails

The shipped service does NOT retry on failure (it logs and re-throws). For password reset / payment receipts:

1. Add an entity `EmailQueue` with `ToEmail`, `Subject`, `BodyHtml`, `Attempts`, `Status`, `LastError`, `CreatedOn`.
2. Replace direct `IEmailService.SendEmailAsync` calls with `IEmailQueueService.EnqueueAsync(...)` that just inserts the row.
3. Add a TickerQ job `EmailDispatchJob` that runs every minute, picks up `Status = Pending`, calls `IEmailService.SendEmailAsync`, updates status. Use exponential backoff via `Attempts`.
4. Wire `IAuditLogger.LogEmailSentAsync` from inside the job (not the enqueue site).

## 7. Switch to SES / SendGrid / Mailgun

The cleanest swap is to keep the `IEmailService` interface and write a new implementation:

1. Create `src/backend/Libraries/Services/Services/Email/SesEmailService.cs` implementing `IEmailService`. Use `AWSSDK.SimpleEmailV2` `SendEmailAsync`.
2. In `Program.cs:97-103`, switch the registration based on a config flag:
   ```csharp
   var emailProvider = configuration["EmailSettings:Provider"] ?? "Smtp";
   if (emailProvider == "SES")
       builder.Services.AddScoped<IEmailService, SesEmailService>();
   else
       builder.Services.AddScoped<IEmailService>(sp => new EmailService(...));
   ```
3. Existing call sites are unchanged — they only see `IEmailService`.

## 8. Add CC

Use `SendEmailWithCCAsync(toEmails, ccEmails, subject, htmlBody)`. The current API does not have a `SendTemplatedEmailWithCCAsync`; add one in `IEmailService` + `EmailService` if you need templating + CC together.
