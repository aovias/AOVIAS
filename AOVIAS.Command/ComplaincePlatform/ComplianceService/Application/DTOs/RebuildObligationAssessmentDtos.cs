namespace ComplianceService.Application.DTOs;

/// <summary>
/// Phase 8 validation snapshot: obligation and deficiency counts after a rebuild.
/// Returned by GET /internal/v1/obligations/validate.
/// </summary>
public sealed class ObligationValidationReport
{
    public Guid OrganizationId   { get; init; }
    public Guid FrameworkId      { get; init; }
    public int  TotalObligations { get; init; }

    // Obligation implementation distribution
    public int  Implemented          { get; init; }
    public int  PartiallyImplemented { get; init; }
    public int  NotImplemented       { get; init; }
    public int  NotStarted           { get; init; }

    // Assessment status
    public int  Verified { get; init; }
    public int  Passed   { get; init; }
    public int  Failed   { get; init; }

    public decimal OverallReadinessScore { get; init; }

    /// <summary>True when at least one obligation has been moved out of the default NotAssessed state.</summary>
    public bool HasAssessmentData { get; init; }

    // Deficiency summary
    public int  DeficiencyTotal    { get; init; }
    public int  DeficiencyCritical { get; init; }
    public int  DeficiencyMajor    { get; init; }
    public int  DeficiencyModerate { get; init; }
    public int  DeficiencyMinor    { get; init; }

    public DateTime ValidatedAt { get; init; }
}

/// <summary>
/// Detailed result for a single hospital × framework rebuild pass.
/// </summary>
public sealed class HospitalFrameworkRebuildResult
{
    public Guid   HospitalId    { get; init; }
    public Guid   OrganizationId { get; init; }
    public Guid   FrameworkId   { get; init; }
    public bool   Succeeded     { get; init; }
    public int    ObligationsUpdated { get; init; }
    public int    AnswersProcessed   { get; init; }
    public bool   SnapshotQueued     { get; init; }
    public string? ErrorMessage  { get; init; }
}

/// <summary>
/// Aggregated report returned by <c>RebuildObligationAssessmentCommandHandler</c>.
/// </summary>
public sealed class RebuildObligationAssessmentReport
{
    public bool   DryRun              { get; init; }
    public int    TotalHospitals      { get; init; }
    public int    ProcessedHospitals  { get; init; }
    public int    SucceededHospitals  { get; init; }
    public int    FailedHospitals     { get; init; }
    public int    TotalObligationsUpdated { get; init; }
    public int    TotalAnswersProcessed   { get; init; }
    public DateTime RebuiltAt         { get; init; }
    public IReadOnlyList<HospitalFrameworkRebuildResult> Results { get; init; }
        = Array.Empty<HospitalFrameworkRebuildResult>();
}
