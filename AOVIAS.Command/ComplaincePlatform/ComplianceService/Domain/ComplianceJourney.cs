using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;
using OperationalMode = Shared.Infrastructure.Domain.Enums.OperationalMode;

namespace ComplianceService.Domain;

/// <summary>
/// Core entity representing a hospital's compliance journey for a specific framework.
/// Multi-tenant: Organization → Hospital → Framework
/// </summary>
public class ComplianceJourney : AggregateRoot
{
    // Identity
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid HospitalId { get; private set; }
    public Guid FrameworkId { get; private set; }

    // Status
    public ComplianceLifecycleState Status { get; private set; } = ComplianceLifecycleState.Draft;

    /// <summary>
    /// NABH operational phase for workspace/scoring (distinct from <see cref="Status"/> lifecycle).
    /// </summary>
    public OperationalMode OperationalMode { get; private set; } = OperationalMode.SelfAssessment;

    // Dates
    public DateTime? EnrollmentDate { get; private set; }
    public DateTime? SubmissionDate { get; private set; }
    public DateTime? ApprovalDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public DateTime? RenewalWindowStartDate { get; private set; }
    public DateTime? ReapplyAllowedDate { get; private set; }

    // Configuration
    public int ValidityPeriodInMonths { get; private set; }
    public decimal MinimumRequiredScore { get; private set; }

    // Audit metadata
    public DateTime? AuditDate { get; private set; }
    public string? AuditTeamDetails { get; private set; }
    public DateTime? CorrectiveActionDeadline { get; private set; }

    // Financials
    public Guid? EnrollmentPaymentId { get; private set; }
    public decimal? EnrollmentFeeAmount { get; private set; }
    public DateTime? EnrollmentFeePaidOn { get; private set; }

    // External References
    public string? ExternalReferenceId { get; private set; }
    public string? CertificateNumber { get; private set; }

    // Scores
    public decimal CurrentReadinessScore { get; private set; }
    public decimal RiskScore { get; private set; }

    // Flags
    public bool IsPublicCertificateVisible { get; private set; }
    public bool IsLocked { get; private set; }

    // Reasons
    public string? SuspendedReason { get; private set; }
    public string? RejectionReason { get; private set; }

    // Audit Trail
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Navigation Properties
    public ComplianceFramework? Framework { get; private set; }

    // Private constructor for EF Core
    private ComplianceJourney() { }

    public ComplianceJourney(
        Guid tenantId,
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        int validityPeriodInMonths,
        decimal minimumRequiredScore)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        OrganizationId = organizationId;
        HospitalId = hospitalId;
        FrameworkId = frameworkId;
        ValidityPeriodInMonths = validityPeriodInMonths;
        MinimumRequiredScore = minimumRequiredScore;
        Status = ComplianceLifecycleState.Draft;
        OperationalMode = OperationalMode.SelfAssessment;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Advances operational mode by exactly one step (0→1→…→4). No skipping or backward moves.
    /// </summary>
    public void SetOperationalMode(OperationalMode target)
    {
        if (target == OperationalMode)
        {
            return;
        }

        if ((int)target != (int)OperationalMode + 1)
        {
            throw new InvalidOperationException(
                $"Invalid operational mode transition from {OperationalMode} to {target}. Advance one step at a time.");
        }

        OperationalMode = target;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the compliance journey to a new state.
    /// Validates business rules, logs history, and raises domain events.
    /// </summary>
    public void TransitionTo(
        ComplianceLifecycleState newStatus,
        Guid changedBy,
        string? remarks = null,
        Dictionary<string, object>? additionalData = null)
    {
        // Validate transition
        if (!Shared.Infrastructure.Domain.ComplianceLifecycleStateMachine.IsValidTransition(Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Invalid state transition from {Status} to {newStatus}");
        }

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        // State-specific logic
        switch (newStatus)
        {
            case ComplianceLifecycleState.Enrolled:
                HandleEnrolledTransition();
                break;
            case ComplianceLifecycleState.GapAssessment:
                HandleGapAssessmentTransition();
                break;
            case ComplianceLifecycleState.InProgress:
                HandleInProgressTransition();
                break;
            case ComplianceLifecycleState.Submitted:
                HandleSubmittedTransition();
                break;
            case ComplianceLifecycleState.AuditScheduled:
                HandleAuditScheduledTransition();
                break;
            case ComplianceLifecycleState.AuditCompleted:
                HandleAuditCompletedTransition();
                break;
            case ComplianceLifecycleState.Approved:
                HandleApprovedTransition();
                break;
            case ComplianceLifecycleState.Rejected:
                HandleRejectedTransition(remarks);
                break;
            case ComplianceLifecycleState.Suspended:
                HandleSuspendedTransition(remarks);
                break;
            case ComplianceLifecycleState.Expired:
                HandleExpiredTransition();
                break;
            case ComplianceLifecycleState.RenewalInProgress:
                HandleRenewalInProgressTransition();
                break;
            case ComplianceLifecycleState.Renewed:
                HandleRenewedTransition();
                break;
        }

        // Raise domain event for history tracking (will be handled by repository)
        // The actual history record will be created by the application layer
    }

    private void HandleEnrolledTransition()
    {
        EnrollmentDate = DateTime.UtcNow;
        AddDomainEvent(new ComplianceEnrolledDomainEvent(
            TenantId,
            OrganizationId,
            HospitalId,
            FrameworkId,
            Id));
    }

    private void HandleGapAssessmentTransition()
    {
        // MinimumRequiredScore should already be set from framework configuration
        // No additional logic needed
    }

    private void HandleInProgressTransition()
    {
        // Unlock if previously locked
        if (IsLocked)
        {
            IsLocked = false;
        }
    }

    private void HandleSubmittedTransition()
    {
        if (CurrentReadinessScore < MinimumRequiredScore)
        {
            throw new InvalidOperationException(
                $"Cannot submit: ReadinessScore ({CurrentReadinessScore}) is below minimum required ({MinimumRequiredScore})");
        }

        SubmissionDate = DateTime.UtcNow;
        IsLocked = true;
        AddDomainEvent(new ComplianceSubmittedDomainEvent(
            TenantId,
            OrganizationId,
            HospitalId,
            FrameworkId,
            Id,
            CurrentReadinessScore));
    }

    private void HandleAuditScheduledTransition()
    {
        // Validation should be done in application layer (audit team assigned, date defined)
        AddDomainEvent(new ComplianceAuditScheduledDomainEvent(
            TenantId,
            OrganizationId,
            HospitalId,
            FrameworkId,
            Id));
    }

    private void HandleAuditCompletedTransition()
    {
        // Validation should be done in application layer (findings recorded)
        AddDomainEvent(new ComplianceAuditCompletedDomainEvent(
            TenantId,
            OrganizationId,
            HospitalId,
            FrameworkId,
            Id));
    }

    private void HandleApprovedTransition()
    {
        ApprovalDate = DateTime.UtcNow;
        ExpiryDate = ApprovalDate.Value.AddMonths(ValidityPeriodInMonths);
        RenewalWindowStartDate = ExpiryDate.Value.AddDays(-90);
        CertificateNumber = GenerateCertificateNumber();
        IsPublicCertificateVisible = true;
        IsLocked = true;
        AddDomainEvent(new ComplianceApprovedDomainEvent(
            TenantId,
            OrganizationId,
            HospitalId,
            FrameworkId,
            Id,
            CertificateNumber!,
            ExpiryDate.Value));
    }

    private void HandleRejectedTransition(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("RejectionReason is required when rejecting compliance");
        }

        RejectionReason = reason;
        IsLocked = false; // Allow reapplication
        AddDomainEvent(new ComplianceRejectedDomainEvent(
            TenantId,
            OrganizationId,
            HospitalId,
            FrameworkId,
            Id,
            reason));
    }

    private void HandleSuspendedTransition(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("SuspendedReason is required when suspending compliance");
        }

        SuspendedReason = reason;
        IsPublicCertificateVisible = false;
        AddDomainEvent(new ComplianceSuspendedDomainEvent(
            TenantId,
            OrganizationId,
            HospitalId,
            FrameworkId,
            Id,
            reason));
    }

    private void HandleExpiredTransition()
    {
        IsPublicCertificateVisible = false;
        AddDomainEvent(new ComplianceExpiredDomainEvent(
            TenantId,
            OrganizationId,
            HospitalId,
            FrameworkId,
            Id,
            ExpiryDate!.Value));
    }

    private void HandleRenewalInProgressTransition()
    {
        IsLocked = false; // Allow updates for renewal
        AddDomainEvent(new ComplianceRenewalInProgressDomainEvent(
            TenantId,
            OrganizationId,
            HospitalId,
            FrameworkId,
            Id));
    }

    private void HandleRenewedTransition()
    {
        var previousApprovalDate = ApprovalDate;
        ApprovalDate = DateTime.UtcNow;
        ExpiryDate = ApprovalDate.Value.AddMonths(ValidityPeriodInMonths);
        RenewalWindowStartDate = ExpiryDate.Value.AddDays(-90);
        CertificateNumber = GenerateCertificateNumber();
        IsPublicCertificateVisible = true;
        IsLocked = true;
        AddDomainEvent(new ComplianceRenewedDomainEvent(
            TenantId,
            OrganizationId,
            HospitalId,
            FrameworkId,
            Id,
            CertificateNumber!,
            ExpiryDate.Value,
            previousApprovalDate));
    }

    private string GenerateCertificateNumber()
    {
        // Format: FRM-{FrameworkCode}-{Year}-{Sequence}
        // For now, use GUID-based approach. In production, use sequence generator
        var frameworkCode = "FRM"; // Will be set from Framework navigation if available
        if (Framework != null && !string.IsNullOrEmpty(Framework.Name))
        {
            frameworkCode = Framework.Name.Length > 4 
                ? Framework.Name.Substring(0, 4).ToUpper() 
                : Framework.Name.ToUpper();
        }
        var year = DateTime.UtcNow.Year;
        var sequence = Id.ToString().Substring(0, 8).ToUpper();
        return $"{frameworkCode}-{year}-{sequence}";
    }

    // Business methods

    /// <summary>
    /// Obsolete. CurrentReadinessScore is no longer the source of truth for displayed compliance
    /// percentages; all screens now read live OE-based scores from IComplianceScoreService (the
    /// canonical single source of truth). Persisting a readiness score here reintroduces stale,
    /// divergent values and must not be used for display.
    /// </summary>
    [Obsolete("Compliance percentages are now computed live via IComplianceScoreService. Do not persist/read CurrentReadinessScore for display.")]
    public void UpdateReadinessScore(decimal score)
    {
        if (IsLocked)
        {
            throw new InvalidOperationException("Cannot update readiness score when journey is locked");
        }

        CurrentReadinessScore = score;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRiskScore(decimal score)
    {
        RiskScore = score;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates risk score based on current state and audit findings.
    /// Formula:
    /// - Expired: +30
    /// - Suspended: +50
    /// - Major NC unresolved: +20 each
    /// - Minor NC unresolved: +5 each
    /// </summary>
    public decimal CalculateRiskScore(
        int unresolvedMajorNcCount,
        int unresolvedMinorNcCount)
    {
        decimal riskScore = 0;

        // State-based risk
        if (Status == ComplianceLifecycleState.Expired)
        {
            riskScore += 30;
        }
        else if (Status == ComplianceLifecycleState.Suspended)
        {
            riskScore += 50;
        }

        // Audit finding-based risk
        riskScore += unresolvedMajorNcCount * 20;
        riskScore += unresolvedMinorNcCount * 5;

        return riskScore;
    }

    public void SetExternalReferenceId(string referenceId)
    {
        ExternalReferenceId = referenceId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetReapplyAllowedDate(DateTime date)
    {
        ReapplyAllowedDate = date;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Confirms that enrollment payment has been completed.
    /// This should be called after successful payment gateway confirmation.
    /// </summary>
    public void ConfirmEnrollmentPayment(Guid paymentId, decimal amount, DateTime paidOn)
    {
        if (paymentId == Guid.Empty)
        {
            throw new ArgumentException("PaymentId cannot be empty.", nameof(paymentId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Enrollment fee amount must be greater than zero.");
        }

        EnrollmentPaymentId = paymentId;
        EnrollmentFeeAmount = amount;
        EnrollmentFeePaidOn = paidOn;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Schedules an audit by setting the planned date and team details.
    /// Should be called before transitioning to AuditScheduled.
    /// </summary>
    public void ScheduleAudit(DateTime auditDate, string teamDetails)
    {
        if (auditDate == default)
        {
            throw new ArgumentException("Audit date must be specified.", nameof(auditDate));
        }

        if (string.IsNullOrWhiteSpace(teamDetails))
        {
            throw new ArgumentException("Audit team details must be provided.", nameof(teamDetails));
        }

        AuditDate = auditDate;
        AuditTeamDetails = teamDetails;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the global corrective action deadline for resolving audit findings.
    /// Should be called when audit is completed.
    /// </summary>
    public void SetCorrectiveActionDeadline(DateTime deadline)
    {
        if (deadline == default)
        {
            throw new ArgumentException("Corrective action deadline must be specified.", nameof(deadline));
        }

        CorrectiveActionDeadline = deadline;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Ensures that configuration-dependent fields are initialized from framework configuration
    /// when missing or zero (e.g., for existing journeys created before configuration was added).
    /// </summary>
    public void EnsureConfiguration(int defaultValidityMonths, decimal minimumRequiredScore)
    {
        if (ValidityPeriodInMonths <= 0)
        {
            ValidityPeriodInMonths = defaultValidityMonths;
        }

        if (MinimumRequiredScore <= 0)
        {
            MinimumRequiredScore = minimumRequiredScore;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the renewal window start date explicitly from framework configuration.
    /// </summary>
    public void SetRenewalWindowStartDate(DateTime renewalWindowStartDate)
    {
        RenewalWindowStartDate = renewalWindowStartDate;
        UpdatedAt = DateTime.UtcNow;
    }
}
