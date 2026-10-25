namespace ComplianceService.Domain
{
    public record ChapterScoreResult(
        Guid ChapterId,
        decimal ChapterScore,
        int CompletedTasks,
        int TotalTasks,
        decimal RiskPenaltyApplied
    );
}
