# Contributing & Best Practices

House style for working in an App Template codebase — yours or the template itself.

If you are new here, read [`GETTING-STARTED.md`](GETTING-STARTED.md) first. This document assumes you already know where things live.

---

## Code Style

### Backend (.NET)

- Use `async/await` for all I/O operations
- Use meaningful names for variables, methods, and classes
- Follow C# naming conventions (PascalCase for public, camelCase for private)
- Keep methods small and focused
- Add XML documentation for public APIs

### Frontend (Vue/TypeScript)

- Use Composition API with `<script setup>`
- Use TypeScript for all files
- Follow Vue 3 naming conventions
- Keep components small and reusable
- Use composables for shared logic

---

## Do's and Don'ts

### ✅ DO's

#### Architecture & Design

1. **Use the established patterns** - Follow existing code structure and conventions
2. **Extend base classes** - Use `BaseEntity`, `TimestampedEntity`, `BaseService`, `BaseController`
3. **Use dependency injection** - Register services in `Program.cs` and inject them
4. **Separate concerns** - Controllers → Services → Data Access
5. **Use DTOs** - Never expose entities directly to the API

#### Backend Development

6. **Use `TimestampedEntity`** for all entities needing audit tracking
7. **Register mappings** in `MappingProfile.cs` for all DTOs
8. **Handle errors gracefully** - Use try/catch and return appropriate HTTP status codes
9. **Log important operations** - Use `ILogger` for debugging and monitoring
10. **Validate input** - Check required fields and business rules in services
11. **Use `MainDbContext` only** - the EF Core context file is `Libraries/Data/Data/MainDbContext.cs`
12. **Keep Domain entity-only** - entities live in `Libraries/Domain/Models`, DTOs in `Libraries/Shared/Dto`, enums in `Libraries/Shared/Enum`, and security catalogs in `Libraries/Shared/Security`
13. **Keep one top-level backend type per file** for domain models/entities and service-local types
14. **Split service contracts and implementations** - `IThingService.cs` and `ThingService.cs` live in the same service folder

#### Frontend Development

15. **Use shared UI components** from `@apptemplate/ui`
16. **Use TypeScript interfaces** for all data types
17. **Handle loading states** - Show spinners during API calls
18. **Handle errors** - Display user-friendly messages with toast notifications
19. **Use Vue Router** for navigation, not direct window.location changes

#### Database & Migrations

20. **Create migrations for every schema change**
21. **Use meaningful migration names** (e.g., `AddUserProfile`, not `Migration1`)
22. **Test migrations locally** before committing
23. **Read the generated migration** before committing it - an unexpected drop column is much cheaper to catch here
24. **Include rollback strategy** - Test that migrations can be reverted

#### Security

25. **Use session-based auth** - Never bypass the auth middleware
26. **Guard every endpoint** - Put `[RequireAccessFunction(...)]` on every controller action; an action without one is open to any logged-in user
27. **Don't trust client data** - Always validate on the server
28. **Use environment variables or user secrets** for secrets - Never hardcode credentials
29. **Guard record ownership** - Use `EnsureOwnedAsync` or `[RequireOwnership]` when records belong to a particular user

#### Code Quality

30. **Write self-documenting code** - Clear names over comments
31. **Keep methods under 50 lines** - Extract to helper methods if longer
32. **Remove dead code** - Don't comment out, delete it (git has history)
33. **Format code** - Use Prettier (frontend) and IDE formatting (backend)

---

### ❌ DON'Ts

#### Critical - Never Do These

1. **Don't modify `BaseService.cs`** - Extend it with your own base class if needed
2. **Don't modify `BaseController.cs`** - Extend it instead
3. **Don't modify `BaseEntity.cs` or `TimestampedEntity.cs`** - They're core infrastructure
4. **Don't disable the session validation middleware** - Security critical
5. **Don't commit credentials** - No passwords, API keys, or secrets in code

#### Backend Anti-Patterns

6. **Don't put business logic in controllers** - Use services
7. **Don't bypass the DbContext** - Don't write raw SQL unless absolutely necessary
8. **Don't create circular dependencies** - Services shouldn't depend on each other circularly
9. **Don't disable or work around the audit logging system** - if it is in your way, the design is wrong, not the audit trail
10. **Don't skip migrations** - Never modify the database manually
11. **Don't add design-time DbContext factories** - startup applies migrations through `MainDbContext`
12. **Don't use `AppTemplateDbContext`** - rename old template context references to `MainDbContext`
13. **Don't put DTOs, enums, security catalogs, services, helpers, or converters under `Libraries/Domain`**
14. **Don't commit logs, temp output, local generated artifacts, `bin/`, `obj/`, or `node_modules/`**

#### Frontend Anti-Patterns

15. **Don't call the API directly** - Use a service module in `services/`, never a bare `fetch`
16. **Don't ignore errors** - Always handle catch blocks
17. **Don't use `any` type** - Define proper TypeScript interfaces
18. **Don't modify shared packages** for app-specific features - Use composition
19. **Don't hardcode URLs** - Use shared runtime constants, not frontend `.env` files
20. **Don't put project data in the shell** - Routes, nav items, and permission codes belong in `src/frontend/main/src/app-config/`, not in layouts or composables
21. **Don't rely on a hidden menu item as a permission check** - The API decides; the UI only reflects it

#### Performance

22. **Don't fetch all data** - Use pagination for large datasets
23. **Don't make unnecessary API calls** - Cache when appropriate
24. **Don't load unused data** - Only include related entities when needed
25. **Don't block the UI** - Use async operations with loading states

#### General

26. **Don't commit `node_modules`** - It's in `.gitignore`
27. **Don't commit `bin/obj`** - They're in `.gitignore`
28. **Don't modify `.gitignore` to include build artifacts**
29. **Don't push directly to main** - Use feature branches and PRs
30. **Don't leave console.log statements** - Remove before committing

---

## Common Mistakes to Avoid

### 1. Forgetting to Register Services

```csharp
// ❌ Wrong - Service not registered, will throw at runtime
public class MyController : BaseController
{
    private readonly IMyService _myService; // Runtime exception!
}

// ✅ Correct - Register in Program.cs
builder.Services.AddScoped<IMyService, MyService>();
```

### 2. Not Mapping DTOs

```csharp
// ❌ Wrong - No mapping configured
return Ok(_mapper.Map<MyDto>(entity)); // Empty/wrong data

// ✅ Correct - Add to MappingProfile.cs
TypeAdapterConfig<MyEntity, MyDto>.NewConfig();
```

### 3. Not Handling Null

```csharp
// ❌ Wrong - Will throw NullReferenceException
var user = await _userService.GetByIdAsync(id);
return user.Name; // Null reference if not found

// ✅ Correct
var user = await _userService.GetByIdAsync(id);
if (user == null)
    return NotFound("User not found");
return user.Name;
```

### 4. Not Using Loading States

```vue
<!-- ❌ Wrong - No loading indication -->
<template>
  <div v-for="item in items" :key="item.id">
    {{ item.name }}
  </div>
</template>

<!-- ✅ Correct - Show loading state -->
<template>
  <AppSpinner v-if="isLoading" />
  <div v-else v-for="item in items" :key="item.id">
    {{ item.name }}
  </div>
</template>
```

### 5. Not Catching Errors

```typescript
// ❌ Wrong - Uncaught error crashes the app
const data = await api.get("/api/items");

// ✅ Correct - Handle errors gracefully
try {
  const data = await api.get("/api/items");
} catch (error) {
  toast.error("Failed to load items");
}
```

---

## Git Workflow

### Branch Naming

- `feature/add-user-profile` - New features
- `bugfix/fix-login-error` - Bug fixes
- `hotfix/security-patch` - Urgent production fixes
- `refactor/cleanup-services` - Code improvements

### Commit Messages

Use clear, descriptive commit messages:

```
✅ Good:
- Add user profile page with avatar upload
- Fix session timeout not redirecting to login
- Update Product entity with category relationship

❌ Bad:
- Fixed stuff
- Updates
- WIP
```

### Before Committing

1. Run the build: `dotnet build` and `pnpm build`
2. Run type checking: `pnpm type-check`
3. Test your changes locally
4. Remove debug statements and console.logs
5. Review your own changes before pushing

---

## Pull Request Guidelines

### Before Creating a PR

- [ ] Code builds without errors
- [ ] All existing tests pass
- [ ] New features have appropriate error handling
- [ ] No hardcoded values that should be configurable
- [ ] Database migrations are included if needed
- [ ] Documentation updated if needed

### PR Description Template

```markdown
## Summary

Brief description of changes

## Changes

- Added X feature
- Fixed Y bug
- Updated Z configuration

## Testing

How to test these changes

## Screenshots (if UI changes)

[Include screenshots]

## Migration Required?

- [ ] Yes - Run migrations after deployment
- [ ] No
```

---

## Testing

### Backend

Test API endpoints using:

- The built-in OpenAPI document (`/openapi/v1.json`), or a `.http` file such as `src/backend/API/Reports.http`
- Postman, Bruno, or a similar client
- The Playwright API specs under `tests/specs/api/` and `tests/specs/auth/`

```bash
cd tests && pnpm run test:api
```

### Frontend

- Unit tests run on Vitest: `pnpm --dir src/frontend test:unit`
- Component and page behaviour worth pinning down goes in a unit test; whole journeys go in a Playwright E2E spec
- Still check the UI by hand at a few screen sizes before you call it done

```bash
cd tests && pnpm run test:e2e
```

Authenticated runs read credentials from `tests/.env.dev.local`. The committed `tests/.env.dev` leaves them blank on purpose — do not fill it in and commit it.

### Integration

Before deployment:

1. Start all services locally
2. Test complete workflows end-to-end
3. Verify database migrations apply cleanly from an empty database
4. Test the authentication flow, including logout and session expiry
