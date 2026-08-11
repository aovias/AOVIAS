namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO for a page of framework catalog entries for an organization.
/// Mirrors the shape expected by Gateway.DTOs.FrameworkCatalogPageDto.
/// </summary>
public class FrameworkCatalogPageDto
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyList<FrameworkCatalogItemDto> Items { get; init; } = Array.Empty<FrameworkCatalogItemDto>();
}

/// <summary>
/// DTO for a single framework catalog item.
/// </summary>
public class FrameworkCatalogItemDto
{
    public Guid FrameworkId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string AccreditationBody { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsDeprecated { get; init; }
    public bool IsAlreadyActive { get; init; }
    public int EstimatedChapters { get; init; }
    public int EstimatedControls { get; init; }
}

