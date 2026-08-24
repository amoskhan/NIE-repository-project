using Application.Security;
using Domain.Enums;

namespace Application.Tests;

public sealed class AccessFunctionCatalogTests
{
    [Fact]
    public void Declares_every_access_function_code_exactly_once()
    {
        var codes = AccessFunctionCatalog.AccessFunctions.Select(item => item.Code).ToList();

        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Grants_only_codes_that_the_catalog_defines()
    {
        var known = AccessFunctionCatalog.AccessFunctions
            .Select(item => item.Code)
            .ToHashSet(StringComparer.Ordinal);

        var dangling = AccessFunctionCatalog.Roles
            .SelectMany(role => role.AccessFunctionCodes.Select(code => $"{role.Code}:{code}"))
            .Where(entry => !known.Contains(entry.Split(':', 2)[1]))
            .ToList();

        Assert.Empty(dangling);
    }

    [Fact]
    public void Grants_the_administrator_every_defined_access_function()
    {
        var administrator = AccessFunctionCatalog.Roles.Single(role => role.Code == "SYSTEM_ADMIN");
        var missing = AccessFunctionCatalog.AccessFunctions
            .Select(item => item.Code)
            .Except(administrator.AccessFunctionCodes, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void Keeps_access_control_administration_away_from_the_non_administrator_roles()
    {
        var privileged = new[]
        {
            AccessFunctionCodes.Api.AccessControlRolesManage,
            AccessFunctionCodes.Api.AccessControlAssignmentsManage,
            AccessFunctionCodes.Api.ApplicationAccessManage,
            AccessFunctionCodes.Screen.AccessControlView,
        };

        var leaks = AccessFunctionCatalog.Roles
            .Where(role => role.Code != "SYSTEM_ADMIN")
            .SelectMany(role => role.AccessFunctionCodes
                .Where(code => privileged.Contains(code, StringComparer.Ordinal))
                .Select(code => $"{role.Code}:{code}"))
            .ToList();

        Assert.Empty(leaks);
    }

    [Fact]
    public void Keeps_write_scoped_procurement_codes_away_from_the_read_only_viewer()
    {
        var viewer = AccessFunctionCatalog.Roles.Single(role => role.Code == "READ_ONLY_VIEWER");

        Assert.DoesNotContain(AccessFunctionCodes.Api.ProcurementOrderManage, viewer.AccessFunctionCodes);
        Assert.DoesNotContain(AccessFunctionCodes.Api.ProcurementOrderApprove, viewer.AccessFunctionCodes);
        Assert.DoesNotContain(AccessFunctionCodes.Api.ProcurementVendorManage, viewer.AccessFunctionCodes);
        Assert.DoesNotContain(AccessFunctionCodes.Api.DocumentManage, viewer.AccessFunctionCodes);
        Assert.Contains(AccessFunctionCodes.Api.ProcurementOrderRead, viewer.AccessFunctionCodes);
    }

    [Fact]
    public void Reserves_purchase_order_approval_for_the_operations_manager()
    {
        var approvers = AccessFunctionCatalog.Roles
            .Where(role => role.AccessFunctionCodes.Contains(
                AccessFunctionCodes.Api.ProcurementOrderApprove,
                StringComparer.Ordinal))
            .Select(role => role.Code)
            .ToList();

        Assert.Equal(["SYSTEM_ADMIN", "OPERATIONS_MANAGER"], approvers);
    }

    [Fact]
    public void Gives_every_role_a_unique_identifier_and_code()
    {
        var roles = AccessFunctionCatalog.Roles;

        Assert.Equal(roles.Count, roles.Select(role => role.Id).Distinct().Count());
        Assert.Equal(roles.Count, roles.Select(role => role.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(Guid.Empty, roles.Select(role => role.Id));
    }

    [Fact]
    public void Routes_every_screen_access_function_without_an_http_method()
    {
        var wrong = AccessFunctionCatalog.AccessFunctions
            .Where(item => item.Type == EAccessFunctionType.Screen)
            .Where(item => string.IsNullOrWhiteSpace(item.Route) || item.HttpMethod is not null)
            .Select(item => item.Code)
            .ToList();

        Assert.Empty(wrong);
    }

    [Fact]
    public void Gives_every_api_access_function_a_route_and_an_http_method()
    {
        var wrong = AccessFunctionCatalog.AccessFunctions
            .Where(item => item.Type == EAccessFunctionType.Api)
            .Where(item =>
                string.IsNullOrWhiteSpace(item.Route) ||
                string.IsNullOrWhiteSpace(item.HttpMethod) ||
                string.IsNullOrWhiteSpace(item.ResourceName))
            .Select(item => item.Code)
            .ToList();

        Assert.Empty(wrong);
    }

    [Fact]
    public void Prefixes_every_code_with_the_kind_of_thing_it_protects()
    {
        var wrong = AccessFunctionCatalog.AccessFunctions
            .Where(item => item.Type switch
            {
                EAccessFunctionType.Screen => !item.Code.StartsWith("screen.", StringComparison.Ordinal),
                EAccessFunctionType.Api => !item.Code.StartsWith("api.", StringComparison.Ordinal),
                _ => false,
            })
            .Select(item => item.Code)
            .ToList();

        Assert.Empty(wrong);
    }

    [Fact]
    public void Describes_every_access_function_for_the_administration_screen()
    {
        var incomplete = AccessFunctionCatalog.AccessFunctions
            .Where(item =>
                string.IsNullOrWhiteSpace(item.Name) ||
                string.IsNullOrWhiteSpace(item.Module) ||
                string.IsNullOrWhiteSpace(item.Description) ||
                item.DisplayOrder <= 0)
            .Select(item => item.Code)
            .ToList();

        Assert.Empty(incomplete);
    }

    [Fact]
    public void Lists_no_duplicate_grants_inside_a_single_role()
    {
        var duplicated = AccessFunctionCatalog.Roles
            .Where(role => role.AccessFunctionCodes.Count !=
                role.AccessFunctionCodes.Distinct(StringComparer.Ordinal).Count())
            .Select(role => role.Code)
            .ToList();

        Assert.Empty(duplicated);
    }

    [Fact]
    public void Lets_every_role_reach_its_own_notification_and_table_preferences()
    {
        var missing = AccessFunctionCatalog.Roles
            .Where(role => !role.AccessFunctionCodes.Contains(
                AccessFunctionCodes.Api.DataTablePreferenceManage,
                StringComparer.Ordinal))
            .Select(role => role.Code)
            .ToList();

        Assert.Empty(missing);
    }
}
