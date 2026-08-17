import { expect, test } from "@playwright/test";
import { existsSync, readFileSync } from "node:fs";
import * as path from "node:path";

const repoRoot = process.cwd().endsWith(`${path.sep}tests`)
  ? path.resolve(process.cwd(), "..")
  : process.cwd();

const buildPath = (relativePath: string) =>
  path.resolve(repoRoot, "build", relativePath);

test.describe("nginx maintenance page", () => {
  test("serves a branded maintenance page for upstream outages", () => {
    const nginxConfig = readFileSync(buildPath("nginx.conf"), "utf8");
    const dockerfile = readFileSync(buildPath("Dockerfile.ui"), "utf8");
    const maintenancePath = buildPath("maintenance.html");

    expect(existsSync(maintenancePath)).toBe(true);

    const maintenanceHtml = readFileSync(maintenancePath, "utf8");

    expect(nginxConfig).toContain("error_page 502 503 504 /maintenance.html;");
    expect(nginxConfig).toContain("location = /maintenance.html");
    expect(nginxConfig).not.toContain("error_page 502 503 504 /50x.html;");

    expect(dockerfile).toContain(
      "COPY build/maintenance.html /usr/share/nginx/html/maintenance.html",
    );

    expect(maintenanceHtml).toContain(
      "<title>App Template - Maintenance</title>",
    );
    expect(maintenanceHtml).toContain("Service temporarily unavailable");

    // The page is served precisely when upstreams are down, so it must not depend on
    // anything it would have to fetch over the network.
    expect(maintenanceHtml).not.toContain("<script");
    expect(maintenanceHtml).not.toMatch(/<link[^>]+href="https?:/);
  });
});
