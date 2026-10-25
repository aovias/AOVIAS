using MediatR;
using OrganizationService.Application.Commands;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.Queries;

namespace OrganizationService.Application.Handlers;

public class CreateMembershipHandler(IMembershipAppService svc) : IRequestHandler<CreateMembershipCommand, MembershipDto>
{
    public Task<MembershipDto> Handle(CreateMembershipCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, request.ActorUserId, ct);
}

public class UpdateMembershipHandler(IMembershipAppService svc) : IRequestHandler<UpdateMembershipCommand, MembershipDto?>
{
    public Task<MembershipDto?> Handle(UpdateMembershipCommand request, CancellationToken ct) => svc.UpdateAsync(request.Id, request.Request, request.ActorUserId, ct);
}

public class ListMembershipHandler(IMembershipAppService svc) : IRequestHandler<ListMembershipQuery, PagedResultDto<MembershipDto>>
{
    public Task<PagedResultDto<MembershipDto>> Handle(ListMembershipQuery request, CancellationToken ct) =>
        svc.ListAsync(request.OrganizationId, request.UserId, request.Page, request.PageSize, request.Search, request.Role, request.DepartmentId, request.MembershipStatus, request.IncludeInactive, ct);
}

public class GetMembershipHistoryHandler(IMembershipAppService svc) : IRequestHandler<GetMembershipHistoryQuery, IReadOnlyList<MembershipHistoryDto>>
{
    public Task<IReadOnlyList<MembershipHistoryDto>> Handle(GetMembershipHistoryQuery request, CancellationToken ct) => svc.GetHistoryAsync(request.MembershipId, ct);
}

public class TransferMembershipHandler(IMembershipAppService svc) : IRequestHandler<TransferMembershipCommand, MembershipDto>
{
    public Task<MembershipDto> Handle(TransferMembershipCommand request, CancellationToken ct) => svc.TransferAsync(request.Request, request.ActorUserId, ct);
}

public class CreateInvitationHandler(IInvitationAppService svc) : IRequestHandler<CreateInvitationCommand, InvitationDto>
{
    public Task<InvitationDto> Handle(CreateInvitationCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, request.ActorUserId, ct);
}

public class BulkCreateInvitationsHandler(IInvitationAppService svc) : IRequestHandler<BulkCreateInvitationsCommand, IReadOnlyList<InvitationDto>>
{
    public Task<IReadOnlyList<InvitationDto>> Handle(BulkCreateInvitationsCommand request, CancellationToken ct)
        => svc.BulkCreateAsync(request.Request, request.ActorUserId, ct);
}

public class ListInvitationsHandler(IInvitationAppService svc) : IRequestHandler<ListInvitationsQuery, PagedResultDto<InvitationDto>>
{
    public Task<PagedResultDto<InvitationDto>> Handle(ListInvitationsQuery request, CancellationToken ct)
        => svc.ListAsync(request.OrganizationId, request.Status, request.Page, request.PageSize, ct);
}

public class AcceptInvitationHandler(IInvitationAppService svc) : IRequestHandler<AcceptInvitationCommand, MembershipDto>
{
    public Task<MembershipDto> Handle(AcceptInvitationCommand request, CancellationToken ct) => svc.AcceptAsync(request.InvitationId, request.UserId, ct);
}

public class RevokeInvitationHandler(IInvitationAppService svc) : IRequestHandler<RevokeInvitationCommand, Unit>
{
    public async Task<Unit> Handle(RevokeInvitationCommand request, CancellationToken ct) { await svc.RevokeAsync(request.InvitationId, request.ActorUserId, ct); return Unit.Value; }
}

public class ResendInvitationHandler(IInvitationAppService svc) : IRequestHandler<ResendInvitationCommand, InvitationDto>
{
    public Task<InvitationDto> Handle(ResendInvitationCommand request, CancellationToken ct) => svc.ResendAsync(request.InvitationId, request.ActorUserId, ct);
}

public class CancelInvitationHandler(IInvitationAppService svc) : IRequestHandler<CancelInvitationCommand, Unit>
{
    public async Task<Unit> Handle(CancelInvitationCommand request, CancellationToken ct)
    {
        await svc.CancelAsync(request.InvitationId, request.ActorUserId, ct);
        return Unit.Value;
    }
}

public class CreateOrganizationalUnitHandler(IOrganizationalUnitAppService svc) : IRequestHandler<CreateOrganizationalUnitCommand, OrganizationalUnitDto>
{
    public Task<OrganizationalUnitDto> Handle(CreateOrganizationalUnitCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, request.ActorUserId, ct);
}

public class UpdateOrganizationalUnitHandler(IOrganizationalUnitAppService svc) : IRequestHandler<UpdateOrganizationalUnitCommand, OrganizationalUnitDto>
{
    public Task<OrganizationalUnitDto> Handle(UpdateOrganizationalUnitCommand request, CancellationToken ct) => svc.UpdateAsync(request.Id, request.Request, request.ActorUserId, ct);
}

public class MoveOrganizationalUnitHandler(IOrganizationalUnitAppService svc) : IRequestHandler<MoveOrganizationalUnitCommand, OrganizationalUnitDto>
{
    public Task<OrganizationalUnitDto> Handle(MoveOrganizationalUnitCommand request, CancellationToken ct) => svc.MoveAsync(request.Id, request.Request, request.ActorUserId, ct);
}

public class SetOrganizationalUnitStatusHandler(IOrganizationalUnitAppService svc) : IRequestHandler<SetOrganizationalUnitStatusCommand, OrganizationalUnitDto>
{
    public Task<OrganizationalUnitDto> Handle(SetOrganizationalUnitStatusCommand request, CancellationToken ct) => svc.SetStatusAsync(request.Id, request.Status, request.ActorUserId, ct);
}

public class SoftDeleteOrganizationalUnitHandler(IOrganizationalUnitAppService svc) : IRequestHandler<SoftDeleteOrganizationalUnitCommand, Unit>
{
    public async Task<Unit> Handle(SoftDeleteOrganizationalUnitCommand request, CancellationToken ct)
    {
        await svc.SoftDeleteAsync(request.Id, request.ActorUserId, ct);
        return Unit.Value;
    }
}

public class CreateOrgDepartmentHandler(IDepartmentHierarchyAppService svc) : IRequestHandler<CreateOrgDepartmentCommand, DepartmentTreeNodeDto>
{
    public Task<DepartmentTreeNodeDto> Handle(CreateOrgDepartmentCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, request.ActorUserId, ct);
}

public class GetOrganizationTreeHandler(IOrganizationGraphAppService svc) : IRequestHandler<GetOrganizationTreeQuery, OrganizationTreeNodeDto?>
{
    public Task<OrganizationTreeNodeDto?> Handle(GetOrganizationTreeQuery request, CancellationToken ct) => svc.GetOrganizationTreeAsync(request.OrganizationId, ct);
}

public class GetDepartmentTreeHandler(IDepartmentHierarchyAppService svc) : IRequestHandler<GetDepartmentTreeQuery, IReadOnlyList<DepartmentTreeNodeDto>>
{
    public Task<IReadOnlyList<DepartmentTreeNodeDto>> Handle(GetDepartmentTreeQuery request, CancellationToken ct) => svc.GetTreeAsync(request.OrganizationId, request.HospitalId, ct);
}

public class GetOrganizationalUnitTreeHandler(IOrganizationalUnitAppService svc) : IRequestHandler<GetOrganizationalUnitTreeQuery, IReadOnlyList<OrganizationalUnitDto>>
{
    public Task<IReadOnlyList<OrganizationalUnitDto>> Handle(GetOrganizationalUnitTreeQuery request, CancellationToken ct)
        => svc.GetTreeAsync(request.OrganizationId, request.HospitalId, request.IncludeInactive, ct);
}

public class GetOrganizationalUnitTypesHandler(IOrganizationalUnitAppService svc) : IRequestHandler<GetOrganizationalUnitTypesQuery, IReadOnlyList<OrganizationalUnitTypeDto>>
{
    public Task<IReadOnlyList<OrganizationalUnitTypeDto>> Handle(GetOrganizationalUnitTypesQuery request, CancellationToken ct)
        => svc.GetUnitTypesAsync(ct);
}

public class GetOrganizationGraphHandler(IOrganizationGraphAppService svc) : IRequestHandler<GetOrganizationGraphQuery, OrganizationGraphDto>
{
    public Task<OrganizationGraphDto> Handle(GetOrganizationGraphQuery request, CancellationToken ct) => svc.GetGraphAsync(request.OrganizationId, ct);
}
