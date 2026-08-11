using MediatR;
using OrganizationService.Application.Contracts;

namespace OrganizationService.Application.Queries;

public record ListMembershipQuery(
    Guid? OrganizationId,
    Guid? UserId,
    int Page,
    int PageSize,
    string? Search = null,
    string? Role = null,
    Guid? DepartmentId = null,
    string? MembershipStatus = null,
    bool IncludeInactive = false) : IRequest<PagedResultDto<MembershipDto>>;
public record GetMembershipHistoryQuery(Guid MembershipId) : IRequest<IReadOnlyList<MembershipHistoryDto>>;
public record GetOrganizationTreeQuery(Guid OrganizationId) : IRequest<OrganizationTreeNodeDto?>;
public record GetDepartmentTreeQuery(Guid OrganizationId, Guid? HospitalId) : IRequest<IReadOnlyList<DepartmentTreeNodeDto>>;
public record GetOrganizationalUnitTreeQuery(Guid OrganizationId, Guid? HospitalId, bool IncludeInactive = false) : IRequest<IReadOnlyList<OrganizationalUnitDto>>;
public record GetOrganizationalUnitTypesQuery() : IRequest<IReadOnlyList<OrganizationalUnitTypeDto>>;
public record GetOrganizationGraphQuery(Guid OrganizationId) : IRequest<OrganizationGraphDto>;
