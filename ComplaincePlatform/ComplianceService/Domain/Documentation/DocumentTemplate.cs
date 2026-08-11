using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

public class DocumentTemplate : BaseEntity
{
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ControlledDocumentType DocumentType { get; set; } = ControlledDocumentType.Sop;
    public string ContentTemplate { get; set; } = string.Empty;
    /// <summary>JSON section schema for Authoring Studio (Sprint 15B).</summary>
    public string? SectionsSchemaJson { get; set; }
    public string? AuthoringSchemaKey { get; set; }
    public string? Description { get; set; }
    public bool IsBuiltIn { get; set; }
    public bool IsActive { get; set; } = true;
}
