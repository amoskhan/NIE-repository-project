# Getting Started

> **Who this is for:** a student starting a course, challenge, or capstone project on App Template.
> **Time:** about 25 minutes to get running, about an hour more to ship your first feature.
> **What you need before you start:** the [prerequisites in the README](../README.md#prerequisites).

This guide has two halves.

- **Part 1 — Your first half hour**: get the app running, log in, and learn where things live.
- **Part 2 — Your first feature**: add one entity end to end, from database table to sidebar menu item.

Work through Part 1 without skipping. Almost every "it doesn't work" question is answered by something in it.

---

# Part 1 — Your first half hour

## Step 1. Get the code

If your instructor gave you a repository, clone that. If you are starting from the template itself, scaffold with Copier so your project gets its own namespace and can pull template updates later:

```bash
pip install --user copier
copier copy --trust gh:your-org/app-template ./my-app
cd ./my-app
git init && git add . && git commit -m "chore: scaffold from App Template"
```

Either way, open the folder in VS Code.

## Step 2. Start the database and cache

The app needs PostgreSQL (data) and Valkey (sessions and cache). A local mail sink and an S3 emulator are there too, so email and file storage work without any cloud account. All of them are in one Compose file.

**Option A — dev container (least setup).** VS Code will offer _"Reopen in Container"_ when it sees `.devcontainer/`. Accept it. The container brings up .NET 10, Node, pnpm, PostgreSQL, Valkey, Mailpit, and a LocalStack S3 emulator, points the app at them through `containerEnv`, and runs `dotnet restore` plus `pnpm install` for you. When it finishes you still need one command from Step 3 — `pnpm build` inside `src/frontend` — and then you can go to Step 4.

**Option B — Docker Desktop on your own machine.**

```bash
docker compose -f .devcontainer/docker-compose.yml up -d postgres valkey mailpit
```

Check they came up:

```bash
docker compose -f .devcontainer/docker-compose.yml ps
```

| Service    | Port        | What it is                                                              |
| ---------- | ----------- | ----------------------------------------------------------------------- |
| postgres   | 5432        | Your database, `AppTemplate`, user and password both `postgres`         |
| valkey     | 6379        | Session and cache store                                                 |
| mailpit    | 1025 / 8025 | Catches every email the app sends; read them at <http://localhost:8025> |
| localstack | 4566        | S3 emulator, only needed if you switch file storage to the S3 provider  |

If a port is already in use, stop whatever is holding it, or change the published port in `.devcontainer/docker-compose.yml` and match it in `src/backend/API/appsettings.json`.

## Step 3. Install dependencies

```bash
# frontend workspace (from the repo root)
cd src/frontend
pnpm install
pnpm build          # builds @apptemplate/ui and @apptemplate/shared first, then both apps
cd ../..

# backend
dotnet restore src/backend/AppTemplate.sln
```

`pnpm build` matters the first time: the `main` and `auth` apps import the two workspace packages, and those packages must exist as built output before the apps will start.

## Step 4. Run it

Four processes need to be running: the Auth API, the Main API, and a Vite dev server for each of the two SPAs.

In VS Code, open **Run and Debug** and pick **"All Services (Hot Reload)"**. That is the whole thing in one click, and it is defined in `.vscode/launch.json` if you want to see what it does.

From a terminal instead, one line per terminal, all from the repo root:

```bash
dotnet watch run --project src/backend/Auth    # http://localhost:5001
dotnet watch run --project src/backend/API     # http://localhost:5002
pnpm --dir src/frontend dev                    # http://localhost:8001 and :8002
```

**What happens on first boot.** The Main API applies every pending EF Core migration and then runs the seeder (`src/backend/Libraries/Data/Data/MainDbContextSeeder.cs`). That creates your tables and fills in code tables, roles, access functions, workflow transitions, and demo accounts. You do not run migrations by hand for a first run.

Watch the Main API terminal until it stops printing migration lines and settles on the "Now listening on: http://localhost:5002" message. If it throws a connection error, PostgreSQL is not up — go back to Step 2.

## Step 5. Log in

Open <http://localhost:8002>. There is no session yet, so the main app bounces you to the login app at <http://localhost:8001>.

Log in with a seeded demo account. Two of them, **`alice`** and **`bob`**, are seeded with the **Administrator** role in Development, which means you can reach the admin screens straight away.

The demo user IDs and passwords are defined in the seeder, `src/backend/Libraries/Data/Data/MainDbContextSeeder.cs`. Open it and read them there — a value pasted into a document goes stale, the seeder does not.

You can also use the **Register** link to create your own account, and **Forgot password** to reset one. Both are handled entirely by the Auth API against the local users table; nothing leaves your machine. The reset email is delivered to the Mailpit sink — open <http://localhost:8025> and click the link from there.

Once you are in you should see the sidebar, a dashboard, and the sample procurement screens (Vendors, Catalog, Approvals, Order History) plus the admin section (Users & Roles, Access Functions, Audit Logs).

### If login fails

| Symptom                                                   | Cause                                                                        |
| --------------------------------------------------------- | ---------------------------------------------------------------------------- |
| "Network error" on the login page                         | The Auth API on `:5001` is not running                                       |
| Login succeeds, then the main app kicks you back to login | Valkey is not running, so the session was never stored                       |
| Login succeeds but the sidebar is nearly empty            | Your account has no role assigned; sign in as `alice` and use Users & Roles  |
| 500 from the Main API on every request                    | PostgreSQL is not running, or migrations failed — read the Main API terminal |

## Step 6. Learn the shape of the codebase

The single most useful thing you can do in your first hour is understand the split between **template-owned** files and **yours**.

### The backend, one request at a time

A request to the Main API travels like this:

```
Browser
  -> X-Session-Id header
  -> SessionValidationMiddleware      src/backend/API/Middleware/
       looks the token up in Valkey, puts UserId/roles/access functions on HttpContext.Items
  -> [RequireAccessFunction("...")]   src/backend/API/Authorization/RequireAccessFunctionAttribute.cs
       403 if the user does not hold that access function
  -> Controller                       src/backend/API/Controllers/
       HTTP only: validate input, map DTO <-> entity, choose a status code
  -> Service                          src/backend/Libraries/Services/Services/
       all business logic lives here
  -> MainDbContext                    src/backend/Libraries/Data/Data/MainDbContext.cs
  -> PostgreSQL
```

| Folder                                     | What lives there                                              |
| ------------------------------------------ | ------------------------------------------------------------- |
| `src/backend/Libraries/Domain/Models/`     | Entities **only** — no DTOs, no services, no enums            |
| `src/backend/Libraries/Data/`              | `MainDbContext`, migrations, the seeder                       |
| `src/backend/Libraries/Services/Services/` | Business logic, one folder per service                        |
| `src/backend/Libraries/Shared/Dto/`        | Data transfer objects — the shapes the API actually returns   |
| `src/backend/Libraries/Shared/Security/`   | `AccessFunctionCatalog.cs`, the canonical list of permissions |
| `src/backend/API/Controllers/`             | HTTP endpoints                                                |
| `src/backend/API/Mapping/`                 | Mapster entity-to-DTO configuration                           |
| `src/backend/Auth/`                        | The local identity provider and session issuing               |

Read one vertical slice before you write anything. `Vendor` is the shortest one:

- `src/backend/Libraries/Domain/Models/Samples/Procurement/Vendor.cs`
- `src/backend/Libraries/Shared/Dto/Samples/Procurement/VendorDto.cs`
- `src/backend/Libraries/Services/Services/Samples/Procurement/Vendor/VendorService.cs`
- `src/backend/API/Controllers/Samples/Procurement/VendorController.cs`

### The frontend, and the one folder that is yours

```
src/frontend/
|-- main/          the app you build in (dev server :8002)
|-- auth/          login, register, reset password (dev server :8001)
`-- packages/
    |-- ui/        @apptemplate/ui     — AppButton, AppDataTable, AppModal, ...
    `-- shared/    @apptemplate/shared — runtime constants, i18n, utilities
```

Inside `main/src`:

| Path                            | Yours to edit? | What it is                                                                                                                                 |
| ------------------------------- | -------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `app-config/routes.ts`          | **Yes**        | Your routes                                                                                                                                |
| `app-config/navigation.ts`      | **Yes**        | Your sidebar menu items                                                                                                                    |
| `app-config/accessFunctions.ts` | **Yes**        | Your access-function codes and permission bundles                                                                                          |
| `app-config/branding.ts`        | **Yes**        | `BRAND_LOGO` (the sidebar/header logo) and `FEEDBACK_FUNCTION_PREFIX` — nothing else                                                       |
| `theme/appTheme.ts`             | **Yes**        | The product name (`brandLabel`) and the colour preset (`defaultPreset`)                                                                    |
| `staff/pages/`                  | **Yes**        | Your screens                                                                                                                               |
| `services/`                     | **Yes**        | Your API clients                                                                                                                           |
| `types/`                        | **Yes**        | Your TypeScript types                                                                                                                      |
| `composables/`                  | Mostly no      | Shell behaviour (`usePermissions`, `useAuth`, `useToast`)                                                                                  |
| `components/common/`            | No             | Template-owned and **locked** by `tools/template-guardrails` — the pre-commit hook rejects edits. Use `@apptemplate/ui` components instead |
| `staff/layouts/`                | No             | The shell chrome that reads `app-config/*`                                                                                                 |

The rule the whole shell depends on: **project data lives in `app-config/`, never in the layout or composable files.** Those files carry a `TEMPLATE-OWNED SHELL` comment at the top. If you find yourself editing one, you are almost certainly meant to be editing something in `app-config/` instead.

## Step 7. Look around while it is running

Ten minutes of clicking beats an hour of reading.

1. **Users & Roles** (admin section) — assign a role to a user and watch the sidebar change on their next login.
2. **Access Functions** — every permission the backend knows about, seeded from `AccessFunctionCatalog.cs`.
3. **Audit Logs** — the trail written automatically for auditable operations.
4. **Vendors** — then open `VendorManagement.vue` beside it and match what you see on screen to what is in the file.
5. `http://localhost:5002/openapi/v1.json` — the full Main API surface. Load it into Scalar, Swagger UI, Postman, or your IDE's REST client.
6. `http://localhost:8025` — the Mailpit inbox. Trigger a password reset and watch the message arrive.

---

# Part 2 — Build your first feature

Now add something of your own: a **Course** entity with a list screen, full CRUD, and its own permissions. Fourteen small steps, all following patterns already in the repo. Substitute your real domain object for `Course` as you go.

> If you are pair-programming with an AI agent, point it at `.ai/features/` first. Each folder there documents the exact files a feature touches. The agent will produce code that matches the template instead of code that fights it.

## 1. The entity

`src/backend/Libraries/Domain/Models/Course.cs`

```csharp
namespace Domain.Models;

public class Course : TimestampedEntity
{
    public string Title { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
    public int Credits { get; set; }
    public bool IsActive { get; set; } = true;
}
```

Inherit `TimestampedEntity` when you want `CreatedOn` / `CreatedBy` / `UpdatedOn` / `UpdatedBy` filled in for you — which is nearly always. Inherit `BaseEntity` only if you truly do not want those four columns. Both give you an `int Id` primary key.

Keep `Domain/Models/` free of anything that is not an entity. One top-level type per file.

## 2. Register the DbSet

`src/backend/Libraries/Data/Data/MainDbContext.cs` — add it alongside the others:

```csharp
public DbSet<Course> Courses { get; set; } = default!;
```

Add any indexes or constraints in `OnModelCreating` in the same file, following the existing entries.

## 3. Create the migration

```bash
dotnet ef migrations add AddCourse \
  --project src/backend/Libraries/Data \
  --startup-project src/backend/API
```

Open the generated file under `src/backend/Libraries/Data/Migrations/` and read it. If it drops a column you did not expect, your model and your database have drifted — fix that now, not later.

You do not need to run `database update`: the Main API applies migrations on startup. Restarting the API is enough. (`docs/MIGRATIONS.md` covers the manual commands for when you do need them.)

## 4. The DTO

`src/backend/Libraries/Shared/Dto/CourseDto.cs`

```csharp
namespace Shared.Dto;

public class CourseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
    public int Credits { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
```

**Never return an entity from a controller.** The DTO is the contract; the entity is an implementation detail. Keeping them separate is what lets you add an internal column later without breaking the frontend.

## 5. The mapping

`src/backend/API/Mapping/MappingProfile.cs`, inside `MappingConfig.RegisterMappings()`:

```csharp
// Course mappings
TypeAdapterConfig<Course, CourseDto>.NewConfig();
TypeAdapterConfig<CourseDto, Course>.NewConfig();
```

Mapster maps same-named properties automatically. You only add `.Map(...)` when the names differ or you are flattening a relation — see the `CatalogItem` mapping in the same file for an example that pulls `VendorName` off a navigation property.

Forgetting this step is the classic first bug: the API returns objects with every field null.

## 6. The service

Two files in `src/backend/Libraries/Services/Services/Course/`.

`ICourseService.cs`

```csharp
using Models = Domain.Models;

namespace Services.Services.Course;

public interface ICourseService : IBaseService<Models.Course>
{
    Task<IList<Models.Course>> GetActiveAsync();
}
```

`CourseService.cs`

```csharp
using Models = Domain.Models;
using Data.Data;
using Microsoft.EntityFrameworkCore;

namespace Services.Services.Course;

public class CourseService : BaseService<Models.Course>, ICourseService
{
    public CourseService(MainDbContext context) : base(context)
    { }

    public async Task<IList<Models.Course>> GetActiveAsync()
    {
        return await Records.Where(course => course.IsActive).ToListAsync();
    }
}
```

`BaseService<T>` already gives you `GetAllAsync`, `GetByIdAsync` (with optional includes), `SaveAsync`, `SaveOrUpdateAsync`, and `DeleteAsync`. Add a method only for behaviour those do not cover. `Records` is the protected `DbSet<T>`.

**Do not modify `BaseService.cs`.** It is marked `DO NOT CHANGE THIS SERVICE` and every service in the app depends on it.

## 7. Register the service

`src/backend/API/Program.cs`, with the other `AddScoped` lines:

```csharp
builder.Services.AddScoped<ICourseService, CourseService>();
```

Miss this and your controller throws at the first request with an "unable to resolve service" message.

## 8. Define the access functions

Permissions are **access functions**: string codes seeded into the database, granted to roles, checked by an attribute on each endpoint.

In `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs`:

```csharp
// inside AccessFunctionCodes.Api
public const string CourseRead = "api.course.read";
public const string CourseManage = "api.course.manage";
```

Then add each one to the `AccessFunctions` list, following the shape of the entries already there:

```csharp
new(
    AccessFunctionCodes.Api.CourseRead,
    "Read courses",
    "Courses",
    EAccessFunctionType.Api,
    "CourseController.GetAll",
    "/api/Course",
    "GET",
    "Read the course catalogue.",
    400),
```

Finally, grant them in the `Roles` list. `Administrator` is granted every code automatically (it selects all of them), so you only need to decide about the other roles — for example give `Manager` both codes and `User` only `CourseRead`.

The seeder is idempotent: it upserts on every startup, so restarting the Main API is all it takes for the new access functions to appear in the database and on the Access Functions admin screen.

## 9. The controller

`src/backend/API/Controllers/CourseController.cs`

```csharp
using API.Authorization;
using Shared.Dto;
using Domain.Models;
using Shared.Security;
using Services.Services.Course;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class CourseController : BaseController
{
    private readonly ICourseService _courseService;
    private readonly IMapper _mapper;
    private readonly ILogger<CourseController> _logger;

    public CourseController(
        ICourseService courseService,
        IMapper mapper,
        ILogger<CourseController> logger)
    {
        _courseService = courseService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.CourseRead)]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetAll()
    {
        var courses = await _courseService.GetAllAsync();
        return Ok(_mapper.Map<List<CourseDto>>(courses));
    }

    [HttpGet("{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.CourseRead)]
    public async Task<ActionResult<CourseDto>> Get(int id)
    {
        var course = await _courseService.GetByIdAsync(id);
        if (course == null) return NotFound("Course not found");
        return Ok(_mapper.Map<CourseDto>(course));
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.CourseManage)]
    public async Task<ActionResult<CourseDto>> Save([FromBody] CourseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Code is required");

        var saved = await _courseService.SaveAsync(_mapper.Map<Course>(dto));
        _logger.LogInformation("Created course {Id}", saved.Id);
        return Ok(_mapper.Map<CourseDto>(saved));
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.CourseManage)]
    public async Task<ActionResult<CourseDto>> Edit([FromBody] CourseDto dto)
    {
        if (dto.Id <= 0) return BadRequest("Invalid ID");

        var existing = await _courseService.GetByIdAsync(dto.Id);
        if (existing == null) return NotFound("Course not found");

        existing.Title = dto.Title;
        existing.Code = dto.Code;
        existing.Description = dto.Description;
        existing.Credits = dto.Credits;
        existing.IsActive = dto.IsActive;

        var updated = await _courseService.SaveOrUpdateAsync(existing);
        _logger.LogInformation("Updated course {Id}", updated.Id);
        return Ok(_mapper.Map<CourseDto>(updated));
    }

    [HttpPost("Delete/{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.CourseManage)]
    public async Task<ActionResult> Delete(int id)
    {
        if (!await _courseService.DeleteAsync(id)) return NotFound("Course not found");
        _logger.LogInformation("Deleted course {Id}", id);
        return Ok();
    }
}
```

Three things worth noticing:

- Inheriting `BaseController` gives you the route template `api/[controller]/[action]` and the session accessors `UserId`, `UserName`, `UserRoles`, `UserAccessFunctions`, `IsAdmin`. So `GetAll` is reachable at `GET /api/Course/GetAll`.
- **Copy the `Edit` shape exactly.** Loading the entity and assigning fields one at a time is what stops a caller from overwriting `CreatedBy`, an owner id, or any other column you never meant to expose. Mapping a request DTO straight onto a loaded entity is the mass-assignment bug.
- Every endpoint carries `[RequireAccessFunction]`. An endpoint with no attribute is reachable by any logged-in user. If your entity is owned per-user, also look at `EnsureOwnedAsync` on `BaseController` and `RequireOwnershipAttribute`.

Restart the Main API and try it: `GET http://localhost:5002/api/Course/GetAll` with an `X-Session-Id` header. A 403 here means step 8 did not grant the code to your role.

## 10. The frontend API client

`src/frontend/main/src/services/courseService.ts`

```typescript
import api from "./api";

export interface CourseDto {
  id?: number;
  title: string;
  code: string;
  description?: string | null;
  credits: number;
  isActive: boolean;
  createdOn?: string | null;
  updatedOn?: string | null;
}

const courseService = {
  async getAll(): Promise<CourseDto[]> {
    const response = await api.get<CourseDto[]>("/api/Course/GetAll");
    return response.data;
  },

  async getById(id: number): Promise<CourseDto> {
    const response = await api.get<CourseDto>(`/api/Course/Get/${id}`);
    return response.data;
  },

  async save(dto: CourseDto): Promise<CourseDto> {
    const endpoint = dto.id ? "/api/Course/Edit" : "/api/Course/Save";
    const response = await api.post<CourseDto>(endpoint, dto);
    return response.data;
  },

  async delete(id: number): Promise<void> {
    await api.post(`/api/Course/Delete/${id}`);
  },
};

export default courseService;
```

Always import the shared `api` client from `./api`. It attaches the session header, resolves the API base URL at runtime, and redirects to login on a 401. A raw `fetch` gets none of that.

Never hardcode `http://localhost:5002`. Paths are relative; the base URL comes from `@apptemplate/shared`.

`src/frontend/main/src/services/vendorService.ts` is the file this one is modelled on.

## 11. Mirror the access-function codes in the frontend

`src/frontend/main/src/app-config/accessFunctions.ts` — inside `AccessFunctionCode.Api`:

```typescript
CourseRead: "api.course.read",
CourseManage: "api.course.manage",
```

and a bundle to gate the route and the menu item with:

```typescript
export const COURSE_PERMISSIONS = [AccessFunctionCode.Api.CourseRead] as const;
```

These strings **must** match `AccessFunctionCatalog.cs` character for character. The backend is the source of truth; this file is a typed mirror so your Vue code gets autocomplete and a compile error instead of a silent typo.

## 12. The page

`src/frontend/main/src/staff/pages/staff/CourseManagement.vue`

Copy `VendorManagement.vue` in the same folder and adapt it — it is the reference implementation for a list-plus-modal CRUD screen, and it already handles loading state, search, filters, the confirm dialog, and toasts. The skeleton:

```vue
<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useToast, AppDataTable, AppButton, AppBadge } from "@apptemplate/ui";
import courseService, { type CourseDto } from "@/services/courseService";

const toast = useToast();
const isLoading = ref(true);
const rows = ref<CourseDto[]>([]);

async function load() {
  isLoading.value = true;
  try {
    rows.value = await courseService.getAll();
  } catch (error) {
    toast.error("Failed to load courses");
    console.error(error);
  } finally {
    isLoading.value = false;
  }
}

onMounted(load);
</script>
```

Four habits to keep from the reference page:

1. Use components from `@apptemplate/ui` — `AppDataTable`, `AppButton`, `AppInput`, `AppModal`, `AppBadge`, `AppSelect`. Do not hand-roll a table.
2. Always show a loading state. `AppStatePanel` covers empty, error, and no-results.
3. Always `catch` and surface the failure with `toast.error(...)`. A silent catch is worse than a crash.
4. Confirm before deleting. Use **`AppConfirmDialog` from `@apptemplate/ui`** in your own pages. The sample pages import `@/components/common/ConfirmDialog.vue`, but that file is **template-owned and locked** by `tools/template-guardrails` — editing it fails the pre-commit hook.

Dropdown values that belong to a lookup list should come from the code tables rather than being hardcoded — see `useCodeTableOptions` and `codeTableService.ts`.

## 13. The route

`src/frontend/main/src/app-config/routes.ts` — add to `PROJECT_ROUTES`:

```typescript
{
  path: "courses",
  name: "courses",
  component: () => import("@/staff/pages/staff/CourseManagement.vue"),
  meta: {
    permissions: [...COURSE_PERMISSIONS],
    title: "Courses",
  },
},
```

Import `COURSE_PERMISSIONS` from `@/app-config/accessFunctions` at the top of the file. The router guard reads `meta.permissions` (array) or `meta.permission` (single value) and blocks the navigation if the user holds none of them.

## 14. The sidebar entry

`src/frontend/main/src/app-config/navigation.ts` — add to `PRIMARY_NAV_ITEMS`:

```typescript
{
  name: "Courses",
  icon: "school",
  route: "courses",
  permissions: [...COURSE_PERMISSIONS],
},
```

`route` is the route **name** from step 13, not a path. `icon` is a [Material Symbols](https://fonts.google.com/icons) name. The shell hides the item automatically for users without the permission — that is why you never write `v-if="isAdmin"` in a layout.

## Verify the whole slice

1. Restart the Main API. Watch for the migration and seeding lines.
2. Reload the frontend (`pnpm --dir src/frontend dev` picks up changes on save).
3. Log out and back in — access functions are attached to the session at login, so an existing session will not see the new permission.
4. **Courses** should now be in the sidebar. Open it, create a record, edit it, delete it.
5. Open **Audit Logs** and confirm your actions were recorded.
6. Log in as a user without the permission and confirm the menu item is gone _and_ the API returns 403. Both layers must hold — a hidden menu item is not security.

### When it does not work

| Symptom                                   | Almost always                                                                     |
| ----------------------------------------- | --------------------------------------------------------------------------------- |
| 500 on startup, "relation does not exist" | You created the migration but the API did not restart                             |
| DTO comes back with every field null      | Step 5 — the Mapster mapping is missing                                           |
| "Unable to resolve service for type"      | Step 7 — the service is not registered in `Program.cs`                            |
| 403 from every endpoint                   | Step 8 — the access function is not granted to your role, or you did not re-login |
| Menu item never appears                   | Step 11 typo — the frontend code string does not match the backend one            |
| Route 404s                                | Step 13 — `component` path is wrong, or `name` and the nav `route` disagree       |
| TypeScript errors on `@apptemplate/ui`    | Run `pnpm build` in `src/frontend` so the workspace packages are built            |

---

## Where to go next

| You want to...                                  | Read                                                                                                                                                                                                     |
| ----------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Follow the house style                          | [`CONTRIBUTING.md`](CONTRIBUTING.md)                                                                                                                                                                     |
| Return the right status codes and toasts        | [`error-handling.md`](error-handling.md)                                                                                                                                                                 |
| Understand routing and the built-in endpoints   | [`API-REFERENCE.md`](API-REFERENCE.md)                                                                                                                                                                   |
| Change or roll back the database                | [`MIGRATIONS.md`](MIGRATIONS.md)                                                                                                                                                                         |
| Know what the auth layer protects you from      | [`security-model.md`](security-model.md)                                                                                                                                                                 |
| Add a background job, chatbot, PDF, or workflow | the matching dossier in [`../.ai/features/`](../.ai/features/)                                                                                                                                           |
| Work with an AI agent on this repo              | [`../.ai/README.md`](../.ai/README.md)                                                                                                                                                                   |
| Write up your own design                        | the stubs in [`architecture.md`](architecture.md), [`data-model.md`](data-model.md), [`design-spec.md`](design-spec.md), [`requirements/`](requirements/) and their guides in [`templates/`](templates/) |

## Making it yours

When your own domain is in place and you no longer need the training wheels:

- **Rename the app.** The product name and colour preset live in `src/frontend/main/src/theme/appTheme.ts` (`brandLabel`, `defaultPreset`); the logo and the feedback namespace live in `src/frontend/main/src/app-config/branding.ts` (`BRAND_LOGO`, `FEEDBACK_FUNCTION_PREFIX`). `python tools/template-rename/rename.py --to MyApp` changes the .NET namespace.
- **Delete the procurement sample.** Entities, DTOs, services, controllers, pages, and access-function codes are all tagged with `SAMPLE:` comments. Follow [`.ai/features/_samples/procurement/remove.md`](../.ai/features/_samples/procurement/remove.md) — it lists every file and every code block, so you do not leave a controller pointing at a service you just deleted.
- **Replace the demo accounts.** They are development-only. Remove them from the seeder before anyone else can reach your deployment.
- **Write down your design.** `docs/architecture.md`, `docs/data-model.md`, and `docs/requirements/` are empty on purpose. Filling them in is usually part of the grade, and it is what makes an AI agent useful on your codebase instead of guessy.
