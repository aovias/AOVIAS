using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Reporting;

/// <summary>
/// Builds publishing reliability metrics for Sprint 8D dashboards.
/// </summary>
public static class PublishingReliabilityReportBuilder
{
    public static PublishingReliabilityReport Build(
        PublishingSession session,
        PublishingPipelineState pipelineState,
        PublishingContext context,
        GoldenPackageComparisonResult? goldenComparison,
        bool packageIntegrityVerified,
        bool deterministicMatched)
    {
        var stageResults = pipelineState.Stages.ToList();
        var failed = stageResults.Count(stage => stage.State == PublishingStageState.Failed);
        var total = stageResults.Count;
        var successRate = total == 0 ? 0 : (total - failed) * 100d / total;
        var average = total == 0
            ? TimeSpan.Zero
            : TimeSpan.FromMilliseconds(stageResults.Average(stage => stage.Duration.TotalMilliseconds));

        return new PublishingReliabilityReport
        {
            SessionId = session.SessionId,
            Success = pipelineState.IsSuccessful,
            SuccessRate = successRate,
            AverageStageDuration = average,
            StageFailureCount = failed,
            RetryCount = context.TotalRetryCount,
            RecoveryEventCount = context.RecoveryEventCount,
            PackageIntegrityVerified = packageIntegrityVerified,
            DeterministicFingerprintMatched = deterministicMatched,
            RegressionStatusPassed = goldenComparison?.RequiresApproval != true,
            GoldenComparison = goldenComparison,
            StageResults = stageResults
        };
    }
}
