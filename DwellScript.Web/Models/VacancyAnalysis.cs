namespace DwellScript.Web.Models;

public class VacancyAnalysis
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public int DaysOnMarket { get; set; }
    public string? Context { get; set; }
    public int Score { get; set; }
    public string InsightsJson { get; set; } = "[]";   // JSON array of insights
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
