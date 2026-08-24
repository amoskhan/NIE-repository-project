export interface NotificationEmailPreviewOptions {
  content: string;
  logoUrl: string;
  applicationName: string;
}

function escapeAttribute(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll('"', "&quot;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;");
}

export function buildNotificationEmailPreview({
  content,
  logoUrl,
  applicationName,
}: NotificationEmailPreviewOptions): string {
  const safeLogoUrl = escapeAttribute(logoUrl);
  const safeApplicationName = escapeAttribute(applicationName);

  return `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><style>
*{box-sizing:border-box}body{margin:0;background:#f2f4f7;color:#172033;font:13px/1.55 Arial,sans-serif;overflow-wrap:anywhere}
.gutter{padding:18px 10px}.shell{width:100%;max-width:640px;margin:0 auto;overflow:hidden;border:1px solid #dfe4eb;border-radius:8px;background:#fff}
.accent{height:4px;background:#0055a5}.header{display:flex;align-items:center;justify-content:space-between;gap:16px;padding:18px 20px;border-bottom:1px solid #e5e9ef}
.header img{display:block;width:170px;max-width:68%;height:auto}.brand{text-align:right}.brand strong{display:block;color:#101828;font-size:14px}.brand span{color:#667085;font-size:10px}
.content{padding:26px 24px;color:#344054}.content table{max-width:100%}.footer{padding:18px 20px;border-top:1px solid #e5e9ef;background:#f8fafc;color:#667085;font-size:10px;line-height:1.55}
@media(max-width:420px){.brand{display:none}.content{padding:22px 18px}}
</style></head><body><div class="gutter"><div class="shell"><div class="accent"></div><div class="header"><img src="${safeLogoUrl}" alt="NIE and NTU Singapore"><div class="brand"><strong>${safeApplicationName}</strong><span>Application workflow notification</span></div></div><div class="content">${content}</div><div class="footer">This operational notification was sent by <strong>${safeApplicationName}</strong> because you are part of the relevant application workflow.<br><span style="color:#98a2b3">This mailbox is not monitored. Sign in to ${safeApplicationName} to review or respond.</span></div></div></div></body></html>`;
}
