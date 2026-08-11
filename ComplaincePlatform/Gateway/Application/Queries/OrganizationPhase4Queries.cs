using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record ListMembershipProxyQuery(
    string ApiVersion,
    Guid? OrganizationId,
    Guid? UserId,
    int Page,
    int PageSize,
    string? Search = null,
    string? Role = null,
    Guid? DepartmentId = null,
    string? MembershipStatus = null,
    bool IncludeInactive = false) : IRequest<IActionResult>;
public record GetOrganizationTreeProxyQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public record GetDepartmentTreeProxyQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId) : IRequest<IActionResult>;
public record GetOrganizationGraphProxyQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public record GetMembershipHistoryProxyQuery(string ApiVersion, Guid MembershipId) : IRequest<IActionResult>;
public record GetOrganizationalUnitTreeProxyQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId, bool IncludeInactive = false) : IRequest<IActionResult>;
public record GetOrganizationalUnitTypesProxyQuery(string ApiVersion) : IRequest<IActionResult>;
