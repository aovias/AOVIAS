using MediatR;
using Platform.Dashboard.Models;

namespace Gateway.Application.Queries;

public class GetDashboardExperienceQuery : IRequest<DashboardExperienceResult>
{
    public Guid HospitalId { get; }

    public Guid? FrameworkId { get; }

    public bool ForceRefresh { get; }

    public GetDashboardExperienceQuery(Guid hospitalId, Guid? frameworkId = null, bool forceRefresh = false)
    {
        HospitalId = hospitalId;
        FrameworkId = frameworkId;
        ForceRefresh = forceRefresh;
    }
}
