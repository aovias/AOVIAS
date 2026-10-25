using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class IncidentAttachment : BaseEntity
{
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public Guid? UploadedByUserId { get; set; }
}
