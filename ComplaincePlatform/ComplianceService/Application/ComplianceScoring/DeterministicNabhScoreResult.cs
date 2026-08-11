namespace ComplianceService.Application.ComplianceScoring;

public sealed class DeterministicNabhScoreResult
{
    public Guid HospitalId { get; init; }
    public Guid FrameworkId { get; init; }
    public Guid? JourneyId { get; init; }
    public decimal CompliancePercent { get; init; }
    public bool Pass { get; init; }
    public decimal RiskScore { get; init; }
    public IReadOnlyList<DeterministicChapterScore> Chapters { get; init; } = [];
}

public sealed class DeterministicChapterScore
{
    public Guid ChapterId { get; init; }
    public decimal CompliancePercent { get; init; }
    public bool Pass { get; init; }
    public IReadOnlyList<DeterministicStandardScore> Standards { get; init; } = [];
}

public sealed class DeterministicStandardScore
{
    public Guid StandardId { get; init; }
    public decimal CompliancePercent { get; init; }
    public bool Pass { get; init; }
    public int MandatoryNonCompliantCount { get; init; }
}
