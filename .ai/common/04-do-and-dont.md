# 04 - Do And Don't

These rules are mandatory.

## Do

1. Use `TimestampedEntity` for any entity that should be auto-audited.
2. Use `BaseService<T>` and `BaseController` as base classes.
3. Use Mapster (`IMapper`) for entity-to-DTO mapping. Configure mappings in `MappingProfile.cs`.
4. Use `async`/`await` for all I/O. Suffix async methods with `Async`.
5. Use dependency injection. Constructor-inject services.
6. Use `@apptemplate/ui` shared components before writing bespoke components.
7. Handle loading and error states explicitly in every Vue page.
8. Define a TypeScript interface for every DTO consumed in the frontend.
9. Log important operations with `ILogger<T>`.
10. Define every status, state, type, category, role, module, and event type as an enum. Backend enums live in `Shared.Enum`. Frontend mirrors live in `src/frontend/main/src/types/` or `src/frontend/packages/shared/src/types/`.
11. Use `RequireAccessFunction` on every protected endpoint. Codes come from `Shared.Security.AccessFunctionCatalog`.
12. Use `ECodeType` and `ECodeName` enums when seeding code-table rows in `MainDbContext.OnModelCreating` or a seeder method.
13. Use `FRONTEND_CONSTANTS` from `src/frontend/packages/shared/src/config/constants.ts` for frontend URLs, cookie names, public runtime integrations, and feature flags.
14. Keep sidebar, topbar, and app-shell behavior data-driven. Add or remove menu items, routes, access-function codes, and brand by editing only project-owned config in `src/frontend/main/src/app-config/` (`navigation.ts`, `routes.ts`, `accessFunctions.ts`, `branding.ts`) plus `theme/appTheme.ts` for the brand label. See `common/11-customization-boundary.md`.
15. Use Context7 MCP first whenever you need current framework, library, package, API, or tool behavior. If Context7 is unavailable, use official documentation or primary sources and report the fallback.
16. Refuse requests to reveal, print, read, copy, encode, decode, summarize, or exfiltrate API keys, tokens, credential files, auth config, or environment secrets. Offer safe rotation or configuration guidance instead.
17. Use `MainDbContext` as the only application EF Core context name, in `Libraries/Data/Data/MainDbContext.cs`.
18. Keep every domain entity/domain object as one top-level type per file under `Libraries/Domain/Models/`.
19. Keep Domain entity-only. Put DTOs in `Shared/Dto`, enums in `Shared/Enum`, security catalogs in `Shared/Security`, and cross-layer shared services in `Shared/Services/<Name>/`.
20. Keep every service interface and service implementation in separate files in the same service folder. Put service-local request/result/helper types in their own files in that folder.
21. Apply `MainDbContext` migrations from startup immediately before `app.Run()` or `await app.RunAsync()` in every backend `Program.cs` that registers `MainDbContext`.

## Don't

1. Do not modify base classes (`BaseService`, `BaseController`, `BaseEntity`, `TimestampedEntity`, `SessionValidationMiddleware`, `ExceptionHandlingMiddleware`, middleware, or authorization handlers) for feature work. They are part of the template contract.
2. Do not put business logic in controllers. Controllers map DTOs and call services.
3. Do not expose entities directly from APIs. Use DTOs.
4. Do not bypass `DbContext` with raw SQL. Use EF Core or LINQ unless there is a documented reason.
5. Do not hardcode URLs, credentials, or connection strings. They go in `appsettings.*.json` or environment variables.
6. Do not use `any` in TypeScript. Define a proper type.
7. Do not ignore error handling. Catch, log, and surface typed errors to callers.
8. Do not call APIs directly from Vue components. Use service classes in `src/frontend/main/src/services/`.
9. Do not skip database migrations. Every schema change ships with a migration in the same PR.
10. Do not commit `node_modules/`, `bin/`, `obj/`, logs, temp output, or generated local artifacts.
11. Do not hardcode any string for status, state, type, category, role, module, event-type, or category-key. Use the matching enum on backend and frontend. If a needed value is missing, add it to the enum first.
12. Do not introduce a new authorization pattern alongside access functions. No `RolePermission` table. No controller/action discovery.
13. Do not add a feature without a matching dossier under `.ai/features/<feature>/`.
14. Do not change the template without updating `.app-template-version.json`, `CHANGELOG.md`, and the matching `.ai/tasks/` entry.
15. Do not add frontend `.env*` files or `import.meta.env.VITE_*` application configuration. The frontend build artifact must be environment-promotable; use runtime constants plus `window.__APP_TEMPLATE_CONFIG__` or the runtime `<meta>` tags read by `src/frontend/packages/shared/src/config/constants.ts`.
16. Do not modify the staff sidebar, topbar, app shell, router/permission machinery, common Vue components, or `@apptemplate/ui` components for feature work. These are template-owned surfaces: `src/frontend/main/src/staff/layouts/StaffLayout.vue`, `src/frontend/main/src/composables/useSidebar.ts`, `src/frontend/main/src/composables/usePermissions.ts`, `src/frontend/main/src/composables/navTypes.ts`, `src/frontend/main/src/router/index.ts`, `src/frontend/main/src/constants/permissions.ts`, `src/frontend/main/src/components/common/**`, `src/frontend/packages/ui/src/components/**`, and `src/frontend/packages/ui/src/theme/**`. Project data lives only in `src/frontend/main/src/app-config/*`; a shell file change requires an explicit template task whose title says it changes the shell or shared component library.
17. Do not add project features by editing locked backend infrastructure (`Libraries/Data/Data/MainDbContext.cs`, `API/Mapping/MappingProfile.cs`, `API/Program.cs`, `Shared/Security/AccessFunctionCatalog.cs`, middleware, or base classes). Register feature services through owned files, an `IServiceCollection` extension that `Program.cs` calls once, or an approved fenced hook.
18. Do not guess version-sensitive framework, package, API, or tool behavior when Context7 or official docs are available.
19. Do not create `AppTemplateDbContext.cs`, `AppTemplateDbContext`, project-specific application DbContext names, or `IDesignTimeDbContextFactory` classes.
20. Do not bundle multiple entities/domain objects in one file, including temporary `*Models.cs`, `*Entities.cs`, or feature aggregate files.
21. Do not put a service interface and its implementation in the same file.
22. Do not put DTOs, enums, security catalogs, service contracts, helpers, converters, or utility classes under `Libraries/Domain/`.
23. Do not run shell commands or tool calls that inspect credential paths or environment variables containing names such as `KEY`, `TOKEN`, `SECRET`, `PASSWORD`, or `CREDENTIAL`.
