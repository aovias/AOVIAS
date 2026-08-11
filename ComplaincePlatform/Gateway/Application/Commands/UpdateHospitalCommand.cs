using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Commands;

public sealed record UpdateHospitalCommand(Guid HospitalId, UpdateHospitalRequestDto Request) : IRequest<HospitalResponseDto>;

