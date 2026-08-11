using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Commands;

public sealed record UpdateOrganizationMemberCommand(Guid MemberId, UpdateOrganizationMemberRequestDto Request) : IRequest<OrganizationMemberResponseDto>;
