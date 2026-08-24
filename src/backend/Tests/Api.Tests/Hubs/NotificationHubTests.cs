using Api.Hubs;
using Api.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace Api.Tests.Hubs;

/// <summary>
/// The notification group must be derived from the validated server session only —
/// a client that can pick its own group can read another user's notifications.
/// </summary>
public sealed class NotificationHubTests
{
    [Theory]
    [InlineData("staff-01", "notification-user:staff-01")]
    [InlineData("STAFF-01", "notification-user:staff-01")]
    [InlineData("  Staff-01\t", "notification-user:staff-01")]
    public void The_group_name_is_trimmed_and_lowercased(string userId, string expected)
    {
        Assert.Equal(expected, NotificationHub.UserGroup(userId));
    }

    [Fact]
    public void Different_users_never_share_a_group()
    {
        Assert.NotEqual(NotificationHub.UserGroup("staff-01"), NotificationHub.UserGroup("staff-02"));
    }

    [Fact]
    public async Task A_validated_session_joins_only_its_own_group()
    {
        using var hub = CreateHub(MvcTestContext.CreateHttpContext(userId: "Staff-01"));

        await hub.OnConnectedAsync();

        await hub.Groups.Received(1).AddToGroupAsync(
            "connection-1",
            "notification-user:staff-01",
            Arg.Any<CancellationToken>());
        hub.Context.DidNotReceive().Abort();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_connection_without_a_validated_session_user_is_aborted(string? userId)
    {
        using var hub = CreateHub(MvcTestContext.CreateHttpContext(userId: userId));

        await hub.OnConnectedAsync();

        hub.Context.Received(1).Abort();
        await hub.Groups.DidNotReceive().AddToGroupAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_connection_with_no_http_context_is_aborted()
    {
        using var hub = CreateHub(httpContext: null);

        await hub.OnConnectedAsync();

        hub.Context.Received(1).Abort();
        await hub.Groups.DidNotReceive().AddToGroupAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static NotificationHub CreateHub(HttpContext? httpContext)
    {
        var features = new FeatureCollection();
        if (httpContext is not null)
        {
            var httpContextFeature = Substitute.For<IHttpContextFeature>();
            httpContextFeature.HttpContext.Returns(httpContext);
            features.Set(httpContextFeature);
        }

        var callerContext = Substitute.For<HubCallerContext>();
        callerContext.ConnectionId.Returns("connection-1");
        callerContext.Features.Returns(features);

        return new NotificationHub
        {
            Context = callerContext,
            Groups = Substitute.For<IGroupManager>(),
        };
    }
}
