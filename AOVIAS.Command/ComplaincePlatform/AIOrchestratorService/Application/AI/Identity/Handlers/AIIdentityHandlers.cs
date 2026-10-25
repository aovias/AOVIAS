using AIOrchestratorService.Application.AI.Identity.Commands;
using AIOrchestratorService.Application.AI.Identity.Contracts;
using AIOrchestratorService.Application.AI.Identity.Queries;
using MediatR;

namespace AIOrchestratorService.Application.AI.Identity.Handlers;

public sealed class ListAIProfilesHandler(IAIProfileService svc) : IRequestHandler<ListAIProfilesQuery, IReadOnlyList<AIProfileDto>>
{
    public Task<IReadOnlyList<AIProfileDto>> Handle(ListAIProfilesQuery request, CancellationToken ct) => svc.ListAsync(ct);
}

public sealed class GetAIProfileHandler(IAIProfileService svc) : IRequestHandler<GetAIProfileQuery, AIProfileDto?>
{
    public Task<AIProfileDto?> Handle(GetAIProfileQuery request, CancellationToken ct) => svc.GetAsync(request.Id, ct);
}

public sealed class GetProfileByCopilotPersonaHandler(IAIProfileService svc) : IRequestHandler<GetProfileByCopilotPersonaQuery, AIProfileDto?>
{
    public Task<AIProfileDto?> Handle(GetProfileByCopilotPersonaQuery request, CancellationToken ct) => svc.GetByCopilotPersonaAsync(request.PersonaKey, ct);
}

public sealed class UpsertAIProfileHandler(IAIProfileService svc) : IRequestHandler<UpsertAIProfileCommand, AIProfileDto>
{
    public Task<AIProfileDto> Handle(UpsertAIProfileCommand request, CancellationToken ct) => svc.UpsertAsync(request.Request, ct);
}

public sealed class ListAutomationPoliciesHandler(IAIAutomationPolicyService svc) : IRequestHandler<ListAutomationPoliciesQuery, IReadOnlyList<AIAutomationPolicyDto>>
{
    public Task<IReadOnlyList<AIAutomationPolicyDto>> Handle(ListAutomationPoliciesQuery request, CancellationToken ct) => svc.ListAsync(request.ProfileId, ct);
}

public sealed class UpsertAutomationPolicyHandler(IAIAutomationPolicyService svc) : IRequestHandler<UpsertAutomationPolicyCommand, AIAutomationPolicyDto>
{
    public Task<AIAutomationPolicyDto> Handle(UpsertAutomationPolicyCommand request, CancellationToken ct) => svc.UpsertAsync(request.Request, ct);
}

public sealed class ListPromptProfilesHandler(IAIPromptProfileService svc) : IRequestHandler<ListPromptProfilesQuery, IReadOnlyList<AIPromptProfileDto>>
{
    public Task<IReadOnlyList<AIPromptProfileDto>> Handle(ListPromptProfilesQuery request, CancellationToken ct) => svc.ListAsync(ct);
}

public sealed class UpsertPromptHandler(IAIPromptProfileService svc) : IRequestHandler<UpsertPromptCommand, AIPromptProfileDto>
{
    public Task<AIPromptProfileDto> Handle(UpsertPromptCommand request, CancellationToken ct) => svc.UpsertAsync(request.Request, ct);
}

public sealed class EvaluatePromptHandler(IAIPromptProfileService svc) : IRequestHandler<EvaluatePromptCommand, PromptEvaluateResult>
{
    public Task<PromptEvaluateResult> Handle(EvaluatePromptCommand request, CancellationToken ct) => svc.EvaluateAsync(request.Request, ct);
}

public sealed class RollbackPromptHandler(IAIPromptProfileService svc) : IRequestHandler<RollbackPromptCommand, Unit>
{
    public async Task<Unit> Handle(RollbackPromptCommand request, CancellationToken ct) { await svc.RollbackAsync(request.PromptProfileId, request.TargetVersion, ct); return Unit.Value; }
}

public sealed class ListExecutionsHandler(IAIExecutionAuditService svc) : IRequestHandler<ListExecutionsQuery, PagedResult<AIExecutionAuditDto>>
{
    public Task<PagedResult<AIExecutionAuditDto>> Handle(ListExecutionsQuery request, CancellationToken ct) => svc.ListAsync(request.Page, request.PageSize, request.ProfileId, ct);
}

public sealed class GetExecutionHandler(IAIExecutionAuditService svc) : IRequestHandler<GetExecutionQuery, AIExecutionAuditDto?>
{
    public Task<AIExecutionAuditDto?> Handle(GetExecutionQuery request, CancellationToken ct) => svc.GetAsync(request.Id, ct);
}

public sealed class GetAIAnalyticsHandler(IAIAnalyticsService svc) : IRequestHandler<GetAIAnalyticsQuery, AIAnalyticsDto>
{
    public Task<AIAnalyticsDto> Handle(GetAIAnalyticsQuery request, CancellationToken ct) => svc.GetDashboardAsync(ct);
}

public sealed class ListPendingApprovalsHandler(IAIApprovalService svc) : IRequestHandler<ListPendingApprovalsQuery, IReadOnlyList<AIApprovalDto>>
{
    public Task<IReadOnlyList<AIApprovalDto>> Handle(ListPendingApprovalsQuery request, CancellationToken ct) => svc.ListPendingAsync(ct);
}

public sealed class GrantAIApprovalHandler(IAIApprovalService svc) : IRequestHandler<GrantAIApprovalCommand, Unit>
{
    public async Task<Unit> Handle(GrantAIApprovalCommand request, CancellationToken ct) { await svc.GrantAsync(request.ApprovalId, request.ApprovedByUserId, ct); return Unit.Value; }
}

public sealed class ListProfilePermissionsHandler(IAIProfilePermissionService svc) : IRequestHandler<ListProfilePermissionsQuery, IReadOnlyList<ProfilePermissionDto>>
{
    public Task<IReadOnlyList<ProfilePermissionDto>> Handle(ListProfilePermissionsQuery request, CancellationToken ct) => svc.ListAsync(request.ProfileId, ct);
}
