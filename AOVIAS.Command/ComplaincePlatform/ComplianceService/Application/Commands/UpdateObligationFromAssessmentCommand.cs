namespace ComplianceService.Application.Commands;

/// <summary>
/// Updates an OrganizationComplianceObligation based on the outcome of an assessment answer.
/// The ComplianceStatus string follows the KnowledgeBase CoreAnswerMapper vocabulary:
/// "compliant", "partially_compliant", "non_compliant", "not_applicable".
/// </summary>
public class UpdateObligationFromAssessmentCommand
{
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// CS-native ObjectiveElement GUID. When the caller only has a KB ComplianceNode ID
    /// (cross-service boundary), set this to the KB node ID and supply
    /// <see cref="ObjectiveElementCode"/> so the handler can fall back to a code-based lookup.
    /// </summary>
    public Guid ObjectiveElementId { get; set; }

    /// <summary>
    /// OE Code (e.g. "AAC.1.a"). Optional — when provided and the direct GUID lookup fails
    /// (because the caller supplied a KB ComplianceNode ID instead of a CS OE ID),
    /// the handler performs a secondary lookup by <see cref="Domain.OrganizationComplianceObligation.Code"/>.
    /// </summary>
    public string? ObjectiveElementCode { get; set; }

    /// <summary>"compliant" | "partially_compliant" | "non_compliant" | "not_applicable"</summary>
    public string ComplianceStatus { get; set; }

    public DateTime AssessedAt { get; set; }
    public string? AssessedBy { get; set; }

    /// <summary>When false, caller runs batch deficiency evaluation after bulk updates.</summary>
    public bool EvaluateDeficiencies { get; set; } = true;

    public UpdateObligationFromAssessmentCommand(
        Guid organizationId,
        Guid objectiveElementId,
        string complianceStatus,
        DateTime assessedAt,
        string? assessedBy = null)
    {
        OrganizationId = organizationId;
        ObjectiveElementId = objectiveElementId;
        ComplianceStatus = complianceStatus;
        AssessedAt = assessedAt;
        AssessedBy = assessedBy;
    }
}
