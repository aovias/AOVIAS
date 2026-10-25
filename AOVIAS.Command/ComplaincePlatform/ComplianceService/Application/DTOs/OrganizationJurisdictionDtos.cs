namespace ComplianceService.Application.DTOs;

/// <summary>
/// Request DTO to update organization primary jurisdiction.
/// Mirrors Gateway.DTOs.UpdateJurisdictionRequestDto.
/// </summary>
public class UpdateOrganizationJurisdictionRequestDto
{
    public string Country { get; set; } = string.Empty;
    public string? Region { get; set; }
    public bool Force { get; set; }
}

/// <summary>
/// Simple jurisdiction DTO (country/region) for organization.
/// Mirrors Gateway.DTOs.JurisdictionDto.
/// </summary>
public class OrganizationJurisdictionDto
{
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for organization jurisdiction update.
/// Mirrors Gateway.DTOs.UpdateJurisdictionResponseDto.
/// </summary>
public class UpdateOrganizationJurisdictionResponseDto
{
    public OrganizationJurisdictionDto PrimaryJurisdiction { get; set; } = new();
    public bool MarketplaceCacheRefreshed { get; set; }
}

