using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record ListIdentityUsersAdminQuery(
    string ApiVersion,
    Guid OrganizationId,
    Guid ActorUserId,
    int Page,
    int PageSize,
    string? Search,
    string? Role,
    Guid? DepartmentId,
    string? MembershipStatus,
    bool IncludeInactive) : IRequest<IActionResult>;

public record GetIdentityUserAdminQuery(string ApiVersion, Guid OrganizationId, Guid UserId, Guid ActorUserId) : IRequest<IActionResult>;
