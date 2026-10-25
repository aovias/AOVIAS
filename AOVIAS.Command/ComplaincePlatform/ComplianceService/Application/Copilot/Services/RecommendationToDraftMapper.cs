using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Maps a <see cref="CopilotRecommendation"/> to the most appropriate <see cref="DraftType"/>
/// and builds a <see cref="DraftGenerationRequest"/> for the generation engine.
/// </summary>
public static class RecommendationToDraftMapper
{
    /// <summary>
    /// Selects the appropriate draft type for a given recommendation category.
    /// </summary>
    public static DraftType MapToDraftType(RecommendationCategory category) =>
        category switch
        {
            RecommendationCategory.Deficiency    => DraftType.CapaDraft,
            RecommendationCategory.CAPA          => DraftType.CapaDraft,
            RecommendationCategory.Risk          => DraftType.CapaDraft,
            RecommendationCategory.Accreditation => DraftType.ReadinessSummary,
            RecommendationCategory.Verification  => DraftType.EvidenceChecklist,
            RecommendationCategory.Evidence      => DraftType.EvidenceChecklist,
            RecommendationCategory.Executive     => DraftType.ExecutiveComplianceBriefing,
            RecommendationCategory.Analytics     => DraftType.ExecutiveComplianceBriefing,
            RecommendationCategory.Escalation    => DraftType.ExecutiveComplianceBriefing,
            RecommendationCategory.Department    => DraftType.ReadinessSummary,
            _                                    => DraftType.ReadinessSummary,
        };

    /// <summary>
    /// Builds a generation request from a source recommendation.
    /// </summary>
    public static DraftGenerationRequest FromRecommendation(
        CopilotRecommendation recommendation,
        Guid organizationId,
        string? personaOverride = null)
    {
        var draftType = MapToDraftType(recommendation.Category);

        return new DraftGenerationRequest
        {
            DraftType        = draftType,
            SourceType       = DraftSourceType.Recommendation,
            Persona          = personaOverride ?? recommendation.Persona,
            OrganizationId   = organizationId,
            RecommendationId = recommendation.Id,
            SourceEntityId   = recommendation.SourceEntityId,
        };
    }

    /// <summary>
    /// Returns a human-readable label for the draft action shown on a recommendation card.
    /// Example: "Generate CAPA Draft", "Generate Readiness Summary".
    /// </summary>
    public static string GetActionLabel(DraftType draftType) =>
        draftType switch
        {
            DraftType.CapaDraft                    => "Generate CAPA Draft",
            DraftType.ExecutiveComplianceBriefing   => "Generate Executive Briefing",
            DraftType.ReadinessSummary              => "Generate Readiness Summary",
            DraftType.EvidenceChecklist             => "Generate Evidence Checklist",
            DraftType.AuditPreparationBrief         => "Generate Audit Brief",
            DraftType.ManagementReviewSummary       => "Generate Management Review",
            _                                       => "Generate Draft",
        };
}
