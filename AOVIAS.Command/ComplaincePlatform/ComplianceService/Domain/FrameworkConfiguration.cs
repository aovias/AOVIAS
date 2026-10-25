using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Framework-specific configuration that defines business rules for compliance journeys.
/// Prevents hardcoding of framework-specific values.
/// </summary>
public class FrameworkConfiguration : PlatformEntity
{
    public Guid FrameworkId { get; private set; }
    public ComplianceFramework Framework { get; private set; } = null!;

    /// <summary>
    /// Default validity period in months for certificates issued under this framework.
    /// </summary>
    public int DefaultValidityMonths { get; private set; }

    /// <summary>
    /// Minimum readiness score required before submission (0-100).
    /// </summary>
    public decimal MinimumReadinessScore { get; private set; }

    /// <summary>
    /// Number of days before expiry when renewal window opens.
    /// </summary>
    public int RenewalDaysBeforeExpiry { get; private set; }

    /// <summary>
    /// Number of days to wait before allowing reapplication after rejection.
    /// </summary>
    public int ReapplyWaitingDays { get; private set; }

    /// <summary>
    /// Number of days allowed to resolve major non-conformities.
    /// </summary>
    public int MajorNcResolutionDays { get; private set; }

    private FrameworkConfiguration() { }

    public FrameworkConfiguration(
        string platformId,
        Guid frameworkId,
        int defaultValidityMonths,
        decimal minimumReadinessScore,
        int renewalDaysBeforeExpiry = 90,
        int reapplyWaitingDays = 30,
        int majorNcResolutionDays = 60)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);
        Id = Guid.NewGuid();
        PlatformId = platformId.Trim();
        FrameworkId = frameworkId;
        DefaultValidityMonths = defaultValidityMonths;
        MinimumReadinessScore = minimumReadinessScore;
        RenewalDaysBeforeExpiry = renewalDaysBeforeExpiry;
        ReapplyWaitingDays = reapplyWaitingDays;
        MajorNcResolutionDays = majorNcResolutionDays;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateConfiguration(
        int? defaultValidityMonths = null,
        decimal? minimumReadinessScore = null,
        int? renewalDaysBeforeExpiry = null,
        int? reapplyWaitingDays = null,
        int? majorNcResolutionDays = null)
    {
        if (defaultValidityMonths.HasValue)
            DefaultValidityMonths = defaultValidityMonths.Value;
        if (minimumReadinessScore.HasValue)
            MinimumReadinessScore = minimumReadinessScore.Value;
        if (renewalDaysBeforeExpiry.HasValue)
            RenewalDaysBeforeExpiry = renewalDaysBeforeExpiry.Value;
        if (reapplyWaitingDays.HasValue)
            ReapplyWaitingDays = reapplyWaitingDays.Value;
        if (majorNcResolutionDays.HasValue)
            MajorNcResolutionDays = majorNcResolutionDays.Value;
    }
}
