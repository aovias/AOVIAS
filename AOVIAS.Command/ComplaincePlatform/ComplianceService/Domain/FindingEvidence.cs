using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class FindingEvidence : BaseEntity
{
    public Guid FindingId { get; set; }
    public Finding Finding { get; set; } = null!;

    public Guid EvidenceId { get; set; }
    public Evidence Evidence { get; set; } = null!;
}
