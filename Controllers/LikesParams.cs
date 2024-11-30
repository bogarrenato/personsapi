using API.Helpers;

namespace API.Controllers;

public class LikesParams : PaginationParams
{
    public int UserId { get; set; }
    public required string Predicate { get; set; }
}