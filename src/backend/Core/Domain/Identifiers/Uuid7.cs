namespace Domain.Identifiers;

/// <summary>
/// Canonical UUIDv7 identity factory for application entities.
/// </summary>
public static class Uuid7
{
    /// <summary>
    /// Creates a time-ordered RFC 9562 UUID version 7 using the .NET runtime implementation.
    /// </summary>
    public static Guid New() => Guid.CreateVersion7();

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="value"/> is a non-empty UUIDv7.
    /// </summary>
    public static bool IsValid(Guid value) => value != Guid.Empty && value.Version == 7;
}
