using DwellScript.Web.Models;
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
    private readonly IWebHostEnvironment _env;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(
        AuthService authService,
        ILogger<AuthController> logger,
        IWebHostEnvironment env,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _authService = authService;
        _logger = logger;
        _env = env;
        _userManager = userManager;
        _signInManager = signInManager;
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
    public async Task<IActionResult> SendMagicLink([FromForm] string email, [FromForm] string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest(new { message = "Please enter a valid email address." });

        try
        {
            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            await _authService.SendMagicLinkAsync(email.Trim().ToLower(), safeReturnUrl);
            return Ok(new { message = "Check your inbox — we sent you a login link!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending magic link to {Email}", email);
            return StatusCode(500, new { message = "Something went wrong. Please try again." });
        }
    }

    // GET /Auth/VerifyMagicLink?token=...&email=...&returnUrl=...
    [AllowAnonymous]
    public async Task<IActionResult> VerifyMagicLink(string token, string email, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Login), new { error = "invalid" });

        var success = await _authService.VerifyMagicLinkAsync(email, token);
        if (!success)
            return RedirectToAction(nameof(Login), new { error = "expired" });

        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

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

        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

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

    // ── Dev-only test helpers (E2E) ──────────────────────────────────────

    // POST /Auth/TestLogin  —  signs in any email without magic link (dev only)
    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TestLogin([FromForm] string email)
    {
        if (!_env.IsDevelopment())
            return Forbid();

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        return Ok(new { message = "Signed in.", userId = user.Id });
    }

    // POST /Auth/TestSetTier  —  sets the subscription tier for a user (dev only)
    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TestSetTier([FromForm] string email, [FromForm] string tier)
    {
        if (!_env.IsDevelopment())
            return Forbid();

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound(new { message = "User not found." });

        user.Tier = tier switch
        {
            "Starter" => SubscriptionTier.Starter,
            "Pro"     => SubscriptionTier.Pro,
            _         => SubscriptionTier.Free
        };

        await _userManager.UpdateAsync(user);
        return Ok(new { message = $"Tier set to {user.Tier}." });
    }
}
