import { existsSync, readFileSync, readdirSync } from "node:fs";
import { basename, relative, resolve } from "node:path";
import { describe, expect, it } from "vitest";

const repositoryRoot = resolve(process.cwd(), "../../../..");
const repositoryPath = (...segments: string[]) =>
  resolve(repositoryRoot, ...segments);
const readRepositoryFile = (...segments: string[]) =>
  readFileSync(repositoryPath(...segments), "utf8");
const ignoredDirectoryNames = new Set(["bin", "obj", "node_modules", "dist", "coverage"]);
const walkFiles = (directory: string): string[] =>
  readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) {
      return ignoredDirectoryNames.has(entry.name) ? [] : walkFiles(path);
    }

    return [path];
  });

describe("backend-configured administration features", () => {
  it("does not ship Global Settings or Monitoring routes and sidebar entries", () => {
    const navigation = readRepositoryFile(
      "src/frontend/apps/main/src/app-config/navigation.ts",
    );
    const routes = readRepositoryFile(
      "src/frontend/apps/main/src/app-config/routes.ts",
    );

    expect(navigation).not.toMatch(/Global Settings|Monitoring/);
    expect(routes).not.toMatch(/global-settings|MonitoringPage|monitoring/);
    expect(routes).toContain('redirect: { name: "notification-administration" }');
  });

  it("does not ship the removed frontend and backend feature files", () => {
    const removedFileNames = new Set([
      "GlobalSettingsPage.vue",
      "MonitoringPage.vue",
      "globalSettingsService.ts",
      "GlobalSettingsController.cs",
      "GlobalSetting.cs",
      "GlobalSettingDto.cs",
      "GlobalSettingService.cs",
      "IGlobalSettingService.cs",
    ]);
    const unexpectedFiles = walkFiles(repositoryPath("src"))
      .filter((path) => removedFileNames.has(basename(path)))
      .map((path) => relative(repositoryRoot, path).replaceAll("\\", "/"));

    expect(unexpectedFiles).toEqual([]);
  });

  it("removes runtime settings persistence and access-function registrations", () => {
    const runtimeSources = [
      "src/backend/Hosts/Api/Program.cs",
      "src/backend/Hosts/Api/Validation/DataTables/DataTableRequestValidators.cs",
      "src/backend/Infrastructure/Persistence/Data/MainDbContext.cs",
      "src/backend/Infrastructure/Persistence/Data/MainDbContextSeeder.cs",
      "src/backend/Core/Application/Security/AccessFunctionCatalog.cs",
      "src/frontend/apps/main/src/app-config/accessFunctions.ts",
    ]
      .map((path) => readRepositoryFile(path))
      .join("\n");

    expect(runtimeSources).not.toMatch(
      /DbSet<GlobalSetting>|modelBuilder\.Entity<GlobalSetting>|SeedGlobalSettings|SettingsView|SettingsRead|Api\.SettingsManage/,
    );
  });

  it("retains backend configuration, observability, and platform health probes", () => {
    const apiProgram = readRepositoryFile("src/backend/Hosts/Api/Program.cs");
    const apiObservability = repositoryPath(
      "src/backend/Hosts/Api/Extensions/Observability/ObservabilityExtensions.cs",
    );
    const authObservability = repositoryPath(
      "src/backend/Hosts/Auth/Extensions/ObservabilityExtensions.cs",
    );

    expect(apiProgram).toContain("builder.AddObservability(");
    expect(apiProgram).toContain("builder.Services.AddHealthChecks()");
    expect(apiProgram).toContain('app.MapHealthChecks("/health")');
    expect(apiProgram).toContain('app.MapHealthChecks("/health/ready"');
    expect(apiProgram).toContain('app.MapGet("/health/live"');
    expect(existsSync(apiObservability)).toBe(true);
    expect(existsSync(authObservability)).toBe(true);
  });
});
