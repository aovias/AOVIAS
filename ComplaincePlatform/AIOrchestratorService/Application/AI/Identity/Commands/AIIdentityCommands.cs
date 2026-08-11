using AIOrchestratorService.Application.AI.Identity.Contracts;
using MediatR;

namespace AIOrchestratorService.Application.AI.Identity.Commands;

public sealed record UpsertAIProfileCommand(UpsertAIProfileRequest Request) : IRequest<AIProfileDto>;
public sealed record UpsertAutomationPolicyCommand(UpsertAutomationPolicyRequest Request) : IRequest<AIAutomationPolicyDto>;
public sealed record UpsertPromptCommand(UpsertPromptRequest Request) : IRequest<AIPromptProfileDto>;
public sealed record EvaluatePromptCommand(PromptEvaluateRequest Request) : IRequest<PromptEvaluateResult>;
public sealed record RollbackPromptCommand(Guid PromptProfileId, int TargetVersion) : IRequest<Unit>;
public sealed record GrantAIApprovalCommand(Guid ApprovalId, Guid ApprovedByUserId) : IRequest<Unit>;
