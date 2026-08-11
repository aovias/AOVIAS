using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Commands;

public sealed record CreateHospitalCommand(CreateHospitalRequestDto Request) : IRequest<HospitalResponseDto>;

