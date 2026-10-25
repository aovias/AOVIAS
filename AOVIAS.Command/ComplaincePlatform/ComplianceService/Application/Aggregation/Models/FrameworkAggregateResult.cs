namespace ComplianceService.Application.Aggregation.Models
{
    public class FrameworkAggregateResult
    {
        public Guid FrameworkId { get; set; }
        public decimal AverageScore { get; set; }          // 1–5
        public decimal CompliancePercent { get; set; }     // 0–100
        public int ChapterCount { get; set; }
        public int NaExcludedCount { get; set; }
        public List<ChapterAggregateResult> Chapters { get; set; } = new();
    }
}

