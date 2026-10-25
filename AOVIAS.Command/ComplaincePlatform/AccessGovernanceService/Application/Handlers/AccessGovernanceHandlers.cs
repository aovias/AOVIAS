using AccessGovernanceService.Application.Contracts;
using AccessGovernanceService.Application.Services;
using MediatR;

namespace AccessGovernanceService.Application.Commands;

public record CreateRoleCommand(CreateRoleRequest Request, Guid ActorUserId) : IRequest<RoleDto>;
public record UpdateRoleCommand(Guid RoleId, UpdateRoleRequest Request, Guid ActorUserId) : IRequest<Unit>;
public record AssignRoleCommand(Guid RoleId, AssignRoleRequest Request, Guid ActorUserId) : IRequest<Unit>;
public record RevokeRoleAssignmentCommand(Guid AssignmentId, Guid ActorUserId) : IRequest<Unit>;
public record GrantPermissionToRoleCommand(Guid RoleId, Guid PermissionId, Guid ActorUserId) : IRequest<Unit>;
public record CreatePermissionCommand(CreatePermissionRequest Request) : IRequest<PermissionDto>;
public record CreatePolicyCommand(CreatePolicyRequest Request) : IRequest<PolicyDto>;
public record UpdatePolicyCommand(Guid PolicyId, UpdatePolicyRequest Request) : IRequest<Unit>;
public record PublishPolicyCommand(Guid PolicyId) : IRequest<Unit>;
public record ArchivePolicyCommand(Guid PolicyId) : IRequest<Unit>;
public record CreateDelegationCommand(CreateDelegationRequest Request, Guid ActorUserId) : IRequest<DelegationDto>;
public record RevokeDelegationCommand(Guid DelegationId, Guid ActorUserId) : IRequest<Unit>;
public record AssignResponsibilityCommand(AssignResponsibilityRequest Request, Guid ActorUserId) : IRequest<ResponsibilityDto>;
public record SetApprovalAuthorityCommand(SetApprovalAuthorityRequest Request, Guid ActorUserId) : IRequest<ApprovalAuthorityDto>;
public record EvaluateAuthorizationCommand(EvaluateAuthorizationRequest Request) : IRequest<AuthorizationDecisionDto>;

public class CreateRoleHandler(IRoleAppService svc) : IRequestHandler<CreateRoleCommand, RoleDto>
{
    public Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, request.ActorUserId, ct);
}
public class UpdateRoleHandler(IRoleAppService svc) : IRequestHandler<UpdateRoleCommand, Unit>
{
    public async Task<Unit> Handle(UpdateRoleCommand request, CancellationToken ct) { await svc.UpdateAsync(request.RoleId, request.Request, request.ActorUserId, ct); return Unit.Value; }
}
public class AssignRoleHandler(IRoleAppService svc) : IRequestHandler<AssignRoleCommand, Unit>
{
    public async Task<Unit> Handle(AssignRoleCommand request, CancellationToken ct) { await svc.AssignRoleAsync(request.RoleId, request.Request, request.ActorUserId, ct); return Unit.Value; }
}
public class RevokeRoleAssignmentHandler(IRoleAppService svc) : IRequestHandler<RevokeRoleAssignmentCommand, Unit>
{
    public async Task<Unit> Handle(RevokeRoleAssignmentCommand request, CancellationToken ct) { await svc.RevokeAssignmentAsync(request.AssignmentId, request.ActorUserId, ct); return Unit.Value; }
}
public class GrantPermissionHandler(IRoleAppService svc) : IRequestHandler<GrantPermissionToRoleCommand, Unit>
{
    public async Task<Unit> Handle(GrantPermissionToRoleCommand request, CancellationToken ct) { await svc.GrantPermissionAsync(request.RoleId, request.PermissionId, request.ActorUserId, ct); return Unit.Value; }
}
public class CreatePermissionHandler(IPermissionAppService svc) : IRequestHandler<CreatePermissionCommand, PermissionDto>
{
    public Task<PermissionDto> Handle(CreatePermissionCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, ct);
}
public class CreatePolicyHandler(IPolicyAppService svc) : IRequestHandler<CreatePolicyCommand, PolicyDto>
{
    public Task<PolicyDto> Handle(CreatePolicyCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, ct);
}
public class UpdatePolicyHandler(IPolicyAppService svc) : IRequestHandler<UpdatePolicyCommand, Unit>
{
    public async Task<Unit> Handle(UpdatePolicyCommand request, CancellationToken ct) { await svc.UpdateAsync(request.PolicyId, request.Request, ct); return Unit.Value; }
}
public class PublishPolicyHandler(IPolicyAppService svc) : IRequestHandler<PublishPolicyCommand, Unit>
{
    public async Task<Unit> Handle(PublishPolicyCommand request, CancellationToken ct) { await svc.PublishAsync(request.PolicyId, ct); return Unit.Value; }
}
public class ArchivePolicyHandler(IPolicyAppService svc) : IRequestHandler<ArchivePolicyCommand, Unit>
{
    public async Task<Unit> Handle(ArchivePolicyCommand request, CancellationToken ct) { await svc.ArchiveAsync(request.PolicyId, ct); return Unit.Value; }
}
public class CreateDelegationHandler(IDelegationAppService svc) : IRequestHandler<CreateDelegationCommand, DelegationDto>
{
    public Task<DelegationDto> Handle(CreateDelegationCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, request.ActorUserId, ct);
}
public class RevokeDelegationHandler(IDelegationAppService svc) : IRequestHandler<RevokeDelegationCommand, Unit>
{
    public async Task<Unit> Handle(RevokeDelegationCommand request, CancellationToken ct) { await svc.RevokeAsync(request.DelegationId, request.ActorUserId, ct); return Unit.Value; }
}
public class AssignResponsibilityHandler(IResponsibilityAppService svc) : IRequestHandler<AssignResponsibilityCommand, ResponsibilityDto>
{
    public Task<ResponsibilityDto> Handle(AssignResponsibilityCommand request, CancellationToken ct) => svc.AssignAsync(request.Request, request.ActorUserId, ct);
}
public class SetApprovalAuthorityHandler(IApprovalAuthorityAppService svc) : IRequestHandler<SetApprovalAuthorityCommand, ApprovalAuthorityDto>
{
    public Task<ApprovalAuthorityDto> Handle(SetApprovalAuthorityCommand request, CancellationToken ct) => svc.SetAsync(request.Request, request.ActorUserId, ct);
}
public class EvaluateAuthorizationHandler(IAuthorizationEvaluatorService svc) : IRequestHandler<EvaluateAuthorizationCommand, AuthorizationDecisionDto>
{
    public Task<AuthorizationDecisionDto> Handle(EvaluateAuthorizationCommand request, CancellationToken ct) => svc.EvaluateAsync(request.Request, ct);
}
