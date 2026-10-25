using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Queries;

public sealed record GetHospitalQuery(Guid HospitalId) : IRequest<HospitalResponseDto>;

