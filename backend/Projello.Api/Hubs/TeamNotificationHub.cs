using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Projello.Api.Services.Realtime;

namespace Projello.Api.Hubs;

[Authorize]
public sealed class TeamNotificationHub : Hub
{
    private readonly PresenceTracker _presence;

    public TeamNotificationHub(PresenceTracker presence)
    {
        _presence = presence;
    }

    // Every signed-in app keeps this connection open, so it doubles as "online" status
    public override Task OnConnectedAsync()
    {
        if (Context.UserIdentifier is { } userId) _presence.Connected(userId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.UserIdentifier is { } userId) _presence.Disconnected(userId);
        return base.OnDisconnectedAsync(exception);
    }
}
