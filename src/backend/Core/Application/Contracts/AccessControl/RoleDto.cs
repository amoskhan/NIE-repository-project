using Domain.Enums;

namespace Application.Contracts;

/// <summary>
/// DTO for listing user roles (simplified for frontend).
/// </summary>
public class UserRoleListDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = default!;
    public Guid Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RoleCode { get; set; }
    public string? RoleName { get; set; }
    public string? AssignedBy { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO describing a system access function.
/// </summary>
public class AccessFunctionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Module { get; set; } = default!;
    public EAccessFunctionType Type { get; set; }
    public string ResourceName { get; set; } = default!;
    public string? Route { get; set; }
    public string? HttpMethod { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemFunction { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for role information.
/// </summary>
public class RoleDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemRole { get; set; }
    public int DisplayOrder { get; set; }
    public int AssignedUserCount { get; set; }
    public List<AccessFunctionDto> AccessFunctions { get; set; } = new();
    public List<Guid> AccessFunctionIds { get; set; } = new();
}

/// <summary>
/// DTO for creating a new role.
/// </summary>
public class CreateRoleDto
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public required List<Guid> AccessFunctionIds { get; set; }
}

/// <summary>
/// DTO for updating a role.
/// </summary>
public class UpdateRoleDto
{
    public required Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public required List<Guid> AccessFunctionIds { get; set; }
}

/// <summary>
/// DTO for user role assignment.
/// </summary>
public class UserRoleDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = default!;
    public string RoleName { get; set; } = default!;
    public DateTime AssignedOn { get; set; }
    public string? AssignedBy { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for assigning a role to a user.
/// </summary>
public class AssignRoleDto
{
    public required string UserId { get; set; }
    public required Guid RoleId { get; set; }
    public DateTime? ExpiresOn { get; set; }
}

/// <summary>
/// Aggregated user row for the access-control screen.
/// </summary>
public class UserAccessSummaryDto
{
    public string UserId { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? DepartmentDescription { get; set; }
    public string? Designation { get; set; }
    public string? Title { get; set; }
    public string? ProfileSource { get; set; }
    public List<UserRoleDto> Assignments { get; set; } = new();
    public List<ApplicationAccessDto> ApplicationAccesses { get; set; } = new();
    public List<string> AccessFunctionCodes { get; set; } = new();
}

/// <summary>
/// Bundled response used by the access-control screen.
/// </summary>
public class AccessControlOverviewDto
{
    public List<UserAccessSummaryDto> Users { get; set; } = new();
    public List<RoleDto> Roles { get; set; } = new();
    public List<AccessFunctionDto> AccessFunctions { get; set; } = new();
    public List<ApplicationDto> Applications { get; set; } = new();
}

/// <summary>
/// Current user's roles and access functions for screen-level gating.
/// </summary>
public class CurrentAccessProfileDto
{
    public string UserId { get; set; } = default!;
    public List<string> RoleCodes { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
    public List<string> AccessFunctionCodes { get; set; } = new();
    public List<Guid> ApplicationIds { get; set; } = new();
}
