using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

/// <summary>
/// Immutable document version — content is never overwritten.
/// </summary>
public class DocumentVersion : BaseEntity
{
    public Guid ControlledDocumentId { get; set; }
    public ControlledDocument ControlledDocument { get; set; } = null!;

    public int VersionNumber { get; set; } = 1;
    public string VersionLabel { get; set; } = "1.0";
    public string Content { get; set; } = string.Empty;
    /// <summary>Authoring schema key (sop, policy, form, committee-minutes).</summary>
    public string? AuthoringSchemaKey { get; set; }
    public string? ChangeSummary { get; set; }
    public ControlledDocumentStatus StatusAtCreation { get; set; } = ControlledDocumentStatus.Draft;

    public bool IsCurrent { get; set; }
    public bool IsSuperseded { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }

    public ICollection<DocumentApproval> Approvals { get; set; } = new List<DocumentApproval>();
    public ICollection<DocumentSection> Sections { get; set; } = new List<DocumentSection>();
}
