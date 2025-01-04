

using System.Security.Claims;
using System.Text.Json;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using personsapi.DTOs;
using StackExchange.Redis;

namespace API.Controllers;

[Authorize]
//Imapper -automapper
public class UsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService, IConnectionMultiplexer redis) : BaseApiController
{
    private readonly IDatabase _database = redis.GetDatabase();
    private const string USER_CACHE_PREFIX = "user:";
    private const string USERS_LIST_CACHE_PREFIX = "users:list:";
       private class CachedPagedList
    {
        public List<MemberDto> Items { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }

  [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery] UserParams userParams)
    {
        userParams.CurrentUsername = User.GetUsername();
        var cacheKey = GetUsersListCacheKey(userParams);

        // Próbáljuk először a cache-ből
        var cachedResult = await _database.StringGetAsync(cacheKey);
        PagedList<MemberDto> users;

        if (!cachedResult.IsNullOrEmpty)
        {
            Console.WriteLine($"Cache hit for users list with parameters: {cacheKey}");
            var cachedUsers = JsonSerializer.Deserialize<CachedPagedList>(cachedResult!);
            users = new PagedList<MemberDto>(
                cachedUsers.Items,
                cachedUsers.TotalCount,
                cachedUsers.CurrentPage,
                cachedUsers.PageSize
            );
        }
        else
        {
            users = await unitOfWork.UserRepository.GetMembersAsync(userParams);

            // Cache-eljük az eredményt
            var cacheData = new CachedPagedList
            {
                Items = users.ToList(),
                TotalCount = users.TotalCount,
                CurrentPage = users.CurrentPage,
                PageSize = users.PageSize
            };

            await _database.StringSetAsync(
                cacheKey,
                JsonSerializer.Serialize(cacheData),
                TimeSpan.FromMinutes(10) // Rövid cache idő a lista esetében
            );
        }

        Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);
        return Ok(users);
    }

    private string GetUserCacheKey(string username) => $"{USER_CACHE_PREFIX}{username}";
    private string GetUsersListCacheKey(UserParams userParams)
    {
        // Konstruáljuk a cache kulcsot az összes releváns paraméterből
        return $"{USERS_LIST_CACHE_PREFIX}" +
               $"page:{userParams.PageNumber}:" +
               $"size:{userParams.PageSize}:" +
               $"gender:{userParams.Gender ?? "all"}:" +
               $"minAge:{userParams.MinAge}:" +
               $"maxAge:{userParams.MaxAge}:" +
               $"orderBy:{userParams.OrderBy ?? "lastActive"}:" +
               $"currentUser:{userParams.CurrentUsername}";
    }

    private async Task InvalidateUsersListCache()
    {
        // Pattern alapú törlés a SCAN használatával
        var pattern = $"{USERS_LIST_CACHE_PREFIX}*";
        var cursor = 0L;
        do
        {
            var scan = await _database.ExecuteAsync("SCAN", cursor.ToString(), "MATCH", pattern, "COUNT", "100");
            var entries = (RedisResult[])scan[1];
            cursor = (long)scan[0];

            if (entries.Length > 0)
            {
                var keys = entries.Select(e => (RedisKey)e.ToString()).ToArray();
                await _database.KeyDeleteAsync(keys);
            }
        }
        while (cursor != 0);
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<MemberDto>> GetUser(string username)
    {
        // Próbáljuk először Redis-ből
        var cachedUser = await _database.StringGetAsync(username);

        if (!cachedUser.IsNullOrEmpty)
        {
            Console.WriteLine("Cache hit");
            return Ok(JsonSerializer.Deserialize<MemberDto>(cachedUser!));
        }

        // Ha nincs Redis-ben, akkor SQL-ből
        var user = await unitOfWork.UserRepository.GetMemberAsync(username);

        if (user == null)
        {
            return NotFound();
        }

        // Cache-eljük az eredményt
        await _database.StringSetAsync(
            username,
            JsonSerializer.Serialize(user),
            TimeSpan.FromMinutes(5)
        );

        return Ok(user);
    }


    [HttpPost("add-photo")]
    public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
    {
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
        if (user == null)
        {
            return BadRequest("User not found");
        }

        var result = await photoService.AddPhotoAsync(file);

        if (result.Error != null)
        {
            return BadRequest(result.Error.Message);
        }

        var photo = new Photo
        {
            Url = result.SecureUrl.AbsoluteUri,
            PublicId = result.PublicId
        };

        user.Photos.Add(photo);
        if (await unitOfWork.Complete())
        {
            return CreatedAtAction(nameof(GetUser), new { username = user.UserName }, mapper.Map<PhotoDto>(photo));
        }

        return BadRequest("Problem adding photo");

    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
    {
        var username = User.GetUsername();
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(username);

        if (user == null)
        {
            return NotFound("User not found");
        }

        mapper.Map(memberUpdateDto, user);
        unitOfWork.UserRepository.Update(user);

        if (await unitOfWork.Complete())
        {
            // Invalidate the cache for this user
            await _database.KeyDeleteAsync(GetUserCacheKey(username));
            return NoContent();
        }

        return BadRequest("Failed to update user");
    }

    [HttpPut("set-main-photo/{photoId}")]
    public async Task<ActionResult> SetMainPhoto(int photoId)
    {
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

        if (user == null)
        {
            return BadRequest("User not found");
        }

        var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

        if (photo == null)
        {
            return NotFound();
        }

        if (photo.IsMain)
        {
            return BadRequest("This is already your main photo");
        }

        var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
        if (currentMain != null)
        {
            currentMain.IsMain = false;
        }

        photo.IsMain = true;

        if (await unitOfWork.Complete())
        {
            return NoContent();
        }

        return BadRequest("Failed to set main photo");
    }

    [HttpDelete("delete-photo/{photoId}")]
    public async Task<ActionResult> DeletePhoto(int photoId)
    {
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

        if (user == null)
        {
            return BadRequest("User not found");
        }

        var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

        if (photo == null)
        {
            return NotFound();
        }

        if (photo.IsMain)
        {
            return BadRequest("You cannot delete your main photo");
        }

        if (photo.PublicId != null)
        {
            var result = await photoService.DeletePhotoAsync(photo.PublicId);
            if (result.Error != null)
            {
                return BadRequest(result.Error.Message);
            }
        }

        user.Photos.Remove(photo);

        if (await unitOfWork.Complete())
        {
            return Ok();
        }

        return BadRequest("Failed to delete the photo");
    }


}
