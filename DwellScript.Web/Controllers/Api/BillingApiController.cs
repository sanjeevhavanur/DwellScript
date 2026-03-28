using DwellScript.Web.Data;
using DwellScript.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DwellScript.Web.Controllers.Api;

[ApiController]
[Route("api/billing")]
[Authorize]
public class BillingApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public BillingApiController(AppDbContext db, UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _db = db;
        _userManager = userManager;
        _config = config;
    }

    // GET /api/billing/status
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var propCount = await _db.Properties.CountAsync(p => p.UserId == userId && !p.IsArchived);
        var genCount  = await _db.Generations.CountAsync(g => g.UserId == userId);

        int? genMax  = user.Tier == SubscriptionTier.Free ? 3 : user.Tier == SubscriptionTier.Starter ? 30 : null;
        int? propMax = user.Tier == SubscriptionTier.Free ? 1 : user.Tier == SubscriptionTier.Starter ? 5 : null;

        return Ok(new
        {
            Tier             = user.Tier.ToString(),
            GenerationsUsed  = genCount,
            GenerationsMax   = genMax,
            PropertyCount    = propCount,
            PropertyMax      = propMax,
            NextBillingDate  = (DateTime?)null   // TODO: pull from Stripe
        });
    }

    // POST /api/billing/checkout
    [HttpPost("checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
    {
        var userId = _userManager.GetUserId(User)!;
        var user   = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        // TODO: create Stripe Checkout Session
        // Placeholder returns an error until Stripe keys are configured
        var secretKey = _config["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
            return StatusCode(503, new { message = "Billing is not configured yet." });

        return Ok(new { url = (string?)null });
    }

    // GET /api/billing/portal
    [HttpGet("portal")]
    public async Task<IActionResult> Portal()
    {
        var userId = _userManager.GetUserId(User)!;
        var user   = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        // TODO: create Stripe Billing Portal session
        return StatusCode(503, new { message = "Billing portal not configured yet." });
    }

    // GET /api/billing/invoices
    [HttpGet("invoices")]
    public IActionResult Invoices()
    {
        // TODO: fetch from Stripe
        return Ok(Array.Empty<object>());
    }

    // GET /api/billing/invoice/{id}
    [HttpGet("invoice/{id}")]
    public IActionResult DownloadInvoice(string id)
    {
        // TODO: redirect to Stripe invoice PDF
        return StatusCode(503, new { message = "Not configured yet." });
    }

    // DELETE /api/billing/subscription
    [HttpDelete("subscription")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel()
    {
        var userId = _userManager.GetUserId(User)!;
        var user   = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        // TODO: cancel via Stripe
        return StatusCode(503, new { message = "Billing not configured yet." });
    }
}

public record CheckoutDto(string Tier, bool Annual);
