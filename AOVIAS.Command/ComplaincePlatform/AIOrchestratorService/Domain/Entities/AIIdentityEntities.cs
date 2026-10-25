using AIOrchestratorService.Domain.Enums;

namespace AIOrchestratorService.Domain.Entities;

public class AIAssistantProfile
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CapabilitiesJson { get; set; } = "[]";
    public Guid? OwnerUserId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public AIProfileStatus Status { get; set; } = AIProfileStatus.Draft;
    public int Version { get; set; } = 1;
    public string? CopilotPersonaKey { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AIIdentity
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string PlatformId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid? GovernanceOwnerUserId { get; set; }
    public AIIdentityLifecycleStatus LifecycleStatus { get; set; } = AIIdentityLifecycleStatus.Active;
    public DateTime CreatedAt { get; set; }
}

public class AIIdentityScopeAssignment
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ResponsibilityId { get; set; }
}

public class AIProfilePermissionGrant
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public AIActionType ActionType { get; set; }
    public bool Enabled { get; set; } = true;
}

public class AIAutomationPolicy
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AutomationMode Mode { get; set; } = AutomationMode.HumanApprovalRequired;
    public bool BusinessHoursOnly { get; set; }
    public decimal ConfidenceThreshold { get; set; } = 0.7m;
    public string RiskLevel { get; set; } = "Medium";
    public Guid? ApprovalPolicyId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class AIApprovalPolicy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ApprovalRequirementType ApprovalType { get; set; }
    public Guid? GovernanceApprovalChainId { get; set; }
    public decimal? ConfidenceThreshold { get; set; }
    public string? RiskLevelThreshold { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class AIPromptProfile
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PromptLayerType LayerType { get; set; }
    public Guid? ProfileId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? FrameworkCode { get; set; }
    public PromptProfileStatus Status { get; set; } = PromptProfileStatus.Draft;
    public int CurrentVersion { get; set; } = 1;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AIPromptVersion
{
    public Guid Id { get; set; }
    public Guid PromptProfileId { get; set; }
    public int Version { get; set; }
    public string Locale { get; set; } = "en-IN";
    public string SystemPrompt { get; set; } = string.Empty;
    public string? RolePrompt { get; set; }
    public string? PublishedByUserId { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AIConversationContext
{
    public Guid Id { get; set; }
    public Guid? ProfileId { get; set; }
    public Guid? UserId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? FrameworkCode { get; set; }
    public int RetentionDays { get; set; } = 90;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}

public class AIConversationMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public int Sequence { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? ReferencedDocumentsJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AIExecutionContext
{
    public Guid Id { get; set; }
    public Guid? ExecutionAuditId { get; set; }
    public Guid? ProfileId { get; set; }
    public Guid? ConversationId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string ContextJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}

public class AIExecutionAudit
{
    public Guid Id { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public Guid? ProfileId { get; set; }
    public Guid? PromptVersionId { get; set; }
    public Guid HospitalId { get; set; }
    public string UseCase { get; set; } = string.Empty;
    public string ModelProvider { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int LatencyMs { get; set; }
    public decimal Confidence { get; set; }
    public string ToolsUsedJson { get; set; } = "[]";
    public string DocumentsAccessedJson { get; set; } = "[]";
    public ExecutionOutcome Outcome { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.NotRequired;
    public Guid? ApprovedByUserId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
}

public class AIUsageStatistic
{
    public Guid Id { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? ProfileId { get; set; }
    public int ExecutionCount { get; set; }
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    public int AvgLatencyMs { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public int HumanOverrideCount { get; set; }
    public decimal AutomationRate { get; set; }
}

public class AIApprovalRequest
{
    public Guid Id { get; set; }
    public Guid ExecutionAuditId { get; set; }
    public Guid? ProfileId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public Guid? ApprovedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class AIOutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
}
