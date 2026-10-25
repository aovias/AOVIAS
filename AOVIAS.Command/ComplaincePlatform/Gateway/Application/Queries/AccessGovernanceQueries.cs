using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record ListRolesQuery(string ApiVersion, int Page, int PageSize) : IRequest<IActionResult>;
public record GetRoleByIdQuery(string ApiVersion, Guid RoleId) : IRequest<IActionResult>;
public record ListPermissionsQuery(string ApiVersion) : IRequest<IActionResult>;
public record ListPoliciesQuery(string ApiVersion) : IRequest<IActionResult>;
public record GetPolicyByIdQuery(string ApiVersion, Guid PolicyId) : IRequest<IActionResult>;
public record ListRoleAssignmentsQuery(string ApiVersion, Guid? UserId) : IRequest<IActionResult>;
public record ListDelegationsQuery(string ApiVersion) : IRequest<IActionResult>;
public record ListResponsibilitiesQuery(string ApiVersion) : IRequest<IActionResult>;
public record ListApprovalAuthoritiesQuery(string ApiVersion) : IRequest<IActionResult>;
public record GetEffectivePermissionsQuery(string ApiVersion, Guid UserId) : IRequest<IActionResult>;
public record GetAccessGovernanceLookupsQuery(string ApiVersion, string Category) : IRequest<IActionResult>;
