using System.Security.Claims;
using System.Text;
using API.Entities;
using API.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;

namespace API.Services;

public class TokenService(IConfiguration config, UserManager<AppUser> userManager) : ITokenService
{
    public async Task<string> CreateToken(AppUser user)
    {
        var tokenKey = config["TokenKey"] ?? throw new Exception("TokenKey not found in appsettings.json");

        if (tokenKey.Length < 64)
        {
            throw new Exception("TokenKey must be at least 64 characters long");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
        Console.WriteLine(user.UserName);

        if (user.UserName == null)
        {
            throw new Exception("User name is null");
        }

        var claims = new List<Claim>{
         new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                  new Claim(ClaimTypes.Name, user.UserName),
        };

        var roles = await userManager.GetRolesAsync(user);

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);


        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);

    }
}
