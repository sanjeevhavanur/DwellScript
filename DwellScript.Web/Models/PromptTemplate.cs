namespace DwellScript.Web.Models;

public class PromptTemplate
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;       // "FULL_GENERATION", "SECTION_REGEN", etc.
    public string PromptText { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
