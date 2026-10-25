namespace KnowledgeBase.Domain.Entities;

public class GapAnalysisItem
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public Guid NodeId { get; set; }
    public string NodeCode { get; set; } = string.Empty;
    public string NodeTitle { get; set; } = string.Empty;
    public Guid? ChapterId { get; set; }
    public Guid? StandardId { get; set; }
    public int Score { get; set; }
    public string ComplianceStatus { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;

    public GapAnalysisReport Report { get; set; } = null!;
    public ComplianceNode Node { get; set; } = null!;
}
