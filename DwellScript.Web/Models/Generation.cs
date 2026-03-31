namespace DwellScript.Web.Models;

public class Generation
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public GenerationType Type { get; set; }
    public string? RefinementInstruction { get; set; }
    public string? RegeneratedSection { get; set; }  // "LTR","STR","Social","Headlines"
    public string? LtrOutput { get; set; }
    public string? StrOutput { get; set; }
    public string? SocialOutput { get; set; }
    public string? HeadlinesJson { get; set; }        // JSON array of 3 strings
    public bool IsFlaggedForAnalyzer { get; set; } = false;
    public string? PersonaKey { get; set; }           // e.g. "remote-worker", "pet-owner"
    public decimal UsageUnitsConsumed { get; set; }   // 1.0 full, 0.25 section regen
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum GenerationType { Full, SectionRegen, AnalyzerApplied, PersonaGeneration, PersonaRefine }
