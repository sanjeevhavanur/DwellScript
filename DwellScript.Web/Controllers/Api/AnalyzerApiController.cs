using DwellScript.Web.Data;
using DwellScript.Web.Models;
using DwellScript.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DwellScript.Web.Controllers.Api;

[ApiController]
[Route("api/analyzer")]
[Authorize]
public class AnalyzerApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly VacancyAnalyzerService _analyzerService;
    private readonly SubscriptionService _subscriptionService;
    private readonly ILogger<AnalyzerApiController> _logger;

    public AnalyzerApiController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        VacancyAnalyzerService analyzerService,
        SubscriptionService subscriptionService,
        ILogger<AnalyzerApiController> logger)
    {
        _db = db;
        _userManager = userManager;
        _analyzerService = analyzerService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    // POST /api/analyzer/analyze
    [HttpPost("analyze")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (!_subscriptionService.HasAccess(user, Feature.VacancyAnalyzer))
            return StatusCode(403, new { message = "Vacancy Analyzer requires a Pro subscription." });

        var prop = await _db.Properties
            .FirstOrDefaultAsync(p => p.Id == dto.PropertyId && p.UserId == user.Id);
        if (prop == null)
            return NotFound(new { message = "Property not found." });

        // Get latest generation for listing copy context
        var latestGen = await _db.Generations
            .Where(g => g.PropertyId == dto.PropertyId && g.UserId == user.Id)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync();

        try
        {
            var result = await _analyzerService.AnalyzeAsync(prop, latestGen, dto.DaysOnMarket, dto.Context);

            _logger.LogInformation("Vacancy analysis completed for property {PropertyId}, score: {Score}",
                dto.PropertyId, result.Score);

            return Ok(new
            {
                score    = result.Score,
                insights = result.Insights.Select(i => new
                {
                    title    = i.Title,
                    detail   = i.Detail,
                    severity = i.Severity
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vacancy analysis failed for property {PropertyId}", dto.PropertyId);
            return StatusCode(500, new { message = "Analysis failed. Please try again." });
        }
    }
}

public record AnalyzeDto(int PropertyId, int DaysOnMarket, string? Context);
