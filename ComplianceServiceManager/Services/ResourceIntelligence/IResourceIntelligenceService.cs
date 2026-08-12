using ComplianceServiceManager.Models.ResourceIntelligence;
using ComplianceServiceManager.Models.RuntimeProfiles;

namespace ComplianceServiceManager.Services.ResourceIntelligence;

/// <summary>
/// Advisory resource intelligence over S3 telemetry. Does not mutate runtime, Git, or agent sessions.
/// </summary>
public interface IResourceIntelligenceService
{
    ResourceThresholdPolicy Policy { get; }

    ResourceAssessment? LatestAssessment { get; }

    event Action<ResourceAssessment>? AssessmentUpdated;

    /// <summary>Consumes an S3 snapshot into the bounded rolling history and recalculates assessment.</summary>
    ResourceAssessment AssessFromSnapshot(
        ComplianceServiceManager.Models.LocalResources.LocalResourceSnapshot? snapshot,
        string? requestedProfileId = null);

    ResourceAssessment Assess(string? requestedProfileId = null);

    RuntimeProfileResourceEstimate EstimateProfile(RuntimeProfileDefinition profile);

    IReadOnlyList<RuntimeProfileResourceEstimate> RankProfilesLightestFirst();
}
