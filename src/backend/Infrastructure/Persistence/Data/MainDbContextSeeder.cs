using Application.Features.Notifications;
using Application.Security;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class MainDbContextSeeder
{
    public static void Seed(MainDbContext context, bool? includeDevelopmentData = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        SeedCodes(context);
        SeedAccessFunctions(context);
        SeedApplications(context);
        SeedRoles(context);
        SeedRoleAccessFunctions(context);
        SeedWorkflowTransitions(context);
        SeedNotificationConfiguration(context);

        if (includeDevelopmentData ?? IsDevelopmentEnvironment())
        {
            SeedDevelopmentUserRoles(context);
        }

    }

    public static async Task SeedAsync(
        MainDbContext context,
        bool? includeDevelopmentData = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await SeedCodesAsync(context, cancellationToken);
        await SeedAccessFunctionsAsync(context, cancellationToken);
        await SeedApplicationsAsync(context, cancellationToken);
        await SeedRolesAsync(context, cancellationToken);
        await SeedRoleAccessFunctionsAsync(context, cancellationToken);
        await SeedWorkflowTransitionsAsync(context, cancellationToken);
        await SeedNotificationConfigurationAsync(context, cancellationToken);

        if (includeDevelopmentData ?? IsDevelopmentEnvironment())
        {
            await SeedDevelopmentUserRolesAsync(context, cancellationToken);
        }

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
        HashSet<(Guid RoleId, Guid AccessFunctionId)> desiredLinks)
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
        HashSet<(Guid RoleId, Guid AccessFunctionId)> desiredLinks,
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

    private static HashSet<(Guid RoleId, Guid AccessFunctionId)> GetDesiredRoleAccessFunctionLinks(MainDbContext context)
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

    private static async Task<HashSet<(Guid RoleId, Guid AccessFunctionId)>> GetDesiredRoleAccessFunctionLinksAsync(
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

    private static void SeedNotificationConfiguration(MainDbContext context)
    {
        foreach (var definition in NotificationEventCatalog.Events)
        {
            var policy = context.NotificationPolicies
                .SingleOrDefault(item => item.EventKey == definition.EventKey);
            if (policy is null)
            {
                policy = CreateNotificationPolicy(definition);
                context.NotificationPolicies.Add(policy);
            }

            UpdateNotificationPolicy(policy, definition);
            var templates = context.NotificationTemplates
                .Where(template =>
                    template.EventKey == definition.EventKey &&
                    template.Channel == NotificationChannels.Email)
                .OrderByDescending(template => template.Version)
                .ToList();
            EnsureSystemTemplate(context, definition, templates);
        }

        SaveIfChanged(context);
    }

    private static void SeedApplications(MainDbContext context)
    {
        foreach (var seed in GetApplicationSeeds())
        {
            var existing = context.Applications.SingleOrDefault(application => application.Id == seed.Id);
            if (existing is null)
            {
                context.Applications.Add(seed);
                continue;
            }

            UpdateApplication(existing, seed);
        }

        SaveIfChanged(context);
    }

    private static async Task SeedApplicationsAsync(
        MainDbContext context,
        CancellationToken cancellationToken)
    {
        foreach (var seed in GetApplicationSeeds())
        {
            var existing = await context.Applications
                .SingleOrDefaultAsync(application => application.Id == seed.Id, cancellationToken);
            if (existing is null)
            {
                context.Applications.Add(seed);
                continue;
            }

            UpdateApplication(existing, seed);
        }

        await SaveIfChangedAsync(context, cancellationToken);
    }

    private static IEnumerable<Domain.Models.Application> GetApplicationSeeds()
    {
        yield return new Domain.Models.Application
        {
            Id = SystemApplicationIds.Core,
            Name = "NIE Template",
            Description = "Core platform and shared administration features.",
            ProjectKey = "nie-template",
            IsActive = true
        };
        yield return new Domain.Models.Application
        {
            Id = SystemApplicationIds.Procurement,
            Name = "Procurement Sample",
            Description = "Real-world procurement sample included with the template.",
            ProjectKey = "procurement",
            IsActive = true
        };
    }

    private static void UpdateApplication(
        Domain.Models.Application target,
        Domain.Models.Application source)
    {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Repository = source.Repository;
        target.Branch = source.Branch;
        target.ProjectKey = source.ProjectKey;
        target.IsActive = source.IsActive;
    }

    private static async Task SeedNotificationConfigurationAsync(
        MainDbContext context,
        CancellationToken cancellationToken)
    {
        foreach (var definition in NotificationEventCatalog.Events)
        {
            var policy = await context.NotificationPolicies
                .SingleOrDefaultAsync(
                    item => item.EventKey == definition.EventKey,
                    cancellationToken);
            if (policy is null)
            {
                policy = CreateNotificationPolicy(definition);
                context.NotificationPolicies.Add(policy);
            }

            UpdateNotificationPolicy(policy, definition);
            var templates = await context.NotificationTemplates
                .Where(template =>
                    template.EventKey == definition.EventKey &&
                    template.Channel == NotificationChannels.Email)
                .OrderByDescending(template => template.Version)
                .ToListAsync(cancellationToken);
            EnsureSystemTemplate(context, definition, templates);
        }

        await SaveIfChangedAsync(context, cancellationToken);
    }

    private static NotificationPolicy CreateNotificationPolicy(
        NotificationEventDefinition definition) =>
        new()
        {
            EventKey = definition.EventKey,
            InAppEnabled = definition.InAppEnabled,
            EmailEnabled = definition.EmailEnabled,
            PushEnabled = definition.PushEnabled,
            IsActive = true,
        };

    private static void UpdateNotificationPolicy(
        NotificationPolicy policy,
        NotificationEventDefinition definition)
    {
        policy.DisplayName = definition.DisplayName;
        policy.Description = definition.Description;
        policy.Category = definition.Category;
        if (!definition.SupportsReminderConfiguration)
        {
            policy.ReminderAfterHours = null;
            policy.EscalationAfterHours = null;
        }
    }

    private static void EnsureSystemTemplate(
        MainDbContext context,
        NotificationEventDefinition definition,
        IReadOnlyCollection<NotificationTemplate> templates)
    {
        if (templates.Count == 0)
        {
            context.NotificationTemplates.Add(CreateSystemTemplate(definition, 1));
            return;
        }

        var published = templates
            .Where(template => template.IsPublished)
            .OrderByDescending(template => template.Version)
            .FirstOrDefault();
        if (published is null ||
            !string.Equals(published.PublishedBy, "system", StringComparison.Ordinal) ||
            (published.Subject == definition.DefaultSubject &&
             published.Content == definition.DefaultContent))
        {
            return;
        }

        foreach (var template in templates.Where(template => template.IsPublished))
        {
            template.IsPublished = false;
        }

        context.NotificationTemplates.Add(CreateSystemTemplate(
            definition,
            templates.Max(template => template.Version) + 1));
    }

    private static NotificationTemplate CreateSystemTemplate(
        NotificationEventDefinition definition,
        int version) =>
        new()
        {
            EventKey = definition.EventKey,
            Channel = NotificationChannels.Email,
            Version = version,
            Subject = definition.DefaultSubject,
            Content = definition.DefaultContent,
            IsPublished = true,
            PublishedBy = "system",
            PublishedOn = BuildingBlocks.Helpers.DateTimeHelper.Now,
        };

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
        new Code { Type = ECodeType.TITLE.ToString(), Name = ECodeName.MR.ToString(), Description = "", DisplayName = "Mr.", DisplayOrder = 1, IsActive = true },
        new Code { Type = ECodeType.TITLE.ToString(), Name = ECodeName.MRS.ToString(), Description = "", DisplayName = "Mrs.", DisplayOrder = 2, IsActive = true },
        new Code { Type = ECodeType.USER_TYPE.ToString(), Name = ECodeName.ADMIN.ToString(), Description = "", DisplayName = "Administrator", DisplayOrder = 3, IsActive = true },
        new Code { Type = ECodeType.USER_TYPE.ToString(), Name = ECodeName.USER.ToString(), Description = "", DisplayName = "Non-Admin User", DisplayOrder = 4, IsActive = true },
        // === SAMPLE: procurement Code rows (reference vertical; remove only after approved replacement) ===
        new Code { Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.IT_SERVICES.ToString(), Description = "", DisplayName = "IT Services", DisplayOrder = 5, IsActive = true },
        new Code { Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.OFFICE_SUPPLIES.ToString(), Description = "", DisplayName = "Office Supplies", DisplayOrder = 6, IsActive = true },
        new Code { Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.MAINTENANCE.ToString(), Description = "", DisplayName = "Maintenance", DisplayOrder = 7, IsActive = true },
        new Code { Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.CONSULTING.ToString(), Description = "", DisplayName = "Consulting", DisplayOrder = 8, IsActive = true },
        new Code { Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.LOGISTICS.ToString(), Description = "", DisplayName = "Logistics", DisplayOrder = 9, IsActive = true },
        new Code { Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.HARDWARE.ToString(), Description = "", DisplayName = "Hardware", DisplayOrder = 10, IsActive = true },
        new Code { Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.SOFTWARE.ToString(), Description = "", DisplayName = "Software", DisplayOrder = 11, IsActive = true },
        new Code { Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.FURNITURE.ToString(), Description = "", DisplayName = "Furniture", DisplayOrder = 12, IsActive = true },
        new Code { Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.STATIONERY.ToString(), Description = "", DisplayName = "Stationery", DisplayOrder = 13, IsActive = true },
        new Code { Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.CLEANING.ToString(), Description = "", DisplayName = "Cleaning", DisplayOrder = 14, IsActive = true },
        new Code { Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.EACH.ToString(), Description = "", DisplayName = "Each", DisplayOrder = 15, IsActive = true },
        new Code { Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.BOX.ToString(), Description = "", DisplayName = "Box", DisplayOrder = 16, IsActive = true },
        new Code { Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.PACK.ToString(), Description = "", DisplayName = "Pack", DisplayOrder = 17, IsActive = true },
        new Code { Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.SET.ToString(), Description = "", DisplayName = "Set", DisplayOrder = 18, IsActive = true },
        new Code { Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.HOUR.ToString(), Description = "", DisplayName = "Hour", DisplayOrder = 19, IsActive = true },
        new Code { Type = ECodeType.DELIVERY_LOCATION.ToString(), Name = ECodeName.MAIN_OFFICE.ToString(), Description = "", DisplayName = "Main Office", DisplayOrder = 20, IsActive = true },
        new Code { Type = ECodeType.DELIVERY_LOCATION.ToString(), Name = ECodeName.WAREHOUSE.ToString(), Description = "", DisplayName = "Warehouse", DisplayOrder = 21, IsActive = true },
        new Code { Type = ECodeType.DELIVERY_LOCATION.ToString(), Name = ECodeName.BRANCH_OFFICE.ToString(), Description = "", DisplayName = "Branch Office", DisplayOrder = 22, IsActive = true },
        new Code { Type = ECodeType.CURRENCY.ToString(), Name = ECodeName.SGD.ToString(), Description = "", DisplayName = "SGD - Singapore Dollar", DisplayOrder = 23, IsActive = true },
        new Code { Type = ECodeType.CURRENCY.ToString(), Name = ECodeName.USD.ToString(), Description = "", DisplayName = "USD - US Dollar", DisplayOrder = 24, IsActive = true }
        // === END SAMPLE ===
        ];

    private static List<AccessFunction> GetAccessFunctionSeeds() =>
        AccessFunctionCatalog.AccessFunctions
            .Select(definition => new AccessFunction
            {
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

    private static List<UserRole> GetDevelopmentUserRoleSeeds() =>
    [
        new UserRole
        {
            UserId = "devia",
            RoleId = SystemRoleIds.Administrator,
            AssignedOn = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            IsActive = true
        },
        new UserRole
        {
            UserId = "kamaludemy",
            RoleId = SystemRoleIds.Administrator,
            AssignedOn = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            IsActive = true
        }
    ];

    private static List<WorkflowTransition> GetWorkflowTransitionSeeds() =>
    [
        new WorkflowTransition { FromState = EWorkflowState.Draft.ToString(), ToState = EWorkflowState.Submitted.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Submit for Review", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 },
        new WorkflowTransition { FromState = EWorkflowState.Submitted.ToString(), ToState = EWorkflowState.UnderReview.ToString(), RequiredRole = ERole.Manager.ToString(), DisplayLabel = "Start Review", RequiresRemarks = false, IsActive = true, DisplayOrder = 1 },
        new WorkflowTransition { FromState = EWorkflowState.UnderReview.ToString(), ToState = EWorkflowState.Approved.ToString(), RequiredRole = ERole.Manager.ToString(), DisplayLabel = "Approve", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 },
        new WorkflowTransition { FromState = EWorkflowState.UnderReview.ToString(), ToState = EWorkflowState.Rejected.ToString(), RequiredRole = ERole.Manager.ToString(), DisplayLabel = "Reject", RequiresRemarks = true, IsActive = true, DisplayOrder = 2 },
        new WorkflowTransition { FromState = EWorkflowState.UnderReview.ToString(), ToState = EWorkflowState.ReturnedForRevision.ToString(), RequiredRole = ERole.Manager.ToString(), DisplayLabel = "Return for Revision", RequiresRemarks = true, IsActive = true, DisplayOrder = 3 },
        new WorkflowTransition { FromState = EWorkflowState.ReturnedForRevision.ToString(), ToState = EWorkflowState.Submitted.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Resubmit", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 },
        new WorkflowTransition { FromState = EWorkflowState.Approved.ToString(), ToState = EWorkflowState.Completed.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Mark as Completed", RequiresRemarks = false, IsActive = true, DisplayOrder = 1 },
        new WorkflowTransition { FromState = EWorkflowState.Draft.ToString(), ToState = EWorkflowState.Cancelled.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Cancel", RequiresRemarks = true, IsActive = true, DisplayOrder = 2 },
        new WorkflowTransition { FromState = EWorkflowState.Submitted.ToString(), ToState = EWorkflowState.Cancelled.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Cancel", RequiresRemarks = true, IsActive = true, DisplayOrder = 2 },
        new WorkflowTransition { FromState = EWorkflowState.Rejected.ToString(), ToState = EWorkflowState.Draft.ToString(), RequiredRole = ERole.Administrator.ToString(), DisplayLabel = "Re-open as Draft", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 }
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

}
