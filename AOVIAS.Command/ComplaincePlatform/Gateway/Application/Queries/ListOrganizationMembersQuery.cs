using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Queries;

public sealed record ListOrganizationMembersQuery(Guid OrganizationId) : IRequest<List<OrganizationMemberResponseDto>>;
