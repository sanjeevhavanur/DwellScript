using Microsoft.Extensions.Logging;

namespace DwellScript.Web.Services;

public class FairHousingScanResult
{
    public bool HasViolations { get; set; }
    public List<string> Violations { get; set; } = new();
}

public class FairHousingFilter
{
    private static readonly string[] ProhibitedPhrases =
    [
        "no children", "adults only", "perfect for couples", "no kids",
        "christian home", "english speakers only", "ideal for professionals",
        "no section 8", "walking distance to church"
    ];

    private readonly ILogger<FairHousingFilter> _logger;

    public FairHousingFilter(ILogger<FairHousingFilter> logger)
    {
        _logger = logger;
    }

    public Task<FairHousingScanResult> ScanAsync(string output)
    {
        var result = new FairHousingScanResult();
        var lower = output.ToLowerInvariant();

        foreach (var phrase in ProhibitedPhrases)
        {
            if (lower.Contains(phrase))
            {
                result.HasViolations = true;
                result.Violations.Add(phrase);
            }
        }

        if (result.HasViolations)
            _logger.LogWarning("Fair Housing violations detected: {Violations}", string.Join(", ", result.Violations));

        return Task.FromResult(result);
    }
}
