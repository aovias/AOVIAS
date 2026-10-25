using AIOrchestratorService.Domain;
using Shared.Infrastructure.DTO;

namespace AIOrchestratorService.Application.Services;

/// <summary>
/// Structured request containing all context needed for AI-assisted draft generation.
/// This is pure context assembly - no AI calls or prompt text.
/// </summary>
public class DraftingRequest
{
    /// <summary>
    /// The type of draft to generate.
    /// </summary>
    public DraftType DraftType { get; set; }

    /// <summary>
    /// The ID of the source entity this draft is for.
    /// </summary>
    public Guid SourceEntityId { get; set; }

    /// <summary>
    /// Compliance program context (if applicable).
    /// </summary>
    public ComplianceProgramContext? ComplianceProgram { get; set; }

    /// <summary>
    /// Control context (if applicable).
    /// </summary>
    public ControlContext? Control { get; set; }

    /// <summary>
    /// Compliance task context (if applicable).
    /// </summary>
    public ComplianceTaskContext? ComplianceTask { get; set; }

    /// <summary>
    /// Evidence items related to the task (if applicable).
    /// </summary>
    public List<EvidenceContext> Evidences { get; set; } = new();

    /// <summary>
    /// Audit finding context (if applicable).
    /// </summary>
    public AuditFindingContext? AuditFinding { get; set; }

    /// <summary>
    /// Aggregated explainability context from all relevant entities.
    /// </summary>
    public List<ExplainabilityContext> ExplainabilityContexts { get; set; } = new();
}

/// <summary>
/// Snapshot of relevant ComplianceProgram data.
/// </summary>
public class ComplianceProgramContext
{
    public Guid Id { get; set; }
    public string PlatformId { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ExplainabilityDto? Explainability { get; set; }

    /// <summary>
    /// Creates a context from individual properties.
    /// </summary>
    public static ComplianceProgramContext Create(
        Guid id,
        string platformId,
        Guid organizationId,
        string name,
        ExplainabilityDto? explainability = null)
    {
        return new ComplianceProgramContext
        {
            Id = id,
            PlatformId = platformId,
            OrganizationId = organizationId,
            Name = name,
            Explainability = explainability
        };
    }
}

/// <summary>
/// Snapshot of relevant Control data.
/// </summary>
public class ControlContext
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExplainabilityDto? Explainability { get; set; }

    /// <summary>
    /// Creates a context from individual properties.
    /// </summary>
    public static ControlContext Create(
        Guid id,
        Guid programId,
        string code,
        string description,
        ExplainabilityDto? explainability = null)
    {
        return new ControlContext
        {
            Id = id,
            ProgramId = programId,
            Code = code,
            Description = description,
            Explainability = explainability
        };
    }
}

/// <summary>
/// Snapshot of relevant ComplianceTask data.
/// </summary>
public class ComplianceTaskContext
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RegulationId { get; set; }
    public Guid ObjectiveElementId { get; set; }
    public string AssignedToUserId { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public ExplainabilityDto? Explainability { get; set; }

    /// <summary>
    /// Creates a context from individual properties.
    /// </summary>
    public static ComplianceTaskContext Create(
        Guid id,
        Guid organizationId,
        Guid regulationId,
        Guid objectiveElementId,
        string assignedToUserId,
        DateTime dueDate,
        string status,
        ExplainabilityDto? explainability = null)
    {
        return new ComplianceTaskContext
        {
            Id = id,
            OrganizationId = organizationId,
            RegulationId = regulationId,
            ObjectiveElementId = objectiveElementId,
            AssignedToUserId = assignedToUserId,
            DueDate = dueDate,
            Status = status,
            Explainability = explainability
        };
    }
}

/// <summary>
/// Snapshot of relevant Evidence data.
/// </summary>
public class EvidenceContext
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DateTime UploadedOn { get; set; }
    public string Metadata { get; set; } = string.Empty;

    /// <summary>
    /// Creates a context from individual properties.
    /// </summary>
    public static EvidenceContext Create(
        Guid id,
        Guid taskId,
        string evidenceType,
        string fileUrl,
        DateTime uploadedOn,
        string metadata = "")
    {
        return new EvidenceContext
        {
            Id = id,
            TaskId = taskId,
            EvidenceType = evidenceType,
            FileUrl = fileUrl,
            UploadedOn = uploadedOn,
            Metadata = metadata
        };
    }
}

/// <summary>
/// Snapshot of relevant AuditFinding data.
/// </summary>
public class AuditFindingContext
{
    public Guid Id { get; set; }
    public Guid AuditSessionId { get; set; }
    public string ControlCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExplainabilityDto? Explainability { get; set; }

    /// <summary>
    /// Creates a context from individual properties.
    /// </summary>
    public static AuditFindingContext Create(
        Guid id,
        Guid auditSessionId,
        string controlCode,
        string severity,
        string description,
        ExplainabilityDto? explainability = null)
    {
        return new AuditFindingContext
        {
            Id = id,
            AuditSessionId = auditSessionId,
            ControlCode = controlCode,
            Severity = severity,
            Description = description,
            Explainability = explainability
        };
    }
}

/// <summary>
/// Explainability context with source entity information.
/// </summary>
public class ExplainabilityContext
{
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid SourceEntityId { get; set; }
    public ExplainabilityDto Explainability { get; set; } = null!;
}
