using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Audit review attachment — not compliance evidence (Sprint 7A).
/// </summary>
public class FindingAttachment : BaseEntity
{
    public Guid FindingId { get; set; }
    public Finding Finding { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public FindingAttachmentType AttachmentType { get; set; } = FindingAttachmentType.Document;
    public long? FileSizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
