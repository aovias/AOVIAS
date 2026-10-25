using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Utilities;

/// <summary>
/// Maps commercial audit-type labels to <see cref="AuditType"/> and back.
/// </summary>
public static class AuditTypeMapping
{
    public static string ToCommercialLabel(AuditType type) => type switch
    {
        AuditType.InternalAudit => "Internal",
        AuditType.DepartmentAudit => "Department",
        AuditType.FollowUpAudit => "Follow-up",
        AuditType.MockAudit => "Mock NABH",
        AuditType.FocusedAudit => "Focused",
        AuditType.ConsultantReview => "Consultant Review",
        AuditType.NabhPreAssessment => "NABH Pre-Assessment",
        AuditType.NabhFinalAssessment => "NABH Final Assessment",
        AuditType.Surveillance => "Surveillance",
        _ => type.ToString(),
    };

    public static bool TryParseCommercial(string? label, out AuditType type)
    {
        type = AuditType.InternalAudit;
        if (string.IsNullOrWhiteSpace(label))
            return false;

        var key = label.Trim().ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty);

        switch (key)
        {
            case "internal":
            case "internalaudit":
                type = AuditType.InternalAudit;
                return true;
            case "department":
            case "departmentaudit":
                type = AuditType.DepartmentAudit;
                return true;
            case "followup":
            case "followupaudit":
                type = AuditType.FollowUpAudit;
                return true;
            case "mocknabh":
            case "mock":
            case "mockaudit":
                type = AuditType.MockAudit;
                return true;
            case "focused":
            case "focusedaudit":
                type = AuditType.FocusedAudit;
                return true;
            case "consultantreview":
            case "consultant":
                type = AuditType.ConsultantReview;
                return true;
            case "nabhpreassessment":
            case "preassessment":
                type = AuditType.NabhPreAssessment;
                return true;
            case "nabhfinalassessment":
            case "finalassessment":
                type = AuditType.NabhFinalAssessment;
                return true;
            case "surveillance":
                type = AuditType.Surveillance;
                return true;
            default:
                return Enum.TryParse(label, ignoreCase: true, out type);
        }
    }

    /// <summary>Core commercial types required by Phase 2 Sprint 2.</summary>
    public static IReadOnlyList<AuditType> CommercialCoreTypes { get; } =
    [
        AuditType.InternalAudit,
        AuditType.DepartmentAudit,
        AuditType.FollowUpAudit,
        AuditType.MockAudit,
        AuditType.FocusedAudit,
    ];
}
