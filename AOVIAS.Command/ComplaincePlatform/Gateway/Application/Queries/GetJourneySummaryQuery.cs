using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Queries;

/// <summary>
/// Query for retrieving journey summary by hospital ID, optionally filtered by framework ID.
/// </summary>
public class GetJourneySummaryQuery : IRequest<JourneySummaryDto?>
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string ApiVersion { get; set; }

    public GetJourneySummaryQuery(Guid hospitalId, Guid? frameworkId = null, string apiVersion = "1.0")
    {
        HospitalId = hospitalId;
        FrameworkId = frameworkId;
        ApiVersion = apiVersion;
    }
}
