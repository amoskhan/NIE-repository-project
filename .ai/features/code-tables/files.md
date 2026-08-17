# Code Tables — File Map

## Owned files

### Backend

| Path                                                           | Layer      | Purpose                                                                                                                                                                                                                                                             |
| -------------------------------------------------------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Domain/Models/Code.cs`                  | Entity     | The lookup row — `Name`, `Type`, `Description`, `DisplayName`, `DisplayOrder`, `IsActive`. Inherits `BaseEntity` (no audit)                                                                                                                                         |
| `src/backend/Libraries/Shared/Enum/ECodeType.cs`               | Enum       | Single source of truth for `Code.Type` values: `TITLE`, `USER_TYPE`, `VENDOR_CATEGORY`, `CATALOG_CATEGORY`, `UNIT_OF_MEASURE`, `DELIVERY_LOCATION`, `CURRENCY`. Comment in the file states: "All code types should be present here and never be hardcoded anywhere" |
| `src/backend/Libraries/Shared/Enum/ECodeName.cs`               | Enum       | All allowed `Code.Name` values, grouped by their `Type` with `//Type - ECodeType.X` comments. Same hardcoding prohibition                                                                                                                                           |
| `src/backend/Libraries/Shared/Dto/CodeDto.cs`                  | DTO        | Projection used by `GetAllByCodeType` — `id`, `displayName`, `description`, `displayOrder`, `isActive`                                                                                                                                                              |
| `src/backend/Libraries/Services/Services/Code/ICodeService.cs` | Interface  | `GetAllByCodeTypeAsync(string codeType)`, plus CRUD for admin                                                                                                                                                                                                       |
| `src/backend/Libraries/Services/Services/Code/CodeService.cs`  | Service    | EF Core impl. Filters by `Type` and `IsActive`, orders by `DisplayOrder`                                                                                                                                                                                            |
| `src/backend/API/Controllers/CodeController.cs`                | Controller | `GetAllByCodeType(codeType)` returns `CodeDto[]`; gated by `AccessFunctionCodes.Api.CodeRead`. CRUD endpoints for admin gated by an admin code                                                                                                                      |

### Frontend

| Path                                                       | Layer      | Purpose                                                                                                                                                          |
| ---------------------------------------------------------- | ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/main/src/services/codeTableService.ts`       | Service    | `CodeTableType` const object (mirrors `ECodeType`), `getByType(type, forceRefresh)`, two-Map caching (`codeTableCache` + `pendingRequests`)                      |
| `src/frontend/main/src/composables/useCodeTableOptions.ts` | Composable | Reactive `codeTableOptions: Partial<Record<CodeTableTypeValue, ListFilterOption[]>>`, `loadCodeTableOptions(forceRefresh)`, parallel `Promise.allSettled` loader |

## Touched files

| Path                                                                         | What it contains                                                          | Why must be touched                                                                                                                                 |
| ---------------------------------------------------------------------------- | ------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Data/Data/MainDbContext.cs`                           | DbSet `Codes` + Fluent API for unique index on `(Type, Name)`             | Required for migrations and dedupe                                                                                                                  |
| `src/backend/API/Extensions/DatabaseSeeder.cs` `SeedCodesAsync`              | Seed rows keyed off `ECodeType.X.ToString()` and `ECodeName.Y.ToString()` | Adding a new code value means adding a seed entry here                                                                                              |
| `src/backend/API/Mapping/MappingProfile.cs`                                  | Mapster `Code → CodeDto`                                                  | Required so `ProjectToType<CodeDto>` works in the service                                                                                           |
| `src/backend/API/Program.cs`                                                 | `builder.Services.AddScoped<ICodeService, CodeService>()`                 | DI registration                                                                                                                                     |
| `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs`             | `AccessFunctionCodes.Api.CodeRead` and its seed definition                | Gates the read endpoint                                                                                                                             |
| `src/frontend/main/src/services/codeTableService.ts` `CodeTableType` literal | Mirror of `ECodeType` in FE constant form                                 | Adding a new ECodeType MUST add a matching property here. The TypeScript `CodeTableTypeValue` is the union of values; the type system catches drift |

## Migrations

| Migration                                                        | What it does                                                                                                                                            |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| First migration containing `Codes`                               | Creates `Codes` table with unique `(Type, Name)` constraint                                                                                             |
| (created on each new code) optional `<timestamp>_AddXxxCodes.cs` | Most projects rely on `DatabaseSeeder` upsert on startup; an explicit migration is only needed if you want to enforce a fixed set of rows in the schema |

## External dependencies

None — pure EF Core + Mapster on the BE; pure axios + Vue reactive on the FE. The optional Valkey cache (per-type) is a forward-looking integration that is not yet wired into `CodeService`; FE caching covers the hot path.
