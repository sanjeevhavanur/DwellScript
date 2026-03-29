using DwellScript.Web.Data;
using DwellScript.Web.Models;
using DwellScript.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using StripeEventTypes = Stripe.EventTypes;
using StripeSubscriptionService = Stripe.SubscriptionService;

namespace DwellScript.Web.Controllers.Api;

[ApiController]
[Route("api/billing")]
[Authorize]
public class BillingApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly UsageService _usageService;
    private readonly IResendEmailService _email;
    private readonly ILogger<BillingApiController> _logger;

    public BillingApiController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        UsageService usageService,
        IResendEmailService email,
        ILogger<BillingApiController> logger)
    {
        _db = db;
        _userManager = userManager;
        _config = config;
        _usageService = usageService;
        _email = email;
        _logger = logger;

        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    // GET /api/billing/status
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var propCount = await _db.Properties.CountAsync(p => p.UserId == userId && !p.IsArchived);
        var (genUsed, genMax) = await _usageService.GetMonthlyUsageAsync(userId);

        int? propMax = user.Tier == SubscriptionTier.Free ? 1
                     : user.Tier == SubscriptionTier.Starter ? 10
                     : null;

        DateTime? nextBillingDate = null;
        if (!string.IsNullOrWhiteSpace(user.StripeSubscriptionId))
        {
            try
            {
                var subService = new StripeSubscriptionService();
                var sub = await subService.GetAsync(user.StripeSubscriptionId);
                nextBillingDate = sub.BillingCycleAnchor;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch Stripe subscription for user {UserId}", userId);
            }
        }

        return Ok(new
        {
            tier             = user.Tier.ToString(),
            generationsUsed  = genUsed,
            generationsMax   = genMax == -1 ? (int?)null : genMax,
            propertyCount    = propCount,
            propertyMax      = propMax,
            nextBillingDate  = nextBillingDate,
            gracePeriodEndsAt = user.GracePeriodEndsAt
        });
    }

    // POST /api/billing/checkout
    [HttpPost("checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var priceId = (dto.Tier.ToLower(), dto.Annual) switch
        {
            ("starter", false) => _config["Stripe:PriceStarterMonthly"],
            ("starter", true)  => _config["Stripe:PriceStarterAnnual"],
            ("pro",     false) => _config["Stripe:PriceProMonthly"],
            ("pro",     true)  => _config["Stripe:PriceProAnnual"],
            _                  => null
        };

        if (string.IsNullOrWhiteSpace(priceId))
            return BadRequest(new { message = "Invalid plan selected." });

        var baseUrl = _config["AppBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

        var options = new SessionCreateOptions
        {
            Mode               = "subscription",
            LineItems          = new List<SessionLineItemOptions>
            {
                new() { Price = priceId, Quantity = 1 }
            },
            SuccessUrl         = $"{baseUrl}/Billing?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl          = $"{baseUrl}/Billing",
            ClientReferenceId  = user.Id,
            CustomerEmail      = string.IsNullOrWhiteSpace(user.StripeCustomerId) ? user.Email : null,
            Customer           = string.IsNullOrWhiteSpace(user.StripeCustomerId) ? null : user.StripeCustomerId,
            SubscriptionData   = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string> { ["userId"] = user.Id }
            }
        };

        try
        {
            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return Ok(new { url = session.Url });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe checkout session creation failed for user {UserId}", user.Id);
            return StatusCode(500, new { message = "Could not create checkout session. Please try again." });
        }
    }

    // GET /api/billing/portal
    [HttpGet("portal")]
    public async Task<IActionResult> Portal()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
            return BadRequest(new { message = "No active subscription found." });

        var baseUrl = _config["AppBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

        try
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer  = user.StripeCustomerId,
                ReturnUrl = $"{baseUrl}/Billing"
            };
            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);
            return Ok(new { url = session.Url });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe portal session creation failed for user {UserId}", user.Id);
            return StatusCode(500, new { message = "Could not open billing portal. Please try again." });
        }
    }

    // GET /api/billing/invoices
    [HttpGet("invoices")]
    public async Task<IActionResult> Invoices()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
            return Ok(Array.Empty<object>());

        try
        {
            var service = new InvoiceService();
            var invoices = await service.ListAsync(new InvoiceListOptions
            {
                Customer = user.StripeCustomerId,
                Limit    = 12
            });

            var result = invoices.Data.Select(inv => new
            {
                id          = inv.Id,
                date        = inv.Created,
                description = inv.Lines.Data.FirstOrDefault()?.Description ?? "DwellScript Subscription",
                amount      = inv.AmountPaid / 100.0,
                status      = inv.Status,
                invoiceUrl  = inv.HostedInvoiceUrl
            });

            return Ok(result);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Could not fetch invoices for user {UserId}", user.Id);
            return Ok(Array.Empty<object>());
        }
    }

    // GET /api/billing/invoice/{id}
    [HttpGet("invoice/{id}")]
    public async Task<IActionResult> DownloadInvoice(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        try
        {
            var service = new InvoiceService();
            var invoice = await service.GetAsync(id);

            // Verify invoice belongs to this customer
            if (invoice.CustomerId != user.StripeCustomerId)
                return Forbid();

            if (string.IsNullOrWhiteSpace(invoice.InvoicePdf))
                return NotFound(new { message = "Invoice PDF not available." });

            return Redirect(invoice.InvoicePdf);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Could not fetch invoice {InvoiceId}", id);
            return NotFound(new { message = "Invoice not found." });
        }
    }

    // POST /api/billing/webhook  — Stripe posts here; must bypass auth + CSRF
    [HttpPost("webhook")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook()
    {
        var webhookSecret = _config["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
            return StatusCode(503, "Webhook not configured.");

        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret,
                throwOnApiVersionMismatch: false);

            _logger.LogInformation("Stripe webhook received: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case StripeEventTypes.CheckoutSessionCompleted:
                    await HandleCheckoutCompletedAsync(stripeEvent.Data.Object as Session);
                    break;

                case StripeEventTypes.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdatedAsync(stripeEvent.Data.Object as Subscription);
                    break;

                case StripeEventTypes.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeletedAsync(stripeEvent.Data.Object as Subscription);
                    break;

                case StripeEventTypes.InvoicePaymentFailed:
                    await HandlePaymentFailedAsync(stripeEvent.Data.Object as Invoice);
                    break;

                case StripeEventTypes.InvoicePaymentSucceeded:
                    await HandlePaymentSucceededAsync(stripeEvent.Data.Object as Invoice);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature validation failed");
            return BadRequest("Invalid webhook signature.");
        }
    }

    // ── Webhook handlers ──────────────────────────────────────────────────────

    private async Task HandleCheckoutCompletedAsync(Session? session)
    {
        if (session == null) return;

        var userId = session.ClientReferenceId;
        if (string.IsNullOrWhiteSpace(userId)) return;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        // Fetch the subscription to get the price ID
        var subService = new StripeSubscriptionService();
        var sub = await subService.GetAsync(session.SubscriptionId);

        user.StripeCustomerId      = session.CustomerId;
        user.StripeSubscriptionId  = sub.Id;
        user.Tier                  = Resolvetier(sub);
        user.GracePeriodEndsAt     = null;

        await _userManager.UpdateAsync(user);
        _logger.LogInformation("Checkout completed: user {UserId} upgraded to {Tier}", userId, user.Tier);
    }

    private async Task HandleSubscriptionUpdatedAsync(Subscription? sub)
    {
        if (sub == null) return;

        var user = await FindUserBySubscriptionAsync(sub.Id);
        if (user == null) return;

        user.Tier             = Resolvetier(sub);
        user.GracePeriodEndsAt = null;

        await _userManager.UpdateAsync(user);
        _logger.LogInformation("Subscription updated: user {UserId} is now {Tier}", user.Id, user.Tier);
    }

    private async Task HandleSubscriptionDeletedAsync(Subscription? sub)
    {
        if (sub == null) return;

        var user = await FindUserBySubscriptionAsync(sub.Id);
        if (user == null) return;

        user.Tier                 = SubscriptionTier.Free;
        user.StripeSubscriptionId = null;
        user.GracePeriodEndsAt    = null;

        await _userManager.UpdateAsync(user);
        _logger.LogInformation("Subscription deleted: user {UserId} downgraded to Free", user.Id);
    }

    private async Task HandlePaymentFailedAsync(Invoice? invoice)
    {
        if (invoice == null) return;

        var user = await FindUserByCustomerAsync(invoice.CustomerId);
        if (user == null) return;

        // 3-day grace period before downgrade
        user.GracePeriodEndsAt = DateTime.UtcNow.AddDays(3);
        await _userManager.UpdateAsync(user);

        // Send dunning email
        await _email.SendAsync(
            to:      user.Email!,
            subject: "Action required: Your DwellScript payment failed",
            html:    $"<p>Hi {user.FullName ?? "there"},</p>" +
                     $"<p>We couldn't process your payment for DwellScript. " +
                     $"Please update your payment method to keep your {user.Tier} plan active.</p>" +
                     $"<p><a href=\"{_config["AppBaseUrl"]}/Billing\">Update payment method →</a></p>" +
                     $"<p>Your access will continue for 3 more days while we retry.</p>");

        _logger.LogInformation("Payment failed for user {UserId}, grace period set", user.Id);
    }

    private async Task HandlePaymentSucceededAsync(Invoice? invoice)
    {
        if (invoice == null) return;

        var user = await FindUserByCustomerAsync(invoice.CustomerId);
        if (user == null) return;

        user.GracePeriodEndsAt = null;
        await _userManager.UpdateAsync(user);
        _logger.LogInformation("Payment succeeded for user {UserId}, grace period cleared", user.Id);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private SubscriptionTier Resolvetier(Subscription sub)
    {
        var priceId = sub.Items.Data.FirstOrDefault()?.Price.Id ?? "";

        if (priceId == _config["Stripe:PriceProMonthly"] ||
            priceId == _config["Stripe:PriceProAnnual"])
            return SubscriptionTier.Pro;

        if (priceId == _config["Stripe:PriceStarterMonthly"] ||
            priceId == _config["Stripe:PriceStarterAnnual"])
            return SubscriptionTier.Starter;

        return SubscriptionTier.Free;
    }

    private async Task<ApplicationUser?> FindUserBySubscriptionAsync(string subscriptionId)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.StripeSubscriptionId == subscriptionId);
    }

    private async Task<ApplicationUser?> FindUserByCustomerAsync(string customerId)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
    }
}

public record CheckoutDto(string Tier, bool Annual);
