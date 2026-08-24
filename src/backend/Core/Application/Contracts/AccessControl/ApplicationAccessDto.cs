namespace Application.Contracts;

public class ApplicationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Repository { get; set; }
    public string? Branch { get; set; }
    public string ProjectKey { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class ApplicationAccessDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = default!;
    public string ApplicationProjectKey { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = default!;
    public string RoleName { get; set; } = default!;
    public DateTime AssignedOn { get; set; }
    public string? AssignedBy { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsActive { get; set; }
}

public class AssignApplicationAccessDto
{
    public required Guid ApplicationId { get; set; }
    public required string UserId { get; set; }
    public required Guid RoleId { get; set; }
    public DateTime? ExpiresOn { get; set; }
}

public enum AccessAssignmentScope
{
    Global = 1,
    Application = 2
}

public class AssignAccessDto
{
    public required string UserId { get; set; }
    public AccessAssignmentScope Scope { get; set; }
    public required List<Guid> RoleIds { get; set; }
    public List<Guid> ApplicationIds { get; set; } = new();
    public DateTime? ExpiresOn { get; set; }
}

public class AccessAssignmentResultDto
{
    public List<UserRoleDto> GlobalAssignments { get; set; } = new();
    public List<ApplicationAccessDto> ApplicationAssignments { get; set; } = new();
}
