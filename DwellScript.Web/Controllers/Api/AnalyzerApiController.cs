using DwellScript.Web.Data;
using DwellScript.Web.Models;
using DwellScript.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

        var latestGen = await _db.Generations
            .Where(g => g.PropertyId == dto.PropertyId && g.UserId == user.Id)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync();

        try
        {
            var result = await _analyzerService.AnalyzeAsync(prop, latestGen, dto.DaysOnMarket, dto.Context);

            // Persist the analysis
            var analysis = new VacancyAnalysis
            {
                PropertyId    = dto.PropertyId,
                UserId        = user.Id,
                DaysOnMarket  = dto.DaysOnMarket,
                Context       = dto.Context,
                Score         = result.Score,
                InsightsJson  = JsonSerializer.Serialize(result.Insights)
            };
            _db.VacancyAnalyses.Add(analysis);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Vacancy analysis saved, Id: {Id}, score: {Score}", analysis.Id, result.Score);

            return Ok(new
            {
                id       = analysis.Id,
                score    = result.Score,
                createdAt = analysis.CreatedAt,
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

    // GET /api/analyzer/history/{propertyId}
    [HttpGet("history/{propertyId:int}")]
    public async Task<IActionResult> History(int propertyId)
    {
        var userId = _userManager.GetUserId(User)!;

        var prop = await _db.Properties
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.UserId == userId);
        if (prop == null) return NotFound(new { message = "Property not found." });

        var history = await _db.VacancyAnalyses
            .Where(a => a.PropertyId == propertyId && a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Score,
                a.DaysOnMarket,
                a.Context,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(history);
    }

    // GET /api/analyzer/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var analysis = await _db.VacancyAnalyses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (analysis == null) return NotFound();

        var insights = JsonSerializer.Deserialize<List<Services.AnalysisInsight>>(
            analysis.InsightsJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new();

        return Ok(new
        {
            analysis.Id,
            analysis.Score,
            analysis.DaysOnMarket,
            analysis.Context,
            analysis.CreatedAt,
            insights = insights.Select(i => new
            {
                title    = i.Title,
                detail   = i.Detail,
                severity = i.Severity
            })
        });
    }

    // DELETE /api/analyzer/{id}
    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var analysis = await _db.VacancyAnalyses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (analysis == null) return NotFound();

        _db.VacancyAnalyses.Remove(analysis);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}

public record AnalyzeDto(int PropertyId, int DaysOnMarket, string? Context);
