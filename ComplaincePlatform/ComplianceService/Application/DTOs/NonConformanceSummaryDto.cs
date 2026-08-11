namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO for non-conformance summary by severity.
/// </summary>
public class NonConformanceSummaryDto
{
    /// <summary>
    /// Total count of non-conformances
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Count of major non-conformances
    /// </summary>
    public int MajorCount { get; set; }
    
    /// <summary>
    /// Count of minor non-conformances
    /// </summary>
    public int MinorCount { get; set; }
    
    /// <summary>
    /// Count of observations
    /// </summary>
    public int ObservationCount { get; set; }
    
    /// <summary>
    /// Count of opportunities for improvement (OFI)
    /// </summary>
    public int OFICount { get; set; }
    
    /// <summary>
    /// Count of unresolved non-conformances
    /// </summary>
    public int UnresolvedCount { get; set; }
}
