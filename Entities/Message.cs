using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities;


//Join table between two messages
public class Message
{
    public int Id { get; set; }
    public required string SenderUsername { get; set; }
    public required string RecipientUsername { get; set; }
    public required string Content { get; set; }
    public DateTime? DateRead { get; set; }
    public DateTime MessageSent { get; set; } = DateTime.UtcNow;
    public bool SenderDeleted { get; set; }
    // When one man deletes it , the other still can read  and when both deleted we remove from DB
    public bool RecipientDeleted { get; set; }


    // Navigation properties
    public int SenderId { get; set; }
    //Null forgiven for EF
    public AppUser Sender { get; set; } = null!;
    public int RecipientId { get; set; }
    //Null forgiven for EF
    public AppUser Recipient { get; set; } = null!;
}