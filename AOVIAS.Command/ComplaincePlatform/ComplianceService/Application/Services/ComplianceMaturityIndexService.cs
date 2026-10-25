using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Services;

/// <summary>
/// Calculates the Compliance Maturity Index (CMI) from per-stage scores.
///
/// Formula:
///   CMI = (CoreScore × 0.40) + (CommitmentScore × 0.30) + (AchievementScore × 0.20) + (ExcellenceScore × 0.10)
///
/// All inputs are in the range 0–100. The result is rounded to two decimal places.
/// </summary>
public sealed class ComplianceMaturityIndexService
{
    private const decimal CoreWeight       = 0.40m;
    private const decimal CommitmentWeight = 0.30m;
    private const decimal AchievementWeight = 0.20m;
    private const decimal ExcellenceWeight = 0.10m;

    /// <summary>
    /// Calculates the CMI given four stage scores (0–100 each).
    /// </summary>
    /// <param name="coreScore">Core compliance readiness score (0–100).</param>
    /// <param name="commitmentScore">Commitment compliance readiness score (0–100).</param>
    /// <param name="achievementScore">Achievement compliance readiness score (0–100).</param>
    /// <param name="excellenceScore">Excellence compliance readiness score (0–100).</param>
    /// <returns>A fully populated <see cref="ComplianceMaturityIndexDto"/>.</returns>
    public ComplianceMaturityIndexDto Calculate(
        decimal coreScore,
        decimal commitmentScore,
        decimal achievementScore,
        decimal excellenceScore)
    {
        var cmiValue =
            (Clamp(coreScore)       * CoreWeight) +
            (Clamp(commitmentScore) * CommitmentWeight) +
            (Clamp(achievementScore) * AchievementWeight) +
            (Clamp(excellenceScore) * ExcellenceWeight);

        return new ComplianceMaturityIndexDto
        {
            Value             = Math.Round(cmiValue,           2),
            CoreScore         = Math.Round(coreScore,          2),
            CommitmentScore   = Math.Round(commitmentScore,    2),
            AchievementScore  = Math.Round(achievementScore,   2),
            ExcellenceScore   = Math.Round(excellenceScore,    2),
        };
    }

    private static decimal Clamp(decimal value) =>
        Math.Max(0m, Math.Min(100m, value));
}
