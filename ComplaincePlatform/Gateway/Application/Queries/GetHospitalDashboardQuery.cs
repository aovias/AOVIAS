using Gateway.DTOs;
using MediatR;
using System;

namespace Gateway.Application.Queries;

/// <summary>
/// Query to request the hospital-specific dashboard data (department heatmap information).
/// </summary>
public sealed class GetHospitalDashboardQuery : IRequest<HospitalDashboardDto>
{
    public Guid HospitalId { get; }

    public GetHospitalDashboardQuery(Guid hospitalId)
    {
        HospitalId = hospitalId;
    }
}
