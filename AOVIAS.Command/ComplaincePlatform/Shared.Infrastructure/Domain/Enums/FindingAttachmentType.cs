namespace Shared.Infrastructure.Domain.Enums;

/// <summary>Finding attachment types — audit review artifacts, not compliance evidence.</summary>
public enum FindingAttachmentType
{
    Document = 1,
    Image = 2,
    AuditNote = 3,
    Report = 4,
    Screenshot = 5,
}
