using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities;
public class Connection
{
    public required string ConnectionId { get; set; }
    public required string Username { get; set; }
}
