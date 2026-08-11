using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

/// <summary>
/// Command to transition a compliance journey to a new lifecycle state via ComplianceService.
/// </summary>
public class TransitionJourneyCommand : IRequest<IActionResult>
{
    public Guid JourneyId { get; }
    public object RequestBody { get; }
    public string ApiVersion { get; }

    public TransitionJourneyCommand(Guid journeyId, object requestBody, string apiVersion = "1.0")
    {
        JourneyId = journeyId;
        RequestBody = requestBody;
        ApiVersion = apiVersion;
    }
}

