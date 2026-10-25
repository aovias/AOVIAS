using AccessGovernanceService.Application.Contracts;
using AccessGovernanceService.Application.Services;
using MediatR;

namespace AccessGovernanceService.Application.Queries;

public record ListRolesQuery(int Page, int PageSize) : IRequest<PagedResultDto<RoleListItemDto>>;
public record GetRoleByIdQuery(Guid Id) : IRequest<RoleDto?>;
public record ListPermissionsQuery(Guid? PermissionGroupValueId) : IRequest<IReadOnlyList<PermissionDto>>;
public record ListPoliciesQuery() : IRequest<IReadOnlyList<PolicyDto>>;
public record GetPolicyByIdQuery(Guid Id) : IRequest<PolicyDetailDto?>;
public record ListDelegationsQuery() : IRequest<IReadOnlyList<DelegationDto>>;
public record ListRoleAssignmentsQuery(Guid? UserId) : IRequest<IReadOnlyList<RoleAssignmentDto>>;
public record ListResponsibilitiesQuery() : IRequest<IReadOnlyList<ResponsibilityDto>>;
public record ListApprovalAuthoritiesQuery() : IRequest<IReadOnlyList<ApprovalAuthorityDto>>;
public record GetEffectivePermissionsQuery(Guid UserId) : IRequest<IReadOnlyList<EffectivePermissionDto>>;
public record GetLookupsQuery(string Category) : IRequest<IReadOnlyList<LookupValueDto>>;

public class ListRolesHandler(IRoleAppService svc) : IRequestHandler<ListRolesQuery, PagedResultDto<RoleListItemDto>>
{
    public Task<PagedResultDto<RoleListItemDto>> Handle(ListRolesQuery request, CancellationToken ct) => svc.ListAsync(request.Page, request.PageSize, ct);
}
public class GetRoleByIdHandler(IRoleAppService svc) : IRequestHandler<GetRoleByIdQuery, RoleDto?>
{
    public Task<RoleDto?> Handle(GetRoleByIdQuery request, CancellationToken ct) => svc.GetByIdAsync(request.Id, ct);
}
public class ListPermissionsHandler(IPermissionAppService svc) : IRequestHandler<ListPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    public Task<IReadOnlyList<PermissionDto>> Handle(ListPermissionsQuery request, CancellationToken ct) => svc.ListAsync(request.PermissionGroupValueId, ct);
}
public class ListPoliciesHandler(IPolicyAppService svc) : IRequestHandler<ListPoliciesQuery, IReadOnlyList<PolicyDto>>
{
    public Task<IReadOnlyList<PolicyDto>> Handle(ListPoliciesQuery request, CancellationToken ct) => svc.ListAsync(ct);
}
public class GetPolicyByIdHandler(IPolicyAppService svc) : IRequestHandler<GetPolicyByIdQuery, PolicyDetailDto?>
{
    public Task<PolicyDetailDto?> Handle(GetPolicyByIdQuery request, CancellationToken ct) => svc.GetByIdAsync(request.Id, ct);
}
public class ListDelegationsHandler(IDelegationAppService svc) : IRequestHandler<ListDelegationsQuery, IReadOnlyList<DelegationDto>>
{
    public Task<IReadOnlyList<DelegationDto>> Handle(ListDelegationsQuery request, CancellationToken ct) => svc.ListAsync(ct);
}
public class ListRoleAssignmentsHandler(IRoleAppService svc) : IRequestHandler<ListRoleAssignmentsQuery, IReadOnlyList<RoleAssignmentDto>>
{
    public Task<IReadOnlyList<RoleAssignmentDto>> Handle(ListRoleAssignmentsQuery request, CancellationToken ct) => svc.ListAssignmentsAsync(request.UserId, ct);
}
public class ListResponsibilitiesHandler(IResponsibilityAppService svc) : IRequestHandler<ListResponsibilitiesQuery, IReadOnlyList<ResponsibilityDto>>
{
    public Task<IReadOnlyList<ResponsibilityDto>> Handle(ListResponsibilitiesQuery request, CancellationToken ct) => svc.ListAsync(ct);
}
public class ListApprovalAuthoritiesHandler(IApprovalAuthorityAppService svc) : IRequestHandler<ListApprovalAuthoritiesQuery, IReadOnlyList<ApprovalAuthorityDto>>
{
    public Task<IReadOnlyList<ApprovalAuthorityDto>> Handle(ListApprovalAuthoritiesQuery request, CancellationToken ct) => svc.ListAsync(ct);
}
public class GetEffectivePermissionsHandler(IAuthorizationEvaluatorService svc) : IRequestHandler<GetEffectivePermissionsQuery, IReadOnlyList<EffectivePermissionDto>>
{
    public Task<IReadOnlyList<EffectivePermissionDto>> Handle(GetEffectivePermissionsQuery request, CancellationToken ct) => svc.GetEffectivePermissionsDetailedAsync(request.UserId, ct);
}
public class GetLookupsHandler(LookupAppService svc) : IRequestHandler<GetLookupsQuery, IReadOnlyList<LookupValueDto>>
{
    public Task<IReadOnlyList<LookupValueDto>> Handle(GetLookupsQuery request, CancellationToken ct) => svc.GetByCategoryAsync(request.Category, ct);
}
