using DwellScript.Web.Data;
using Microsoft.Extensions.Logging;

namespace DwellScript.Web.Services;

public class UsageService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UsageService> _logger;

    public UsageService(AppDbContext db, ILogger<UsageService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> HasQuotaAsync(string userId)
    {
        // TODO: implement quota check per subscription tier
        return await Task.FromResult(true);
    }
}
