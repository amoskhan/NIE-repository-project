# 11 - Customization Boundary

The App Template is a shared starting point. Derived projects edit their own features and inherit the shell, authentication, and platform infrastructure.

## The One Rule

Locked files contain machinery, not project data. Project data lives in `src/frontend/main/src/app-config/*` on the frontend and in feature-owned backend files.

## Frontend

### Locked - Template-Owned

| Path                                                  | What it is                            |
| ----------------------------------------------------- | ------------------------------------- |
| `src/frontend/main/src/staff/layouts/StaffLayout.vue` | Staff shell                           |
| `src/frontend/main/src/composables/useSidebar.ts`     | Sidebar responsive state              |
| `src/frontend/main/src/composables/usePermissions.ts` | Navigation filtering and role labels  |
| `src/frontend/main/src/composables/navTypes.ts`       | Navigation item shape                 |
| `src/frontend/main/src/router/index.ts`               | Router machinery                      |
| `src/frontend/main/src/constants/permissions.ts`      | Permission-resolution machinery       |
| `src/frontend/main/src/components/common/**`          | Shared common components              |
| `src/frontend/packages/ui/src/components/**`          | `@apptemplate/ui` components          |
| `src/frontend/packages/ui/src/theme/**`               | Shared theme runtime                  |
| `src/frontend/packages/shared/**`                     | Shared constants, i18n, Sentry, types |

### Project-Owned

| Path                                                                | What you put here                        |
| ------------------------------------------------------------------- | ---------------------------------------- |
| `src/frontend/main/src/app-config/navigation.ts`                    | Sidebar menu items                       |
| `src/frontend/main/src/app-config/routes.ts`                        | Project routes                           |
| `src/frontend/main/src/app-config/accessFunctions.ts`               | Access-function codes, role maps, labels |
| `src/frontend/main/src/app-config/branding.ts`                      | Logo asset and feedback-widget prefix    |
| `src/frontend/main/src/theme/appTheme.ts`                           | Brand label and theme presets            |
| `src/frontend/main/src/staff/pages/**`, `src/pages/**`              | Feature pages                            |
| `src/frontend/main/src/components/**` outside `common`              | Feature components                       |
| `src/frontend/main/src/services/**`, `types/**`, domain composables | Feature plumbing                         |

The whole `src/frontend/auth/` app and the Auth API are locked for feature work. Rebrand auth through its theme/config files only.

## Backend

### Locked - Template-Owned

| Path                                                 | What it is                                                                  |
| ---------------------------------------------------- | --------------------------------------------------------------------------- |
| `Libraries/Domain/Models/BaseEntity.cs`              | Entity base class                                                           |
| `Libraries/Domain/Models/TimestampedEntity.cs`       | Auditable entity base class                                                 |
| `Libraries/Shared/Models/IOwnedEntity.cs`            | Ownership marker contract                                                   |
| `Libraries/Services/Services/Base/IBaseService.cs`   | Generic CRUD contract                                                       |
| `Libraries/Services/Services/Base/BaseService.cs`    | Generic CRUD base implementation                                            |
| `API/Controllers/BaseController.cs`                  | Session/auth context base                                                   |
| `API/Middleware/**`                                  | Session validation, exception handling, security headers, correlation, ETag |
| `API/Authorization/**`                               | `RequireAccessFunction` attribute and handlers                              |
| `Libraries/Data/Data/MainDbContext.cs`               | DbContext with auto-audit and auto-timestamps                               |
| `API/Mapping/MappingProfile.cs`                      | Mapster registration                                                        |
| `API/Program.cs`                                     | Service registration and middleware pipeline                                |
| `Libraries/Shared/Security/AccessFunctionCatalog.cs` | Canonical access-function codes and role bundles                            |

### Project-Owned

- New entities: `Libraries/Domain/Models/<Entity>.cs`.
- New DTOs: `Libraries/Shared/Dto/<Dto>.cs`.
- New enums: `Libraries/Shared/Enum/E<Name>.cs`.
- New services: `Libraries/Services/Services/<Feature>/I<Name>Service.cs` and `Libraries/Services/Services/<Feature>/<Name>Service.cs`.
- New service-local request/result/helper types: their own files in the same `Libraries/Services/Services/<Feature>/` folder.
- New controllers: `API/Controllers/*` extending `BaseController`.
- New migrations: `Libraries/Data/Migrations/*`.

Domain is not project-owned for arbitrary supporting types. Keep Domain entity-only.

## Extending Locked Backend Files

Some cross-cutting registrations must reference project types. Do not scatter feature edits across locked files. Use one of these patterns:

- Register through a feature-owned `IServiceCollection` extension that `Program.cs` calls once.
- Use a `partial` class or extension owned by the feature.
- Follow an existing fenced `// === SAMPLE ... ===` hook exactly when one exists.

## Inheriting Upstream Changes

- Frontend shell file: copy the newer locked file from the template over the project file.
- `@apptemplate/ui` or `@apptemplate/shared`: bump the workspace package.
- Backend infrastructure: apply the matching `.ai/tasks/NNNN-*` task.

## Related

- Feature dossier: `.ai/features/app-shell-navigation/`
- Hard rules: `.ai/common/04-do-and-dont.md`
- Versioning: `.ai/common/09-template-versioning.md`
