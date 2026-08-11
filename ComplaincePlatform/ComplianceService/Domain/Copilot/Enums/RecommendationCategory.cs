namespace ComplianceService.Domain.Copilot.Enums;

/// <summary>
/// Classifies which compliance domain generated this recommendation.
/// Used for filtering, routing to personas, and analytics.
/// </summary>
public enum RecommendationCategory
{
    Accreditation = 0,
    Deficiency    = 1,
    CAPA          = 2,
    SLA           = 3,
    Escalation    = 4,
    Evidence      = 5,
    Verification  = 6,
    Task          = 7,
    Executive     = 8,
    Department    = 9,
    Risk          = 10,
    Analytics     = 11,
}
