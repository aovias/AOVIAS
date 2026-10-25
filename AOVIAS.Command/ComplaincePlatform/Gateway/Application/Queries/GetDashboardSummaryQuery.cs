using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Queries;

/// <summary>
/// Query for retrieving dashboard summary.
/// </summary>
public class GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>
{
    public Guid HospitalId { get; set; }

    public GetDashboardSummaryQuery(Guid hospitalId)
    {
        HospitalId = hospitalId;
    }
}
