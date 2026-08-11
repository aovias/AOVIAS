using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class AuditLog : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid PerformedBy { get; set; }
    public DateTime Timestamp { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

