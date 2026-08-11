using ComplianceService.Domain.Enums;

namespace ComplianceService.Domain
{
    public record RiskSnapshot(
        Guid RiskId,
        Guid? ChapterId,
        Guid? TaskId,
        ComplianceSeverity Severity,
        int RiskScore
    );
}
