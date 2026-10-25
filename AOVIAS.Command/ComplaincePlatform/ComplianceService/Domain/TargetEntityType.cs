namespace ComplianceService.Domain;

/// <summary>
/// Type of entity that is the target of a copilot question.
/// Determined based on provided context identifiers.
/// </summary>
public enum TargetEntityType
{
    /// <summary>
    /// No specific entity target identified.
    /// </summary>
    None,

    /// <summary>
    /// Question targets a compliance program.
    /// </summary>
    Program,

    /// <summary>
    /// Question targets a control.
    /// </summary>
    Control,

    /// <summary>
    /// Question targets a compliance task.
    /// </summary>
    Task,

    /// <summary>
    /// Question targets evidence.
    /// </summary>
    Evidence,

    /// <summary>
    /// Question targets an audit finding.
    /// </summary>
    AuditFinding,

    /// <summary>
    /// Question targets an audit session.
    /// </summary>
    Audit,

    /// <summary>
    /// Unknown or unidentifiable entity type.
    /// </summary>
    Unknown
}
