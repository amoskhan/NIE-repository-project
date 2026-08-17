# Email Notifications — Verify

## Backend

```bash
dotnet build src/backend/AppTemplate.sln
dotnet run --project src/backend/API
```

## Local SMTP catcher (Mailpit)

```bash
# Start the catcher — it accepts everything and delivers nothing
docker run -d --name mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit

# Confirm the shipped settings point at it (there is no appsettings.Development.json)
grep -A 8 "EmailSettings" src/backend/API/appsettings.json
# Expect: SmtpHost "localhost", SmtpPort 1025, EnableSsl false, and a
# SenderEmail that is actually filled in.
```

## SenderEmail is configured (do this before anything else)

```bash
# SenderEmail must be a non-empty value in every environment file that sends mail.
grep -rn '"SenderEmail"' src/backend/API/appsettings*.json
# Expect: no occurrence of "SenderEmail": ""
```

If you find a blank one, fix it. `EmailService` falls back to `<appname>@example.edu` on a placeholder
domain, which will be rejected by any real relay and silently accepted by the catcher — so the bug
only shows up after you deploy.

```bash
# And confirm no SMTP password was committed
grep -rn '"SmtpPassword"' src/backend/API/appsettings*.json
# Expect: only empty strings — real values come from user-secrets or env vars
```

## Trigger a send programmatically

The cleanest way is a small temporary endpoint or a scratch controller; for verification we'll lean on whatever caller already exists in your project (e.g. password reset, approval). For the template repo, the procurement sample's approval flow triggers an email when `ProcessApproval` is called.

```bash
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"alice"}' | jq -r .sessionToken)

curl -s -X POST http://localhost:5002/api/AccessControl/AssignRole \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{"userId":"alice","roleId":1}'

# (Replace with whatever your project uses to trigger an email — e.g. submit a PO)
curl -s -X POST http://localhost:5002/api/PurchaseOrder/Submit \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{"id":1}' | jq

# Open Mailpit
open http://localhost:8025
# Expect: a captured email whose From header is exactly your configured SenderEmail
```

## Without a trigger — direct service smoke

Add a temporary minimal endpoint:

```csharp
app.MapGet("/dev/test-email", async (IEmailService email) =>
{
    await email.SendBaseTemplatedEmailAsync(
        toEmail: "test@local",
        subject: "Smoke test",
        contentHtml: "<p>Hello from the smoke test.</p>",
        toName: "Smoke Tester");
    return Results.Ok();
}).RequireHost("localhost"); // dev only
```

```bash
curl -s http://localhost:5002/dev/test-email -H "X-Session-Id: $SESSION"
# Open Mailpit → confirm the message renders inside the BaseTemplate shell
# (header, content, footer, year) and carries your branding, not a placeholder.
```

Remove the endpoint before merging.

## Audit row

```sql
SELECT "Action", "EntityId", "Outcome", "AdditionalData"
FROM "AuditLogs"
WHERE "Action" = 53      -- EAuditAction.EmailSent
ORDER BY "Timestamp" DESC LIMIT 5;
-- Expect: rows with EntityId = recipient email and AdditionalData JSON containing { recipient, subject }
```

## SMTP failure path

```bash
# Stop Mailpit to simulate a dead SMTP
docker stop mailpit

# Trigger another send. The API logs should contain:
#   "Failed to send email to ... — Subject: ..."
# and the call should bubble up an exception (caller chooses how to handle).

# Restart Mailpit
docker start mailpit
```

## STARTTLS check (production-mode setup)

```bash
# Use openssl to confirm the SMTP host serves STARTTLS
openssl s_client -connect localhost:587 -starttls smtp -servername localhost
# Expect: a successful TLS handshake. If failure, EmailSettings.EnableSsl=true would still fail.
```

## Sender header

```bash
# Send a smoke email and inspect the From header in Mailpit. It must read:
#   From: <SenderName> <your-configured-SenderEmail>
#
# If it reads <something>@example.edu you left SenderEmail blank and are seeing
# the placeholder fallback. Set it explicitly.
```

## Bcc archive

```bash
# Add an entry to BccEmails in src/backend/API/appsettings.json:
# "BccEmails": [ "archive@localhost" ]
# Send a smoke email. In Mailpit, both the original recipient and archive@localhost
# should receive it.
```
