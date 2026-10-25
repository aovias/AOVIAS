using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

/// <summary>
/// Cross-reference from authoring content to policies, evidence, OEs, findings, CAPAs.
/// </summary>
public class DocumentCrossReference : BaseEntity
{
    public Guid ControlledDocumentId { get; set; }
    public ControlledDocument ControlledDocument { get; set; } = null!;
    public Guid? SectionId { get; set; }
    public string? BlockId { get; set; }

    public DocumentCrossReferenceType ReferenceType { get; set; }
    public Guid TargetId { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
    public string? TargetRoute { get; set; }
}
