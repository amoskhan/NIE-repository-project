# Procurement — Remove

Do this once your own entities exist. The sample is a teaching aid; leaving it in a finished project is
confusing at best and looks unfinished at worst.

If your `.ai/tasks/` directory contains a task that removes the procurement sample, follow that task —
it is the line-by-line version and it is what the alignment agent consumes. Otherwise, work through
the steps below.

## 1. Pre-check

```bash
# Nothing to do if it's already gone
ls src/backend/Libraries/Domain/Models/Samples/Procurement/ 2>/dev/null || echo "already removed"

# Commit or stash first — this touches a lot of files
git status --short
```

## 2. Delete the owned files

Everything listed under **Owned files** in [`files.md`](./files.md) is delete-with-feature. The sample
was deliberately collected under `Samples/Procurement/` directories so this is mostly a directory
delete rather than a file hunt:

```bash
rm -rf src/backend/Libraries/Domain/Models/Samples/Procurement/
rm -rf src/backend/Libraries/Shared/Dto/Samples/Procurement/
rm -rf src/backend/Libraries/Shared/Enum/Samples/Procurement/
rm -rf src/backend/Libraries/Services/Services/Samples/Procurement/
rm -rf src/backend/API/Controllers/Samples/
```

Then the frontend pages and services — check [`files.md`](./files.md) for the exact paths in your tree,
since a project that has already reorganised things will not match the template layout.

## 3. Remove the fenced wiring

Some wiring cannot live in a sample directory because it has to sit inside a shared file. Every such
block is fenced:

```csharp
// === SAMPLE: procurement ... ===
...
```

Delete each fenced block, fence comments included. They appear in:

- `src/backend/API/Program.cs` — service registrations
- `src/backend/API/Mapping/MappingProfile.cs` — Mapster mappings
- `src/backend/API/Extensions/DatabaseSeeder.cs` — the demo seed and its helper methods
- `src/backend/Libraries/Data/Data/MainDbContext.cs` — `DbSet`s and relationship configuration
- `src/backend/Libraries/Data/Data/MainDbContextSeeder.cs` — procurement `Code` rows
- `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs` — `Procurement*` codes and their role seeds
- `src/frontend/main/src/app-config/accessFunctions.ts` — the frontend mirror of those codes

Find any you missed:

```bash
grep -rn "SAMPLE: procurement" src/
# Expect: no output when you are done
```

## 4. Remove the frontend project data

These are yours to edit — the shell reads them, it does not contain them:

- `src/frontend/main/src/app-config/navigation.ts` — drop the procurement nav items
- `src/frontend/main/src/app-config/routes.ts` — drop the procurement routes
- `src/frontend/main/src/app-config/accessFunctions.ts` — drop the procurement codes and role mappings

## 5. Drop the tables

```bash
dotnet ef migrations add RemoveProcurementSample \
  --project src/backend/Libraries/Data \
  --startup-project src/backend/API

dotnet ef database update \
  --project src/backend/Libraries/Data \
  --startup-project src/backend/API
```

Read the generated migration before applying it. EF drops tables it no longer sees a `DbSet` for —
confirm it is dropping the procurement tables and nothing else.

## 6. Rename the feedback namespace

The feedback widget's `function_id` prefix follows the sample. Change `procurement.` to your own
project slug — see [`.ai/features/feedback-widget/customize.md`](../../feedback-widget/customize.md) §1.

## 7. Verify

```bash
dotnet build src/backend/AppTemplate.sln
# Expect: 0 errors

grep -rin "procurement\|purchaseorder\|catalogitem" src/ --include=*.cs --include=*.ts --include=*.vue
# Expect: no output

pnpm -C src/frontend --filter main type-check
pnpm -C src/frontend --filter main build:production
```

Then run the app: sign in, load the dashboard, click through your own feature. No dead nav items, no
404 routes, no console errors.

## 8. Rollback

```bash
git restore .
dotnet ef migrations remove --project src/backend/Libraries/Data --startup-project src/backend/API
```
