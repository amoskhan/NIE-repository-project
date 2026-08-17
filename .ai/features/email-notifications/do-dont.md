# Email Notifications — Do and Don't

## DO ✅

1. **DO** call `SendBaseTemplatedEmailAsync(toEmail, subject, contentHtml)` for ad-hoc notifications. The base template adds the shared header / footer / date / year so emails are visually consistent across services.
2. **DO** create a per-event template under `src/backend/API/Templates/` (e.g. `ApprovalRequest.html`) when the email layout is non-trivial. Use `{Placeholder}` tokens and pass them through the `placeholders` dictionary to `SendTemplatedEmailAsync`.
3. **DO** call `IAuditLogger.LogEmailSentAsync(recipient, subject, "Success" | "Failed")` after the send completes. The audit row carries the recipient + subject so you can answer "did we actually send that?" later.
4. **DO** set `EmailSettings.SenderEmail` explicitly in every environment. It is a required setting — there is no address the template can guess for you, because it depends on which domain you control and what your relay will accept. The blank-value fallback (`<appname>@example.edu`) is a deliberately undeliverable placeholder, not a default to rely on.
5. **DO** populate `EmailSettings.AppName` with a short identifier (`"myapp"`, `"studyplanner"`, `"teamtracker"`). It shows up in the base template header/footer and in the `{AppName}` placeholder.
6. **DO** use `EnableSsl = true` (STARTTLS) for any real relay. Plain SMTP is fine against a local catcher (`localhost:1025`) and never acceptable when the mail actually leaves the machine.
7. **DO** populate `BccEmails` with an archive mailbox if your project needs a record of what it sent. The service appends each entry as a BCC on every outgoing message.
8. **DO** wrap email sends in a TickerQ job for retry-critical workflows (e.g. password reset, payment confirmation). The shipped `EmailService` does NOT retry on failure.
9. **DO** validate recipient strings before calling — `SendCoreAsync` filters empty entries (`.Where(e => !string.IsNullOrWhiteSpace(e))`) and skips when no valid `To` remains, but checking earlier surfaces bugs faster.
10. **DO** keep templates pure HTML + `{Placeholder}` tokens. Razor-style logic does not work; the replacement is a literal string `Replace`.
11. **DO** develop against a local mail catcher — `SmtpHost = "localhost"`, `SmtpPort = 1025` reaches Mailpit (or mailhog), which accepts everything and shows it at `http://localhost:8025`. Nothing leaves your machine, so you can iterate on the password-reset email without mailing anyone.

## DON'T ❌

1. **DON'T** call `SendEmailAsync(rawHtml)` directly from random services unless the body is a single self-contained HTML page. The base template gives you the brand surface — skipping it produces orphaned-looking emails.
2. **DON'T** put runtime data into the template file itself (e.g. hardcoding "Dear {{User.Name}}"). The placeholder-replace is a flat string substitution; embedding logic forces the template to know your data shape.
3. **DON'T** include attachments via the current API — the service does not expose them. Add an overload with `IEnumerable<MimeEntity>` if you need it; do NOT inline files as base64 in HTML.
4. **DON'T** chain `SendCoreAsync` calls in a tight loop. Each call opens and closes a fresh SMTP connection. For batch sends, accept a `List<string>` (the service supports this) so MailKit reuses the connection.
5. **DON'T** log full HTML bodies. They contain user data and increase log volume. Log recipient + subject + outcome only.
6. **DON'T** swallow MailKit `SmtpProtocolException` — it indicates server-side rejection (bad credentials, blocked sender). Let it propagate so the caller can decide on retry.
7. **DON'T** assume `EmailSettings.SmtpUsername` is non-empty. The service's `if (!string.IsNullOrEmpty(_settings.SmtpUsername) && !string.IsNullOrEmpty(_settings.SmtpPassword))` check skips authentication for anonymous relays — which is exactly what a local mail catcher is.
8. **DON'T** leave `SenderEmail` blank and hope. The fallback address is on a placeholder domain nobody owns; a real relay will refuse it and your emails will vanish with a confusing error.
9. **DON'T** set `SenderEmail` to a personal address. Use a project mailbox — recipients reply to the sender, and a teammate's inbox is not where those replies should land.
10. **DON'T** put SMTP credentials in `appsettings.json`. Use `dotnet user-secrets` locally and environment variables (`EmailSettings__SmtpPassword`) or a secret store when deployed.
11. **DON'T** assume the recipient's mail server will accept your sender domain. Mail from a domain with no SPF/DKIM record pointing at your relay lands in spam or is rejected outright. If you are sending to real inboxes, use a mailbox on a domain you can add those records to, or a transactional provider that handles it for you.
