using DwellScript.Web.Data;
using DwellScript.Web.Models;
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

    public AnalyzerApiController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // POST /api/analyzer/analyze
    [HttpPost("analyze")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeDto dto)
    {
        var userId = _userManager.GetUserId(User)!;
        var user   = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        if (user.Tier != SubscriptionTier.Pro)
            return StatusCode(403, new { message = "Vacancy Analyzer requires a Pro subscription." });

        var prop = await _db.Properties.FirstOrDefaultAsync(p => p.Id == dto.PropertyId && p.UserId == userId);
        if (prop == null) return NotFound(new { message = "Property not found." });

        // TODO: call VacancyAnalyzerService
        return Ok(new
        {
            Score = 72,
            Insights = new[]
            {
                new { Title = "Listing copy needs refinement", Detail = "Your current description lacks emotional appeal. Consider highlighting unique features.", Severity = "medium" },
                new { Title = "Competitive pricing", Detail = "Your rent is within 5% of similar units in the area. Pricing looks good.", Severity = "low" },
                new { Title = "Photos may be limiting interest", Detail = "Listings with 10+ photos get 3x more inquiries. Consider adding more.", Severity = "high" }
            }
        });
    }
}

public record AnalyzeDto(int PropertyId, int DaysOnMarket, string? Context);
