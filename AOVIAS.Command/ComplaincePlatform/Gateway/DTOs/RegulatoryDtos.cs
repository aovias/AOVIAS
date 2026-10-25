namespace Gateway.DTOs;

/// <summary>
/// DTO representing organization-level regulatory overview.
/// </summary>
public class RegulatoryOverviewDto
{
    public int ActiveAccreditations { get; set; }
    public int UpcomingRenewals { get; set; }
    public RiskIndexDto RiskIndex { get; set; } = new();
    public JurisdictionDto PrimaryJurisdiction { get; set; } = new();
}

/// <summary>
/// Risk index details including classification and trend.
/// </summary>
public class RiskIndexDto
{
    public int Score { get; set; }
    public string Classification { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public int Delta { get; set; }
    public DateTime LastAssessedAt { get; set; }
}

/// <summary>
/// Jurisdiction information for an organization.
/// </summary>
public class JurisdictionDto
{
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}

/// <summary>
/// Request to update organization primary jurisdiction.
/// </summary>
public class UpdateJurisdictionRequestDto
{
    public string Country { get; set; } = string.Empty;
    public string? Region { get; set; }
    public bool Force { get; set; }
}

/// <summary>
/// Response for jurisdiction update operation.
/// </summary>
public class UpdateJurisdictionResponseDto
{
    public JurisdictionDto PrimaryJurisdiction { get; set; } = new();
    public bool MarketplaceCacheRefreshed { get; set; }
}

/// <summary>
/// Accreditation summary row for listing endpoints.
/// Includes RowVersion (as base64 string) for optimistic concurrency control.
/// Clients should include this value in If-Match header for Suspend/Archive operations.
/// </summary>
public class AccreditationSummaryDto
{
    public Guid Id { get; set; }
    public Guid FrameworkId { get; set; }
    public string FrameworkName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double CompliancePercentage { get; set; }
    public string RiskClassification { get; set; } = string.Empty;
    public DateTime? ExpiresOn { get; set; }
    /// <summary>
    /// RowVersion for optimistic concurrency control (base64 encoded byte array).
    /// Include this value in If-Match header when performing Suspend/Archive operations.
    /// </summary>
    public string? RowVersion { get; set; }
}

/// <summary>
/// Upcoming renewal DTO.
/// </summary>
public class RenewalDto
{
    public Guid AccreditationId { get; set; }
    public Guid FrameworkId { get; set; }
    public string FrameworkName { get; set; } = string.Empty;
    public DateTime RenewalDueOn { get; set; }
    public int DaysRemaining { get; set; }
}

/// <summary>
/// Page of framework catalog entries.
/// </summary>
public class FrameworkCatalogPageDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<FrameworkCatalogItemDto> Items { get; set; } = Array.Empty<FrameworkCatalogItemDto>();
}

/// <summary>
/// Framework catalog item including activation flags and estimates.
/// </summary>
public class FrameworkCatalogItemDto
{
    public Guid FrameworkId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string AccreditationBody { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsDeprecated { get; set; }
    public bool IsAlreadyActive { get; set; }
    public int EstimatedChapters { get; set; }
    public int EstimatedControls { get; set; }
}

/// <summary>
/// Activation metadata for a framework.
/// Downstream service computes required scope fields, default SLA config and risk defaults.
/// </summary>
public class ActivationMetadataDto
{
    public IReadOnlyList<string> RequiredScopeFields { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> DefaultSlaConfig { get; set; } = new();
    public Dictionary<string, object> RiskEngineDefaults { get; set; } = new();
}

/// <summary>
/// Request to activate an accreditation.
/// </summary>
public class ActivateAccreditationRequestDto
{
    public Guid FrameworkId { get; set; }
    public Guid ComplianceOfficer { get; set; }
    public IReadOnlyList<string> Departments { get; set; } = Array.Empty<string>();
    public Dictionary<string, object>? ScopeMetadata { get; set; }
}

/// <summary>
/// Response from activate accreditation.
/// </summary>
public class ActivateAccreditationResponseDto
{
    public Guid AccreditationId { get; set; }
    public string Status { get; set; } = "Activated";
}

/// <summary>
/// Request to suspend an accreditation.
/// </summary>
public class SuspendAccreditationRequestDto
{
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Optional current status of the accreditation as seen by the client.
    /// Used by Gateway as a fail-fast guard to prevent obviously invalid transitions
    /// (e.g., attempting to suspend an already Suspended/Archived accreditation).
    /// Source of truth remains ComplianceService - this is a client-side optimization.
    /// </summary>
    public string? CurrentStatus { get; set; }
}

/// <summary>
/// Response for accreditation lifecycle changes (suspend/archive).
/// Includes updated RowVersion for subsequent operations.
/// </summary>
public class AccreditationLifecycleChangeResponseDto
{
    public Guid AccreditationId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    /// <summary>
    /// Updated RowVersion after the lifecycle change (base64 encoded byte array).
    /// Use this value in If-Match header for subsequent operations.
    /// </summary>
    public string? RowVersion { get; set; }
}

/// <summary>
/// Response for risk recalculation trigger.
/// </summary>
public class RiskRecalculationResponseDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "Accepted";
}

