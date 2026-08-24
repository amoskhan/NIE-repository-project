using Domain.Identifiers;

namespace Application.Security;

/// <summary>
/// Stable UUIDv7 identifiers for the template's demonstrator application scopes.
/// </summary>
public static class SystemApplicationIds
{
    public static readonly Guid Core = Guid.Parse("019fc374-e85b-7001-8000-000000000001");
    public static readonly Guid Procurement = Guid.Parse("019fc374-e85b-7002-8000-000000000002");

    public static IReadOnlyList<Guid> All { get; } = [Core, Procurement];

    static SystemApplicationIds()
    {
        if (All.Any(id => !Uuid7.IsValid(id)))
        {
            throw new InvalidOperationException("Every predefined application ID must be a non-empty UUIDv7.");
        }
    }
}
