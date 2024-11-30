using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers;

public class LikesController : BaseApiController
{
    private readonly ILikesRepository _likesRepository;

    public LikesController(ILikesRepository likesRepository)
    {
        _likesRepository = likesRepository;
    }

    [HttpPost("{targetUserId:int}")]
    public async Task<ActionResult> ToggleLike(int targetUserId)
    {
        var sourceUserId = User.GetUserId();

        if (sourceUserId == targetUserId)
        {
            return BadRequest("You cannot like yourself.");
        }

        // Check if the like already exists
        var existingLike = await _likesRepository.GetUserLike(sourceUserId, targetUserId);

        if (existingLike == null)
        {
            var like = new UserLike
            {
                SourceUserId = sourceUserId,
                TargetUserId = targetUserId
            };

            _likesRepository.AddLike(like);
        }
        else
        {
            _likesRepository.DeleteLike(existingLike);
        }

        // Save changes
        if (await _likesRepository.SaveChanges())
        {
            return Ok();
        }

        return BadRequest("Failed to like user.");
    }

    [HttpGet("list")]
    public async Task<ActionResult<IEnumerable<int>>> GetCurrentUserLikeIds()
    {
        var sourceUserId = User.GetUserId();

        // Fetch the like IDs
        var likeIds = await _likesRepository.GetCurrentUserLikeIds(sourceUserId);
        return Ok(likeIds);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUserLikes([FromQuery] LikesParams likesParams)
    {
        likesParams.UserId = User.GetUserId();
        Console.WriteLine("AAAAAA");
        Console.WriteLine(JsonSerializer.Serialize(likesParams));
        Console.WriteLine("AAAAAA");

        // Fetch user likes based on the predicate
        var users = await _likesRepository.GetUserLikes(likesParams);

        Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

        return Ok(users);
    }
}