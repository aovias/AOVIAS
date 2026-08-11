using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public class OrganizationPhase4ProxyHandlers
{
    public class ListMembershipHandler(IProxyService proxy) : IRequestHandler<ListMembershipProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListMembershipProxyQuery request, CancellationToken ct)
        {
            var q = $"/api/v{request.ApiVersion}/membership?page={request.Page}&pageSize={request.PageSize}";
            if (request.OrganizationId.HasValue) q += $"&organizationId={request.OrganizationId}";
            if (request.UserId.HasValue) q += $"&userId={request.UserId}";
            if (!string.IsNullOrWhiteSpace(request.Search)) q += $"&search={Uri.EscapeDataString(request.Search)}";
            if (!string.IsNullOrWhiteSpace(request.Role)) q += $"&role={Uri.EscapeDataString(request.Role)}";
            if (request.DepartmentId.HasValue) q += $"&departmentId={request.DepartmentId}";
            if (!string.IsNullOrWhiteSpace(request.MembershipStatus)) q += $"&membershipStatus={Uri.EscapeDataString(request.MembershipStatus)}";
            if (request.IncludeInactive) q += "&includeInactive=true";
            return proxy.ProxyToOrganizationServiceAsync(q, HttpMethod.Get, cancellationToken: ct);
        }
    }

    public class CreateMembershipHandler(IProxyService proxy) : IRequestHandler<CreateMembershipProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateMembershipProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/membership", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class UpdateMembershipHandler(IProxyService proxy) : IRequestHandler<UpdateMembershipProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateMembershipProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/membership/{request.Id}", HttpMethod.Put, request.RequestBody, cancellationToken: ct);
    }

    public class TransferMembershipHandler(IProxyService proxy) : IRequestHandler<TransferMembershipProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(TransferMembershipProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/membership/transfer", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class GetMembershipHistoryHandler(IProxyService proxy) : IRequestHandler<GetMembershipHistoryProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetMembershipHistoryProxyQuery request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/membership/history?membershipId={request.MembershipId}", HttpMethod.Get, cancellationToken: ct);
    }

    public class GetOrganizationTreeHandler(IProxyService proxy) : IRequestHandler<GetOrganizationTreeProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetOrganizationTreeProxyQuery request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/organizations/tree?organizationId={request.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public class GetDepartmentTreeHandler(IProxyService proxy) : IRequestHandler<GetDepartmentTreeProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetDepartmentTreeProxyQuery request, CancellationToken ct)
        {
            var q = $"/api/v{request.ApiVersion}/departments/tree?organizationId={request.OrganizationId}";
            if (request.HospitalId.HasValue) q += $"&hospitalId={request.HospitalId}";
            return proxy.ProxyToOrganizationServiceAsync(q, HttpMethod.Get, cancellationToken: ct);
        }
    }

    public class GetOrganizationGraphHandler(IProxyService proxy) : IRequestHandler<GetOrganizationGraphProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetOrganizationGraphProxyQuery request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/organization/graph?organizationId={request.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public class CreateInvitationHandler(IProxyService proxy) : IRequestHandler<CreateInvitationProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateInvitationProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/invitations", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class AcceptInvitationHandler(IProxyService proxy) : IRequestHandler<AcceptInvitationProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(AcceptInvitationProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/invitations/{request.InvitationId}/accept", HttpMethod.Post, cancellationToken: ct);
    }

    public class GetOrganizationalUnitTreeHandler(IProxyService proxy) : IRequestHandler<GetOrganizationalUnitTreeProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetOrganizationalUnitTreeProxyQuery request, CancellationToken ct)
        {
            var q = $"/api/v{request.ApiVersion}/organizational-units/tree?organizationId={request.OrganizationId}&includeInactive={request.IncludeInactive}";
            if (request.HospitalId.HasValue) q += $"&hospitalId={request.HospitalId}";
            return proxy.ProxyToOrganizationServiceAsync(q, HttpMethod.Get, cancellationToken: ct);
        }
    }

    public class GetOrganizationalUnitTypesHandler(IProxyService proxy) : IRequestHandler<GetOrganizationalUnitTypesProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetOrganizationalUnitTypesProxyQuery request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/organizational-units/types", HttpMethod.Get, cancellationToken: ct);
    }

    public class CreateOrganizationalUnitHandler(IProxyService proxy) : IRequestHandler<CreateOrganizationalUnitProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateOrganizationalUnitProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/organizational-units", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class UpdateOrganizationalUnitHandler(IProxyService proxy) : IRequestHandler<UpdateOrganizationalUnitProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateOrganizationalUnitProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/organizational-units/{request.Id}", HttpMethod.Put, request.RequestBody, cancellationToken: ct);
    }

    public class MoveOrganizationalUnitHandler(IProxyService proxy) : IRequestHandler<MoveOrganizationalUnitProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(MoveOrganizationalUnitProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/organizational-units/{request.Id}/move", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class SetOrganizationalUnitStatusHandler(IProxyService proxy) : IRequestHandler<SetOrganizationalUnitStatusProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(SetOrganizationalUnitStatusProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/organizational-units/{request.Id}/{request.Action}", HttpMethod.Post, cancellationToken: ct);
    }

    public class DeleteOrganizationalUnitHandler(IProxyService proxy) : IRequestHandler<DeleteOrganizationalUnitProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(DeleteOrganizationalUnitProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/organizational-units/{request.Id}", HttpMethod.Delete, cancellationToken: ct);
    }

    public class CreateOrgDepartmentHandler(IProxyService proxy) : IRequestHandler<CreateOrgDepartmentProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateOrgDepartmentProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/departments", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }
}
