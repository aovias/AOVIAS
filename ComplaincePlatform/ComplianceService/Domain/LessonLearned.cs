using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Searchable organizational knowledge from closed incidents.</summary>
public class LessonLearned : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid SourceIncidentId { get; set; }
    public Incident? SourceIncident { get; set; }

    public string LessonNumber { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string? DepartmentsJson { get; set; }
    public string? RelatedSopsJson { get; set; }
    public string? TrainingNeeded { get; set; }
    public string? PreventiveActions { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
}
