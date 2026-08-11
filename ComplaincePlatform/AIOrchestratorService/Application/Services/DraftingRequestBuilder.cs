using AIOrchestratorService.Domain;

namespace AIOrchestratorService.Application.Services;

/// <summary>
/// Builder for assembling structured drafting requests from relevant entity data.
/// This is pure context assembly - no AI calls or prompt generation.
/// </summary>
public class DraftingRequestBuilder
{
    private DraftType _draftType;
    private Guid _sourceEntityId;
    private ComplianceProgramContext? _complianceProgram;
    private ControlContext? _control;
    private ComplianceTaskContext? _complianceTask;
    private readonly List<EvidenceContext> _evidences = new();
    private AuditFindingContext? _auditFinding;

    /// <summary>
    /// Sets the draft type and source entity ID.
    /// </summary>
    public DraftingRequestBuilder ForDraft(DraftType draftType, Guid sourceEntityId)
    {
        _draftType = draftType;
        _sourceEntityId = sourceEntityId;
        return this;
    }

    /// <summary>
    /// Adds compliance program context.
    /// </summary>
    public DraftingRequestBuilder WithComplianceProgram(ComplianceProgramContext? program)
    {
        _complianceProgram = program;
        return this;
    }

    /// <summary>
    /// Adds control context.
    /// </summary>
    public DraftingRequestBuilder WithControl(ControlContext? control)
    {
        _control = control;
        return this;
    }

    /// <summary>
    /// Adds compliance task context.
    /// </summary>
    public DraftingRequestBuilder WithComplianceTask(ComplianceTaskContext? task)
    {
        _complianceTask = task;
        return this;
    }

    /// <summary>
    /// Adds evidence context.
    /// </summary>
    public DraftingRequestBuilder WithEvidence(EvidenceContext evidence)
    {
        _evidences.Add(evidence);
        return this;
    }

    /// <summary>
    /// Adds multiple evidence contexts.
    /// </summary>
    public DraftingRequestBuilder WithEvidences(IEnumerable<EvidenceContext> evidences)
    {
        _evidences.AddRange(evidences);
        return this;
    }

    /// <summary>
    /// Adds audit finding context.
    /// </summary>
    public DraftingRequestBuilder WithAuditFinding(AuditFindingContext? finding)
    {
        _auditFinding = finding;
        return this;
    }

    /// <summary>
    /// Builds the structured DraftingRequest with all collected context and explainability information.
    /// </summary>
    public DraftingRequest Build()
    {
        var request = new DraftingRequest
        {
            DraftType = _draftType,
            SourceEntityId = _sourceEntityId,
            ComplianceProgram = _complianceProgram,
            Control = _control,
            ComplianceTask = _complianceTask,
            Evidences = new List<EvidenceContext>(_evidences),
            AuditFinding = _auditFinding
        };

        // Collect explainability contexts from all entities
        var explainabilityContexts = new List<ExplainabilityContext>();

        if (_complianceProgram?.Explainability != null)
        {
            explainabilityContexts.Add(new ExplainabilityContext
            {
                SourceEntityType = nameof(ComplianceProgramContext),
                SourceEntityId = _complianceProgram.Id,
                Explainability = _complianceProgram.Explainability
            });
        }

        if (_control?.Explainability != null)
        {
            explainabilityContexts.Add(new ExplainabilityContext
            {
                SourceEntityType = nameof(ControlContext),
                SourceEntityId = _control.Id,
                Explainability = _control.Explainability
            });
        }

        if (_complianceTask?.Explainability != null)
        {
            explainabilityContexts.Add(new ExplainabilityContext
            {
                SourceEntityType = nameof(ComplianceTaskContext),
                SourceEntityId = _complianceTask.Id,
                Explainability = _complianceTask.Explainability
            });
        }

        if (_auditFinding?.Explainability != null)
        {
            explainabilityContexts.Add(new ExplainabilityContext
            {
                SourceEntityType = nameof(AuditFindingContext),
                SourceEntityId = _auditFinding.Id,
                Explainability = _auditFinding.Explainability
            });
        }

        request.ExplainabilityContexts = explainabilityContexts;

        return request;
    }
}
