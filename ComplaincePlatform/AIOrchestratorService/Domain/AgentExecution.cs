namespace AIOrchestratorService.Domain;

public enum AgentExecutionStatus
{
    Running = 1,
    AwaitingApproval = 2,
    Approved = 3,
    Rejected = 4,
    TimedOut = 5,
    Failed = 6,
    Completed = 7,
    Compensating = 8,
    Cancelled = 9
}

public enum AgentFailureType
{
    None = 0,
    Transient = 1,
    Validation = 2,
    PolicyViolation = 3,
    SystemError = 4,
    DependencyFailure = 5
}

public sealed class AgentExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string WorkflowTemplateId { get; set; } = string.Empty;
    public string WorkflowTemplateVersion { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string ContextVersionId { get; set; } = string.Empty;
    public AgentExecutionStatus Status { get; set; } = AgentExecutionStatus.Running;
    public string CurrentStepId { get; set; } = string.Empty;
    public string CompletedStepIdsJson { get; set; } = "[]";
    public string PendingStepIdsJson { get; set; } = "[]";
    public AgentFailureType FailureType { get; set; }
    public string? FailureReason { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovalReason { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class AgentExecutionStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExecutionId { get; set; }
    public string StepId { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string DependsOnJson { get; set; } = "[]";
    public bool IsParallelizable { get; set; }
    public AgentExecutionStatus Status { get; set; } = AgentExecutionStatus.Running;
    public int RetryCount { get; set; }
    public string InputJson { get; set; } = "{}";
    public string OutputJson { get; set; } = "{}";
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovalReason { get; set; }
    public DateTime? ApprovalRequestedAtUtc { get; set; }
    public DateTime? ApprovalExpiresAtUtc { get; set; }
    public AgentFailureType FailureType { get; set; }
    public string? FailureReason { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class AgentExecutionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExecutionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
