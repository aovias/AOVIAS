using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Queries;

public sealed record GetOrganizationMemberQuery(Guid MemberId) : IRequest<OrganizationMemberResponseDto>;
