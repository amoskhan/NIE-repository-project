using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Enum;
using Shared.Security;

namespace Data.Data;

public static class MainDbContextSeeder
{
    private static readonly string[] SeededIdentityTables =
    [
        "Codes",
        "AccessFunctions",
        "Roles",
        "RoleAccessFunctions",
        "UserRoles",
        "UserAccounts",
        "WorkflowTransitions"
    ];

    /// <summary>
    /// The same hasher the Auth API uses (<c>Auth/Services/LocalIdentityService.cs</c>), so the
    /// demo passwords seeded below actually verify at the login endpoint.
    /// </summary>
    private static readonly PasswordHasher<UserAccount> PasswordHasher = new();

    public static void Seed(MainDbContext context, bool? includeDevelopmentData = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        SeedCodes(context);
        SeedAccessFunctions(context);
        SeedRoles(context);
        SeedRoleAccessFunctions(context);
        SeedWorkflowTransitions(context);

        if (includeDevelopmentData ?? IsDevelopmentEnvironment())
        {
            SeedDevelopmentUserAccounts(context);
            SeedDevelopmentUserRoles(context);
        }

        ResetIdentitySequences(context);
    }

    public static async Task SeedAsync(
        MainDbContext context,
        bool? includeDevelopmentData = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await SeedCodesAsync(context, cancellationToken);
        await SeedAccessFunctionsAsync(context, cancellationToken);
        await SeedRolesAsync(context, cancellationToken);
        await SeedRoleAccessFunctionsAsync(context, cancellationToken);
        await SeedWorkflowTransitionsAsync(context, cancellationToken);

        if (includeDevelopmentData ?? IsDevelopmentEnvironment())
        {
            await SeedDevelopmentUserAccountsAsync(context, cancellationToken);
            await SeedDevelopmentUserRolesAsync(context, cancellationToken);
        }

        await ResetIdentitySequencesAsync(context, cancellationToken);
    }

    private static bool IsDevelopmentEnvironment()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
    }

    private static void SeedCodes(MainDbContext context)
    {
        foreach (var seed in GetCodeSeeds())
        {
            var existing = context.Codes.SingleOrDefault(code => code.Type == seed.Type && code.Name == seed.Name);

            if (existing is null)
            {
                context.Codes.Add(seed);
                continue;
            }

            existing.Description = seed.Description;
            existing.DisplayName = seed.DisplayName;
            existing.DisplayOrder = seed.DisplayOrder;
            existing.IsActive = seed.IsActive;
        }

        SaveIfChanged(context);
    }

    private static async Task SeedCodesAsync(MainDbContext context, CancellationToken cancellationToken)
    {
        foreach (var seed in GetCodeSeeds())
        {
            var existing = await context.Codes
                .SingleOrDefaultAsync(code => code.Type == seed.Type && code.Name == seed.Name, cancellationToken);

            if (existing is null)
            {
                context.Codes.Add(seed);
                continue;
            }

            existing.Description = seed.Description;
            existing.DisplayName = seed.DisplayName;
            existing.DisplayOrder = seed.DisplayOrder;
            existing.IsActive = seed.IsActive;
        }

        await SaveIfChangedAsync(context, cancellationToken);
    }

    private static void SeedAccessFunctions(MainDbContext context)
    {
        var seedCodes = AccessFunctionCatalog.AccessFunctions
            .Select(seed => seed.Code)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var seed in GetAccessFunctionSeeds())
        {
            var existing = context.AccessFunctions.SingleOrDefault(function => function.Code == seed.Code);

            if (existing is null)
            {
                context.AccessFunctions.Add(seed);
                continue;
            }

            UpdateAccessFunction(existing, seed);
        }

        foreach (var existing in context.AccessFunctions.Where(function => function.IsSystemFunction).ToList())
        {
            if (!seedCodes.Contains(existing.Code))
            {
                existing.IsActive = false;
            }
        }

        SaveIfChanged(context);
    }

    private static async Task SeedAccessFunctionsAsync(MainDbContext context, CancellationToken cancellationToken)
    {
        var seedCodes = AccessFunctionCatalog.AccessFunctions
            .Select(seed => seed.Code)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var seed in GetAccessFunctionSeeds())
        {
            var existing = await context.AccessFunctions
                .SingleOrDefaultAsync(function => function.Code == seed.Code, cancellationToken);

            if (existing is null)
            {
                context.AccessFunctions.Add(seed);
                continue;
            }

            UpdateAccessFunction(existing, seed);
        }

        var existingSystemFunctions = await context.AccessFunctions
            .Where(function => function.IsSystemFunction)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingSystemFunctions)
        {
            if (!seedCodes.Contains(existing.Code))
            {
                existing.IsActive = false;
            }
        }

        await SaveIfChangedAsync(context, cancellationToken);
    }

    private static void UpdateAccessFunction(AccessFunction existing, AccessFunction seed)
    {
        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.Module = seed.Module;
        existing.Type = seed.Type;
        existing.ResourceName = seed.ResourceName;
        existing.Route = seed.Route;
        existing.HttpMethod = seed.HttpMethod;
        existing.IsActive = seed.IsActive;
        existing.IsSystemFunction = seed.IsSystemFunction;
        existing.DisplayOrder = seed.DisplayOrder;
    }

    private static void SeedRoles(MainDbContext context)
    {
        var seedIds = AccessFunctionCatalog.Roles.Select(role => role.Id).ToHashSet();

        foreach (var seed in GetRoleSeeds())
        {
            var existing = context.Roles.SingleOrDefault(role => role.Id == seed.Id || role.Code == seed.Code);

            if (existing is null)
            {
                context.Roles.Add(seed);
                continue;
            }

            UpdateRole(existing, seed);
        }

        foreach (var existing in context.Roles.Where(role => role.IsSystemRole).ToList())
        {
            if (!seedIds.Contains(existing.Id))
            {
                existing.IsActive = false;
            }
        }

        SaveIfChanged(context);
    }

    private static async Task SeedRolesAsync(MainDbContext context, CancellationToken cancellationToken)
    {
        var seedIds = AccessFunctionCatalog.Roles.Select(role => role.Id).ToHashSet();

        foreach (var seed in GetRoleSeeds())
        {
            var existing = await context.Roles
                .SingleOrDefaultAsync(role => role.Id == seed.Id || role.Code == seed.Code, cancellationToken);

            if (existing is null)
            {
                context.Roles.Add(seed);
                continue;
            }

            UpdateRole(existing, seed);
        }

        var existingSystemRoles = await context.Roles
            .Where(role => role.IsSystemRole)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingSystemRoles)
        {
            if (!seedIds.Contains(existing.Id))
            {
                existing.IsActive = false;
            }
        }

        await SaveIfChangedAsync(context, cancellationToken);
    }

    private static void UpdateRole(Role existing, Role seed)
    {
        existing.Code = seed.Code;
        existing.Name = seed.Name;
        existing.Description = seed.Description;
        existing.IsActive = seed.IsActive;
        existing.IsSystemRole = seed.IsSystemRole;
        existing.DisplayOrder = seed.DisplayOrder;
    }

    private static void SeedRoleAccessFunctions(MainDbContext context)
    {
        var desiredLinks = GetDesiredRoleAccessFunctionLinks(context);

        foreach (var (roleId, accessFunctionId) in desiredLinks)
        {
            var existing = context.RoleAccessFunctions.SingleOrDefault(link =>
                link.RoleId == roleId && link.AccessFunctionId == accessFunctionId);

            if (existing is null)
            {
                context.RoleAccessFunctions.Add(new RoleAccessFunction
                {
                    RoleId = roleId,
                    AccessFunctionId = accessFunctionId
                });
            }
        }

        RemoveStaleRoleAccessFunctions(context, desiredLinks);
        SaveIfChanged(context);
    }

    private static async Task SeedRoleAccessFunctionsAsync(MainDbContext context, CancellationToken cancellationToken)
    {
        var desiredLinks = await GetDesiredRoleAccessFunctionLinksAsync(context, cancellationToken);

        foreach (var (roleId, accessFunctionId) in desiredLinks)
        {
            var existing = await context.RoleAccessFunctions.SingleOrDefaultAsync(link =>
                link.RoleId == roleId && link.AccessFunctionId == accessFunctionId, cancellationToken);

            if (existing is null)
            {
                context.RoleAccessFunctions.Add(new RoleAccessFunction
                {
                    RoleId = roleId,
                    AccessFunctionId = accessFunctionId
                });
            }
        }

        await RemoveStaleRoleAccessFunctionsAsync(context, desiredLinks, cancellationToken);
        await SaveIfChangedAsync(context, cancellationToken);
    }

    private static void RemoveStaleRoleAccessFunctions(
        MainDbContext context,
        HashSet<(int RoleId, int AccessFunctionId)> desiredLinks)
    {
        var seededRoleIds = AccessFunctionCatalog.Roles.Select(role => role.Id).ToHashSet();
        var staleLinks = context.RoleAccessFunctions
            .Where(link => seededRoleIds.Contains(link.RoleId))
            .ToList()
            .Where(link => !desiredLinks.Contains((link.RoleId, link.AccessFunctionId)))
            .ToList();

        context.RoleAccessFunctions.RemoveRange(staleLinks);
    }

    private static async Task RemoveStaleRoleAccessFunctionsAsync(
        MainDbContext context,
        HashSet<(int RoleId, int AccessFunctionId)> desiredLinks,
        CancellationToken cancellationToken)
    {
        var seededRoleIds = AccessFunctionCatalog.Roles.Select(role => role.Id).ToHashSet();
        var existingSeededRoleLinks = await context.RoleAccessFunctions
            .Where(link => seededRoleIds.Contains(link.RoleId))
            .ToListAsync(cancellationToken);

        var staleLinks = existingSeededRoleLinks
            .Where(link => !desiredLinks.Contains((link.RoleId, link.AccessFunctionId)))
            .ToList();

        context.RoleAccessFunctions.RemoveRange(staleLinks);
    }

    private static HashSet<(int RoleId, int AccessFunctionId)> GetDesiredRoleAccessFunctionLinks(MainDbContext context)
    {
        var functionIdsByCode = context.AccessFunctions
            .Where(function => function.IsSystemFunction)
            .Select(function => new { function.Code, function.Id })
            .ToDictionary(function => function.Code, function => function.Id, StringComparer.Ordinal);

        return AccessFunctionCatalog.Roles
            .SelectMany(role => role.AccessFunctionCodes
                .Where(functionIdsByCode.ContainsKey)
                .Select(code => (role.Id, functionIdsByCode[code])))
            .ToHashSet();
    }

    private static async Task<HashSet<(int RoleId, int AccessFunctionId)>> GetDesiredRoleAccessFunctionLinksAsync(
        MainDbContext context,
        CancellationToken cancellationToken)
    {
        var functionIdsByCode = await context.AccessFunctions
            .Where(function => function.IsSystemFunction)
            .Select(function => new { function.Code, function.Id })
            .ToDictionaryAsync(function => function.Code, function => function.Id, StringComparer.Ordinal, cancellationToken);

        return AccessFunctionCatalog.Roles
            .SelectMany(role => role.AccessFunctionCodes
                .Where(functionIdsByCode.ContainsKey)
                .Select(code => (role.Id, functionIdsByCode[code])))
            .ToHashSet();
    }

    private static void SeedWorkflowTransitions(MainDbContext context)
    {
        foreach (var seed in GetWorkflowTransitionSeeds())
        {
            var existing = context.WorkflowTransitions.SingleOrDefault(transition =>
                transition.FromState == seed.FromState
                && transition.ToState == seed.ToState
                && transition.RequiredRole == seed.RequiredRole);

            if (existing is null)
            {
                context.WorkflowTransitions.Add(seed);
                continue;
            }

            UpdateWorkflowTransition(existing, seed);
        }

        SaveIfChanged(context);
    }

    private static async Task SeedWorkflowTransitionsAsync(MainDbContext context, CancellationToken cancellationToken)
    {
        foreach (var seed in GetWorkflowTransitionSeeds())
        {
            var existing = await context.WorkflowTransitions.SingleOrDefaultAsync(transition =>
                transition.FromState == seed.FromState
                && transition.ToState == seed.ToState
                && transition.RequiredRole == seed.RequiredRole,
                cancellationToken);

            if (existing is null)
            {
                context.WorkflowTransitions.Add(seed);
                continue;
            }

            UpdateWorkflowTransition(existing, seed);
        }

        await SaveIfChangedAsync(context, cancellationToken);
    }

    private static void UpdateWorkflowTransition(WorkflowTransition existing, WorkflowTransition seed)
    {
        existing.DisplayLabel = seed.DisplayLabel;
        existing.RequiresRemarks = seed.RequiresRemarks;
        existing.IsActive = seed.IsActive;
        existing.DisplayOrder = seed.DisplayOrder;
        existing.UiConditions = seed.UiConditions;
    }

    private static void SeedDevelopmentUserAccounts(MainDbContext context)
    {
        foreach (var seed in GetDevelopmentUserAccountSeeds())
        {
            var existing = context.UserAccounts.SingleOrDefault(account => account.UserId == seed.UserId);

            if (existing is null)
            {
                context.UserAccounts.Add(seed);
                continue;
            }

            UpdateDevelopmentUserAccount(existing, seed);
        }

        SaveIfChanged(context);
    }

    private static async Task SeedDevelopmentUserAccountsAsync(MainDbContext context, CancellationToken cancellationToken)
    {
        foreach (var seed in GetDevelopmentUserAccountSeeds())
        {
            var existing = await context.UserAccounts
                .SingleOrDefaultAsync(account => account.UserId == seed.UserId, cancellationToken);

            if (existing is null)
            {
                context.UserAccounts.Add(seed);
                continue;
            }

            UpdateDevelopmentUserAccount(existing, seed);
        }

        await SaveIfChangedAsync(context, cancellationToken);
    }

    /// <summary>
    /// Refreshes the profile fields of an existing demo account but leaves the password alone,
    /// so a developer who changed their local demo password does not get it reset on every start.
    /// </summary>
    private static void UpdateDevelopmentUserAccount(UserAccount existing, UserAccount seed)
    {
        existing.Name = seed.Name;
        existing.Email = seed.Email;
        existing.Department = seed.Department;
        existing.IsActive = seed.IsActive;

        // Only (re)hash when the account has never had a local password.
        if (string.IsNullOrEmpty(existing.PasswordHash))
        {
            existing.PasswordHash = seed.PasswordHash;
        }
    }

    private static void SeedDevelopmentUserRoles(MainDbContext context)
    {
        foreach (var seed in GetDevelopmentUserRoleSeeds())
        {
            var existing = context.UserRoles.SingleOrDefault(userRole =>
                userRole.UserId == seed.UserId && userRole.RoleId == seed.RoleId);

            if (existing is null)
            {
                context.UserRoles.Add(seed);
                continue;
            }

            existing.AssignedOn = seed.AssignedOn;
            existing.AssignedBy = seed.AssignedBy;
            existing.ExpiresOn = seed.ExpiresOn;
            existing.IsActive = seed.IsActive;
        }

        SaveIfChanged(context);
    }

    private static async Task SeedDevelopmentUserRolesAsync(MainDbContext context, CancellationToken cancellationToken)
    {
        foreach (var seed in GetDevelopmentUserRoleSeeds())
        {
            var existing = await context.UserRoles.SingleOrDefaultAsync(userRole =>
                userRole.UserId == seed.UserId && userRole.RoleId == seed.RoleId, cancellationToken);

            if (existing is null)
            {
                context.UserRoles.Add(seed);
                continue;
            }

            existing.AssignedOn = seed.AssignedOn;
            existing.AssignedBy = seed.AssignedBy;
            existing.ExpiresOn = seed.ExpiresOn;
            existing.IsActive = seed.IsActive;
        }

        await SaveIfChangedAsync(context, cancellationToken);
    }

    private static List<Code> GetCodeSeeds() =>
    [
        new Code { Id = 1, Type = ECodeType.TITLE.ToString(), Name = ECodeName.MR.ToString(), Description = "", DisplayName = "Mr.", DisplayOrder = 1, IsActive = true },
        new Code { Id = 2, Type = ECodeType.TITLE.ToString(), Name = ECodeName.MRS.ToString(), Description = "", DisplayName = "Mrs.", DisplayOrder = 2, IsActive = true },
        new Code { Id = 3, Type = ECodeType.USER_TYPE.ToString(), Name = ECodeName.ADMIN.ToString(), Description = "", DisplayName = "Administrator", DisplayOrder = 3, IsActive = true },
        new Code { Id = 4, Type = ECodeType.USER_TYPE.ToString(), Name = ECodeName.USER.ToString(), Description = "", DisplayName = "Non-Admin User", DisplayOrder = 4, IsActive = true },
        // === SAMPLE: procurement Code rows (removable via task 0003) ===
        new Code { Id = 5, Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.IT_SERVICES.ToString(), Description = "", DisplayName = "IT Services", DisplayOrder = 5, IsActive = true },
        new Code { Id = 6, Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.OFFICE_SUPPLIES.ToString(), Description = "", DisplayName = "Office Supplies", DisplayOrder = 6, IsActive = true },
        new Code { Id = 7, Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.MAINTENANCE.ToString(), Description = "", DisplayName = "Maintenance", DisplayOrder = 7, IsActive = true },
        new Code { Id = 8, Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.CONSULTING.ToString(), Description = "", DisplayName = "Consulting", DisplayOrder = 8, IsActive = true },
        new Code { Id = 9, Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.LOGISTICS.ToString(), Description = "", DisplayName = "Logistics", DisplayOrder = 9, IsActive = true },
        new Code { Id = 10, Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.HARDWARE.ToString(), Description = "", DisplayName = "Hardware", DisplayOrder = 10, IsActive = true },
        new Code { Id = 11, Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.SOFTWARE.ToString(), Description = "", DisplayName = "Software", DisplayOrder = 11, IsActive = true },
        new Code { Id = 12, Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.FURNITURE.ToString(), Description = "", DisplayName = "Furniture", DisplayOrder = 12, IsActive = true },
        new Code { Id = 13, Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.STATIONERY.ToString(), Description = "", DisplayName = "Stationery", DisplayOrder = 13, IsActive = true },
        new Code { Id = 14, Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.CLEANING.ToString(), Description = "", DisplayName = "Cleaning", DisplayOrder = 14, IsActive = true },
        new Code { Id = 15, Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.EACH.ToString(), Description = "", DisplayName = "Each", DisplayOrder = 15, IsActive = true },
        new Code { Id = 16, Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.BOX.ToString(), Description = "", DisplayName = "Box", DisplayOrder = 16, IsActive = true },
        new Code { Id = 17, Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.PACK.ToString(), Description = "", DisplayName = "Pack", DisplayOrder = 17, IsActive = true },
        new Code { Id = 18, Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.SET.ToString(), Description = "", DisplayName = "Set", DisplayOrder = 18, IsActive = true },
        new Code { Id = 19, Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.HOUR.ToString(), Description = "", DisplayName = "Hour", DisplayOrder = 19, IsActive = true },
        new Code { Id = 20, Type = ECodeType.DELIVERY_LOCATION.ToString(), Name = ECodeName.MAIN_OFFICE.ToString(), Description = "", DisplayName = "Main Office", DisplayOrder = 20, IsActive = true },
        new Code { Id = 21, Type = ECodeType.DELIVERY_LOCATION.ToString(), Name = ECodeName.WAREHOUSE.ToString(), Description = "", DisplayName = "Warehouse", DisplayOrder = 21, IsActive = true },
        new Code { Id = 22, Type = ECodeType.DELIVERY_LOCATION.ToString(), Name = ECodeName.BRANCH_OFFICE.ToString(), Description = "", DisplayName = "Branch Office", DisplayOrder = 22, IsActive = true },
        new Code { Id = 23, Type = ECodeType.CURRENCY.ToString(), Name = ECodeName.SGD.ToString(), Description = "", DisplayName = "SGD - Singapore Dollar", DisplayOrder = 23, IsActive = true },
        new Code { Id = 24, Type = ECodeType.CURRENCY.ToString(), Name = ECodeName.USD.ToString(), Description = "", DisplayName = "USD - US Dollar", DisplayOrder = 24, IsActive = true }
        // === END SAMPLE ===
    ];

    private static List<AccessFunction> GetAccessFunctionSeeds() =>
        AccessFunctionCatalog.AccessFunctions
            .Select((definition, index) => new AccessFunction
            {
                Id = index + 1,
                Code = definition.Code,
                Name = definition.Name,
                Description = definition.Description,
                Module = definition.Module,
                Type = definition.Type,
                ResourceName = definition.ResourceName,
                Route = definition.Route,
                HttpMethod = definition.HttpMethod,
                IsActive = true,
                IsSystemFunction = true,
                DisplayOrder = definition.DisplayOrder
            })
            .ToList();

    private static List<Role> GetRoleSeeds() =>
        AccessFunctionCatalog.Roles
            .Select(role => new Role
            {
                Id = role.Id,
                Code = role.Code,
                Name = role.Name,
                Description = role.Description,
                IsActive = true,
                IsSystemRole = true,
                DisplayOrder = role.DisplayOrder
            })
            .ToList();

    /// <summary>
    /// DEVELOPMENT-ONLY demo login accounts for the local identity provider.
    /// <para>
    /// !! These credentials are public knowledge - they are committed to this template. !!
    /// They are only seeded when ASPNETCORE_ENVIRONMENT is "Development" (or when the caller
    /// passes includeDevelopmentData: true). NEVER enable development seeding in a deployed
    /// environment, and delete or change these accounts before shipping anything real.
    /// </para>
    /// <list type="bullet">
    ///   <item><description>admin / Admin@12345 - Administrator</description></item>
    ///   <item><description>alice / Alice@12345 - Administrator</description></item>
    ///   <item><description>bob   / Bob@12345   - standard User</description></item>
    /// </list>
    /// The UserIds here must match <see cref="GetDevelopmentUserRoleSeeds"/> so that roles line up.
    /// </summary>
    private static List<UserAccount> GetDevelopmentUserAccountSeeds() =>
    [
        CreateDevelopmentUserAccount(1, "admin", "Ada Admin", "admin@example.edu", "Platform", "Admin@12345"),
        CreateDevelopmentUserAccount(2, "alice", "Alice Tan", "alice@example.edu", "Digital Services", "Alice@12345"),
        CreateDevelopmentUserAccount(3, "bob", "Bob Lim", "bob@example.edu", "Digital Services", "Bob@12345")
    ];

    private static UserAccount CreateDevelopmentUserAccount(
        int id,
        string userId,
        string name,
        string email,
        string department,
        string developmentPassword)
    {
        var account = new UserAccount
        {
            Id = id,
            UserId = userId,
            Name = name,
            Email = email,
            Department = department,
            IsActive = true,
            MustChangePassword = false,
            FailedLoginCount = 0
        };

        // Hashed with the same PasswordHasher<UserAccount> the Auth API verifies with.
        account.PasswordHash = PasswordHasher.HashPassword(account, developmentPassword);

        return account;
    }

    private static List<UserRole> GetDevelopmentUserRoleSeeds() =>
    [
        new UserRole
        {
            Id = 1,
            UserId = "admin",
            RoleId = (int)ERole.Administrator,
            AssignedOn = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            IsActive = true
        },
        new UserRole
        {
            Id = 2,
            UserId = "alice",
            RoleId = (int)ERole.Administrator,
            AssignedOn = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            IsActive = true
        },
        new UserRole
        {
            Id = 3,
            UserId = "bob",
            RoleId = (int)ERole.User,
            AssignedOn = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            IsActive = true
        }
    ];

    private static List<WorkflowTransition> GetWorkflowTransitionSeeds() =>
    [
        new WorkflowTransition { Id = 1, FromState = EWorkflowState.Draft.ToString(), ToState = EWorkflowState.Submitted.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Submit for Review", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 },
        new WorkflowTransition { Id = 2, FromState = EWorkflowState.Submitted.ToString(), ToState = EWorkflowState.UnderReview.ToString(), RequiredRole = ERole.Manager.ToString(), DisplayLabel = "Start Review", RequiresRemarks = false, IsActive = true, DisplayOrder = 1 },
        new WorkflowTransition { Id = 3, FromState = EWorkflowState.UnderReview.ToString(), ToState = EWorkflowState.Approved.ToString(), RequiredRole = ERole.Manager.ToString(), DisplayLabel = "Approve", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 },
        new WorkflowTransition { Id = 4, FromState = EWorkflowState.UnderReview.ToString(), ToState = EWorkflowState.Rejected.ToString(), RequiredRole = ERole.Manager.ToString(), DisplayLabel = "Reject", RequiresRemarks = true, IsActive = true, DisplayOrder = 2 },
        new WorkflowTransition { Id = 5, FromState = EWorkflowState.UnderReview.ToString(), ToState = EWorkflowState.ReturnedForRevision.ToString(), RequiredRole = ERole.Manager.ToString(), DisplayLabel = "Return for Revision", RequiresRemarks = true, IsActive = true, DisplayOrder = 3 },
        new WorkflowTransition { Id = 6, FromState = EWorkflowState.ReturnedForRevision.ToString(), ToState = EWorkflowState.Submitted.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Resubmit", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 },
        new WorkflowTransition { Id = 7, FromState = EWorkflowState.Approved.ToString(), ToState = EWorkflowState.Completed.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Mark as Completed", RequiresRemarks = false, IsActive = true, DisplayOrder = 1 },
        new WorkflowTransition { Id = 8, FromState = EWorkflowState.Draft.ToString(), ToState = EWorkflowState.Cancelled.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Cancel", RequiresRemarks = true, IsActive = true, DisplayOrder = 2 },
        new WorkflowTransition { Id = 9, FromState = EWorkflowState.Submitted.ToString(), ToState = EWorkflowState.Cancelled.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Cancel", RequiresRemarks = true, IsActive = true, DisplayOrder = 2 },
        new WorkflowTransition { Id = 10, FromState = EWorkflowState.Rejected.ToString(), ToState = EWorkflowState.Draft.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Re-open as Draft", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 }
    ];

    private static void SaveIfChanged(MainDbContext context)
    {
        if (context.ChangeTracker.HasChanges())
        {
            context.SaveChanges();
        }
    }

    private static async Task SaveIfChangedAsync(MainDbContext context, CancellationToken cancellationToken)
    {
        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static void ResetIdentitySequences(MainDbContext context)
    {
        if (!context.Database.IsNpgsql())
        {
            return;
        }

        foreach (var tableName in SeededIdentityTables)
        {
            context.Database.ExecuteSqlRaw(BuildResetIdentitySql(tableName));
        }
    }

    private static async Task ResetIdentitySequencesAsync(MainDbContext context, CancellationToken cancellationToken)
    {
        if (!context.Database.IsNpgsql())
        {
            return;
        }

        foreach (var tableName in SeededIdentityTables)
        {
            await context.Database.ExecuteSqlRawAsync(BuildResetIdentitySql(tableName), cancellationToken);
        }
    }

    private static string BuildResetIdentitySql(string tableName) =>
        $$"""
        DO $$
        DECLARE
            sequence_name text;
        BEGIN
            sequence_name := pg_get_serial_sequence('"{{tableName}}"', 'Id');

            IF sequence_name IS NOT NULL THEN
                EXECUTE format(
                    'SELECT setval(%L, COALESCE((SELECT MAX("Id") FROM "{{tableName}}"), 1), (SELECT MAX("Id") FROM "{{tableName}}") IS NOT NULL)',
                    sequence_name);
            END IF;
        END
        $$;
        """;
}
