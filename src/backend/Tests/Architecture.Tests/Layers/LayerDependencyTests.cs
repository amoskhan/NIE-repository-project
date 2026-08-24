using Application.Abstractions;
using BuildingBlocks.Helpers;
using Infrastructure.Persistence;

namespace Architecture.Tests;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_has_no_outward_layer_dependencies()
    {
        AssertDoesNotReference(
            typeof(Domain.Models.BaseEntity).Assembly,
            "Application",
            "Persistence",
            "AI",
            "Api",
            "Auth",
            "BuildingBlocks",
            "Validation");
    }

    [Fact]
    public void Application_depends_on_abstractions_not_infrastructure_or_hosts()
    {
        AssertDoesNotReference(
            typeof(IApplicationDbContext).Assembly,
            "Persistence",
            "AI",
            "Api",
            "Auth");
    }

    [Fact]
    public void Building_blocks_remain_dependency_light()
    {
        AssertDoesNotReference(
            typeof(DateTimeHelper).Assembly,
            "Domain",
            "Application",
            "Persistence",
            "AI",
            "Api",
            "Auth");
    }

    [Fact]
    public void Infrastructure_implements_the_application_persistence_boundary()
    {
        Assert.Contains(
            typeof(IApplicationDbContext),
            typeof(MainDbContext).GetInterfaces());
    }

    private static void AssertDoesNotReference(
        System.Reflection.Assembly assembly,
        params string[] forbiddenAssemblies)
    {
        var references = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        var violations = forbiddenAssemblies
            .Where(references.Contains)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }
}
