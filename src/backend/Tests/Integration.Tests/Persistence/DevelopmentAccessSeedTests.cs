using Application.Security;
using Infrastructure.Persistence;

namespace Integration.Tests;

public class DevelopmentAccessSeedTests
{
    [Fact]
    public void Authenticated_development_user_receives_the_least_privilege_operations_role()
    {
        var assignment = Assert.Single(
            MainDbContextSeeder.GetDevelopmentUserRoleSeeds(),
            seed => string.Equals(seed.UserId, "NIE25", StringComparison.Ordinal));

        Assert.Equal(SystemRoleIds.User, assignment.RoleId);
        Assert.True(assignment.IsActive);
        Assert.Null(assignment.ExpiresOn);

        var role = Assert.Single(
            AccessFunctionCatalog.Roles,
            seed => seed.Id == assignment.RoleId);

        Assert.Contains(AccessFunctionCodes.Screen.DashboardView, role.AccessFunctionCodes);
        Assert.Contains(AccessFunctionCodes.Api.AccessProfileRead, role.AccessFunctionCodes);
        Assert.DoesNotContain(AccessFunctionCodes.Api.AccessControlRolesManage, role.AccessFunctionCodes);
        Assert.DoesNotContain(AccessFunctionCodes.Api.AccessControlAssignmentsManage, role.AccessFunctionCodes);
    }
}
