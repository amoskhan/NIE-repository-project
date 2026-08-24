using Api.Validation;
using Application.Contracts;

namespace Api.Tests.Validation;

/// <summary>
/// Role and access-assignment payloads decide who gets which privileges, so the
/// shape rules on codes, identifiers and scope combinations are load-bearing.
/// </summary>
public sealed class AccessControlRequestValidatorsTests
{
    private readonly CreateRoleDtoValidator _createRole = new();
    private readonly AssignAccessDtoValidator _assignAccess = new();

    [Theory]
    [InlineData("Approver")]
    [InlineData("finance.manager")]
    [InlineData("A_1-2.3")]
    public void A_role_code_may_start_with_a_letter_and_use_safe_punctuation(string code)
    {
        Assert.True(_createRole.Validate(CreateRole(code)).IsValid);
    }

    [Theory]
    [InlineData("1Approver")]
    [InlineData(".approver")]
    [InlineData("-approver")]
    [InlineData("app rover")]
    [InlineData("app/rover")]
    [InlineData("app;drop")]
    [InlineData("")]
    public void A_role_code_that_is_not_a_safe_identifier_is_rejected(string code)
    {
        Assert.False(_createRole.Validate(CreateRole(code)).IsValid);
    }

    [Fact]
    public void A_role_code_is_capped_at_100_characters()
    {
        Assert.True(_createRole.Validate(CreateRole("a" + new string('b', 99))).IsValid);
        Assert.False(_createRole.Validate(CreateRole("a" + new string('b', 100))).IsValid);
    }

    [Fact]
    public void A_role_must_be_named()
    {
        var request = CreateRole("approver");
        request.Name = string.Empty;

        Assert.False(_createRole.Validate(request).IsValid);
    }

    [Fact]
    public void Granted_access_function_ids_must_be_real_and_unique()
    {
        var withEmpty = CreateRole("approver");
        withEmpty.AccessFunctionIds = [Guid.Empty];

        var duplicated = CreateRole("approver");
        var id = Guid.CreateVersion7();
        duplicated.AccessFunctionIds = [id, id];

        Assert.False(_createRole.Validate(withEmpty).IsValid);
        Assert.False(_createRole.Validate(duplicated).IsValid);
    }

    [Fact]
    public void An_update_must_identify_the_role_it_changes()
    {
        var validator = new UpdateRoleDtoValidator();
        var request = new UpdateRoleDto
        {
            Id = Guid.Empty,
            Code = "approver",
            Name = "Approver",
            AccessFunctionIds = [],
        };

        Assert.False(validator.Validate(request).IsValid);

        request.Id = Guid.CreateVersion7();
        Assert.True(validator.Validate(request).IsValid);
    }

    [Fact]
    public void A_global_assignment_must_not_carry_application_identifiers()
    {
        var request = AssignAccess(AccessAssignmentScope.Global);
        request.ApplicationIds = [Guid.CreateVersion7()];

        Assert.False(_assignAccess.Validate(request).IsValid);
    }

    [Fact]
    public void An_application_scoped_assignment_must_name_at_least_one_application()
    {
        var request = AssignAccess(AccessAssignmentScope.Application);

        Assert.False(_assignAccess.Validate(request).IsValid);

        request.ApplicationIds = [Guid.CreateVersion7()];
        Assert.True(_assignAccess.Validate(request).IsValid);
    }

    [Fact]
    public void An_assignment_must_grant_at_least_one_role()
    {
        var request = AssignAccess(AccessAssignmentScope.Global);
        request.RoleIds = [];

        Assert.False(_assignAccess.Validate(request).IsValid);
    }

    [Fact]
    public void No_more_than_twenty_roles_may_be_assigned_at_once()
    {
        var acceptable = AssignAccess(AccessAssignmentScope.Global);
        acceptable.RoleIds = Ids(20);

        var excessive = AssignAccess(AccessAssignmentScope.Global);
        excessive.RoleIds = Ids(21);

        Assert.True(_assignAccess.Validate(acceptable).IsValid);
        Assert.False(_assignAccess.Validate(excessive).IsValid);
    }

    [Fact]
    public void Duplicate_or_empty_role_identifiers_are_rejected()
    {
        var duplicated = AssignAccess(AccessAssignmentScope.Global);
        var id = Guid.CreateVersion7();
        duplicated.RoleIds = [id, id];

        var empty = AssignAccess(AccessAssignmentScope.Global);
        empty.RoleIds = [Guid.Empty];

        Assert.False(_assignAccess.Validate(duplicated).IsValid);
        Assert.False(_assignAccess.Validate(empty).IsValid);
    }

    [Fact]
    public void An_out_of_range_scope_value_is_rejected()
    {
        var request = AssignAccess((AccessAssignmentScope)99);

        Assert.False(_assignAccess.Validate(request).IsValid);
    }

    [Fact]
    public void The_target_user_id_is_required_and_bounded()
    {
        var missing = AssignAccess(AccessAssignmentScope.Global);
        missing.UserId = string.Empty;

        var oversized = AssignAccess(AccessAssignmentScope.Global);
        oversized.UserId = new string('u', 257);

        Assert.False(_assignAccess.Validate(missing).IsValid);
        Assert.False(_assignAccess.Validate(oversized).IsValid);
    }

    private static List<Guid> Ids(int count) =>
        Enumerable.Range(0, count).Select(_ => Guid.CreateVersion7()).ToList();

    private static CreateRoleDto CreateRole(string code) =>
        new()
        {
            Code = code,
            Name = "Approver",
            Description = "Approves purchase orders",
            AccessFunctionIds = [],
        };

    private static AssignAccessDto AssignAccess(AccessAssignmentScope scope) =>
        new()
        {
            UserId = "user-1",
            Scope = scope,
            RoleIds = [Guid.CreateVersion7()],
        };
}
