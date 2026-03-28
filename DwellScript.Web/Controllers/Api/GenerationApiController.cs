using DwellScript.Web.Data;
using DwellScript.Web.Models;
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

    public GenerationApiController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // POST /api/generation/generate
    [HttpPost("generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate([FromBody] GenerateDto dto)
    {
        var userId = _userManager.GetUserId(User)!;
        var prop = await _db.Properties.FirstOrDefaultAsync(p => p.Id == dto.PropertyId && p.UserId == userId);
        if (prop == null) return NotFound(new { message = "Property not found." });

        // TODO: call GenerationService — stub returns placeholder for now
        var gen = new Generation
        {
            PropertyId          = dto.PropertyId,
            UserId              = userId,
            Type                = GenerationType.Full,
            RefinementInstruction = dto.RefinementInstruction,
            LtrOutput           = "[LTR copy will be generated here]",
            StrOutput           = "[STR copy will be generated here]",
            SocialOutput        = "[Social caption will be generated here]",
            HeadlinesJson       = "[\"Headline 1\",\"Headline 2\",\"Headline 3\"]",
            UsageUnitsConsumed  = 1.0m
        };
        _db.Generations.Add(gen);
        prop.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(gen);
    }

    // POST /api/generation/regen-section
    [HttpPost("regen-section")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenSection([FromBody] RegenDto dto)
    {
        var userId = _userManager.GetUserId(User)!;
        var prop = await _db.Properties.FirstOrDefaultAsync(p => p.Id == dto.PropertyId && p.UserId == userId);
        if (prop == null) return NotFound(new { message = "Property not found." });

        var latest = await _db.Generations
            .Where(g => g.PropertyId == dto.PropertyId)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync();

        // Clone latest, overwrite the requested section
        var gen = new Generation
        {
            PropertyId            = dto.PropertyId,
            UserId                = userId,
            Type                  = GenerationType.SectionRegen,
            RefinementInstruction = dto.Instruction,
            RegeneratedSection    = dto.Section,
            LtrOutput             = latest?.LtrOutput,
            StrOutput             = latest?.StrOutput,
            SocialOutput          = latest?.SocialOutput,
            HeadlinesJson         = latest?.HeadlinesJson,
            UsageUnitsConsumed    = 0.25m
        };

        // TODO: replace just the requested section via GenerationService
        switch (dto.Section?.ToUpper())
        {
            case "LTR":      gen.LtrOutput     = "[Regenerated LTR copy]"; break;
            case "STR":      gen.StrOutput     = "[Regenerated STR copy]"; break;
            case "SOCIAL":   gen.SocialOutput  = "[Regenerated social caption]"; break;
            case "HEADLINES":gen.HeadlinesJson = "[\"New Headline 1\",\"New Headline 2\",\"New Headline 3\"]"; break;
        }

        _db.Generations.Add(gen);
        await _db.SaveChangesAsync();
        return Ok(gen);
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
        return Ok(gen);
    }

    // GET /api/generation/history/{propertyId}
    [HttpGet("history/{propertyId:int}")]
    public async Task<IActionResult> GetHistory(int propertyId)
    {
        var userId = _userManager.GetUserId(User)!;
        var gens = await _db.Generations
            .Where(g => g.PropertyId == propertyId && g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new { g.Id, g.Type, g.CreatedAt, g.RegeneratedSection, g.UsageUnitsConsumed })
            .ToListAsync();
        return Ok(gens);
    }

    // GET /api/generation/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var gen = await _db.Generations.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (gen == null) return NotFound();
        return Ok(gen);
    }

    // DELETE /api/generation/{id}
    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var gen = await _db.Generations.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (gen == null) return NotFound();
        _db.Generations.Remove(gen);
        await _db.SaveChangesAsync();
        return Ok();
    }
}

public record GenerateDto(int PropertyId, string? RefinementInstruction);
public record RegenDto(int PropertyId, string? Section, string? Instruction);
