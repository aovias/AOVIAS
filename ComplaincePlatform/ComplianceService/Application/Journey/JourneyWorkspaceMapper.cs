using ComplianceService.Application.DTOs;
using ComplianceService.Domain;

namespace ComplianceService.Application.Journey;

public static class JourneyWorkspaceMapper
{
    public static ObjectiveElementDto MapBase(ObjectiveElement oe)
    {
        return new ObjectiveElementDto
        {
            Id = oe.Id,
            Code = oe.Code,
            Title = oe.Title,
            Description = oe.Description,
            Classification = oe.Classification,
            IsMandatory = oe.IsMandatory,
            IsScorable = oe.IsScorable,
            IsApplicable = oe.IsApplicable,
            IsDocumentationRequired = oe.IsDocumentationRequired,
            Weight = oe.Weight,
            Tags = oe.Tags,
            Department = oe.Department,
            ResponsibleRole = oe.ResponsibleRole,
            Frequency = oe.Frequency,
            EvidenceCategory = oe.EvidenceCategory
        };
    }

    public static void ApplyScore(ObjectiveElementDto dto, ObjectiveElementScore? latest, ObjectiveElement oe)
    {
        if (latest == null)
        {
            dto.IsNa = false;
            dto.Score = null;
            dto.ScoreLabel = null;
            return;
        }

        dto.IsNa = !latest.IsApplicable;
        dto.NaJustification = latest.NaReason;
        dto.ScoreRationale = latest.Rationale;
        if (latest.IsApplicable)
        {
            dto.Score = latest.Score;
            dto.ScoreLabel = NabhScoreLabel.FromScore(latest.Score);
            dto.IsCoreCompliant = oe.Classification == ObjectiveElementClassification.Core && latest.Score >= 4;
            dto.AuditStatus = latest.Score >= 4 ? "compliant" : "non_compliant";
        }
        else
        {
            dto.Score = null;
            dto.ScoreLabel = null;
            dto.IsCoreCompliant = false;
            dto.AuditStatus = "not_applicable";
        }
    }

    public static void ApplyEvidenceStatus(ObjectiveElementDto dto, bool hasAnyEvidence, bool hasVerifiedEvidence)
    {
        dto.EvidenceVerifiedFlag = hasVerifiedEvidence;
        if (!hasAnyEvidence)
        {
            dto.EvidenceStatus = "missing";
        }
        else if (!hasVerifiedEvidence)
        {
            dto.EvidenceStatus = "pending_verification";
        }
        else
        {
            dto.EvidenceStatus = "complete";
        }
    }
}
