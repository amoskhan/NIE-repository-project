# 02 - C# / .NET Coding Standards

Applies to anything under `src/backend/`.

## Naming

- Classes, methods, and public properties: PascalCase.
- Private fields: `_camelCase`.
- Local variables and parameters: camelCase.
- Constants: PascalCase.
- Async methods: suffix with `Async`.
- Interfaces: prefix with `I` (`IUserService`).
- Enums: prefix with `E` (`ERole`, `ECodeType`).

## Common Imports

Use only the imports the file actually needs. Typical backend imports are:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MapsterMapper;
using Domain.Models;
using Shared.Dto;
using Shared.Enum;
using Shared.Security;
using Data.Data;
```

A repository that prefixes its namespaces uses the matching project prefix throughout, for example `MyProject.Domain.Models` and `MyProject.Shared.Dto`. Pick one convention and keep it consistent across the whole backend.

## Backend File Topology

- The application EF Core context is always `MainDbContext` in `Libraries/Data/Data/MainDbContext.cs`. Do not create `AppTemplateDbContext`, project-specific application DbContext names, or any `IDesignTimeDbContextFactory`.
- Every backend `Program.cs` that registers `MainDbContext` must apply migrations immediately before `app.Run()` or `await app.RunAsync()`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<MainDbContext>();
    context.Database.Migrate();
}
```

- `Libraries/Domain/Models/` contains only domain entities/domain model classes.
- Every domain entity/domain object is one top-level type per file under `Libraries/Domain/Models/`.
- DTOs go under `Libraries/Shared/Dto/`.
- Enums go under `Libraries/Shared/Enum/`.
- Security catalogs go under `Libraries/Shared/Security/`.
- Every service contract and implementation is split into separate files in the same feature folder: `Libraries/Services/Services/<Feature>/IYourEntityService.cs` and `Libraries/Services/Services/<Feature>/YourEntityService.cs`.
- Service-local request/result/helper types also get their own files in the same service folder.
- Multiple implementations of the same interface are allowed, but each implementation is its own file in that folder.

## Entity Pattern

```csharp
public class YourEntity : TimestampedEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public EYourStatus Status { get; set; }
    public int RelatedId { get; set; }
    public virtual RelatedEntity Related { get; set; } = default!;
    public virtual ICollection<Child> Children { get; set; } = new List<Child>();
}
```

Use `= default!` on required reference types and nullable `?` on optional fields. Status fields are enums, not strings.

## DTO Pattern

```csharp
public class YourEntityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public EYourStatus Status { get; set; }
    public string? RelatedName { get; set; }
}
```

DTOs never expose navigation properties. They flatten what the UI needs.

## Service Pattern

`IYourEntityService.cs`

```csharp
public interface IYourEntityService : IBaseService<YourEntity>
{
    Task<IList<YourEntity>> GetActiveAsync();
}
```

`YourEntityService.cs`

```csharp
public class YourEntityService : BaseService<YourEntity>, IYourEntityService
{
    public YourEntityService(MainDbContext context) : base(context) { }

    public Task<IList<YourEntity>> GetActiveAsync() =>
        Records.Where(x => x.Status == EYourStatus.Active).ToListAsync();
}
```

Use `Records` from `BaseService<T>`. Use `Include()` for eager loading.

## Controller Pattern

```csharp
public class YourEntityController : BaseController
{
    private readonly IYourEntityService _service;
    private readonly IMapper _mapper;
    private readonly ILogger<YourEntityController> _logger;

    public YourEntityController(IYourEntityService service, IMapper mapper, ILogger<YourEntityController> logger)
    {
        _service = service;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.YourEntityRead)]
    public async Task<ActionResult<IEnumerable<YourEntityDto>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(_mapper.Map<List<YourEntityDto>>(items));
    }
}
```

## Async Rules

- All I/O is async.
- Never call `.Result` or `.Wait()`.
- Stream large results with `IAsyncEnumerable<T>` when paging is not enough.

## Error Handling

- Do not catch `Exception` to swallow. Throw; global exception middleware returns a typed response.
- Validate user input at the controller boundary.
- Trust internal callers. Do not add defensive null checks for DI-injected services.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/backend/Libraries/Data --startup-project src/backend/API
dotnet ef database update --project src/backend/Libraries/Data --startup-project src/backend/API
dotnet ef migrations remove --project src/backend/Libraries/Data --startup-project src/backend/API
```

Every schema change ships with a migration.
