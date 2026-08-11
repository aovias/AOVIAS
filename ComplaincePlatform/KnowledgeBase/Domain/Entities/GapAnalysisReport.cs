namespace KnowledgeBase.Domain.Entities;

public class GapAnalysisReport
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid FacilityId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public decimal OverallPercent { get; set; }
    public bool MeetsOverallPercent { get; set; }
    public bool CoreRulePassed { get; set; }
    public bool ChapterAveragesPassed { get; set; }
    public bool StandardAveragesPassed { get; set; }
    public bool CommitmentLevelPassed { get; set; }
    public int CriticalCount { get; set; }
    public int MajorCount { get; set; }
    public int MinorCount { get; set; }
    public int CompliantCount { get; set; }

    public Assessment Assessment { get; set; } = null!;
    public ICollection<GapAnalysisItem> Items { get; set; } = new List<GapAnalysisItem>();
}
