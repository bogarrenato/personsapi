using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR;


[Authorize]
public class PresenceHub(PresenceTracker tracker) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.GetUsername() == null)
        {
            throw new HubException("Cannot get user claim");
        }

        var isOnline = await tracker.UserConnected(Context.User.GetUsername(), Context.ConnectionId);
        //When an user connects to a hub and if other people are connected to this hub this is going to send a message to them
        if (isOnline)
        {
            await Clients.Others.SendAsync("UserIsOnline", Context.User?.GetUsername());
        }

        var currentUsers = await tracker.GetOnlineUsers();
        await Clients.Caller.SendAsync("GetOnlineUsers", currentUsers);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {

        if (Context.User?.GetUsername() == null)
        {
            throw new HubException("Cannot get user claim");
        }

        var isOffline = await tracker.UserDisconnected(Context.User.GetUsername(), Context.ConnectionId);
        if (isOffline)
        {
            await Clients.Others.SendAsync("UserIsOffline", Context.User?.GetUsername());
        }
        // var currentUsers = await tracker.GetOnlineUsers();
        // await Clients.All.SendAsync("GetOnlineUsers", currentUsers);

        await base.OnDisconnectedAsync(exception);
    }
}