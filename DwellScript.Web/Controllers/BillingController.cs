using DwellScript.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using StripeSubscriptionService = Stripe.SubscriptionService;

namespace DwellScript.Web.Controllers;

[Authorize]
public class BillingController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        ILogger<BillingController> logger)
    {
        _userManager = userManager;
        _config = config;
        _logger = logger;
        Stripe.StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    public async Task<IActionResult> Index([FromQuery(Name = "session_id")] string? sessionId)
    {
        ViewData["Title"] = "Billing";

        // When Stripe redirects back with a session_id, apply the upgrade immediately
        // without waiting for the async webhook (which may not have fired yet).
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var session = await new SessionService().GetAsync(sessionId);

                    // Only process if this session belongs to this user
                    if (session.ClientReferenceId == user.Id &&
                        session.PaymentStatus == "paid" &&
                        !string.IsNullOrWhiteSpace(session.SubscriptionId))
                    {
                        var sub = await new StripeSubscriptionService().GetAsync(session.SubscriptionId);

                        var priceId = sub.Items.Data.FirstOrDefault()?.Price.Id ?? "";
                        var newTier = priceId == _config["Stripe:PriceProMonthly"] || priceId == _config["Stripe:PriceProAnnual"]
                            ? SubscriptionTier.Pro
                            : priceId == _config["Stripe:PriceStarterMonthly"] || priceId == _config["Stripe:PriceStarterAnnual"]
                            ? SubscriptionTier.Starter
                            : SubscriptionTier.Free;

                        user.Tier                 = newTier;
                        user.StripeCustomerId     = session.CustomerId;
                        user.StripeSubscriptionId = sub.Id;
                        user.GracePeriodEndsAt    = null;

                        await _userManager.UpdateAsync(user);
                        _logger.LogInformation("Checkout success handler: user {UserId} upgraded to {Tier}", user.Id, newTier);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not process checkout session {SessionId} on return", sessionId);
            }

            // Redirect to clean URL so refreshing doesn't re-process
            return RedirectToAction(nameof(Index));
        }

        return View();
    }
}
