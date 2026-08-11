using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

/// <summary>
/// Query to fetch journey details via ComplianceService.
/// </summary>
public class GetJourneyDetailsQuery : IRequest<IActionResult>
{
    public Guid JourneyId { get; }
    public string ApiVersion { get; }

    public GetJourneyDetailsQuery(Guid journeyId, string apiVersion)
    {
        JourneyId = journeyId;
        ApiVersion = apiVersion;
    }
}

