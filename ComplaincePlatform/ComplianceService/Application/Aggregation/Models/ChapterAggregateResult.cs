namespace ComplianceService.Application.Aggregation.Models
{
    public class ChapterAggregateResult
    {
        public Guid ChapterId { get; set; }
        public decimal AverageScore { get; set; }          // 1–5
        public decimal CompliancePercent { get; set; }     // 0–100
        public int StandardCount { get; set; }
        public int NaExcludedCount { get; set; }
        public List<ComplianceStandardAggregateResult> Standards { get; set; } = new();
    }
}

