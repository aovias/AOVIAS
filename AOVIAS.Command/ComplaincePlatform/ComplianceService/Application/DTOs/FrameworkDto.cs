namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO for framework information returned by query APIs (including Phase 1 catalog lifecycle).
/// </summary>
public class FrameworkDto
{
    /// <summary>Canonical id (legacy property name used by older clients).</summary>
    public Guid Id
    {
        get => FrameworkId;
        set => FrameworkId = value;
    }

    /// <summary>Catalog / lifecycle APIs prefer this name.</summary>
    public Guid FrameworkId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int? ValidityPeriodMonths { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Draft | Published | Active | Retired | Archived</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime? PublishedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
}
