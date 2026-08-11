namespace ComplianceService.Domain;

/// <summary>
/// Represents a detected compliance gap with references to related entities.
/// </summary>
public class GapResult
{
    /// <summary>
    /// The type of gap detected.
    /// </summary>
    public GapType GapType { get; set; }

    /// <summary>
    /// Severity of the gap.
    /// </summary>
    public GapSeverity Severity { get; set; }

    /// <summary>
    /// Human-readable description of the gap.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// References to related entities.
    /// </summary>
    public GapReferences References { get; set; } = new();
}

/// <summary>
/// Type of compliance gap.
/// </summary>
public enum GapType
{
    /// <summary>
    /// A control exists but has no corresponding task.
    /// </summary>
    MissingTask,

    /// <summary>
    /// A task exists but has no evidence.
    /// </summary>
    MissingEvidence,

    /// <summary>
    /// Evidence exists but is weak or insufficient.
    /// </summary>
    WeakEvidence,

    /// <summary>
    /// Evidence exists but is not mapped to any control.
    /// </summary>
    UnmappedEvidence,

    /// <summary>
    /// An audit finding exists but has no corrective action or task.
    /// </summary>
    UnaddressedAuditFinding
}

/// <summary>
/// References to entities related to a gap.
/// </summary>
public class GapReferences
{
    /// <summary>
    /// Program ID if applicable.
    /// </summary>
    public Guid? ProgramId { get; set; }

    /// <summary>
    /// Control ID if applicable.
    /// </summary>
    public Guid? ControlId { get; set; }

    /// <summary>
    /// Control code if applicable (e.g., "NABH-01").
    /// </summary>
    public string? ControlCode { get; set; }

    /// <summary>
    /// Task ID if applicable.
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Evidence ID if applicable.
    /// </summary>
    public Guid? EvidenceId { get; set; }

    /// <summary>
    /// Audit finding ID if applicable.
    /// </summary>
    public Guid? AuditFindingId { get; set; }

    /// <summary>
    /// Compliance standard ID if applicable (task / OE scope).
    /// </summary>
    public Guid? ComplianceStandardId { get; set; }

    /// <summary>
    /// Objective Element ID if applicable.
    /// </summary>
    public Guid? ObjectiveElementId { get; set; }
}
