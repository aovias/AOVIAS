using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class ComplianceTaskComment : BaseEntity
{
    public Guid TaskId { get; set; }
    public ComplianceTask Task { get; set; } = null!;

    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }

    public Guid AuthorUserId { get; set; }

    public string Body { get; set; } = string.Empty;
}
