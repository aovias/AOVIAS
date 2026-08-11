using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Commands;

public sealed record CreateOrganizationMemberCommand(CreateOrganizationMemberRequestDto Request) : IRequest<OrganizationMemberResponseDto>;
