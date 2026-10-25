namespace ComplianceService.Application.Contracts;

/// <summary>
/// Centralized obligation state projection — single writer for operational obligation fields.
/// </summary>
public interface IObligationProjectionService
{
    /// <summary>
    /// Recomputes all obligation operational fields from authoritative sources,
    /// evaluates deficiencies, and publishes outbox events for snapshot refresh.
    /// </summary>
    Task<ObligationProjectionResult?> ProjectAsync(
        ObligationProjectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves obligation by hospital + objective element and runs projection.
    /// </summary>
    Task<ObligationProjectionResult?> ProjectByObjectiveElementAsync(
        Guid hospitalId,
        Guid objectiveElementId,
        string triggerSource,
        CancellationToken cancellationToken = default);
}

public sealed record ObligationProjectionRequest(
    Guid ObligationId,
    string TriggerSource,
    bool EvaluateDeficiencies = true,
    bool FlushOutbox = true);

public sealed record ObligationProjectionResult(
    Guid ObligationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ObjectiveElementId,
    string EvidenceStatus,
    string VerificationStatus,
    decimal ReadinessScore,
    decimal CompletionPercentage);

/// <summary>Standard trigger source labels for obligation projection.</summary>
public static class ObligationProjectionTriggers
{
    public const string AssessmentCompleted = "AssessmentCompleted";
    public const string JourneyUpdated = "JourneyUpdated";
    public const string EvidenceUploaded = "EvidenceUploaded";
    public const string EvidenceApproved = "EvidenceApproved";
    public const string EvidenceRejected = "EvidenceRejected";
    public const string EvidenceNeedsMore = "EvidenceNeedsMore";
    public const string EvidenceDeleted = "EvidenceDeleted";
    public const string TaskCompleted = "TaskCompleted";
    public const string TaskReopened = "TaskReopened";
    public const string TaskAssigned = "TaskAssigned";
    public const string DeficiencyClosed = "DeficiencyClosed";
    public const string DeficiencyReopened = "DeficiencyReopened";
}
