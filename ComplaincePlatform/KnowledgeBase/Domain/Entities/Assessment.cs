namespace KnowledgeBase.Domain.Entities;

public class Assessment
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public Guid FrameworkId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Framework Framework { get; set; } = null!;
    public ICollection<AssessmentItem> Items { get; set; } = new List<AssessmentItem>();
}
