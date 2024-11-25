

using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Authorize]
//Imapper -automapper
public class UsersController(IUserRepository userRepository) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
    {
        var users = await userRepository.GetMembersAsync();

        // var usersToReturn = mapper.Map<IEnumerable<MemberDto>>(users);

        return Ok(users);
    }


    // [HttpGet("{id:int}")]
    // public async Task<ActionResult<MemberDto>> GetUser(int id)
    // {
    //     var user = await userRepository.GetUserByIdAsync(id);

    //     if (user == null)
    //     {
    //         return NotFound();
    //     }

    //     return mapper.Map<MemberDto>(user);
    // }

    [HttpGet("{username}")]
    public async Task<ActionResult<MemberDto>> GetUser(string username)
    {
        var user = await userRepository.GetMemberAsync(username);

        if (user == null)
        {
            return NotFound();
        }

        return user;
    }


}
