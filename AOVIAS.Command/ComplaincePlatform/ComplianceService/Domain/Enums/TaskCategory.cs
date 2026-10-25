namespace ComplianceService.Domain.Enums;

/// <summary>
/// Business category for a <see cref="ComplianceTask"/> (Phase 2 task engine).
/// </summary>
public enum TaskCategory
{
    Compliance = 0,
    CAPA = 1,
    Audit = 2,
    Training = 3,
    DocumentReview = 4,
    General = 5
}
