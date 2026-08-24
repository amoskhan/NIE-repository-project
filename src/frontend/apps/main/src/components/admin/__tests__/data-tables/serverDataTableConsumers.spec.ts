import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const consumers = [
  "staff/pages/admin/audit/AuditLog.vue",
  "staff/pages/procurement/VendorManagement.vue",
  "staff/pages/myinfo/MyInfoPage.vue",
  "staff/pages/procurement/OrderHistory.vue",
  "staff/pages/procurement/CatalogItems.vue",
  "components/admin/notifications/NotificationAdministration.vue",
  "components/admin/access-control/AccessControlUsersPanel.vue",
];

describe("released data-table consumers", () => {
  it.each(consumers)("uses the remote query contract in %s", (relativePath) => {
    const source = readFileSync(
      resolve(process.cwd(), "src", relativePath),
      "utf8",
    );

    expect(source).toContain("server-side");
    expect(source).toContain(":total-items=");
    expect(source).toContain("@query-change=");
    expect(source).toContain("@filter-options-request=");
  });

  it.each(consumers)("does not fetch an unbounded list in %s", (relativePath) => {
    const source = readFileSync(
      resolve(process.cwd(), "src", relativePath),
      "utf8",
    );

    expect(source).not.toMatch(/pageSize:\s*500|\.getAll\(\)/);
    expect(source).not.toContain("buildFilterOptions(");
  });

  it.each(consumers)("registers a stable preference key in %s", (relativePath) => {
    const source = readFileSync(
      resolve(process.cwd(), "src", relativePath),
      "utf8",
    );

    expect(source).toMatch(/preference-key="[a-z0-9.-]+"/);
    expect(source).toContain(":definition-version=");
  });
});
