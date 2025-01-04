using System.Security.Cryptography;
using API.Controllers;
using API.Entities;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using API.DTOs;
using Microsoft.EntityFrameworkCore;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers;

public class AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        // using var hmac = new HMACSHA512();

        if (await UserExists(registerDto.Username))
        {
            return BadRequest("Username is taken");
        }
        //map into an AppUser from RegisterDto
        var user = mapper.Map<AppUser>(registerDto);
        user.UserName = registerDto.Username.ToLower();

        var result = await userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            return BadRequest("Failed to register user");
        }

        return new UserDto
        {
            Username = user.UserName,
            Token = await tokenService.CreateToken(user),
            KnownAs = user.KnownAs,
            Gender = user.Gender
        };
    }
    // Ezt hasznalom


    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await userManager.Users
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(x => x.NormalizedUserName == loginDto.Username.ToUpper());


        if (user == null || user.UserName == null)
        {
            return Unauthorized("Invalid username");
        }

        var result = await userManager.CheckPasswordAsync(user, loginDto.Password);

        if (!result)
        {
            return Unauthorized();
        }


        return new UserDto
        {
            Username = user.UserName,
            Token = await tokenService.CreateToken(user),
            PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
            KnownAs = user.KnownAs,
            Gender = user.Gender
        };
    }

    private async Task<bool> UserExists(string username)
    {
        //String comparsion does not work with EF
        return await userManager.Users.AnyAsync(x => x.NormalizedUserName == username.ToUpper());
    }



    // [Authorize]
    // [HttpGet("current")]
    // public async Task<ActionResult<UserDto>> GetCurrentUser()
    // {
    //     var user = await userManager.Users
    //         .Include(p => p.Photos)
    //         .FirstOrDefaultAsync(x => x.NormalizedUserName == User.Identity.Name.ToUpper());

    //     if (user == null)
    //         return NotFound();

    //     return new UserDto
    //     {
    //         Username = user.UserName,
    //         Token = await tokenService.CreateToken(user),
    //         PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
    //         KnownAs = user.KnownAs,
    //         Gender = user.Gender
    //     };
    // }
}
