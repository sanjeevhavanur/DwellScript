using DwellScript.Web.Models;
using Microsoft.Extensions.Logging;

namespace DwellScript.Web.Services;

public enum Feature { VacancyAnalyzer, GenerationHistory, SectionRefinement, UnlimitedProperties, PersonaTargeting }

public class SubscriptionService
{
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(ILogger<SubscriptionService> logger)
    {
        _logger = logger;
    }

    public bool HasAccess(ApplicationUser user, Feature feature)
    {
        return feature switch
        {
            Feature.VacancyAnalyzer      => user.Tier == SubscriptionTier.Pro,
            Feature.GenerationHistory    => user.Tier >= SubscriptionTier.Starter,
            Feature.SectionRefinement    => user.Tier >= SubscriptionTier.Starter,
            Feature.UnlimitedProperties  => user.Tier == SubscriptionTier.Pro,
            Feature.PersonaTargeting     => user.Tier == SubscriptionTier.Pro,
            _                            => false
        };
    }
}
