using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Audit finding identified during compliance audit.
/// Can be Major NC, Minor NC, Observation, or OFI.
/// </summary>
public class ComplianceAuditFinding : Finding
{
    private ComplianceAuditFinding() { }

    public ComplianceAuditFinding(
        Guid complianceJourneyId,
        Guid objectiveElementId,
        NonConformanceType type,
        string description,
        DateTime dueDate)
    {
        Id = Guid.NewGuid();
        Scope = FindingScope.Audit;
        ComplianceJourneyId = complianceJourneyId;
        ObjectiveElementId = objectiveElementId;
        Type = type;
        Description = description;
        DueDate = dueDate;
        DateRaised = DateTime.UtcNow;
        ComplianceStatus = ComplianceEvaluationStatus.NonCompliant;
        RiskLevel = type == NonConformanceType.MajorNC
            ? FindingRiskLevel.High
            : type == NonConformanceType.MinorNC
                ? FindingRiskLevel.Medium
                : FindingRiskLevel.Low;
        IsResolved = false;
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void Resolve()
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        Status = FindingStatus.Closed;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        IsResolved = false;
        ResolvedAt = null;
        Status = FindingStatus.Open;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
