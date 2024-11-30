using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;
public class MessageRepository(DataContext context, IMapper mapper) : IMessageRepository
{
    public void AddMessage(Message message)
    {
        context.Messages.Add(message);
    }

    public void DeleteMessage(Message message)
    {
        context.Messages.Remove(message);
    }

    public async Task<Message?> GetMessage(int id)
    {
        return await context.Messages.FindAsync(id);
    }

    public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
    {
        var query = context.Messages
        .OrderByDescending(m => m.MessageSent).AsQueryable().AsQueryable();
        query = messageParams.Container switch
        {
            "Inbox" => query.Where(u => u.Recipient.UserName == messageParams.Username && u.RecipientDeleted == false),
            "Outbox" => query.Where(u => u.Sender.UserName == messageParams.Username && u.SenderDeleted == false),
            //Unread
            _ => query.Where(u => u.Recipient.UserName == messageParams.Username && u.DateRead == null && u.RecipientDeleted == false)
        };

        var messages = query.ProjectTo<MessageDto>(mapper.ConfigurationProvider);

        return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
    }

    // public Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
    // {
    //     throw new NotImplementedException();
    // }

    public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
    {
        var messages = await context.Messages
                        .Include(x => x.Sender)
                        .ThenInclude(x => x.Photos)
                        .Include(x => x.Recipient)
                        .ThenInclude(x => x.Photos)
                        .Where(x => x.RecipientUsername == currentUsername
                                && x.RecipientDeleted == false
                                && x.SenderUsername == recipientUsername ||
                                x.SenderUsername == currentUsername
                                 && x.SenderDeleted == false
                                 && x.RecipientUsername == recipientUsername)
                        .OrderBy(x => x.MessageSent)
                        .ToListAsync();

        var unreadMessages = messages.Where(x => x.DateRead == null && x.Recipient.UserName == currentUsername).ToList();

        if (unreadMessages.Count != 0)
        {
            foreach (var message in unreadMessages)
            {
                message.DateRead = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }

        return mapper.Map<IEnumerable<MessageDto>>(messages);
    }

    public async Task<bool> SaveAllAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }
}