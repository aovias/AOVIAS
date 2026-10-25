namespace ComplianceService.Domain;

/// <summary>
/// Represents the routed intent from a user's copilot question.
/// Contains the question type and extracted entity identifiers.
/// </summary>
public class CopilotIntent
{
    /// <summary>
    /// The type of question that was detected.
    /// </summary>
    public CopilotQuestionType QuestionType { get; set; }

    /// <summary>
    /// The original user question.
    /// </summary>
    public string OriginalQuestion { get; set; } = string.Empty;

    /// <summary>
    /// Optional program identifier if present in the context or question.
    /// </summary>
    public Guid? ProgramId { get; set; }

    /// <summary>
    /// Optional control identifier if present in the context or question.
    /// </summary>
    public Guid? ControlId { get; set; }

    /// <summary>
    /// Optional regulation identifier if present in the context or question.
    /// </summary>
    public Guid? RegulationId { get; set; }

    /// <summary>
    /// Optional objective element identifier if present in the context or question.
    /// </summary>
    public Guid? ObjectiveElementId { get; set; }

    /// <summary>
    /// Optional task identifier if present in the context or question.
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Additional extracted entities as key-value pairs.
    /// </summary>
    public Dictionary<string, string> AdditionalEntities { get; set; } = new();

    /// <summary>
    /// The target entity type determined from provided context identifiers.
    /// </summary>
    public TargetEntityType TargetEntityType { get; set; }

    /// <summary>
    /// Optional audit identifier if present in the context.
    /// </summary>
    public Guid? AuditId { get; set; }
}
