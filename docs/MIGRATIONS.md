# Database Migration Guide

> **You usually do not need to apply migrations by hand.** The Main API calls `Database.MigrateAsync()` on startup and then runs the seeder, so restarting the API brings your database up to date. The commands below are for creating migrations, inspecting them, rolling back, and generating SQL for a deployment.

## Migration Commands

All commands below are run from the `src` directory. From the repository root, prefix the project paths with `src/`.

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

## Starting fresh in a new project

The template ships with migrations for its own tables (users, roles, access functions, code tables, documents, audit logs, workflow state) plus the procurement sample. You can keep them and add yours on top — that is the simplest path, and the one to take if you are unsure.

If you want a single clean initial migration instead, do it **before** you have any real data:

1. **Remove the sample domain first.** Use the removal task under `.ai/tasks/` so the entities, services, controllers, pages, routes, and access-function codes all go together. Deleting entities by hand and leaving a controller behind is the usual way this goes wrong.

2. **Delete the existing migrations:**

   ```bash
   rm -rf backend/Libraries/Data/Migrations/*
   ```

3. **Drop and recreate the local database** so it does not disagree with the new history:

   ```bash
   # `-v` cannot be combined with a service name — Compose rejects it. Take the
   # whole dev stack down with its volumes, then bring PostgreSQL back up.
   docker compose -f ../.devcontainer/docker-compose.yml down -v
   docker compose -f ../.devcontainer/docker-compose.yml up -d postgres
   ```

4. **Create the initial migration:**

   ```bash
   dotnet ef migrations add InitialCreate --project backend/Libraries/Data --startup-project backend/API
   ```

5. **Start the Main API.** It applies the migration and reseeds. Or apply it explicitly:
   ```bash
   dotnet ef database update --project backend/Libraries/Data --startup-project backend/API
   ```

Never do any of this against a database that holds data you care about.

---

## Migration Best Practices

### Naming Conventions

Use descriptive names that indicate what the migration does:

| ✅ Good Names            | ❌ Bad Names |
| ------------------------ | ------------ |
| `AddUserProfile`         | `Migration1` |
| `AddOrderStatusColumn`   | `Update1`    |
| `CreateProductTable`     | `Changes`    |
| `AddIndexOnEmail`        | `Fix`        |
| `RenameCustomerToClient` | `New`        |

### Before Creating a Migration

1. Ensure the database is up-to-date with existing migrations
2. Double-check your model changes
3. Consider backward compatibility

### After Creating a Migration

1. Review the generated migration code
2. Test the migration locally
3. Test the rollback (`dotnet ef database update <PreviousMigration>`)

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

Make sure PostgreSQL is running (these run from `src/`, like every other command in this guide — drop the `../` if you are at the repository root):

```bash
docker compose -f ../.devcontainer/docker-compose.yml ps
docker compose -f ../.devcontainer/docker-compose.yml up -d postgres
```

### "The model for context 'MainDbContext' has pending changes"

Your entity classes and the migration history disagree. Add a migration for the change you made, or revert the model edit.

### The migration is generated but the table never appears

The Main API applies migrations on startup — if it is already running, restart it. If it started before PostgreSQL was ready, it may have failed the migration step; check its terminal output for the exception rather than assuming it succeeded.
