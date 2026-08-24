import { describe, expect, it } from "vitest";
import { buildNotificationEmailPreview } from "@/components/admin/notifications/notificationEmailPreview";

describe("notification email preview", () => {
  const sourceBrand = ["Code", " Sentinel"].join("");

  it("keeps NIE branding and the fixed wrapper around editable content", () => {
    const document = buildNotificationEmailPreview({
      content: "<p>Editable procurement update</p>",
      logoUrl: "/nie-logo.svg",
      applicationName: "NIE Template",
    });

    expect(document).toContain('<img src="/nie-logo.svg"');
    expect(document).toContain("<strong>NIE Template</strong>");
    expect(document).toContain("<p>Editable procurement update</p>");
    expect(document).toContain("This mailbox is not monitored");
    expect(document).not.toContain(sourceBrand);
  });

  it("uses a sandbox-compatible document without active script content", () => {
    const document = buildNotificationEmailPreview({
      content: "<p>Safe body</p>",
      logoUrl: "/nie-logo.svg",
      applicationName: "NIE Template",
    });

    expect(document).not.toMatch(/<script\b/i);
    expect(document).not.toMatch(/javascript:/i);
  });
});
