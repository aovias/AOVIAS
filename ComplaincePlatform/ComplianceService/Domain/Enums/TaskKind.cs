namespace ComplianceService.Domain.Enums;

/// <summary>
/// Execution kind for a <see cref="ComplianceTask"/> (Phase 2 task engine).
/// </summary>
public enum TaskKind
{
    Standard = 0,
    Approval = 1,
    Review = 2
}
