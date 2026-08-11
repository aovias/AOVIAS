namespace ComplianceService.Domain.Enums;

/// <summary>
/// Catalog lifecycle for a compliance framework version (not Framework Studio).
/// </summary>
public enum FrameworkCatalogStatus
{
    Draft = 0,
    Published = 1,
    Active = 2,
    Retired = 3,
    Archived = 4
}
