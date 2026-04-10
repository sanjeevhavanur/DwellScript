using DwellScript.Web.Data;
using DwellScript.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DwellScript.Web.Controllers;

/// <summary>
/// Handles terms-of-service acceptance for authenticated users who have not yet agreed.
/// </summary>
[Authorize]
public class TermsController : Controller
{
    private const string CurrentTermsVersion = "1.0";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public TermsController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    // GET /Terms/Accept
    public IActionResult Accept(string? returnUrl = null)
    {
        ViewData["HideNav"] = true;
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST /Terms/Accept
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept([FromForm] bool agreed, [FromForm] string? returnUrl = null)
    {
        if (!agreed)
        {
            ViewData["HideNav"] = true;
            ViewData["ReturnUrl"] = returnUrl;
            ModelState.AddModelError(string.Empty, "You must agree to the Terms of Service to continue.");
            return View();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Auth");

        user.TermsAcceptedAt = DateTime.UtcNow;
        user.TermsVersion = CurrentTermsVersion;
        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Property");
    }
}
