using Microsoft.AspNetCore.Identity;

namespace DwellScript.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    public DateTime? GracePeriodEndsAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TermsAcceptedAt { get; set; }
    public string? TermsVersion { get; set; }
    public ICollection<Property> Properties { get; set; } = new List<Property>();
}

public enum SubscriptionTier { Free, Starter, Pro }
