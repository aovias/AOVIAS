namespace Shared.Infrastructure.DTO;

/// <summary>
/// DTO for framework information returned by query APIs.
/// </summary>
public class FrameworkDto
{
    public Guid FrameworkId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Issuing or accreditation body (e.g. QCI); mirrors <c>ComplianceFramework.Authority</c>.
    /// </summary>
    public string Authority { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int ValidityPeriodMonths { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Framework version label (e.g. 5th Edition / Entry Level).</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Catalog lifecycle status name (Draft, Published, Active, Retired, Archived).</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime? PublishedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
}
