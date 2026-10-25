using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.DTOs;

public class JourneyDetailsDto
{
    public Guid JourneyId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    
    /// <summary>
    /// Framework name (e.g., "NABH", "NABL")
    /// </summary>
    public string FrameworkName { get; set; } = string.Empty;

    /// <summary>
    /// Current lifecycle state as string (e.g., "UnderReview", "Approved")
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;
    
    /// <summary>
    /// Current lifecycle state enum (for backward compatibility)
    /// </summary>
    public ComplianceLifecycleState CurrentStatus { get; set; }
    
    public List<ComplianceLifecycleState> ValidNextStates { get; set; } = new();

    /// <summary>
    /// Journey start date (enrollment date)
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// Target accreditation/approval date
    /// </summary>
    public DateTime? TargetAccreditationDate { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>
    /// Overall progress percentage (0-100)
    /// </summary>
    public decimal ProgressPercentage { get; set; }
    
    /// <summary>
    /// Readiness score (0-100)
    /// </summary>
    public decimal ReadinessScore { get; set; }
    
    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public decimal RiskScore { get; set; }
    
    /// <summary>
    /// Risk index (aggregated risk metric)
    /// </summary>
    public decimal RiskIndex { get; set; }

    public bool? SurveillanceDue { get; set; }
    public bool RenewalWindowOpen { get; set; }
}

