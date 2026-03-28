using DwellScript.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DwellScript.Web.Controllers;

public class AuthController : Controller
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // GET /Auth/Login
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Property");

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["HideNav"] = true;
        return View();
    }

    // POST /Auth/SendMagicLink  (jQuery Ajax)
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMagicLink([FromForm] string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest(new { message = "Please enter a valid email address." });

        try
        {
            await _authService.SendMagicLinkAsync(email.Trim().ToLower());
            return Ok(new { message = "Check your inbox — we sent you a login link!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending magic link to {Email}", email);
            return StatusCode(500, new { message = "Something went wrong. Please try again." });
        }
    }

    // GET /Auth/VerifyMagicLink?token=...&email=...
    [AllowAnonymous]
    public async Task<IActionResult> VerifyMagicLink(string token, string email)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Login), new { error = "invalid" });

        var success = await _authService.VerifyMagicLinkAsync(email, token);
        if (!success)
            return RedirectToAction(nameof(Login), new { error = "expired" });

        return RedirectToAction("Index", "Property");
    }

    // GET /Auth/GoogleLogin
    [AllowAnonymous]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
        var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };
        return Challenge(properties, "Google");
    }

    // GET /Auth/GoogleCallback  (handled automatically by middleware, but we need the action for redirect target)
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            _logger.LogWarning("Google OAuth error: {Error}", remoteError);
            return RedirectToAction(nameof(Login), new { error = "google" });
        }

        var info = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (info?.Principal == null)
            return RedirectToAction(nameof(Login), new { error = "google" });

        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name  = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Login), new { error = "google" });

        var userManager   = HttpContext.RequestServices.GetRequiredService<UserManager<Models.ApplicationUser>>();
        var signInManager = HttpContext.RequestServices.GetRequiredService<SignInManager<Models.ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new Models.ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = name,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user);
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        return RedirectToAction("Index", "Property");
    }

    // POST /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
