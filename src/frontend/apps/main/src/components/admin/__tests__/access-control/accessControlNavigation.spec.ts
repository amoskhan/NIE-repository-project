import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { ADMIN_NAV_ITEMS } from "@/app-config/navigation";
import { PROJECT_ROUTES } from "@/app-config/routes";

describe("access-control navigation", () => {
  it("has one Access Control sidebar item and compatibility redirects into its tabs", () => {
    const accessItems = ADMIN_NAV_ITEMS.filter((item) =>
      ["Access Control", "Users & Roles", "Access Functions"].includes(
        item.name,
      ),
    );
    expect(accessItems).toHaveLength(1);
    expect(accessItems[0].name).toBe("Access Control");
    expect(accessItems[0].route).toBe("access-control");

    const canonical = PROJECT_ROUTES.find(
      (route) => route.name === "access-control",
    );
    expect(canonical?.path).toBe("access-control");

    for (const legacyName of ["users", "role-management", "access-functions"]) {
      const legacy = PROJECT_ROUTES.find((route) => route.name === legacyName);
      expect(legacy?.redirect).toBeDefined();
    }
  });

  it("normalizes an invalid access-control tab on initial navigation", () => {
    const users = readFileSync(
      resolve(
        process.cwd(),
        "src/staff/pages/admin/access-control/AccessControlUsersPage.vue",
      ),
      "utf8",
    );

    expect(users).toMatch(
      /watch\([\s\S]*?\(\) => route\.query\.tab[\s\S]*?\{ immediate: true \}[\s\S]*?\);/,
    );
  });
});
