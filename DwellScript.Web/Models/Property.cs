namespace DwellScript.Web.Models;

public class Property
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public PropertyStatus Status { get; set; } = PropertyStatus.Active;
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int? SquareFootage { get; set; }
    public decimal? MonthlyRent { get; set; }
    public string PetPolicy { get; set; } = string.Empty;
    public string Parking { get; set; } = string.Empty;
    public string AmenitiesJson { get; set; } = "[]";   // JSON array
    public string PlatformsJson { get; set; } = "[]";   // JSON array: ["ltr","str","social"]
    public string? Notes { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Generation> Generations { get; set; } = new List<Generation>();
}

public enum PropertyStatus { Active, Vacant, Archived }
