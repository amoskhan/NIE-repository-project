# Data-table user preferences

Database-backed, user-scoped default views for every live application list rendered by the shared `NieDataTable`.

Rules version: 2026.08.07.6
Feature key: data-table-user-preferences
Adoption: **mandatory when an application renders one or more data tables**

## Adoption and navigation

- Menu or entry point: no separate sidebar menu. Every eligible `NieDataTable` exposes the shared **View** preferences action in its list toolbar.
- Dialog sections: Columns, Sorting, Default filters, and Display.
- Visibility: any authenticated user who can open the containing list may manage only their own preferences.
- Applications with no data-table list record this feature as not-applicable. A table consumer may not opt out independently.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET | dotnet | 10.0.0 | runtime |
| ASP.NET Core | Microsoft.AspNetCore.App | 10.0.0 | shared framework |
| Entity Framework Core | Microsoft.EntityFrameworkCore | 10.0.5 | NuGet |
| Npgsql EF provider | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.0 | NuGet |
| FluentValidation | FluentValidation | 12.1.1 | NuGet |
| PostgreSQL | postgresql | 17.0 | service |
| Vue | vue | 3.5.30 | npm |
| TypeScript | typescript | 6.0.2 | npm |

Versions are minimum floors. Derived applications may upgrade to later stable compatible releases after migration, build, security, and regression evidence passes. This feature uses the existing stack and must not introduce a second state, validation, table, or persistence library.

## Numbered rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-DTP-001 | error | coverage | Give every live `NieDataTable` consumer a stable, lowercase, domain-qualified `preferenceKey` and an integer `definitionVersion`. Increment the definition version whenever columns or compatible preference semantics change. | consumer contract tests |
| NIE-DTP-002 | error | ownership | Keep `@nie/ui` provider-neutral. It owns typed preference contracts, normalization, presentation, accessibility, and provide/inject integration; the host application supplies an API-backed `NieDataTablePreferenceStore`. | architecture and type checks |
| NIE-DTP-003 | error | persistence | Store one row per application, signed-in user, and table key in PostgreSQL with a UUIDv7 primary key, JSONB settings, definition version, revision, timestamps, a unique composite index, and an application foreign key. Do not use localStorage as the authority. | migration and PostgreSQL tests |
| NIE-DTP-004 | error | identity-security | Derive the user identity from the authenticated server context. Never accept a user ID from the browser. Authorize every preference endpoint with the personal-preference access function and scope every read, write, and delete by application and user. | controller, authorization, and isolation tests |
| NIE-DTP-005 | error | validation | Validate request bodies with FluentValidation. Permit only page sizes 10, 20, 50, and 100; at most five unique sorts; bounded unique filters and columns; and only compact, comfortable, spacious plus elevated, minimal, striped display values. | validator tests |
| NIE-DTP-006 | error | concurrency | Use an application-managed positive revision as an optimistic concurrency token. Reject stale updates with HTTP 409 and a sanitized problem response; never silently overwrite another browser session. | service and API tests |
| NIE-DTP-007 | error | drift | Treat stored JSON as untrusted and versioned. Before the first data request, remove unknown or duplicate columns, sorts, and filters; append newly introduced columns; repair invalid page/display values; and continue with a safe usable view. Never fail or blank the list solely because a saved view is incompatible. | component normalization tests |
| NIE-DTP-008 | error | repair-ux | Reserve repair mode for a loaded preference whose stored format, definition version, columns, sorts, filters, or display values no longer match the current schema. Show a concise warning without exposing raw JSON or exceptions; the warning opens the shared dialog, explains every human-readable repair, and requires an explicit **Repair and save** action. A save transport or concurrency failure is not schema drift and must never switch a healthy preference into repair mode. | component and browser tests |
| NIE-DTP-009 | error | initial-query | Load and normalize the preference before the table emits its initial server query. Apply saved page size, ordered sorts, and filters to the first API request; do not fetch all rows or perform a hidden initial request with obsolete defaults. | API request and component tests |
| NIE-DTP-010 | error | sorting | Preserve ordered multi-column sorting end to end. Shift-click and the Sorting dialog expose priority one through five; the request carries the ordered array; backend allowlists map each key to provider-translatable expressions; and a stable primary-key tie-breaker makes pagination deterministic. | component, query-map, and endpoint tests |
| NIE-DTP-011 | error | filters | Persist only explicitly supported filter keys and selected primitive values. The shared Default filters builder lists every eligible table column without requiring a column-filter popup to be opened first; selecting a column, searching values, or changing a value page calls its allowlisted server facet endpoint. Preserve multi-select values across facet pages and configured columns, include the complete draft filter set in dependent facet requests, and keep every applied filter, sort, search, page, and page-size change server-backed for live lists. | component and endpoint tests |
| NIE-DTP-012 | error | columns | Let users order and hide eligible columns while preserving non-hideable columns. Reordering changes presentation only; APIs remain DTO/allowlist based and never accept raw field or SQL expressions. | component and security tests |
| NIE-DTP-013 | error | views | Provide compact, comfortable, and spacious density plus elevated, minimal, and striped appearance through the single shared table. Consumers must not fork table markup to add another view. | component and parity tests |
| NIE-DTP-014 | error | responsive | At desktop and tablet sizes, keep a bounded table with sticky headers and internal two-axis scrolling. At phone width, render compact readable cards, keep the single-row pagination footer above the fixed search/filter toolbar without overlap, use nearly all remaining dynamic viewport height, contain overscroll, respect safe areas, and prevent document-level horizontal overflow. The shared preferences dialog must reflow sort and default-filter controls from its own available content width rather than viewport breakpoints, keep every select readable, prevent control overlap, and keep its primary footer action fully visible and at least 44 pixels high at phone, tablet, and desktop widths. | Playwright screenshots, geometry, and overflow assertions |
| NIE-DTP-015 | error | accessibility | The settings action, repair warning, dialog navigation, column visibility/reorder controls, sort controls, filters, and save/reset actions require labels, keyboard operation, visible focus, minimum shared control geometry, modal focus containment, and screen-reader semantics. | component and browser accessibility tests |
| NIE-DTP-016 | error | privacy-audit | Treat saved filters as potentially personal. Do not log or audit the JSON payload or raw filter values. Audit preference entity lifecycle metadata and revision changes, and apply normal retention/access controls to the row. | audit configuration and security review |
| NIE-DTP-017 | error | reset | Provide an idempotent reset that deletes only the signed-in user's selected table preference, restores current screen defaults, and immediately refreshes the server query. | component, service, and API tests |
| NIE-DTP-018 | error | performance | Load preferences lazily per visible table with request deduplication/cache inside the host adapter. Keep one small JSON document per table; do not join preference JSON into business queries or create one row per column/filter. | code review and query inspection |
| NIE-DTP-019 | error | failures | A preference API load failure must leave the list usable with screen defaults and a review warning. Save conflicts or failures keep the dialog open, preserve the user's current in-memory view, and show a separate retryable save error while retaining the prior repair state. On HTTP 409, disable Save and require an explicit **Reload latest** action that fetches the authoritative revision and rebases the dialog before Save is enabled again; do not silently retry a stale revision or mislabel a transport failure as repair. | failure-path tests |
| NIE-DTP-020 | error | verification | Run backend warnings-as-errors build, migration/model review, preference isolation/concurrency/malformed-JSON tests, frontend lint/type-check/build, component tests, and real desktop/tablet/phone browser checks before release. | standard gates and artifacts |
| NIE-DTP-021 | error | weekly-filter-reminder | When a loaded saved preference has one or more active default filters and its server-generated acknowledgement is missing or at least seven days old, show the shared non-dismissible reminder before normal table interaction continues. Warn that records may be hidden and summarize filter labels plus selected counts without exposing values. **Keep for another week** must save the unchanged view through optimistic concurrency and receive a new server UTC acknowledgement. **Remove default filters** must clear only filters, preserve all other preference settings, reset to page one, and immediately request the server without filters. A save failure keeps the reminder and active filters. A conflict must keep normal interaction blocked, require explicit reload/rebase, and re-evaluate the authoritative preference before either restoring the non-dismissible reminder or resuming the table. Do not use localStorage or a client-authored timestamp as the authority, and do not show the reminder for ad-hoc unsaved filters. | service, component, time-boundary, failure, conflict, and browser tests |

## Canonical reference footprint

- `src/backend/Core/Domain/Models/Preferences/UserDataTablePreference.cs`
- `src/backend/Core/Application/Contracts/DataTables/DataTablePreferenceDto.cs`
- `src/backend/Core/Application/Features/DataTablePreferences/`
- `src/backend/Hosts/Api/Controllers/Preferences/UserDataTablePreferenceController.cs`
- `src/backend/Hosts/Api/Validation/DataTables/DataTablePreferenceValidators.cs`
- `src/backend/Infrastructure/Persistence/Data/MainDbContext.cs`
- `src/frontend/packages/ui/src/components/composite/data-table/NieDataTable.vue`
- `src/frontend/packages/ui/src/components/composite/data-table/NieDataTablePreferencesModal.vue`
- `src/frontend/packages/ui/src/components/composite/data-table/NieDataTableFilterReminderModal.vue`
- `src/frontend/packages/ui/src/components/composite/data-table/NieDataTableDefaultFilterBuilder.vue`
- `src/frontend/packages/ui/src/components/composite/data-table/preferences.ts`
- `src/frontend/apps/main/src/services/preferences/dataTablePreferenceService.ts`
- Procurement Vendor, Catalog, and Order History screens as reference consumers only

## Migration and rollback

1. Apply the additive `UserDataTablePreferences` migration before enabling the new frontend.
2. Give each existing table a stable key and definition version, then remove any page-owned initial request so `NieDataTable` controls the first query.
3. Map every allowed sort key on the backend and preserve a deterministic UUIDv7 tie-breaker.
4. If rolling back application code, retain the additive table. Old applications ignore it safely. Drop stored preferences only after an explicit data-retention decision; reset is normally sufficient.

## Portability

The application contract and `@nie/ui` store interface are provider-neutral. PostgreSQL JSONB is the canonical adapter; a replacement relational provider may store the validated JSON as a native JSON or bounded text column while preserving unique scope, optimistic revision, exports, reset, and all contract tests.
