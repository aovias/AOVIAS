using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Queries;

/// <summary>
/// Query for retrieving program overview snapshot by hospital ID, optionally filtered by framework ID.
/// </summary>
public class GetProgramOverviewSnapshotQuery : IRequest<DashboardSnapshotDto>
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }

    public GetProgramOverviewSnapshotQuery(Guid hospitalId, Guid? frameworkId = null)
    {
        HospitalId = hospitalId;
        FrameworkId = frameworkId;
    }
}
