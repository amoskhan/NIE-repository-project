using Api.Authorization;
using Api.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Authorization;

public sealed class RequireOwnershipAttributeTests
{
    [Fact]
    public void Entity_types_that_cannot_be_owned_are_rejected_at_construction()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new RequireOwnershipAttribute(typeof(string)));

        Assert.Equal("entityType", exception.ParamName);
        Assert.Contains("IOwnedEntity", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Route_parameter_defaults_to_id()
    {
        var attribute = new RequireOwnershipAttribute(typeof(OwnedRecord));

        Assert.Equal("id", attribute.RouteParameterName);
        Assert.Equal(typeof(OwnedRecord), attribute.EntityType);
    }

    [Fact]
    public void The_created_filter_must_not_be_reused_because_it_captures_a_scoped_db_context()
    {
        Assert.False(new RequireOwnershipAttribute(typeof(OwnedRecord)).IsReusable);
    }

    [Fact]
    public void The_created_filter_is_closed_over_the_declared_entity_type()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        var services = new ServiceCollection()
            .AddSingleton<DbContext>(dbContext)
            .BuildServiceProvider();

        var filter = new RequireOwnershipAttribute(typeof(OwnedRecord)).CreateInstance(services);

        Assert.IsAssignableFrom<IAsyncActionFilter>(filter);
        Assert.Equal(typeof(OwnedRecord), Assert.Single(filter.GetType().GetGenericArguments()));
    }

    [Fact]
    public void Each_request_gets_its_own_filter_instance()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        var services = new ServiceCollection()
            .AddSingleton<DbContext>(dbContext)
            .BuildServiceProvider();
        var attribute = new RequireOwnershipAttribute(typeof(OwnedRecord));

        Assert.NotSame(attribute.CreateInstance(services), attribute.CreateInstance(services));
    }
}
