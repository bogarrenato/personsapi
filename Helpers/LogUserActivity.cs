using System.Text.Json;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Helpers;

//Last active dolog kezelesere
public class LogUserActivity : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {

        var resultContext = await next();
        if (resultContext.HttpContext.User.Identity?.IsAuthenticated != true)
        {

            return;
        }


        var userId = resultContext.HttpContext.User.GetUserId();

        var unitOfWork = resultContext.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();

        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);

        if (user == null)
        {
            return;
        }

        user.LastActive = DateTime.UtcNow;

        Console.WriteLine(JsonSerializer.Serialize(user.LastActive.ToString()));

        await unitOfWork.Complete();
    }
}
