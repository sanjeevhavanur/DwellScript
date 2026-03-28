using DwellScript.Web.Data;
using DwellScript.Web.Models;
using DwellScript.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DwellScript.Web.Controllers.Api;

[ApiController]
[Route("api/generation")]
[Authorize]
public class GenerationApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GenerationService _generationService;
    private readonly UsageService _usageService;
    private readonly SubscriptionService _subscriptionService;
    private readonly ILogger<GenerationApiController> _logger;

    public GenerationApiController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        GenerationService generationService,
        UsageService usageService,
        SubscriptionService subscriptionService,
        ILogger<GenerationApiController> logger)
    {
        _db = db;
        _userManager = userManager;
        _generationService = generationService;
        _usageService = usageService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    // POST /api/generation/generate
    [HttpPost("generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate([FromBody] GenerateDto dto)
    {
        var userId = _userManager.GetUserId(User)!;

        var prop = await _db.Properties
            .FirstOrDefaultAsync(p => p.Id == dto.PropertyId && p.UserId == userId);
        if (prop == null)
            return NotFound(new { message = "Property not found." });

        if (!await _usageService.HasQuotaAsync(userId))
            return StatusCode(402, new { message = "You've reached your monthly generation limit. Upgrade to Starter for unlimited generations." });

        try
        {
            var result = await _generationService.GenerateFullAsync(prop, dto.RefinementInstruction);

            var gen = new Generation
            {
                PropertyId            = dto.PropertyId,
                UserId                = userId,
                Type                  = GenerationType.Full,
                RefinementInstruction = dto.RefinementInstruction,
                LtrOutput             = result.LtrOutput,
                StrOutput             = result.StrOutput,
                SocialOutput          = result.SocialOutput,
                HeadlinesJson         = result.HeadlinesJson,
                UsageUnitsConsumed    = 1.0m
            };

            _db.Generations.Add(gen);
            prop.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Full generation saved, Id: {Id}, FairHousingViolations: {Violations}",
                gen.Id, result.HasFairHousingViolations);

            return Ok(new
            {
                gen.Id,
                gen.PropertyId,
                gen.Type,
                gen.LtrOutput,
                gen.StrOutput,
                gen.SocialOutput,
                gen.HeadlinesJson,
                gen.CreatedAt,
                gen.UsageUnitsConsumed,
                fairHousingViolations = result.HasFairHousingViolations ? result.FairHousingViolations : null
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            _logger.LogError(ex, "Anthropic API key not configured");
            return StatusCode(503, new { message = "AI service is not configured. Please contact support." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generation failed for property {PropertyId}", dto.PropertyId);
            return StatusCode(500, new { message = "Generation failed. Please try again." });
        }
    }

    // POST /api/generation/regen-section
    [HttpPost("regen-section")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenSection([FromBody] RegenDto dto)
    {
        var userId = _userManager.GetUserId(User)!;

        var prop = await _db.Properties
            .FirstOrDefaultAsync(p => p.Id == dto.PropertyId && p.UserId == userId);
        if (prop == null)
            return NotFound(new { message = "Property not found." });

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (!_subscriptionService.HasAccess(user, Feature.SectionRefinement))
            return StatusCode(402, new { message = "Section regeneration requires a Starter or Pro subscription." });

        var latest = await _db.Generations
            .Where(g => g.PropertyId == dto.PropertyId && g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync();

        if (latest == null)
            return BadRequest(new { message = "No existing generation found. Generate all sections first." });

        if (string.IsNullOrWhiteSpace(dto.Section))
            return BadRequest(new { message = "Section is required." });

        try
        {
            var newSectionContent = await _generationService.RegenerateSectionAsync(
                prop, latest, dto.Section, dto.Instruction);

            // Clone latest generation, overwrite only the requested section
            var gen = new Generation
            {
                PropertyId            = dto.PropertyId,
                UserId                = userId,
                Type                  = GenerationType.SectionRegen,
                RefinementInstruction = dto.Instruction,
                RegeneratedSection    = dto.Section,
                LtrOutput             = latest.LtrOutput,
                StrOutput             = latest.StrOutput,
                SocialOutput          = latest.SocialOutput,
                HeadlinesJson         = latest.HeadlinesJson,
                UsageUnitsConsumed    = 0.25m
            };

            switch (dto.Section.ToUpper())
            {
                case "LTR":       gen.LtrOutput     = newSectionContent; break;
                case "STR":       gen.StrOutput     = newSectionContent; break;
                case "SOCIAL":    gen.SocialOutput  = newSectionContent; break;
                case "HEADLINES": gen.HeadlinesJson = newSectionContent; break;
            }

            _db.Generations.Add(gen);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                gen.Id,
                gen.PropertyId,
                gen.Type,
                gen.RegeneratedSection,
                gen.LtrOutput,
                gen.StrOutput,
                gen.SocialOutput,
                gen.HeadlinesJson,
                gen.CreatedAt,
                gen.UsageUnitsConsumed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Section regen failed for property {PropertyId}, section {Section}",
                dto.PropertyId, dto.Section);
            return StatusCode(500, new { message = "Regeneration failed. Please try again." });
        }
    }

    // GET /api/generation/latest/{propertyId}
    [HttpGet("latest/{propertyId:int}")]
    public async Task<IActionResult> GetLatest(int propertyId)
    {
        var userId = _userManager.GetUserId(User)!;
        var gen = await _db.Generations
            .Where(g => g.PropertyId == propertyId && g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync();

        if (gen == null) return NoContent();

        return Ok(new
        {
            gen.Id,
            gen.PropertyId,
            gen.Type,
            gen.LtrOutput,
            gen.StrOutput,
            gen.SocialOutput,
            gen.HeadlinesJson,
            gen.CreatedAt,
            gen.UsageUnitsConsumed
        });
    }

    // GET /api/generation/history/{propertyId}
    [HttpGet("history/{propertyId:int}")]
    public async Task<IActionResult> GetHistory(int propertyId)
    {
        var userId = _userManager.GetUserId(User)!;

        var user = await _userManager.GetUserAsync(User);
        if (user != null && !_subscriptionService.HasAccess(user, Feature.GenerationHistory))
            return StatusCode(402, new { message = "Generation history requires a Starter or Pro subscription." });

        var gens = await _db.Generations
            .Where(g => g.PropertyId == propertyId && g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new
            {
                g.Id,
                g.Type,
                g.CreatedAt,
                g.RegeneratedSection,
                g.UsageUnitsConsumed
            })
            .ToListAsync();

        return Ok(gens);
    }

    // GET /api/generation/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var gen = await _db.Generations
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (gen == null) return NotFound();

        return Ok(new
        {
            gen.Id,
            gen.PropertyId,
            gen.Type,
            gen.LtrOutput,
            gen.StrOutput,
            gen.SocialOutput,
            gen.HeadlinesJson,
            gen.CreatedAt,
            gen.RegeneratedSection,
            gen.UsageUnitsConsumed
        });
    }

    // DELETE /api/generation/{id}
    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var gen = await _db.Generations
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (gen == null) return NotFound();

        _db.Generations.Remove(gen);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}

public record GenerateDto(int PropertyId, string? RefinementInstruction);
public record RegenDto(int PropertyId, string? Section, string? Instruction);
