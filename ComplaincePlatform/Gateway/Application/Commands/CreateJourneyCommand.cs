using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

/// <summary>
/// Command to create a new compliance journey for a hospital+framework via ComplianceService.
/// </summary>
public class CreateJourneyCommand : IRequest<IActionResult>
{
    public Guid HospitalId { get; }
    public Guid FrameworkId { get; }
    public string ApiVersion { get; }

    public CreateJourneyCommand(Guid hospitalId, Guid frameworkId, string apiVersion)
    {
        HospitalId = hospitalId;
        FrameworkId = frameworkId;
        ApiVersion = apiVersion;
    }
}

