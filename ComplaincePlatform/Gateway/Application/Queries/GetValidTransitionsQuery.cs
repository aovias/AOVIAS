using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

/// <summary>
/// Query to fetch valid next lifecycle transitions for a journey via ComplianceService.
/// </summary>
public class GetValidTransitionsQuery : IRequest<IActionResult>
{
    public Guid JourneyId { get; }
    public string ApiVersion { get; }

    public GetValidTransitionsQuery(Guid journeyId, string apiVersion)
    {
        JourneyId = journeyId;
        ApiVersion = apiVersion;
    }
}

