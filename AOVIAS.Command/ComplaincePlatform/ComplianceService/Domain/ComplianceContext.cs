namespace ComplianceService.Domain;

/// <summary>
/// Structured context containing all relevant compliance data for copilot processing.
/// All properties are nullable to support partial data scenarios.
/// </summary>
public class ComplianceContext
{
    /// <summary>
    /// The regulation if available.
    /// </summary>
    public Regulation? Regulation { get; set; }

    /// <summary>
    /// The compliance standard if available (e.g. from task scope).
    /// </summary>
    public ComplianceStandard? ComplianceStandard { get; set; }

    /// <summary>
    /// The compliance program if available.
    /// </summary>
    public ComplianceProgram? ComplianceProgram { get; set; }

    /// <summary>
    /// The control if available.
    /// </summary>
    public Control? Control { get; set; }

    /// <summary>
    /// The compliance task if available.
    /// </summary>
    public ComplianceTask? ComplianceTask { get; set; }

    /// <summary>
    /// List of evidence associated with the task if available.
    /// </summary>
    public List<Evidence> Evidence { get; set; } = new();

    /// <summary>
    /// List of controls associated with the program if available.
    /// </summary>
    public List<Control> ProgramControls { get; set; } = new();

    /// <summary>
    /// List of tasks associated with the program if available.
    /// </summary>
    public List<ComplianceTask> ProgramTasks { get; set; } = new();

    /// <summary>
    /// Audit session data if available (from AuditService).
    /// </summary>
    public AuditContextData? AuditSession { get; set; }

    /// <summary>
    /// List of audit findings if available (from AuditService).
    /// </summary>
    public List<AuditFindingData> AuditFindings { get; set; } = new();

    /// <summary>
    /// Validation errors encountered during assembly.
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Indicates whether the context is complete and valid.
    /// </summary>
    public bool IsValid => ValidationErrors.Count == 0;
}

/// <summary>
/// Simplified representation of audit session data from AuditService.
/// </summary>
public class AuditContextData
{
    public Guid Id { get; set; }
    public string PlatformId { get; set; } = string.Empty;
    public Guid HospitalId { get; set; }
    public Guid ComplianceProgramId { get; set; }
    public DateTime StartedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Simplified representation of audit finding data from AuditService.
/// </summary>
public class AuditFindingData
{
    public Guid Id { get; set; }
    public Guid AuditSessionId { get; set; }
    public string ControlCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
