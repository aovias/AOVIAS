using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

/// <summary>
/// Query for explaining a compliance task using AI.
/// </summary>
public class ExplainTaskQuery : IRequest<IActionResult>
{
    public object RequestBody { get; set; } = null!;
    public Guid HospitalId { get; set; }

    public ExplainTaskQuery(object requestBody, Guid hospitalId)
    {
        RequestBody = requestBody;
        HospitalId = hospitalId;
    }
}
