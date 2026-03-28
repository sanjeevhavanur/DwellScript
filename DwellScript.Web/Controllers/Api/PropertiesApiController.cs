using DwellScript.Web.Data;
using DwellScript.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DwellScript.Web.Controllers.Api;

[ApiController]
[Route("api/properties")]
[Authorize]
public class PropertiesApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PropertiesApiController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = _userManager.GetUserId(User)!;
        var props = await _db.Properties
            .Where(p => p.UserId == userId && !p.IsArchived)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new {
                p.Id, p.Address, p.City, p.State, p.Zip,
                p.PropertyType, p.Status, p.Bedrooms, p.Bathrooms,
                p.SquareFootage, p.MonthlyRent, p.PlatformsJson,
                p.AmenitiesJson, p.IsArchived,
                GenerationCount = p.Generations.Count
            })
            .ToListAsync();
        return Ok(props);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var p = await _db.Properties.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] PropertyUpsertDto dto)
    {
        var userId = _userManager.GetUserId(User)!;
        var prop = new Property
        {
            UserId       = userId,
            Address      = dto.Address,
            City         = dto.City,
            State        = dto.State,
            Zip          = dto.Zip,
            PropertyType = dto.PropertyType,
            Status       = (PropertyStatus)dto.Status,
            Bedrooms     = dto.Bedrooms,
            Bathrooms    = dto.Bathrooms,
            SquareFootage= dto.SquareFootage,
            MonthlyRent  = dto.MonthlyRent,
            PetPolicy    = dto.PetPolicy ?? string.Empty,
            Parking      = dto.Parking ?? string.Empty,
            Notes        = dto.Notes,
            AmenitiesJson= dto.AmenitiesJson ?? "[]",
            PlatformsJson= dto.PlatformsJson ?? "[]"
        };
        _db.Properties.Add(prop);
        await _db.SaveChangesAsync();
        return Ok(new { prop.Id });
    }

    [HttpPut("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [FromBody] PropertyUpsertDto dto)
    {
        var userId = _userManager.GetUserId(User)!;
        var prop = await _db.Properties.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (prop == null) return NotFound();

        prop.Address      = dto.Address;
        prop.City         = dto.City;
        prop.State        = dto.State;
        prop.Zip          = dto.Zip;
        prop.PropertyType = dto.PropertyType;
        prop.Status       = (PropertyStatus)dto.Status;
        prop.Bedrooms     = dto.Bedrooms;
        prop.Bathrooms    = dto.Bathrooms;
        prop.SquareFootage= dto.SquareFootage;
        prop.MonthlyRent  = dto.MonthlyRent;
        prop.PetPolicy    = dto.PetPolicy ?? string.Empty;
        prop.Parking      = dto.Parking ?? string.Empty;
        prop.Notes        = dto.Notes;
        prop.AmenitiesJson= dto.AmenitiesJson ?? "[]";
        prop.PlatformsJson= dto.PlatformsJson ?? "[]";
        prop.UpdatedAt    = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { prop.Id });
    }

    [HttpPost("{id:int}/archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var prop = await _db.Properties.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (prop == null) return NotFound();
        prop.IsArchived = true;
        prop.Status = PropertyStatus.Archived;
        prop.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok();
    }
}

public record PropertyUpsertDto(
    string Address, string City, string State, string Zip,
    string PropertyType, int Status, int Bedrooms, decimal Bathrooms,
    int? SquareFootage, decimal? MonthlyRent,
    string? PetPolicy, string? Parking, string? Notes,
    string? AmenitiesJson, string? PlatformsJson
);
