
using BuildingBlocks.Globals;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

/// <summary>
/// Authenticated real-time notification stream. The group is derived from the
/// validated server session, never from the client-supplied query string.
/// </summary>
public sealed class NotificationHub : Hub
{
    public static string UserGroup(string userId) =>
        $"notification-user:{userId.Trim().ToLowerInvariant()}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?
            .Items[Constants.KeySessionUserId]?
            .ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }
}
