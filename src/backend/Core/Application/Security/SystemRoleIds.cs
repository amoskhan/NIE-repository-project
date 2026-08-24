using Domain.Identifiers;

namespace Application.Security;

/// <summary>
/// Stable UUIDv7 identifiers for predefined system roles.
/// Role codes remain the authorization contract; these values only provide deterministic seed keys.
/// </summary>
public static class SystemRoleIds
{
    public static readonly Guid Administrator = Guid.Parse("019fc374-e85a-774c-9b18-07e1eab18455");
    public static readonly Guid User = Guid.Parse("019fc374-e85a-7a10-9bf9-d47476a3ee53");
    public static readonly Guid Manager = Guid.Parse("019fc374-e85a-7ec3-a820-06fbba29d0d7");
    public static readonly Guid Viewer = Guid.Parse("019fc374-e85a-72a7-b3f9-f83efeb15443");

    public static IReadOnlyList<Guid> All { get; } =
    [
        Administrator,
        User,
        Manager,
        Viewer
    ];

    static SystemRoleIds()
    {
        if (All.Any(id => !Uuid7.IsValid(id)))
        {
            throw new InvalidOperationException("Every predefined system-role ID must be a non-empty UUIDv7.");
        }
    }
}
