using System.Security.Cryptography;
using DwellScript.Web.Data;
using DwellScript.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Resend;

namespace DwellScript.Web.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IResend _resend;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    private const int TokenExpiryMinutes = 15;

    public AuthService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IResend resend,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _resend = resend;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Creates the user if they don't exist, then sends a magic link email.
    /// </summary>
    public async Task SendMagicLinkAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not create user: {errors}");
            }
        }

        // Generate a cryptographically random token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var tokenHash = HashToken(rawToken);

        // Expire any existing unused tokens for this user
        var existing = await _db.MagicLinkTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        _db.MagicLinkTokens.RemoveRange(existing);

        _db.MagicLinkTokens.Add(new MagicLinkToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(TokenExpiryMinutes)
        });
        await _db.SaveChangesAsync();

        var baseUrl = _config["AppBaseUrl"]?.TrimEnd('/');
        var link = $"{baseUrl}/Auth/VerifyMagicLink?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(email)}";
        var fromEmail = _config["Resend:FromEmail"] ?? "noreply@dwellscript.com";

        var message = new EmailMessage
        {
            From = fromEmail,
            Subject = "Your DwellScript login link"
        };
        message.To.Add(email);
        message.HtmlBody = BuildMagicLinkEmail(link);

        await _resend.EmailSendAsync(message);
        _logger.LogInformation("Magic link sent to {Email}", email);
    }

    /// <summary>
    /// Verifies the magic link token and signs the user in.
    /// Returns true on success.
    /// </summary>
    public async Task<bool> VerifyMagicLinkAsync(string email, string rawToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;

        var tokenHash = HashToken(rawToken);

        var record = await _db.MagicLinkTokens
            .Where(t => t.UserId == user.Id
                     && t.TokenHash == tokenHash
                     && t.UsedAt == null
                     && t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (record == null)
        {
            _logger.LogWarning("Invalid or expired magic link for {Email}", email);
            return false;
        }

        record.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: true);
        _logger.LogInformation("User {Email} signed in via magic link", email);
        return true;
    }

    public async Task SignOutAsync() => await _signInManager.SignOutAsync();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string HashToken(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string BuildMagicLinkEmail(string link) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Inter,sans-serif;background:#f8f9fa;padding:40px 0;">
          <div style="max-width:480px;margin:0 auto;background:#fff;border-radius:8px;padding:40px;border:1px solid #dee2e6;">
            <h2 style="margin-top:0;color:#0d6efd;">DwellScript</h2>
            <p style="font-size:16px;color:#212529;">Click the button below to sign in. This link expires in 15 minutes.</p>
            <a href="{link}" style="display:inline-block;background:#0d6efd;color:#fff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:600;font-size:15px;margin:16px 0;">
              Sign in to DwellScript
            </a>
            <p style="font-size:13px;color:#6c757d;margin-bottom:0;">
              If you didn't request this email, you can safely ignore it.
            </p>
          </div>
        </body>
        </html>
        """;
}
