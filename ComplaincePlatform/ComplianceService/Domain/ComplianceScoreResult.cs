namespace ComplianceService.Domain
{
    public record ComplianceScoreResult(
        decimal OverallScore,
        decimal AuditConfidence,
        IReadOnlyCollection<ChapterScoreResult> ChapterScores
    );
}
