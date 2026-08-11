using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public class AccessGovernanceProxyHandlers
{
    public class ListRolesHandler(IProxyService proxy) : IRequestHandler<ListRolesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListRolesQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/roles?page={request.Page}&pageSize={request.PageSize}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class GetRoleByIdHandler(IProxyService proxy) : IRequestHandler<GetRoleByIdQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/roles/{request.RoleId}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class CreateRoleHandler(IProxyService proxy) : IRequestHandler<CreateRoleCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/roles", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class AssignRoleHandler(IProxyService proxy) : IRequestHandler<AssignRoleCommand, IActionResult>
    {
        public Task<IActionResult> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/roles/users/{request.UserId}/assign/{request.RoleId}", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class GrantPermissionHandler(IProxyService proxy) : IRequestHandler<GrantPermissionToRoleCommand, IActionResult>
    {
        public Task<IActionResult> Handle(GrantPermissionToRoleCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/roles/{request.RoleId}/permissions/{request.PermissionId}", HttpMethod.Post, cancellationToken: cancellationToken);
    }

    public class ListPermissionsHandler(IProxyService proxy) : IRequestHandler<ListPermissionsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListPermissionsQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/permissions", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class CreatePermissionHandler(IProxyService proxy) : IRequestHandler<CreatePermissionCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/permissions", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class ListPoliciesHandler(IProxyService proxy) : IRequestHandler<ListPoliciesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListPoliciesQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/policies", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class CreatePolicyHandler(IProxyService proxy) : IRequestHandler<CreatePolicyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreatePolicyCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/policies", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class PublishPolicyHandler(IProxyService proxy) : IRequestHandler<PublishPolicyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(PublishPolicyCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/policies/{request.PolicyId}/publish", HttpMethod.Post, cancellationToken: cancellationToken);
    }

    public class GetPolicyByIdHandler(IProxyService proxy) : IRequestHandler<GetPolicyByIdQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetPolicyByIdQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/policies/{request.PolicyId}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class UpdatePolicyHandler(IProxyService proxy) : IRequestHandler<UpdatePolicyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdatePolicyCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/policies/{request.PolicyId}", HttpMethod.Put, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class ArchivePolicyHandler(IProxyService proxy) : IRequestHandler<ArchivePolicyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(ArchivePolicyCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/policies/{request.PolicyId}/archive", HttpMethod.Post, cancellationToken: cancellationToken);
    }

    public class ListRoleAssignmentsHandler(IProxyService proxy) : IRequestHandler<ListRoleAssignmentsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListRoleAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var qs = request.UserId.HasValue ? $"?userId={request.UserId}" : string.Empty;
            return proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/role-assignments{qs}", HttpMethod.Get, cancellationToken: cancellationToken);
        }
    }

    public class AssignRoleAssignmentHandler(IProxyService proxy) : IRequestHandler<AssignRoleAssignmentCommand, IActionResult>
    {
        public Task<IActionResult> Handle(AssignRoleAssignmentCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/role-assignments", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class RevokeRoleAssignmentHandler(IProxyService proxy) : IRequestHandler<RevokeRoleAssignmentCommand, IActionResult>
    {
        public Task<IActionResult> Handle(RevokeRoleAssignmentCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/role-assignments/{request.AssignmentId}/revoke", HttpMethod.Post, cancellationToken: cancellationToken);
    }

    public class ListDelegationsHandler(IProxyService proxy) : IRequestHandler<ListDelegationsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListDelegationsQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/delegations", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class CreateDelegationHandler(IProxyService proxy) : IRequestHandler<CreateDelegationCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateDelegationCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/delegations", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class RevokeDelegationHandler(IProxyService proxy) : IRequestHandler<RevokeDelegationCommand, IActionResult>
    {
        public Task<IActionResult> Handle(RevokeDelegationCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/delegations/{request.DelegationId}/revoke", HttpMethod.Post, cancellationToken: cancellationToken);
    }

    public class ListResponsibilitiesHandler(IProxyService proxy) : IRequestHandler<ListResponsibilitiesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListResponsibilitiesQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/responsibilities", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class AssignResponsibilityHandler(IProxyService proxy) : IRequestHandler<AssignResponsibilityCommand, IActionResult>
    {
        public Task<IActionResult> Handle(AssignResponsibilityCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/responsibilities", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class ListApprovalAuthoritiesHandler(IProxyService proxy) : IRequestHandler<ListApprovalAuthoritiesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListApprovalAuthoritiesQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/approval-authorities", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class SetApprovalAuthorityHandler(IProxyService proxy) : IRequestHandler<SetApprovalAuthorityCommand, IActionResult>
    {
        public Task<IActionResult> Handle(SetApprovalAuthorityCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/approval-authorities", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class EvaluateAuthorizationHandler(IProxyService proxy) : IRequestHandler<EvaluateAuthorizationCommand, IActionResult>
    {
        public Task<IActionResult> Handle(EvaluateAuthorizationCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/authorization/evaluate", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class GetEffectivePermissionsHandler(IProxyService proxy) : IRequestHandler<GetEffectivePermissionsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetEffectivePermissionsQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/authorization/effective-permissions?userId={request.UserId}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class GetLookupsHandler(IProxyService proxy) : IRequestHandler<GetAccessGovernanceLookupsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetAccessGovernanceLookupsQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToAccessGovernanceServiceAsync($"/api/v{request.ApiVersion}/access-governance/lookups/{Uri.EscapeDataString(request.Category)}", HttpMethod.Get, cancellationToken: cancellationToken);
    }
}
