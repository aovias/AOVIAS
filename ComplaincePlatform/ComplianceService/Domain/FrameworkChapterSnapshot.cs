using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Level 2 Snapshot: Chapter-level compliance breakdown for a framework.
/// Part of the FrameworkComplianceSnapshot read model.
/// </summary>
public class FrameworkChapterSnapshot : BaseEntity
{
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid ChapterId { get; set; }
    
    public decimal ChapterScore { get; set; }
    public int CompletedTasks { get; set; }
    public int TotalTasks { get; set; }
    public decimal RiskPenalty { get; set; }
    
    public DateTime CalculatedAt { get; set; }
}
