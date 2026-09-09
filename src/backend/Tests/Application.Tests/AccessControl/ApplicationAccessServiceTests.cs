using Application.Abstractions;
using Application.Contracts;
using Application.Features;
using Application.Tests.TestSupport;
using Domain.Models;
using NSubstitute;

namespace Application.Tests;

public sealed class ApplicationAccessServiceTests
{
    [Fact]
    public async Task Application_scope_matches_institutional_user_ids_without_regard_to_case()
    {
        var applicationId = Guid.CreateVersion7();
        var role = new Role
        {
            Code = "OPERATIONS_USER",
            Name = "Operations User",
            IsActive = true,
        };
        var context = Substitute.For<IApplicationDbContext>();
        context.UserRoles.Returns(new FakeDbSet<UserRole>(new UserRole
        {
            UserId = "NIE25",
            Role = role,
            IsActive = true,
        }));
        context.Applications.Returns(new FakeDbSet<Domain.Models.Application>(
            new Domain.Models.Application
            {
                Id = applicationId,
                Name = "Aug1",
                ProjectKey = "aug1",
                IsActive = true,
            }));
        context.ApplicationAccesses.Returns(new FakeDbSet<ApplicationAccess>());
        var service = new ApplicationAccessService(
            context,
            Substitute.For<IAccessFunctionService>());

        var applicationIds = await service.GetAccessibleApplicationIdsAsync("nie25");

        Assert.Contains(applicationId, applicationIds);
    }

    [Fact]
    public async Task Assigning_application_access_canonicalizes_and_deduplicates_legacy_assignments()
    {
        var role = new Role
        {
            Id = Guid.CreateVersion7(),
            Code = "OPERATIONS_USER",
            Name = "Operations User",
            IsActive = true,
        };
        var application = new Domain.Models.Application
        {
            Id = Guid.CreateVersion7(),
            Name = "Aug1",
            ProjectKey = "aug1",
            IsActive = true,
        };
        var accesses = new FakeDbSet<ApplicationAccess>(
            new ApplicationAccess
            {
                UserId = "NIE25",
                ApplicationId = application.Id,
                Application = application,
                RoleId = role.Id,
                Role = role,
            },
            new ApplicationAccess
            {
                UserId = "nie25",
                ApplicationId = application.Id,
                Application = application,
                RoleId = role.Id,
                Role = role,
            });
        var context = Substitute.For<IApplicationDbContext>();
        context.Roles.Returns(new FakeDbSet<Role>(role));
        context.Applications.Returns(new FakeDbSet<Domain.Models.Application>(application));
        context.ApplicationAccesses.Returns(accesses);
        var service = new ApplicationAccessService(
            context,
            Substitute.For<IAccessFunctionService>());

        var assignments = await service.AssignManyAsync(new AssignAccessDto
        {
            UserId = " NiE25 ",
            Scope = AccessAssignmentScope.Application,
            RoleIds = [role.Id],
            ApplicationIds = [application.Id],
        }, "administrator");

        var assignment = Assert.Single(assignments);
        Assert.Equal("nie25", assignment.UserId);
        Assert.Single(accesses.Items);
        Assert.Empty(accesses.Added);
        Assert.Single(accesses.Removed);
    }

    [Fact]
    public async Task Removing_application_access_revokes_every_mixed_case_equivalent_assignment()
    {
        var applicationId = Guid.CreateVersion7();
        var roleId = Guid.CreateVersion7();
        var selectedId = Guid.CreateVersion7();
        var accesses = new FakeDbSet<ApplicationAccess>(
            new ApplicationAccess
            {
                Id = selectedId,
                UserId = "NIE25",
                ApplicationId = applicationId,
                RoleId = roleId,
            },
            new ApplicationAccess
            {
                Id = Guid.CreateVersion7(),
                UserId = "nie25",
                ApplicationId = applicationId,
                RoleId = roleId,
            });
        var context = Substitute.For<IApplicationDbContext>();
        context.ApplicationAccesses.Returns(accesses);
        var accessFunctions = Substitute.For<IAccessFunctionService>();
        var service = new ApplicationAccessService(context, accessFunctions);

        var removed = await service.RemoveAsync(selectedId);

        Assert.True(removed);
        Assert.Empty(accesses.Items);
        Assert.Equal(2, accesses.Removed.Count);
        await context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await accessFunctions.Received(1).InvalidateUsersAsync(
            Arg.Is<IEnumerable<string>>(ids => ids != null && ids.Single() == "nie25"));
    }
}
