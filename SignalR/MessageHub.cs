using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR;


public class MessageHub(IUnitOfWork unitOfWork, IMapper mapper, IHubContext<PresenceHub> presenceHub) : Hub
{

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var otherUser = httpContext?.Request.Query["user"].ToString();

        if (Context.User == null || string.IsNullOrEmpty(otherUser))
        {
            throw new HubException("Cannot get user claim");
        }

        var groupName = GetGroupName(Context.User.GetUsername(), otherUser);

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        var group = await AddToGroup(groupName);

        // Üzenetek lekérése és olvasottá tétele
        var messages = await unitOfWork.MessageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);

        // Mindenképp küldjük el az UpdatedGroup eseményt
        await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

        // Majd mentsük a változásokat
        await unitOfWork.Complete();

        // Végül küldjük el az üzenetszálat
        await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {

        var group = await RemoveFromMessageGroup(Context.ConnectionId);
        await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
        await base.OnDisconnectedAsync(exception);
    }


    private string GetGroupName(string caller, string other)
    {
        //
        var stringCompare = string.CompareOrdinal(caller, other) < 0;
        return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
    }

    private async Task<Group> AddToGroup(string groupName)
    {
        var username = Context.User?.GetUsername() ?? throw new Exception("could not get user");
        var group = await unitOfWork.MessageRepository.GetMessageGroup(groupName);
        Console.WriteLine($"Adding user {username} to group {groupName}");

        var connection = new Connection { ConnectionId = Context.ConnectionId, Username = username };

        if (group == null)
        {
            Console.WriteLine($"Creating new group {groupName}");
            group = new Group { Name = groupName };
            unitOfWork.MessageRepository.AddGroup(group);
        }

        group.Connections.Add(connection);
        Console.WriteLine($"Group now has {group.Connections.Count} connections");

        if (await unitOfWork.Complete())
        {
            return group;
        }

        throw new HubException("Failed to join group");
    }

    private async Task<Group> RemoveFromMessageGroup(string connectionId)
    {
        var group = await unitOfWork.MessageRepository.GetGroupForConnection(Context.ConnectionId);
        var connection = group?.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
        if (connection != null && group != null)
        {
            unitOfWork.MessageRepository.RemoveConnection(connection);
            if (await unitOfWork.Complete())
            {
                return group;
            }
        }

        throw new HubException("Failed to remove from group");

    }

    public async Task SendMessage(CreateMessageDto createMessageDto)
    {
        var username = Context.User?.GetUsername() ?? throw new Exception("could not get user");

        if (username == createMessageDto.RecipientUsername.ToLower())
        {
            throw new HubException("You cannot send messages to yourself");
        }

        var sender = await unitOfWork.UserRepository.GetUserByUsernameAsync(username);
        var recipient = await unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

        if (recipient == null || sender == null || sender.UserName == null || recipient.UserName == null)
        {
            throw new HubException("User not found");
        }

        var message = new Message
        {
            Sender = sender,
            Recipient = recipient,
            SenderUsername = sender.UserName,
            RecipientUsername = recipient.UserName,
            Content = createMessageDto.Content
        };

        var groupName = GetGroupName(sender.UserName, recipient.UserName);
        var group = await unitOfWork.MessageRepository.GetMessageGroup(groupName);
        // Console.WriteLine(groupName);

        if (group != null && group.Connections.Any(x => x.Username == recipient.UserName))
        {
            message.DateRead = DateTime.UtcNow;
        }
        else
        {
            var connections = await PresenceTracker.GetConnectionsForUser(recipient.UserName);
            if (connections != null && connections?.Count != null)
            {
                await presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", new { username = sender.UserName, knownAs = sender.KnownAs });
            }
        }


        unitOfWork.MessageRepository.AddMessage(message);

        if (await unitOfWork.Complete())
        {
            await Clients.Group(groupName).SendAsync("NewMessage", mapper.Map<MessageDto>(message));
        }
    }
}
