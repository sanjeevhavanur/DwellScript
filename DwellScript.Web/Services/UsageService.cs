using DwellScript.Web.Data;
using DwellScript.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DwellScript.Web.Services;

public class UsageService
{
    private const int FreeMonthlyLimit = 3;

    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UsageService> _logger;

    public UsageService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<UsageService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> HasQuotaAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        // Starter and Pro have unlimited generations
        if (user.Tier >= SubscriptionTier.Starter)
            return true;

        // Free tier: 3 full generations per calendar month
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = await _db.Generations
            .Where(g => g.UserId == userId
                     && g.Type == GenerationType.Full
                     && g.CreatedAt >= startOfMonth)
            .CountAsync();

        var hasQuota = count < FreeMonthlyLimit;
        if (!hasQuota)
            _logger.LogInformation("User {UserId} has exhausted free tier quota ({Count}/{Limit})",
                userId, count, FreeMonthlyLimit);

        return hasQuota;
    }

    public async Task<(int Used, int Limit)> GetMonthlyUsageAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (0, 0);

        if (user.Tier >= SubscriptionTier.Starter)
        {
            var used = await _db.Generations
                .Where(g => g.UserId == userId && g.Type == GenerationType.Full
                         && g.CreatedAt >= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc))
                .CountAsync();
            return (used, -1); // -1 = unlimited
        }

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var freeUsed = await _db.Generations
            .Where(g => g.UserId == userId && g.Type == GenerationType.Full && g.CreatedAt >= startOfMonth)
            .CountAsync();

        return (freeUsed, FreeMonthlyLimit);
    }
}
