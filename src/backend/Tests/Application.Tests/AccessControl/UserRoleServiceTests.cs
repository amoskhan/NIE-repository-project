using Application.Abstractions;
using Application.Abstractions.Identity;
using Application.Contracts;
using Application.Features;
using Application.Tests.TestSupport;
using Domain.Models;
using NSubstitute;

namespace Application.Tests;

public sealed class UserRoleServiceTests
{
    [Fact]
    public async Task Role_profile_matches_institutional_user_ids_without_regard_to_case()
    {
        var role = new Role
        {
            Code = "OPERATIONS_USER",
            Name = "Operations User",
            IsActive = true,
        };
        var userRoles = new FakeDbSet<UserRole>(new UserRole
        {
            UserId = "nie25",
            Role = role,
            IsActive = true,
        });
        var context = Substitute.For<IApplicationDbContext>();
        context.UserRoles.Returns(userRoles);
        var service = new UserRoleService(
            context,
            Substitute.For<IUserContextService>(),
            Substitute.For<IAccessFunctionService>());

        var assignments = await service.GetUserRolesAsync("NIE25");

        var assignment = Assert.Single(assignments);
        Assert.Equal("OPERATIONS_USER", assignment.RoleCode);
    }

    [Fact]
    public async Task Removing_a_role_revokes_every_mixed_case_equivalent_assignment()
    {
        var roleId = Guid.CreateVersion7();
        var userRoles = new FakeDbSet<UserRole>(
            new UserRole { UserId = "NIE25", RoleId = roleId, IsActive = true },
            new UserRole { UserId = "nie25", RoleId = roleId, IsActive = true });
        var context = Substitute.For<IApplicationDbContext>();
        context.UserRoles.Returns(userRoles);
        var accessFunctions = Substitute.For<IAccessFunctionService>();
        var service = new UserRoleService(
            context,
            Substitute.For<IUserContextService>(),
            accessFunctions);

        var removed = await service.RemoveRoleAsync("NiE25", roleId);

        Assert.True(removed);
        Assert.Empty(userRoles.Items);
        await context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await accessFunctions.Received(1).InvalidateUsersAsync(
            Arg.Is<IEnumerable<string>>(ids => ids != null && ids.Single() == "nie25"));
    }

    [Fact]
    public async Task Assigning_a_role_canonicalizes_and_deduplicates_legacy_assignments()
    {
        var role = new Role
        {
            Id = Guid.CreateVersion7(),
            Code = "OPERATIONS_USER",
            Name = "Operations User",
            IsActive = true,
        };
        var userRoles = new FakeDbSet<UserRole>(
            new UserRole { UserId = "NIE25", RoleId = role.Id, Role = role, IsActive = true },
            new UserRole { UserId = "nie25", RoleId = role.Id, Role = role, IsActive = true });
        var context = Substitute.For<IApplicationDbContext>();
        context.Roles.Returns(new FakeDbSet<Role>(role));
        context.UserRoles.Returns(userRoles);
        var service = new UserRoleService(
            context,
            Substitute.For<IUserContextService>(),
            Substitute.For<IAccessFunctionService>());

        var assignments = await service.AssignRolesAsync(new AssignAccessDto
        {
            UserId = " NiE25 ",
            Scope = AccessAssignmentScope.Global,
            RoleIds = [role.Id],
        });

        var assignment = Assert.Single(assignments);
        Assert.Equal("nie25", assignment.UserId);
        Assert.Single(userRoles.Items);
        Assert.Empty(userRoles.Added);
        Assert.Single(userRoles.Removed);
    }
}
