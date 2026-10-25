using ComplianceService.Domain.Enums;
using ComplianceService.Domain.Events;using System.ComponentModel.DataAnnotations;

namespace ComplianceService.Domain;

/// <summary>
/// Represents an identified compliance deficiency derived from OrganizationComplianceObligation state.
/// Deficiencies are operational compliance issues requiring remediation — not assessment findings.
/// </summary>
public class ComplianceDeficiency : AggregateRoot
{
    // ── Identity ────────────────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid HospitalId { get; private set; }
    public Guid FrameworkId { get; private set; }
    public Guid ObjectiveElementId { get; private set; }

    /// <summary>FK to OrganizationComplianceObligation (no navigation — cross-aggregate boundary).</summary>
    public Guid ComplianceObligationId { get; private set; }

    // ── Classification ───────────────────────────────────────────────────────
    public string DeficiencyCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DeficiencyType DeficiencyType { get; private set; }
    public DeficiencySeverity Severity { get; private set; }
    public DeficiencyStatus Status { get; private set; } = DeficiencyStatus.Identified;

    // ── Context (denormalized from obligation) ───────────────────────────────
    public ObjectiveElementClassification MaturityLevel { get; private set; }
    public string? RiskCategory { get; private set; }
    public RootCauseCategory? RootCauseCategory { get; private set; }

    // ── Sprint 8 RCA ─────────────────────────────────────────────────────────
    public string? ProblemStatement { get; private set; }
    public string? RootCauseSummary { get; private set; }
    public string? ImpactDescription { get; private set; }
    public string? CorrectiveActionSummary { get; private set; }
    public string? PreventiveActionSummary { get; private set; }
    public string? Why1 { get; private set; }
    public string? Why2 { get; private set; }
    public string? Why3 { get; private set; }
    public string? Why4 { get; private set; }
    public string? Why5 { get; private set; }
    public string? FinalRootCause { get; private set; }

    // ── Assignment ───────────────────────────────────────────────────────────
    public string? AssignedTo { get; private set; }
    public string? Department { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? AssignedDate { get; private set; }
    public DateTime? TargetResolutionDate { get; private set; }
    public DateTime? ActualResolutionDate { get; private set; }

    // ── Sprint 8 risk assessment ─────────────────────────────────────────────
    public int? RiskLikelihood { get; private set; }
    public int? RiskImpact { get; private set; }
    public int? RiskScore { get; private set; }
    public DeficiencyRiskLevel? AssessedRiskLevel { get; private set; }

    // ── Sprint 8 remediation planning ────────────────────────────────────────
    public string? ActionPlan { get; private set; }
    public string? ExpectedOutcome { get; private set; }
    public DateTime? RemediationTargetCompletion { get; private set; }
    public string? SuccessCriteria { get; private set; }

    // ── Sprint 8 effectiveness placeholder ───────────────────────────────────
    public EffectivenessReviewStatus EffectivenessReviewStatus { get; private set; }
        = EffectivenessReviewStatus.NotRequired;

    // ── Phase 2 CAPA identity / ownership ────────────────────────────────────
    public string? CapaNumber { get; private set; }
    public DateTime? OwnerAcceptedAt { get; private set; }
    public string? OwnerAcceptedBy { get; private set; }
    public bool OwnerAcceptanceRejected { get; private set; }
    public DateTime? OwnerRejectedAt { get; private set; }
    public string? OwnerRejectedBy { get; private set; }
    public string? OwnershipRejectionReason { get; private set; }

    /// <summary>Optional linked source label (e.g. Incident, Risk); OriginType remains the typed enum.</summary>
    public string? LinkedSourceType { get; private set; }

    // ── Sprint 11 CAPA — corrective action ───────────────────────────────────
    public string? CorrectiveActionOwner { get; private set; }
    public DateTime? CorrectiveActionStartDate { get; private set; }
    public DateTime? CorrectiveActionTargetDate { get; private set; }
    public DateTime? CorrectiveActionCompletedDate { get; private set; }
    public CapaActionStatus CorrectiveActionStatus { get; private set; } = CapaActionStatus.NotStarted;

    // ── Sprint 11 CAPA — preventive action ───────────────────────────────────
    public string? PreventiveActionOwner { get; private set; }
    public DateTime? PreventiveActionTargetDate { get; private set; }
    public DateTime? PreventiveActionCompletedDate { get; private set; }
    public CapaActionStatus PreventiveActionStatus { get; private set; } = CapaActionStatus.NotStarted;
    public string? PreventiveActionSuccessCriteria { get; private set; }

    // ── Sprint 11 CAPA — effectiveness review ────────────────────────────────
    public DateTime? EffectivenessReviewDate { get; private set; }
    public DateTime? EffectivenessReviewScheduledDate { get; private set; }
    public int? EffectivenessReviewDaysOffset { get; private set; }
    public string? EffectivenessReviewer { get; private set; }
    public string? EffectivenessReviewNotes { get; private set; }
    public string? EffectivenessReviewOutcome { get; private set; }

    // ── Sprint 11 CAPA — recurrence ──────────────────────────────────────────
    public bool IsRecurringIssue { get; private set; }
    public DateTime? RecurrenceDetectedAt { get; private set; }
    public string? RecurrenceReason { get; private set; }

    // ── Legacy assignment (DueDate retained above) ─────────────────────────

    // ── Lifecycle ───────────────────────────────────────────────────────────
    /// <summary>"EvaluationEngine", "Manual", or "AuditFinding"</summary>
    public string Source { get; private set; } = "EvaluationEngine";

    /// <summary>Sprint 7B — typed origin when promoted from a finding.</summary>
    public DeficiencyOriginType? OriginType { get; private set; }

    public Guid? OriginId { get; private set; }

    /// <summary>Finding number when origin is AuditFinding.</summary>
    public string? OriginReference { get; private set; }

    public DateTime? PromotedAt { get; private set; }

    public DateTime DetectedDate { get; private set; }
    public DateTime? ClosedDate { get; private set; }
    public DateTime LastEvaluatedDate { get; private set; }

    // ── Sprint 10 SLA ────────────────────────────────────────────────────────
    public Guid? SlaPolicyId { get; private set; }
    public DateTime? SlaStartDate { get; private set; }
    public DateTime? SlaDueDate { get; private set; }
    public SlaStatus? CurrentSlaStatus { get; private set; }
    public int? CurrentEscalationLevel { get; private set; }
    public EscalationState? CurrentEscalationState { get; private set; }

    // ── Audit ────────────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    [Timestamp]
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // ── EF Core constructor ──────────────────────────────────────────────────
    private ComplianceDeficiency() { }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new deficiency and raises <see cref="DeficiencyIdentifiedDomainEvent"/>.
    /// </summary>
    public static ComplianceDeficiency Create(
        Guid tenantId,
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        Guid complianceObligationId,
        string deficiencyCode,
        string title,
        string? description,
        DeficiencyType deficiencyType,
        DeficiencySeverity severity,
        ObjectiveElementClassification maturityLevel,
        string? riskCategory,
        string source = "EvaluationEngine",
        string? createdBy = null)
    {
        var deficiency = new ComplianceDeficiency
        {
            Id                    = Guid.NewGuid(),
            TenantId              = tenantId,
            OrganizationId        = organizationId,
            HospitalId            = hospitalId,
            FrameworkId           = frameworkId,
            ObjectiveElementId    = objectiveElementId,
            ComplianceObligationId = complianceObligationId,
            DeficiencyCode        = deficiencyCode,
            Title                 = title,
            Description           = description,
            DeficiencyType        = deficiencyType,
            Severity              = severity,
            Status                = DeficiencyStatus.Identified,
            MaturityLevel         = maturityLevel,
            RiskCategory          = riskCategory,
            Source                = source,
            DetectedDate          = DateTime.UtcNow,
            LastEvaluatedDate     = DateTime.UtcNow,
            CreatedAt             = DateTime.UtcNow,
            UpdatedAt             = DateTime.UtcNow,
            CreatedBy             = createdBy,
            UpdatedBy             = createdBy,
        };

        deficiency.AddDomainEvent(new DeficiencyIdentifiedDomainEvent
        {
            DeficiencyId   = deficiency.Id,
            ObligationId   = complianceObligationId,
            HospitalId     = hospitalId,
            FrameworkId    = frameworkId,
            DeficiencyType = deficiencyType,
            Severity       = severity,
        });

        return deficiency;
    }

    /// <summary>Creates a deficiency promoted from an accepted audit finding (Sprint 7B).</summary>
    public static ComplianceDeficiency CreateFromFinding(
        Guid tenantId,
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        Guid complianceObligationId,
        Guid findingId,
        string findingNumber,
        string deficiencyCode,
        string title,
        string? description,
        DeficiencyType deficiencyType,
        DeficiencySeverity severity,
        ObjectiveElementClassification maturityLevel,
        string? riskCategory,
        string? assignedTo = null,
        DateTime? dueDate = null,
        string? createdBy = null,
        DateTime? discoveryDate = null)
    {
        var now = DateTime.UtcNow;
        var deficiency = new ComplianceDeficiency
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = organizationId,
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            ObjectiveElementId = objectiveElementId,
            ComplianceObligationId = complianceObligationId,
            DeficiencyCode = deficiencyCode,
            Title = title,
            Description = description,
            DeficiencyType = deficiencyType,
            Severity = severity,
            Status = DeficiencyStatus.Identified,
            MaturityLevel = maturityLevel,
            RiskCategory = riskCategory,
            Source = "AuditFinding",
            OriginType = DeficiencyOriginType.AuditFinding,
            LinkedSourceType = nameof(DeficiencyOriginType.AuditFinding),
            OriginId = findingId,
            OriginReference = findingNumber,
            PromotedAt = now,
            AssignedTo = assignedTo,
            DueDate = dueDate,
            DetectedDate = discoveryDate ?? now,
            LastEvaluatedDate = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
        };

        deficiency.AddDomainEvent(new DeficiencyIdentifiedDomainEvent
        {
            DeficiencyId = deficiency.Id,
            ObligationId = complianceObligationId,
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            DeficiencyType = deficiencyType,
            Severity = severity,
        });

        return deficiency;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    /// <summary>
    /// Closed / Cancelled CAPA (deficiency) records are immutable for field patches and most mutations.
    /// </summary>
    public void EnsureMutable()
    {
        if (Status is DeficiencyStatus.Closed or DeficiencyStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Cannot modify closed or cancelled CAPA/deficiency {Id} (status: {Status}).");
        }
    }

    /// <summary>
    /// Transitions to a new status. Raises <see cref="DeficiencyStatusChangedDomainEvent"/>.
    /// Closed status must use <see cref="Close"/> instead.
    /// </summary>
    public void TransitionStatus(DeficiencyStatus newStatus, string? updatedBy = null)
    {
        if (Status == DeficiencyStatus.Closed || Status == DeficiencyStatus.Cancelled)
            throw new InvalidOperationException($"Cannot transition a terminal deficiency {Id}.");
        if (newStatus == DeficiencyStatus.Closed)
            throw new InvalidOperationException("Use Close() to close a deficiency.");
        if (newStatus == DeficiencyStatus.Cancelled)
            throw new InvalidOperationException("Use Cancel() to cancel a deficiency.");
        if (!DeficiencyLifecycleTransitions.CanTransition(Status, newStatus))
            throw new InvalidOperationException($"Invalid lifecycle transition {Status} → {newStatus}.");

        var previous = Status;
        Status    = newStatus;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;

        if (newStatus == DeficiencyStatus.Verified && !ActualResolutionDate.HasValue)
        {
            ActualResolutionDate = DateTime.UtcNow;
        }

        AddDomainEvent(new DeficiencyStatusChangedDomainEvent
        {
            DeficiencyId    = Id,
            HospitalId      = HospitalId,
            FrameworkId     = FrameworkId,
            PreviousStatus  = previous,
            NewStatus       = newStatus,
            ChangedBy       = updatedBy,
        });
    }

    /// <summary>
    /// Assigns the deficiency to a person with an optional due date.
    /// Also transitions status to <see cref="DeficiencyStatus.Assigned"/> if currently Identified.
    /// </summary>
    public void Assign(string assignedTo, DateTime? dueDate = null, string? updatedBy = null, string? department = null)
    {
        EnsureMutable();
        var ownerChanged = AssignedTo != assignedTo;
        AssignedTo = assignedTo;
        DueDate = dueDate;
        TargetResolutionDate ??= dueDate;
        if (department != null)
        {
            Department = department;
        }

        if (!AssignedDate.HasValue && !string.IsNullOrWhiteSpace(assignedTo))
        {
            AssignedDate = DateTime.UtcNow;
        }

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;

        if (Status == DeficiencyStatus.Identified)
        {
            TransitionStatus(DeficiencyStatus.Assigned, updatedBy);
        }
        else if (ownerChanged && Status == DeficiencyStatus.Assigned)
        {
            AssignedDate = DateTime.UtcNow;
        }
    }

    public void Cancel(string? cancelledBy = null)
    {
        if (Status == DeficiencyStatus.Closed || Status == DeficiencyStatus.Cancelled)
        {
            return;
        }

        var previous = Status;
        Status = DeficiencyStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = cancelledBy;

        AddDomainEvent(new DeficiencyStatusChangedDomainEvent
        {
            DeficiencyId = Id,
            HospitalId = HospitalId,
            FrameworkId = FrameworkId,
            PreviousStatus = previous,
            NewStatus = DeficiencyStatus.Cancelled,
            ChangedBy = cancelledBy,
        });
    }

    /// <summary>
    /// Owner accepts CAPA ownership. When status is Assigned, transitions to InProgress.
    /// </summary>
    public void AcceptOwnership(string acceptedBy)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(acceptedBy))
            throw new ArgumentException("acceptedBy is required.", nameof(acceptedBy));

        OwnerAcceptedAt = DateTime.UtcNow;
        OwnerAcceptedBy = acceptedBy.Trim();
        OwnerAcceptanceRejected = false;
        OwnerRejectedAt = null;
        OwnerRejectedBy = null;
        OwnershipRejectionReason = null;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = acceptedBy;

        if (Status == DeficiencyStatus.Assigned)
        {
            TransitionStatus(DeficiencyStatus.InProgress, acceptedBy);
        }
    }

    /// <summary>
    /// Owner rejects CAPA ownership. Clears acceptance; leaves assignment in place.
    /// </summary>
    public void RejectOwnership(string rejectedBy, string? reason)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(rejectedBy))
            throw new ArgumentException("rejectedBy is required.", nameof(rejectedBy));

        OwnerAcceptedAt = null;
        OwnerAcceptedBy = null;
        OwnerAcceptanceRejected = true;
        OwnerRejectedAt = DateTime.UtcNow;
        OwnerRejectedBy = rejectedBy.Trim();
        OwnershipRejectionReason = reason;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = rejectedBy;
    }

    public void AssignCapaNumber(string capaNumber)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(capaNumber))
            throw new ArgumentException("capaNumber is required.", nameof(capaNumber));

        if (!string.IsNullOrWhiteSpace(CapaNumber))
            throw new InvalidOperationException($"Deficiency {Id} already has CAPA number {CapaNumber}.");

        CapaNumber = capaNumber.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLinkedSourceType(string? linkedSourceType, string? updatedBy = null)
    {
        LinkedSourceType = linkedSourceType;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Validates CAPA close gates. Throws <see cref="InvalidOperationException"/> when close is not allowed.
    /// </summary>
    public void EnsureCloseable()
    {
        var blocked = CloseBlockedReason();
        if (blocked is not null)
        {
            throw new InvalidOperationException(blocked);
        }
    }

    /// <summary>
    /// Non-throwing counterpart to <see cref="EnsureCloseable"/> (DEF-M5-001).
    ///
    /// <para>
    /// Callers that <i>propose</i> a close — background evaluators reacting to a rule that stopped
    /// firing — must ask before acting, because for them "not yet closeable" is an ordinary state,
    /// not an error. Callers that <i>command</i> a close on an operator's behalf keep using
    /// <see cref="EnsureCloseable"/>, so the operator is told exactly which gate stopped them.
    /// </para>
    ///
    /// <para>
    /// Both read the same gates from <see cref="CloseBlockedReason"/>: one rule set, two ways to
    /// ask. Nothing here relaxes the policy — a deficiency still closes only once it is Verified.
    /// </para>
    /// </summary>
    public bool CanClose() => CloseBlockedReason() is null;

    /// <summary>
    /// The single definition of the CAPA close gates: returns the reason closure is blocked, or
    /// <see langword="null"/> when the deficiency may close.
    /// </summary>
    private string? CloseBlockedReason()
    {
        if (Status != DeficiencyStatus.Verified)
        {
            return $"Deficiency {Id} cannot be closed until status is Verified (current: {Status}).";
        }

        if (HasCapaActivity() && CorrectiveActionStatus != CapaActionStatus.Completed)
        {
            return $"Deficiency {Id} cannot be closed until corrective action is Completed (current: {CorrectiveActionStatus}).";
        }

        if (PreventiveActionStatus != CapaActionStatus.NotStarted
            && PreventiveActionStatus != CapaActionStatus.Completed)
        {
            return $"Deficiency {Id} cannot be closed until preventive action is Completed (current: {PreventiveActionStatus}).";
        }

        if (EffectivenessReviewStatus is not (
            EffectivenessReviewStatus.Effective
            or EffectivenessReviewStatus.NotEffective
            or EffectivenessReviewStatus.NotRequired))
        {
            return $"Deficiency {Id} cannot be closed until effectiveness review is Effective, NotEffective, or NotRequired (current: {EffectivenessReviewStatus}).";
        }

        return null;
    }

    /// <summary>True when any CAPA field indicates CAPA work has started.</summary>
    public bool HasCapaActivity() =>
        !string.IsNullOrWhiteSpace(CapaNumber)
        || CorrectiveActionStatus != CapaActionStatus.NotStarted
        || PreventiveActionStatus != CapaActionStatus.NotStarted
        || !string.IsNullOrWhiteSpace(CorrectiveActionSummary)
        || !string.IsNullOrWhiteSpace(PreventiveActionSummary)
        || CorrectiveActionStartDate.HasValue
        || CorrectiveActionTargetDate.HasValue
        || CorrectiveActionCompletedDate.HasValue
        || PreventiveActionTargetDate.HasValue
        || PreventiveActionCompletedDate.HasValue;

    /// <summary>
    /// Closes the deficiency and raises <see cref="DeficiencyClosedDomainEvent"/>.
    /// </summary>
    public void Close(string? closedBy = null)
    {
        if (Status == DeficiencyStatus.Closed) return;

        EnsureCloseable();

        Status = DeficiencyStatus.Closed;
        ClosedDate = DateTime.UtcNow;
        ActualResolutionDate ??= DateTime.UtcNow;
        CurrentSlaStatus = SlaStatus.Closed;
        CurrentEscalationState = CurrentEscalationState is EscalationState.Acknowledged or EscalationState.Escalated
            ? EscalationState.Resolved
            : CurrentEscalationState;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = closedBy ?? "evaluation-engine";

        AddDomainEvent(new DeficiencyClosedDomainEvent
        {
            DeficiencyId   = Id,
            HospitalId     = HospitalId,
            FrameworkId    = FrameworkId,
            DeficiencyType = DeficiencyType,
            ClosedBy       = closedBy,
        });
    }

    /// <summary>
    /// Updates evaluation metadata and recalculates severity without raising a new Identified event.
    /// </summary>
    public void UpdateEvaluation(DeficiencySeverity newSeverity, string? updatedBy = null)
    {
        LastEvaluatedDate = DateTime.UtcNow;
        UpdatedAt         = DateTime.UtcNow;
        UpdatedBy         = updatedBy;

        if (Severity != newSeverity)
        {
            Severity = newSeverity;
        }
    }

    /// <summary>Manual root-cause classification (Sprint 6 H5).</summary>
    public void SetRootCause(RootCauseCategory rootCause, string? updatedBy = null)
    {
        EnsureMutable();
        RootCauseCategory = rootCause;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Sprint 8 — update RCA workspace and 5 Whys (manual entry).</summary>
    public void UpdateRca(
        RootCauseCategory? rootCause,
        string? problemStatement,
        string? rootCauseSummary,
        string? impactDescription,
        string? correctiveActionSummary,
        string? preventiveActionSummary,
        string? why1,
        string? why2,
        string? why3,
        string? why4,
        string? why5,
        string? finalRootCause,
        string? updatedBy = null)
    {
        EnsureMutable();
        if (rootCause.HasValue)
        {
            RootCauseCategory = rootCause;
        }

        ProblemStatement = problemStatement;
        RootCauseSummary = rootCauseSummary;
        ImpactDescription = impactDescription;
        CorrectiveActionSummary = correctiveActionSummary;
        PreventiveActionSummary = preventiveActionSummary;
        Why1 = why1;
        Why2 = why2;
        Why3 = why3;
        Why4 = why4;
        Why5 = why5;
        FinalRootCause = finalRootCause;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Sprint 8 — likelihood × impact risk matrix.</summary>
    public void UpdateRiskAssessment(int? likelihood, int? impact, string? updatedBy = null)
    {
        EnsureMutable();
        if (likelihood is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(likelihood), "Likelihood must be between 1 and 5.");
        }

        if (impact is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(impact), "Impact must be between 1 and 5.");
        }

        RiskLikelihood = likelihood;
        RiskImpact = impact;
        if (likelihood.HasValue && impact.HasValue)
        {
            RiskScore = likelihood.Value * impact.Value;
            AssessedRiskLevel = DeficiencyLifecycleTransitions.ComputeRiskLevel(likelihood.Value, impact.Value);
        }
        else
        {
            RiskScore = null;
            AssessedRiskLevel = null;
        }

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Sprint 8 — remediation planning fields (not CAPA workflow).</summary>
    public void UpdateRemediationPlan(
        string? actionPlan,
        string? expectedOutcome,
        DateTime? targetCompletion,
        string? successCriteria,
        string? updatedBy = null)
    {
        EnsureMutable();
        ActionPlan = actionPlan;
        ExpectedOutcome = expectedOutcome;
        RemediationTargetCompletion = targetCompletion;
        SuccessCriteria = successCriteria;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SetEffectivenessReviewStatus(EffectivenessReviewStatus status, string? updatedBy = null)
    {
        EnsureMutable();
        EffectivenessReviewStatus = status;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Sprint 11 — formal corrective action tracking.</summary>
    public void UpdateCorrectiveAction(
        string? summary,
        string? owner,
        DateTime? startDate,
        DateTime? targetDate,
        DateTime? completedDate,
        CapaActionStatus? status,
        string? updatedBy = null)
    {
        EnsureMutable();
        if (summary != null)
        {
            CorrectiveActionSummary = summary;
        }

        CorrectiveActionOwner = owner;
        CorrectiveActionStartDate = startDate;
        CorrectiveActionTargetDate = targetDate;
        CorrectiveActionCompletedDate = completedDate;

        if (status.HasValue)
        {
            CorrectiveActionStatus = status.Value;
        }
        else if (completedDate.HasValue)
        {
            CorrectiveActionStatus = CapaActionStatus.Completed;
        }
        else if (startDate.HasValue || !string.IsNullOrWhiteSpace(summary))
        {
            CorrectiveActionStatus = CapaActionStatus.InProgress;
        }

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Sprint 11 — formal preventive action tracking.</summary>
    public void UpdatePreventiveAction(
        string? summary,
        string? owner,
        DateTime? targetDate,
        DateTime? completedDate,
        string? successCriteria,
        CapaActionStatus? status,
        string? updatedBy = null)
    {
        EnsureMutable();
        if (summary != null)
        {
            PreventiveActionSummary = summary;
        }

        PreventiveActionOwner = owner;
        PreventiveActionTargetDate = targetDate;
        PreventiveActionCompletedDate = completedDate;
        PreventiveActionSuccessCriteria = successCriteria;

        if (status.HasValue)
        {
            PreventiveActionStatus = status.Value;
        }
        else if (completedDate.HasValue)
        {
            PreventiveActionStatus = CapaActionStatus.Completed;
        }
        else if (!string.IsNullOrWhiteSpace(summary))
        {
            PreventiveActionStatus = CapaActionStatus.InProgress;
        }

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Sprint 11 — effectiveness review activation.</summary>
    public void UpdateEffectivenessReview(
        EffectivenessReviewStatus? status,
        DateTime? scheduledDate,
        int? daysOffset,
        DateTime? reviewDate,
        string? reviewer,
        string? notes,
        string? outcome,
        string? updatedBy = null)
    {
        EnsureMutable();
        if (status.HasValue)
        {
            EffectivenessReviewStatus = status.Value;
        }

        EffectivenessReviewScheduledDate = scheduledDate;
        EffectivenessReviewDaysOffset = daysOffset;
        EffectivenessReviewDate = reviewDate;
        EffectivenessReviewer = reviewer;
        EffectivenessReviewNotes = notes;
        EffectivenessReviewOutcome = outcome;

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void MarkRecurrence(bool isRecurring, string? reason, string? updatedBy = null)
    {
        IsRecurringIssue = isRecurring;
        RecurrenceDetectedAt = isRecurring ? DateTime.UtcNow : null;
        RecurrenceReason = reason;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void UpdateOwnershipMetadata(string? department, DateTime? targetResolutionDate, string? updatedBy = null)
    {
        if (department != null)
        {
            Department = department;
        }

        if (targetResolutionDate.HasValue)
        {
            TargetResolutionDate = targetResolutionDate;
            DueDate = targetResolutionDate;
        }

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Sprint 10 — attach SLA policy and start clock.</summary>
    public void AssignSla(Guid policyId, DateTime startDate, DateTime dueDate)
    {
        SlaPolicyId = policyId;
        SlaStartDate = startDate;
        SlaDueDate = dueDate;
        CurrentSlaStatus = SlaStatus.OnTrack;
        TargetResolutionDate ??= dueDate;
        DueDate ??= dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSlaStatus(SlaStatus status)
    {
        CurrentSlaStatus = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEscalationState(int? level, EscalationState? state)
    {
        CurrentEscalationLevel = level;
        CurrentEscalationState = state;
        UpdatedAt = DateTime.UtcNow;
    }
}
