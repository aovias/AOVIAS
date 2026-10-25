using AIOrchestratorService.Domain.Enums;

namespace AIOrchestratorService.Application.AI.Identity.Contracts;

public sealed class AIProfileDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ModuleCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Version { get; init; }
    public string? CopilotPersonaKey { get; init; }
    public string? PlatformId { get; init; }
}

public sealed class UpsertAIProfileRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string ModuleCode { get; init; }
    public string? CopilotPersonaKey { get; init; }
    public bool Publish { get; init; }
}

public sealed class AIAutomationPolicyDto
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public bool BusinessHoursOnly { get; init; }
    public decimal ConfidenceThreshold { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class UpsertAutomationPolicyRequest
{
    public Guid ProfileId { get; init; }
    public required string Name { get; init; }
    public AutomationMode Mode { get; init; } = AutomationMode.HumanApprovalRequired;
    public bool BusinessHoursOnly { get; init; }
    public decimal ConfidenceThreshold { get; init; } = 0.7m;
    public string RiskLevel { get; init; } = "Medium";
}

public sealed class AIPromptProfileDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string LayerType { get; init; } = string.Empty;
    public Guid? ProfileId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int CurrentVersion { get; init; }
}

public sealed class UpsertPromptRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public PromptLayerType LayerType { get; init; }
    public Guid? ProfileId { get; init; }
    public required string SystemPrompt { get; init; }
    public string? RolePrompt { get; init; }
    public string Locale { get; init; } = "en-IN";
    public bool Publish { get; init; }
}

public sealed class PromptEvaluateRequest
{
    public Guid? ProfileId { get; init; }
    public string? PromptCode { get; init; }
    public string SampleQuery { get; init; } = string.Empty;
}

public sealed class PromptEvaluateResult
{
    public string MergedSystemPrompt { get; init; } = string.Empty;
    public int LayerCount { get; init; }
}

public sealed class AIExecutionAuditDto
{
    public Guid Id { get; init; }
    public Guid? ProfileId { get; init; }
    public string UseCase { get; init; } = string.Empty;
    public string ModelProvider { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public int LatencyMs { get; init; }
    public decimal Confidence { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
}

public sealed class AIAnalyticsDto
{
    public int TotalExecutions { get; init; }
    public long TotalTokens { get; init; }
    public decimal EstimatedCost { get; init; }
    public int AvgLatencyMs { get; init; }
    public decimal AcceptanceRate { get; init; }
    public decimal AutomationRate { get; init; }
}

public sealed class AIApprovalDto
{
    public Guid Id { get; init; }
    public Guid ExecutionAuditId { get; init; }
    public Guid? ProfileId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class ProfilePermissionDto
{
    public Guid Id { get; init; }
    public string PermissionCode { get; init; } = string.Empty;
    public string ActionType { get; init; } = string.Empty;
    public bool Enabled { get; init; }
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public interface IAIProfileService
{
    Task<IReadOnlyList<AIProfileDto>> ListAsync(CancellationToken ct = default);
    Task<AIProfileDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AIProfileDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<AIProfileDto?> GetByCopilotPersonaAsync(string personaKey, CancellationToken ct = default);
    Task<AIProfileDto> UpsertAsync(UpsertAIProfileRequest request, CancellationToken ct = default);
}

public interface IAIAutomationPolicyService
{
    Task<IReadOnlyList<AIAutomationPolicyDto>> ListAsync(Guid? profileId, CancellationToken ct = default);
    Task<AIAutomationPolicyDto> UpsertAsync(UpsertAutomationPolicyRequest request, CancellationToken ct = default);
    Task<AutomationMode> ResolveModeAsync(Guid profileId, decimal confidence, CancellationToken ct = default);
}

public interface IAIPromptProfileService
{
    Task<IReadOnlyList<AIPromptProfileDto>> ListAsync(CancellationToken ct = default);
    Task<AIPromptProfileDto> UpsertAsync(UpsertPromptRequest request, CancellationToken ct = default);
    Task<PromptEvaluateResult> EvaluateAsync(PromptEvaluateRequest request, CancellationToken ct = default);
    Task<string> ResolveMergedPromptAsync(Guid? profileId, string? templateCode, CancellationToken ct = default);
    Task RollbackAsync(Guid promptProfileId, int targetVersion, CancellationToken ct = default);
}

public interface IAIExecutionAuditService
{
    Task<Guid> RecordAsync(AIExecutionAuditWriteRequest request, CancellationToken ct = default);
    Task<PagedResult<AIExecutionAuditDto>> ListAsync(int page, int pageSize, Guid? profileId, CancellationToken ct = default);
    Task<AIExecutionAuditDto?> GetAsync(Guid id, CancellationToken ct = default);
}

public sealed class AIExecutionAuditWriteRequest
{
    public Guid? RequestedByUserId { get; init; }
    public Guid? ProfileId { get; init; }
    public Guid? PromptVersionId { get; init; }
    public Guid HospitalId { get; init; }
    public string UseCase { get; init; } = string.Empty;
    public string ModelProvider { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int LatencyMs { get; init; }
    public decimal Confidence { get; init; }
    public string ToolsUsedJson { get; init; } = "[]";
    public string DocumentsAccessedJson { get; init; } = "[]";
    public ExecutionOutcome Outcome { get; init; }
    public string TraceId { get; init; } = string.Empty;
}

public interface IAIAnalyticsService
{
    Task<AIAnalyticsDto> GetDashboardAsync(CancellationToken ct = default);
}

public interface IAIApprovalService
{
    Task<IReadOnlyList<AIApprovalDto>> ListPendingAsync(CancellationToken ct = default);
    Task GrantAsync(Guid approvalId, Guid approvedByUserId, CancellationToken ct = default);
}

public interface IAIProfilePermissionService
{
    Task<IReadOnlyList<ProfilePermissionDto>> ListAsync(Guid profileId, CancellationToken ct = default);
    Task<bool> ProfileAllowsActionAsync(Guid profileId, AIActionType action, CancellationToken ct = default);
}
