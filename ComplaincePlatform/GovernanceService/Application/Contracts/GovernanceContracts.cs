namespace GovernanceService.Application.Contracts;

public sealed class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class ResponsibilityDto
{
    public Guid Id { get; init; }
    public string ResponsibilityCode { get; init; } = string.Empty;
    public Guid ResponsibilityTypeValueId { get; init; }
    public string GovernedResourceType { get; init; } = string.Empty;
    public Guid? GovernedResourceId { get; init; }
    public bool RequiresPrimaryOwner { get; init; }
}

public sealed class CreateResponsibilityRequest
{
    public string ResponsibilityCode { get; init; } = string.Empty;
    public Guid ResponsibilityTypeValueId { get; init; }
    public string GovernedResourceType { get; init; } = string.Empty;
    public Guid? GovernedResourceId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public bool RequiresPrimaryOwner { get; init; }
    public AssignGovernanceRequest? InitialAssignment { get; init; }
}

public sealed class UpdateResponsibilityRequest
{
    public Guid ResponsibilityTypeValueId { get; init; }
    public bool RequiresPrimaryOwner { get; init; }
}

public sealed class AssignGovernanceRequest
{
    public string AssigneeType { get; init; } = "Person";
    public Guid AssigneeId { get; init; }
    public string Priority { get; init; } = "Primary";
    public string AssignmentMode { get; init; } = "Permanent";
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string? Reason { get; init; }
}

public sealed class GovernanceAssignmentDto
{
    public Guid Id { get; init; }
    public Guid ResponsibilityId { get; init; }
    public string AssigneeType { get; init; } = string.Empty;
    public Guid AssigneeId { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class CommitteeDto
{
    public Guid Id { get; init; }
    public string CommitteeCode { get; init; } = string.Empty;
    public string CommitteeName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? ChairpersonAssigneeId { get; init; }
    public Guid? SecretaryAssigneeId { get; init; }
    public Guid? OrganizationId { get; init; }
}

public sealed class CreateCommitteeRequest
{
    public string CommitteeCode { get; init; } = string.Empty;
    public string CommitteeName { get; init; } = string.Empty;
    public Guid CommitteeTypeValueId { get; init; }
    public string? Charter { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public DateTime? TermStart { get; init; }
    public DateTime? TermEnd { get; init; }
    public Guid? ChairpersonAssigneeId { get; init; }
    public Guid? SecretaryAssigneeId { get; init; }
}

public sealed class UpdateCommitteeRequest
{
    public string CommitteeName { get; init; } = string.Empty;
    public Guid CommitteeTypeValueId { get; init; }
    public string? Charter { get; init; }
    public DateTime? TermStart { get; init; }
    public DateTime? TermEnd { get; init; }
    public Guid? ChairpersonAssigneeId { get; init; }
    public Guid? SecretaryAssigneeId { get; init; }
}

public sealed class AddCommitteeMemberRequest
{
    public Guid MemberAssigneeId { get; init; }
    public Guid CommitteeRoleValueId { get; init; }
    public DateTime TermStart { get; init; }
    public DateTime? TermEnd { get; init; }
    public bool VotingRights { get; init; } = true;
}

public sealed class ScheduleMeetingRequest
{
    public DateTime ScheduledAt { get; init; }
    public Guid MeetingTypeValueId { get; init; }
    public string? Agenda { get; init; }
    public string? AgendaTemplateCode { get; init; }
}

public sealed class RecordDecisionRequest
{
    public Guid DecisionTypeValueId { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? GovernedResourceType { get; init; }
    public Guid? GovernedResourceId { get; init; }
    public Guid? OwnerAssigneeId { get; init; }
    public string? ImplementationStatus { get; init; }
    public Guid? DeficiencyId { get; init; }
    public Guid? RiskId { get; init; }
    public Guid? TaskId { get; init; }
}

public sealed class CompleteMeetingRequest
{
    public string? Minutes { get; init; }
    public string? AttendanceJson { get; init; }
}

public sealed class AttendanceEntryDto
{
    public Guid MemberAssigneeId { get; init; }
    public string? DisplayName { get; init; }
    public bool Present { get; init; } = true;
    public string? Role { get; init; }
}

public sealed class RecordAttendanceRequest
{
    public IReadOnlyList<AttendanceEntryDto> Attendees { get; init; } = Array.Empty<AttendanceEntryDto>();
    /// <summary>Optional raw JSON override (takes precedence when Attendees empty).</summary>
    public string? AttendanceJson { get; init; }
}

public sealed class CreateActionItemRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? OwnerAssigneeId { get; init; }
    public DateTime? DueDate { get; init; }
    /// <summary>Existing ComplianceTask id, if already created via Gateway.</summary>
    public Guid? TaskId { get; init; }
    public Guid? DecisionId { get; init; }
}

public sealed class LinkActionItemTaskRequest
{
    public Guid TaskId { get; init; }
}

public sealed class UpdateActionItemStatusRequest
{
    public string Status { get; init; } = "Open";
}

public sealed class CommitteeMeetingDto
{
    public Guid Id { get; init; }
    public Guid CommitteeId { get; init; }
    public DateTime ScheduledAt { get; init; }
    public Guid MeetingTypeValueId { get; init; }
    public string? Agenda { get; init; }
    public string? AgendaTemplateCode { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Minutes { get; init; }
    public string? AttendanceJson { get; init; }
    public bool MinutesApproved { get; init; }
    public DateTime? MinutesApprovedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public IReadOnlyList<CommitteeDecisionDto> Decisions { get; init; } = Array.Empty<CommitteeDecisionDto>();
    public IReadOnlyList<CommitteeActionItemDto> ActionItems { get; init; } = Array.Empty<CommitteeActionItemDto>();
}

public sealed class CommitteeDecisionDto
{
    public Guid Id { get; init; }
    public Guid DecisionTypeValueId { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? GovernedResourceType { get; init; }
    public Guid? GovernedResourceId { get; init; }
    public Guid? DeficiencyId { get; init; }
    public Guid? RiskId { get; init; }
    public Guid? TaskId { get; init; }
    public Guid? OwnerAssigneeId { get; init; }
    public string? ImplementationStatus { get; init; }
    public DateTime RecordedAt { get; init; }
}

public sealed class CommitteeActionItemDto
{
    public Guid Id { get; init; }
    public Guid CommitteeMeetingId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? OwnerAssigneeId { get; init; }
    public DateTime? DueDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? TaskId { get; init; }
    public Guid? DecisionId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class AgendaTemplateDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<string> Sections { get; init; } = Array.Empty<string>();
    public bool IsManagementReview { get; init; }
}

public sealed class ManagementReviewAgendaInputDto
{
    public Guid? CommitteeId { get; init; }
    public string TemplateCode { get; init; } = "ManagementReview";
    public IReadOnlyList<string> AgendaSections { get; init; } = Array.Empty<string>();
    public ManagementReviewSummaryBlockDto Incidents { get; init; } = new();
    public ManagementReviewSummaryBlockDto Capas { get; init; } = new();
    public ManagementReviewSummaryBlockDto Risks { get; init; } = new();
    public ManagementReviewSummaryBlockDto Audits { get; init; } = new();
    public string Notes { get; init; } = "Summaries are placeholders unless Gateway enrichment supplies live counts.";
}

public sealed class ManagementReviewSummaryBlockDto
{
    public string Title { get; init; } = string.Empty;
    public int OpenCount { get; init; }
    public int ClosedCount { get; init; }
    public IReadOnlyList<string> Highlights { get; init; } = Array.Empty<string>();
    public bool IsPlaceholder { get; init; } = true;
}

public sealed class ApprovalChainDto
{
    public Guid Id { get; init; }
    public string ChainCode { get; init; } = string.Empty;
    public string ChainName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class CreateApprovalChainRequest
{
    public string ChainCode { get; init; } = string.Empty;
    public string ChainName { get; init; } = string.Empty;
    public Guid ApprovalTypeValueId { get; init; }
    public Guid? OrganizationId { get; init; }
    public List<CreateApprovalStepRequest> Steps { get; init; } = new();
}

public sealed class CreateApprovalStepRequest
{
    public int StepOrder { get; init; }
    public string Mode { get; init; } = "Sequential";
    public string AssigneeType { get; init; } = "Person";
    public Guid AssigneeId { get; init; }
    public string? ConditionExpression { get; init; }
}

public sealed class EscalationChainDto
{
    public Guid Id { get; init; }
    public string ChainCode { get; init; } = string.Empty;
    public string ChainName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class CreateEscalationChainRequest
{
    public string ChainCode { get; init; } = string.Empty;
    public string ChainName { get; init; } = string.Empty;
    public Guid EscalationTypeValueId { get; init; }
    public Guid? OrganizationId { get; init; }
    public List<CreateEscalationLevelRequest> Levels { get; init; } = new();
}

public sealed class CreateEscalationLevelRequest
{
    public int LevelOrder { get; init; }
    public string TargetAssigneeType { get; init; } = "User";
    public Guid? TargetAssigneeId { get; init; }
    public int? DelayHours { get; init; }
    public string? ConditionExpression { get; init; }
}

public sealed class ApprovalEvaluateRequest
{
    public Guid ApprovalChainId { get; init; }
    public string? GovernedResourceType { get; init; }
    public Guid? GovernedResourceId { get; init; }
    public Guid? InitiatorUserId { get; init; }
    public Dictionary<string, string> Context { get; init; } = new();
    /// <summary>Comma-separated step orders already approved (e.g. "1,2").</summary>
    public string? ApprovedSteps { get; init; }
    /// <summary>Step order that was rejected, if any.</summary>
    public int? RejectedStepOrder { get; init; }
}

public sealed class ApprovalDecisionDto
{
    public bool Completed { get; init; }
    public bool Approved { get; init; }
    public string Reason { get; init; } = string.Empty;
    public int? CurrentStep { get; init; }
    public IReadOnlyList<string> MatchedSteps { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PendingApprovers { get; init; } = Array.Empty<string>();
}

public sealed class EscalationResolveRequest
{
    public Guid EscalationChainId { get; init; }
    public int CurrentLevel { get; init; }
    public Guid? CurrentAssigneeUserId { get; init; }
    public Guid? CurrentEmploymentId { get; init; }
    public string? Severity { get; init; }
    public int? HoursSinceOpen { get; init; }
    public Dictionary<string, string> Context { get; init; } = new();
}

public sealed class EscalationResolutionDto
{
    public Guid ChainId { get; init; }
    public int ResolvedLevel { get; init; }
    public string? NextAssigneeType { get; init; }
    public Guid? NextAssigneeId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool EscalationComplete { get; init; }
}

public sealed class GovernanceTimelineDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? PayloadJson { get; init; }
}

public sealed class GovernanceAnalyticsDto
{
    public int ActiveResponsibilities { get; init; }
    public int ActiveCommittees { get; init; }
    public int PublishedApprovalChains { get; init; }
    public int ActiveAssignments { get; init; }
    public int ScheduledMeetings { get; init; }
    public int CompletedMeetings { get; init; }
    public int MeetingsPendingMinutesApproval { get; init; }
    public int OpenActionItems { get; init; }
    public int ActionItemsWithLinkedTasks { get; init; }
    public int ManagementReviewCommittees { get; init; }
}

/// <summary>Phase 4 Sprint 6 — cross-domain governance KPIs (best-effort).</summary>
public sealed class GovernanceKpisDto
{
    public int ActiveCommittees { get; init; }
    public int OpenActionItems { get; init; }
    public int MeetingsThisMonth { get; init; }
    public int PendingMinutesApprovals { get; init; }
    public int DocumentsEffective { get; init; }
    public int TrainingOverdue { get; init; }
    public int OpenIncidents { get; init; }
    public int OpenRisks { get; init; }
    public string SourceNotes { get; init; } = string.Empty;
}

public sealed class LookupValueDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class OwnerLookupResultDto
{
    public IReadOnlyList<GovernanceAssignmentDto> Owners { get; init; } = Array.Empty<GovernanceAssignmentDto>();
}

public interface IResponsibilityAppService
{
    Task<ResponsibilityDto> CreateAsync(CreateResponsibilityRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<ResponsibilityDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResultDto<ResponsibilityDto>> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdateResponsibilityRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid actorUserId, CancellationToken ct = default);
    Task<GovernanceAssignmentDto> AssignAsync(Guid responsibilityId, AssignGovernanceRequest request, Guid actorUserId, CancellationToken ct = default);
}

public interface ICommitteeAppService
{
    Task<CommitteeDto> CreateAsync(CreateCommitteeRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<CommitteeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdateCommitteeRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<CommitteeDto>> ListAsync(CancellationToken ct = default);
    Task AddMemberAsync(Guid committeeId, AddCommitteeMemberRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<CommitteeMeetingDto> ScheduleMeetingAsync(Guid committeeId, ScheduleMeetingRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<CommitteeMeetingDto>> ListMeetingsAsync(Guid committeeId, CancellationToken ct = default);
    Task<CommitteeMeetingDto?> GetMeetingAsync(Guid committeeId, Guid meetingId, CancellationToken ct = default);
    Task CompleteMeetingAsync(Guid committeeId, Guid meetingId, CompleteMeetingRequest request, Guid actorUserId, CancellationToken ct = default);
    Task RecordAttendanceAsync(Guid committeeId, Guid meetingId, RecordAttendanceRequest request, Guid actorUserId, CancellationToken ct = default);
    Task ApproveMinutesAsync(Guid committeeId, Guid meetingId, Guid actorUserId, CancellationToken ct = default);
    Task<CommitteeDecisionDto> RecordDecisionAsync(Guid committeeId, Guid meetingId, RecordDecisionRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<CommitteeActionItemDto> CreateActionItemAsync(Guid committeeId, Guid meetingId, CreateActionItemRequest request, Guid actorUserId, CancellationToken ct = default);
    Task LinkActionItemTaskAsync(Guid committeeId, Guid meetingId, Guid actionItemId, LinkActionItemTaskRequest request, Guid actorUserId, CancellationToken ct = default);
    Task UpdateActionItemStatusAsync(Guid committeeId, Guid meetingId, Guid actionItemId, UpdateActionItemStatusRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<AgendaTemplateDto>> ListAgendaTemplatesAsync(CancellationToken ct = default);
    Task<ManagementReviewAgendaInputDto> GetManagementReviewAgendaAsync(Guid? committeeId, CancellationToken ct = default);
}

public interface IGovernanceKpiService
{
    Task<GovernanceKpisDto> GetKpisAsync(CancellationToken ct = default);
}

public interface IApprovalChainAppService
{
    Task<ApprovalChainDto> CreateAsync(CreateApprovalChainRequest request, CancellationToken ct = default);
    Task PublishAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ApprovalChainDto>> ListAsync(CancellationToken ct = default);
}

public interface IEscalationChainAppService
{
    Task<EscalationChainDto> CreateAsync(CreateEscalationChainRequest request, CancellationToken ct = default);
    Task PublishAsync(Guid id, CancellationToken ct = default);
    Task<EscalationChainDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EscalationChainDto>> ListAsync(CancellationToken ct = default);
}

public interface IApprovalWorkflowEvaluatorService
{
    Task<ApprovalDecisionDto> EvaluateAsync(ApprovalEvaluateRequest request, CancellationToken ct = default);
}

public interface IEscalationResolverService
{
    Task<EscalationResolutionDto> ResolveAsync(EscalationResolveRequest request, CancellationToken ct = default);
}

public interface IGovernanceTimelineService
{
    Task AppendAsync(string eventType, string summary, string? resourceType, Guid? resourceId, Guid? actorUserId, Guid? organizationId, object? payload = null, CancellationToken ct = default);
    Task<IReadOnlyList<GovernanceTimelineDto>> SearchAsync(string? resourceType, Guid? resourceId, Guid? organizationId, CancellationToken ct = default);
    Task<OwnerLookupResultDto> GetOwnersAsync(string resourceType, Guid resourceId, string? responsibilityTypeCode, CancellationToken ct = default);
}

public interface IGovernanceAnalyticsService
{
    Task<GovernanceAnalyticsDto> GetSummaryAsync(CancellationToken ct = default);
}
