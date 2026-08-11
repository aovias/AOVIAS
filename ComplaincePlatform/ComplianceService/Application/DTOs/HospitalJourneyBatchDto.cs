namespace ComplianceService.Application.DTOs;

/// <summary>
/// Request DTO for batch journey lookup by hospital IDs.
/// Used by Gateway to enrich organization-detail response.
/// </summary>
public class HospitalJourneyBatchRequest
{
    public List<Guid> HospitalIds { get; set; } = new();
}

/// <summary>
/// Response item: one hospital's journeys.
/// </summary>
public class HospitalJourneyBatchItemDto
{
    public Guid HospitalId { get; set; }
    public List<HospitalJourneyItemDto> Journeys { get; set; } = new();
}

/// <summary>
/// Single journey entry for the batch response.
/// </summary>
public class HospitalJourneyItemDto
{
    public Guid JourneyId { get; set; }

    /// <summary>
    /// The framework this journey enrolls in. Distinct from <see cref="JourneyId"/>: a journey is a
    /// hospital's enrollment in a framework, and audit/compliance APIs filter by framework, not by
    /// journey. Clients previously had only <see cref="JourneyId"/> and were forced to send it as
    /// <c>frameworkId</c>, which silently matched nothing.
    /// </summary>
    public Guid FrameworkId { get; set; }

    public string FrameworkCode { get; set; } = string.Empty;
    public string FrameworkName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
