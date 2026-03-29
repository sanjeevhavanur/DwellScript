using DwellScript.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DwellScript.Web.Filters;

/// <summary>
/// Populates ViewData with the current user's tier and quota
/// so the shared layout can display them without each controller setting them manually.
/// </summary>
public class UserContextFilter : IAsyncActionFilter
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserContextFilter(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Controller controller && context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(context.HttpContext.User);
            if (user != null)
            {
                controller.ViewData["Tier"] = user.Tier.ToString();
            }
        }

        await next();
    }
}
