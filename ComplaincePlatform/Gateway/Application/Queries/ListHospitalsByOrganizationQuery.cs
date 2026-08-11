using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Queries;

public sealed record ListHospitalsByOrganizationQuery(Guid OrganizationId)
    : IRequest<IReadOnlyList<HospitalResponseDto>>;
