using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using DwellScript.Web.Data;
using DwellScript.Web.Models;
using RentalProperty = DwellScript.Web.Models.Property;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Xml.Linq;

namespace DwellScript.Web.Services;

public class GenerationResult
{
    public string LtrOutput { get; set; } = "";
    public string StrOutput { get; set; } = "";
    public string SocialOutput { get; set; } = "";
    public string HeadlinesJson { get; set; } = "[]";
    public bool HasFairHousingViolations { get; set; }
    public List<string> FairHousingViolations { get; set; } = new();
}

public class GenerationService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly FairHousingFilter _fairHousingFilter;
    private readonly ILogger<GenerationService> _logger;

    public GenerationService(
        AppDbContext db,
        IConfiguration config,
        FairHousingFilter fairHousingFilter,
        ILogger<GenerationService> logger)
    {
        _db = db;
        _config = config;
        _fairHousingFilter = fairHousingFilter;
        _logger = logger;
    }

    public async Task<GenerationResult> GenerateFullAsync(RentalProperty property, string? refinementInstruction)
    {
        var template = await GetTemplateAsync("FULL_GENERATION");
        var prompt = BuildFullPrompt(template.PromptText, property, refinementInstruction);
        var rawResponse = await CallClaudeAsync(prompt, template.SystemPrompt);
        var result = ParseFullResponse(rawResponse);

        // Fair Housing scan across all outputs
        var headlines = SafeDeserializeJson(result.HeadlinesJson);
        var combined = $"{result.LtrOutput} {result.StrOutput} {result.SocialOutput} {string.Join(" ", headlines)}";
        var scan = await _fairHousingFilter.ScanAsync(combined);
        result.HasFairHousingViolations = scan.HasViolations;
        result.FairHousingViolations = scan.Violations;

        return result;
    }

    public async Task<string> RegenerateSectionAsync(
        RentalProperty property,
        Generation latest,
        string section,
        string? instruction)
    {
        var template = await GetTemplateAsync("SECTION_REGEN");
        var prompt = BuildSectionPrompt(template.PromptText, property, latest, section, instruction);
        var rawResponse = await CallClaudeAsync(prompt, template.SystemPrompt);
        return ParseSectionResponse(rawResponse, section);
    }

    private async Task<PromptTemplate> GetTemplateAsync(string key)
    {
        var template = await _db.PromptTemplates
            .Where(t => t.Key == key && t.IsActive)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync();

        if (template == null)
            throw new InvalidOperationException($"No active prompt template found for key: {key}");

        return template;
    }

    private string BuildFullPrompt(string templateText, RentalProperty property, string? refinement)
    {
        var amenities = SafeDeserializeJson(property.AmenitiesJson);
        var platforms = SafeDeserializeJson(property.PlatformsJson);

        return templateText
            .Replace("{Address}", property.Address)
            .Replace("{City}", property.City)
            .Replace("{State}", property.State)
            .Replace("{Zip}", property.Zip)
            .Replace("{PropertyType}", property.PropertyType)
            .Replace("{Bedrooms}", property.Bedrooms.ToString())
            .Replace("{Bathrooms}", property.Bathrooms.ToString())
            .Replace("{SquareFootage}", property.SquareFootage?.ToString() ?? "Not specified")
            .Replace("{MonthlyRent}", property.MonthlyRent?.ToString("F0") ?? "Not specified")
            .Replace("{PetPolicy}", property.PetPolicy)
            .Replace("{Parking}", property.Parking)
            .Replace("{Amenities}", amenities.Count > 0 ? string.Join(", ", amenities) : "None specified")
            .Replace("{Platforms}", platforms.Count > 0 ? string.Join(", ", platforms).ToUpper() : "LTR, STR, Social")
            .Replace("{Notes}", property.Notes ?? "None")
            .Replace("{RefinementInstruction}", string.IsNullOrWhiteSpace(refinement)
                ? ""
                : $"\nAdditional instruction from landlord: {refinement}");
    }

    private string BuildSectionPrompt(
        string templateText,
        RentalProperty property,
        Generation latest,
        string section,
        string? instruction)
    {
        var amenities = SafeDeserializeJson(property.AmenitiesJson);

        var currentCopy = section.ToUpper() switch
        {
            "LTR"       => $"Current LTR copy:\n{latest.LtrOutput}",
            "STR"       => $"Current STR copy:\n{latest.StrOutput}",
            "SOCIAL"    => $"Current Social copy:\n{latest.SocialOutput}",
            "HEADLINES" => $"Current Headlines (JSON):\n{latest.HeadlinesJson}",
            _           => ""
        };

        return templateText
            .Replace("{Section}", section.ToUpper())
            .Replace("{Address}", property.Address)
            .Replace("{City}", property.City)
            .Replace("{State}", property.State)
            .Replace("{PropertyType}", property.PropertyType)
            .Replace("{Bedrooms}", property.Bedrooms.ToString())
            .Replace("{Bathrooms}", property.Bathrooms.ToString())
            .Replace("{MonthlyRent}", property.MonthlyRent?.ToString("F0") ?? "Not specified")
            .Replace("{Amenities}", amenities.Count > 0 ? string.Join(", ", amenities) : "None specified")
            .Replace("{CurrentCopy}", currentCopy)
            .Replace("{RefinementInstruction}", string.IsNullOrWhiteSpace(instruction)
                ? ""
                : $"\nAdditional instruction: {instruction}");
    }

    private async Task<string> CallClaudeAsync(string prompt, string? systemPrompt)
    {
        var apiKey = _config["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        var client = new AnthropicClient(apiKey);

        var messages = new List<Message>
        {
            new(RoleType.User, prompt)
        };

        var parameters = new MessageParameters
        {
            Model = "claude-sonnet-4-5",
            MaxTokens = 4096,
            Messages = messages,
            SystemMessage = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt
        };

        _logger.LogInformation("Calling Claude API, prompt length: {Length}", prompt.Length);
        var response = await client.Messages.GetClaudeMessageAsync(parameters);
        var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "";
        _logger.LogInformation("Claude API response received, length: {Length}", text.Length);

        return text;
    }

    private GenerationResult ParseFullResponse(string rawResponse)
    {
        var result = new GenerationResult();
        try
        {
            var startIdx = rawResponse.IndexOf("<outputs>", StringComparison.OrdinalIgnoreCase);
            var endIdx = rawResponse.IndexOf("</outputs>", StringComparison.OrdinalIgnoreCase);
            if (startIdx < 0 || endIdx < 0)
                throw new InvalidDataException("Could not find <outputs> block in Claude response.");

            var xmlStr = rawResponse[startIdx..(endIdx + "</outputs>".Length)];
            var doc = XDocument.Parse(xmlStr);

            result.LtrOutput    = doc.Root?.Element("ltr")?.Value.Trim()    ?? "";
            result.StrOutput    = doc.Root?.Element("str")?.Value.Trim()    ?? "";
            result.SocialOutput = doc.Root?.Element("social")?.Value.Trim() ?? "";

            var headlines = doc.Root?.Element("headlines")?
                .Elements("headline")
                .Select(h => h.Value.Trim())
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .ToList() ?? new List<string>();

            result.HeadlinesJson = JsonSerializer.Serialize(headlines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude full generation response. Raw: {Raw}",
                rawResponse[..Math.Min(500, rawResponse.Length)]);
            throw new InvalidOperationException("Failed to parse AI response. Please try again.", ex);
        }

        return result;
    }

    private string ParseSectionResponse(string rawResponse, string section)
    {
        var tag = section.ToLower() switch
        {
            "ltr"       => "ltr",
            "str"       => "str",
            "social"    => "social",
            "headlines" => "headlines",
            _           => section.ToLower()
        };

        try
        {
            var startTag = $"<{tag}>";
            var endTag   = $"</{tag}>";
            var start = rawResponse.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
            var end   = rawResponse.IndexOf(endTag,   StringComparison.OrdinalIgnoreCase);

            if (start < 0 || end < 0)
            {
                _logger.LogWarning("Could not find <{Tag}> in section regen response. Returning raw.", tag);
                return rawResponse.Trim();
            }

            var inner = rawResponse[(start + startTag.Length)..end].Trim();

            if (tag == "headlines")
            {
                // Re-parse inner <headline> elements and serialize as JSON array
                var xmlStr = $"<headlines>{inner}</headlines>";
                var doc = XDocument.Parse(xmlStr);
                var heads = doc.Root?
                    .Elements("headline")
                    .Select(h => h.Value.Trim())
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .ToList() ?? new List<string>();
                return JsonSerializer.Serialize(heads);
            }

            return inner;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse section regen response for {Section}", section);
            throw new InvalidOperationException("Failed to parse AI response. Please try again.", ex);
        }
    }

    private static List<string> SafeDeserializeJson(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }
}
