using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Interfaces;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }
    IMessageRepository MessageRepository { get; }
    ILikesRepository LikesRepository { get; }
    //Call savechanges async and return boolean
    Task<bool> Complete();
    //If EF trackes any changes to an entity  -track if something is changed
    bool HasChanges();
}