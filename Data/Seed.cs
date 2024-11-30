using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class Seed
{
    public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        //If there are any elements - dont seed db
        if (await userManager.Users.AnyAsync())
        {
            return;
        }

        //Fetch data from file
        var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        //Convert to users
        var users = JsonSerializer.Deserialize<List<AppUser>>(userData, options);

        if (users == null) { return; }

        var roles = new List<AppRole> {
            new() {Name = "Member"},
            new() {Name = "Admin"},
            new() {Name = "Moderator"}
        };

        foreach (var role in roles)
        {
            await roleManager.CreateAsync(role);
        }

        foreach (var user in users)
        {
            user.UserName = user.UserName!.ToLower();
            //Meg kell feleljen a jelszo igenyeinek
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, "Member");
        }

        var admin = new AppUser
        {
            UserName = "admin",
            KnownAs = "Admin",
            Gender = "",
            City = "",
            Country = ""
        };

        await userManager.CreateAsync(admin, "Pa$$w0rd");
        await userManager.AddToRolesAsync(admin, ["Admin", "Moderator"]);
    }
}
