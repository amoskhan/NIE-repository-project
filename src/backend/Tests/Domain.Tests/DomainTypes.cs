using System.Reflection;
using Domain.Models;

namespace Domain.Tests;

/// <summary>
/// Reflection helpers shared by the domain-wide drift guards.
/// Anything added under <c>Domain.Models</c> is discovered automatically, which is the
/// point: a new entity that breaks a domain-wide invariant fails a test without anyone
/// remembering to extend a hand-maintained list.
/// </summary>
internal static class DomainTypes
{
    /// <summary>
    /// Every publicly constructible entity declared by the Domain assembly.
    /// </summary>
    public static IReadOnlyList<Type> Entities { get; } = typeof(BaseEntity).Assembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true })
        .Where(type => type.Namespace is not null
            && type.Namespace.StartsWith("Domain.Models", StringComparison.Ordinal))
        .Where(type => type.GetConstructor(Type.EmptyTypes) is not null)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    /// <summary>
    /// Constructs an entity exactly the way EF Core materialisation does: parameterless,
    /// so every property initialiser in the model runs.
    /// </summary>
    public static object CreateInstance(Type type) =>
        Activator.CreateInstance(type)
        ?? throw new InvalidOperationException("Could not construct " + type.FullName);

    public static IEnumerable<PropertyInfo> ReadableProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0);
}
