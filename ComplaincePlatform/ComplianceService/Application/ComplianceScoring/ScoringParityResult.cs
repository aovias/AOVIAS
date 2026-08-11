namespace ComplianceService.Application.ComplianceScoring;

public sealed class ScoringParityResult
{
    public Guid HospitalId { get; init; }
    public Guid FrameworkId { get; init; }
    public Guid? JourneyId { get; init; }
    public decimal OldCompliancePercent { get; init; }
    public decimal NewCompliancePercent { get; init; }
    public decimal Delta { get; init; }
    public bool OldPass { get; init; }
    public bool NewPass { get; init; }
    public string Category { get; init; } = "acceptable";
    public IReadOnlyList<string> Mismatches { get; init; } = [];
}
