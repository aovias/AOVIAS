using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class FindingComment : BaseEntity
{
    public Guid FindingId { get; set; }
    public Finding Finding { get; set; } = null!;
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
}
