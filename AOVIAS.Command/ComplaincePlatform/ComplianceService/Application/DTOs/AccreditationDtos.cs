namespace ComplianceService.Application.DTOs;

/// <summary>
/// Accreditation summary row for listing endpoints.
/// Mirrors Gateway.DTOs.AccreditationSummaryDto.
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
/// Mirrors Gateway.DTOs.RenewalDto.
/// </summary>
public class RenewalDto
{
    public Guid AccreditationId { get; set; }
    public Guid FrameworkId { get; set; }
    public string FrameworkName { get; set; } = string.Empty;
    public DateTime RenewalDueOn { get; set; }
    public int DaysRemaining { get; set; }
}

