using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using DwellScript.Web.Data;
using DwellScript.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using RentalProperty = DwellScript.Web.Models.Property;

namespace DwellScript.Web.Services;

public class AnalysisInsight
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = "";

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "medium"; // high | medium | low

    [JsonPropertyName("suggestedFix")]
    public string? SuggestedFix { get; set; }

    [JsonPropertyName("section")]
    public string? Section { get; set; } // ltr | str | social | headlines
}

public class AnalysisResult
{
    public int Score { get; set; }
    public List<AnalysisInsight> Insights { get; set; } = new();
}

public class VacancyAnalyzerService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<VacancyAnalyzerService> _logger;

    public VacancyAnalyzerService(AppDbContext db, IConfiguration config, ILogger<VacancyAnalyzerService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<AnalysisResult> AnalyzeAsync(RentalProperty property, Generation? latestGen, int daysOnMarket, string? context)
    {
        var template = await GetTemplateAsync("VACANCY_ANALYSIS");
        var prompt = BuildPrompt(template.PromptText, property, latestGen, daysOnMarket, context);
        var rawResponse = await CallClaudeAsync(prompt, template.SystemPrompt);
        return ParseResponse(rawResponse);
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

    private string BuildPrompt(string templateText, RentalProperty property, Generation? latestGen, int daysOnMarket, string? context)
    {
        var amenities = SafeDeserializeJson(property.AmenitiesJson);
        var currentLtr = latestGen?.LtrOutput ?? "No listing copy generated yet.";
        var currentStr = latestGen?.StrOutput ?? "No listing copy generated yet.";

        return templateText
            .Replace("{Address}", property.Address)
            .Replace("{City}", property.City)
            .Replace("{State}", property.State)
            .Replace("{PropertyType}", property.PropertyType)
            .Replace("{Bedrooms}", property.Bedrooms.ToString())
            .Replace("{Bathrooms}", property.Bathrooms.ToString())
            .Replace("{SquareFootage}", property.SquareFootage?.ToString() ?? "Not specified")
            .Replace("{MonthlyRent}", property.MonthlyRent?.ToString("F0") ?? "Not specified")
            .Replace("{PetPolicy}", property.PetPolicy)
            .Replace("{Parking}", property.Parking)
            .Replace("{Amenities}", amenities.Count > 0 ? string.Join(", ", amenities) : "None specified")
            .Replace("{DaysOnMarket}", daysOnMarket.ToString())
            .Replace("{CurrentLtr}", currentLtr)
            .Replace("{CurrentStr}", currentStr)
            .Replace("{Context}", string.IsNullOrWhiteSpace(context) ? "No additional context provided." : context);
    }

    private async Task<string> CallClaudeAsync(string prompt, string? systemPrompt)
    {
        var apiKey = _config["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        var client = new AnthropicClient(apiKey);

        var messages = new List<Message> { new(RoleType.User, prompt) };

        var parameters = new MessageParameters
        {
            Model         = "claude-sonnet-4-6",
            MaxTokens     = 2048,
            Messages      = messages,
            SystemMessage = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt
        };

        const int maxAttempts = 3;
        int[] delaysMs = [2000, 5000, 10000];

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation("Calling Claude for vacancy analysis (attempt {Attempt}), prompt length: {Length}", attempt, prompt.Length);
                var response = await client.Messages.GetClaudeMessageAsync(parameters);
                var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "";
                _logger.LogInformation("Vacancy analysis response received, length: {Length}", text.Length);
                return text;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("overloaded_error") && attempt < maxAttempts)
            {
                _logger.LogWarning("Claude API overloaded on attempt {Attempt}, retrying in {Delay}ms", attempt, delaysMs[attempt - 1]);
                await Task.Delay(delaysMs[attempt - 1]);
            }
        }

        _logger.LogInformation("Calling Claude for vacancy analysis (final attempt), prompt length: {Length}", prompt.Length);
        var finalResponse = await client.Messages.GetClaudeMessageAsync(parameters);
        var finalText = finalResponse.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "";
        _logger.LogInformation("Vacancy analysis response received, length: {Length}", finalText.Length);
        return finalText;
    }

    private AnalysisResult ParseResponse(string rawResponse)
    {
        try
        {
            var startIdx = rawResponse.IndexOf("{", StringComparison.Ordinal);
            var endIdx   = rawResponse.LastIndexOf("}", StringComparison.Ordinal);

            if (startIdx < 0 || endIdx < 0)
                throw new InvalidDataException("No JSON object found in Claude response.");

            var json = rawResponse[startIdx..(endIdx + 1)];
            var result = JsonSerializer.Deserialize<AnalysisResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
                throw new InvalidDataException("Deserialized result was null.");

            // Clamp score to 0-100
            result.Score = Math.Max(0, Math.Min(100, result.Score));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse vacancy analysis response. Raw: {Raw}",
                rawResponse[..Math.Min(500, rawResponse.Length)]);
            throw new InvalidOperationException("Failed to parse analysis response. Please try again.", ex);
        }
    }

    private static List<string> SafeDeserializeJson(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }
}
