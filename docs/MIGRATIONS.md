# Database Migration Guide

## Migration Commands

All commands should be run from the `src` directory.

### Add a New Migration

```bash
dotnet ef migrations add <MigrationName> --project backend/Libraries/Data --startup-project backend/API
```

**Example:**
```bash
dotnet ef migrations add AddUserProfile --project backend/Libraries/Data --startup-project backend/API
```

### Apply Migrations to Database

```bash
dotnet ef database update --project backend/Libraries/Data --startup-project backend/API
```

### Remove Last Migration (if not applied)

```bash
dotnet ef migrations remove --project backend/Libraries/Data --startup-project backend/API
```

### Generate SQL Script

```bash
dotnet ef migrations script --project backend/Libraries/Data --startup-project backend/API -o migration.sql
```

### Roll Back to Specific Migration

```bash
dotnet ef database update <MigrationName> --project backend/Libraries/Data --startup-project backend/API
```

---

## For New Projects

When starting a new project from this template:

1. Keep the committed `InitialUuidV7Schema` migration when the project has already created or shared a database. It is the template's tested UUIDv7 schema baseline.
2. Before any environment uses the database, a newly scaffolded application may customize the domain and regenerate its own initial migration. This is the only migration-squash window.
3. Every application primary and foreign key must be `Guid` in .NET and `uuid` in PostgreSQL. New keys come from `Uuid7.New()`/`Guid.CreateVersion7()`; do not use identity/serial integers, `Guid.NewGuid()`, or a UUIDv4 database default.
4. After changing the model, create and inspect the migration:
   ```bash
   dotnet ef migrations add <MigrationName> --project backend/Libraries/Data --startup-project backend/API
   ```
5. Verify every `Id`, foreign key, owner ID, and source ID column is PostgreSQL `uuid`, then apply it:
   ```bash
   dotnet ef database update --project backend/Libraries/Data --startup-project backend/API
   ```

## Existing Applications Moving from Integer IDs

UUIDv7 conversion is a breaking data and API migration. Do not delete migrations, replay `InitialUuidV7Schema`, directly cast integer keys to `uuid`, or drop the old key columns in the first deployment.

Follow task `0032-standardize-uuidv7-primary-keys` with owner approval:

1. inventory every primary/foreign/polymorphic ID and every API, cache, message, export, bookmark, fixture, and integration carrying it;
2. expand with nullable UUID columns while old integer keys remain authoritative;
3. generate an old-to-new mapping in C# with `Guid.CreateVersion7()`, then backfill parents and dependents in dependency order;
4. validate UUID version, uniqueness, nullability, row counts, and foreign-key/orphan integrity against a production-like PostgreSQL copy;
5. deploy an approved dual-read/dual-write compatibility step or maintenance window;
6. contract to UUID constraints and application contracts while retaining old columns and the mapping through the rollback window;
7. remove old columns/sequences only after production verification and owner sign-off.

The exact mapping SQL and rollout order are application-specific and must be reviewed and tested; agents must not invent a generic destructive conversion.

---

## Migration Best Practices

### Naming Conventions

Use descriptive names that indicate what the migration does:

| ✅ Good Names | ❌ Bad Names |
|---------------|--------------|
| `AddUserProfile` | `Migration1` |
| `AddOrderStatusColumn` | `Update1` |
| `CreateProductTable` | `Changes` |
| `AddIndexOnEmail` | `Fix` |
| `RenameCustomerToClient` | `New` |

### Before Creating a Migration

1. Ensure the database is up-to-date with existing migrations
2. Double-check your model changes
3. Consider backward compatibility
4. Confirm every application identifier remains Guid/UUIDv7 and every generated identifier column is PostgreSQL `uuid`

### After Creating a Migration

1. Review the generated migration code
2. Test the migration locally
3. Test the rollback (`dotnet ef database update <PreviousMigration>`)
4. Run the UUIDv7 architecture tests and `dotnet ef migrations has-pending-model-changes`

---

## Troubleshooting

### "The migration has already been applied"

You need to remove it from the database first:
```bash
dotnet ef database update <PreviousMigration> --project backend/Libraries/Data --startup-project backend/API
dotnet ef migrations remove --project backend/Libraries/Data --startup-project backend/API
```

### "No DbContext was found"

Ensure you're specifying the correct startup project:
```bash
--startup-project backend/API
```

### "Connection refused"

Make sure PostgreSQL is running:
```bash
docker ps  # Check if postgres container is running
```
