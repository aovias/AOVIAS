using GovernanceService.Domain.Enums;

namespace GovernanceService.Domain.Entities;

public class CommitteeMembership
{
    public Guid Id { get; private set; }
    public Guid CommitteeId { get; private set; }
    public Guid MemberAssigneeId { get; private set; }
    public Guid CommitteeRoleValueId { get; private set; }
    public DateTime TermStart { get; private set; }
    public DateTime? TermEnd { get; private set; }
    public bool VotingRights { get; private set; }
    public CommitteeMembershipStatus Status { get; private set; }
    public Guid? ReplacedByMembershipId { get; private set; }

    private CommitteeMembership() { }

    public static CommitteeMembership Create(Guid committeeId, Guid memberAssigneeId, Guid committeeRoleValueId, DateTime termStart, DateTime? termEnd, bool votingRights)
    {
        return new CommitteeMembership
        {
            Id = Guid.NewGuid(),
            CommitteeId = committeeId,
            MemberAssigneeId = memberAssigneeId,
            CommitteeRoleValueId = committeeRoleValueId,
            TermStart = termStart,
            TermEnd = termEnd,
            VotingRights = votingRights,
            Status = CommitteeMembershipStatus.Active
        };
    }
}

public class CommitteeMeeting
{
    public Guid Id { get; private set; }
    public Guid CommitteeId { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public Guid MeetingTypeValueId { get; private set; }
    public string? Agenda { get; private set; }
    /// <summary>Template code e.g. Standard, ManagementReview.</summary>
    public string? AgendaTemplateCode { get; private set; }
    public MeetingStatus Status { get; private set; }
    public string? Minutes { get; private set; }
    public string? AttendanceJson { get; private set; }
    public bool MinutesApproved { get; private set; }
    public DateTime? MinutesApprovedAt { get; private set; }
    public Guid? MinutesApprovedByUserId { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<CommitteeDecision> _decisions = new();
    public IReadOnlyCollection<CommitteeDecision> Decisions => _decisions.AsReadOnly();

    private readonly List<CommitteeActionItem> _actionItems = new();
    public IReadOnlyCollection<CommitteeActionItem> ActionItems => _actionItems.AsReadOnly();

    private CommitteeMeeting() { }

    public static CommitteeMeeting Create(
        Guid committeeId,
        DateTime scheduledAt,
        Guid meetingTypeValueId,
        string? agenda,
        string? agendaTemplateCode = null)
    {
        return new CommitteeMeeting
        {
            Id = Guid.NewGuid(),
            CommitteeId = committeeId,
            ScheduledAt = scheduledAt,
            MeetingTypeValueId = meetingTypeValueId,
            Agenda = agenda,
            AgendaTemplateCode = string.IsNullOrWhiteSpace(agendaTemplateCode) ? null : agendaTemplateCode.Trim(),
            Status = MeetingStatus.Scheduled
        };
    }

    public void MarkHeld() => Status = MeetingStatus.Held;

    public void SetAttendance(string? attendanceJson)
    {
        if (Status == MeetingStatus.Completed && MinutesApproved)
            throw new InvalidOperationException("Cannot change attendance after minutes are approved.");
        AttendanceJson = attendanceJson;
    }

    public void Complete(string? minutes, string? attendanceJson)
    {
        if (MinutesApproved)
            throw new InvalidOperationException("Cannot re-complete a meeting with approved minutes.");
        Status = MeetingStatus.Completed;
        Minutes = minutes;
        if (attendanceJson != null)
            AttendanceJson = attendanceJson;
        CompletedAt = DateTime.UtcNow;
    }

    public void ApproveMinutes(Guid approvedByUserId)
    {
        if (Status != MeetingStatus.Completed)
            throw new InvalidOperationException("Minutes can only be approved after the meeting is completed.");
        if (string.IsNullOrWhiteSpace(Minutes))
            throw new InvalidOperationException("Minutes must be recorded before approval.");
        if (MinutesApproved)
            throw new InvalidOperationException("Minutes are already approved.");
        MinutesApproved = true;
        MinutesApprovedAt = DateTime.UtcNow;
        MinutesApprovedByUserId = approvedByUserId;
    }

    public CommitteeDecision RecordDecision(
        Guid decisionTypeValueId,
        string summary,
        string? governedResourceType,
        Guid? governedResourceId,
        Guid? ownerAssigneeId,
        string? implementationStatus,
        Guid? deficiencyId = null,
        Guid? riskId = null,
        Guid? taskId = null)
    {
        if (Status == MeetingStatus.Completed && MinutesApproved)
            throw new InvalidOperationException("Cannot record decisions after minutes are approved.");
        var d = CommitteeDecision.Create(
            Id, decisionTypeValueId, summary, governedResourceType, governedResourceId,
            ownerAssigneeId, implementationStatus, deficiencyId, riskId, taskId);
        _decisions.Add(d);
        return d;
    }

    public CommitteeActionItem AddActionItem(
        string title,
        string? description,
        Guid? ownerAssigneeId,
        DateTime? dueDate,
        Guid? taskId,
        Guid? decisionId)
    {
        if (Status == MeetingStatus.Completed && MinutesApproved)
            throw new InvalidOperationException("Cannot add action items after minutes are approved.");
        var item = CommitteeActionItem.Create(Id, title, description, ownerAssigneeId, dueDate, taskId, decisionId);
        _actionItems.Add(item);
        return item;
    }
}

public class CommitteeDecision
{
    public Guid Id { get; private set; }
    public Guid CommitteeMeetingId { get; private set; }
    public Guid DecisionTypeValueId { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string? GovernedResourceType { get; private set; }
    public Guid? GovernedResourceId { get; private set; }
    public Guid? OwnerAssigneeId { get; private set; }
    public string? ImplementationStatus { get; private set; }
    public Guid? DeficiencyId { get; private set; }
    public Guid? RiskId { get; private set; }
    public Guid? TaskId { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private CommitteeDecision() { }

    public static CommitteeDecision Create(
        Guid meetingId,
        Guid decisionTypeValueId,
        string summary,
        string? governedResourceType,
        Guid? governedResourceId,
        Guid? ownerAssigneeId,
        string? implementationStatus,
        Guid? deficiencyId = null,
        Guid? riskId = null,
        Guid? taskId = null)
    {
        // Prefer typed links; keep governed resource as legacy/generic fallback.
        var resourceType = governedResourceType;
        var resourceId = governedResourceId;
        if (deficiencyId.HasValue)
        {
            resourceType ??= "Deficiency";
            resourceId ??= deficiencyId;
        }
        else if (riskId.HasValue)
        {
            resourceType ??= "Risk";
            resourceId ??= riskId;
        }
        else if (taskId.HasValue)
        {
            resourceType ??= "Task";
            resourceId ??= taskId;
        }

        return new CommitteeDecision
        {
            Id = Guid.NewGuid(),
            CommitteeMeetingId = meetingId,
            DecisionTypeValueId = decisionTypeValueId,
            Summary = summary.Trim(),
            GovernedResourceType = resourceType,
            GovernedResourceId = resourceId,
            OwnerAssigneeId = ownerAssigneeId,
            ImplementationStatus = implementationStatus,
            DeficiencyId = deficiencyId,
            RiskId = riskId,
            TaskId = taskId,
            RecordedAt = DateTime.UtcNow
        };
    }
}

public class CommitteeActionItem
{
    public Guid Id { get; private set; }
    public Guid CommitteeMeetingId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? OwnerAssigneeId { get; private set; }
    public DateTime? DueDate { get; private set; }
    public CommitteeActionItemStatus Status { get; private set; }
    /// <summary>Linked ComplianceTask id (created via Gateway/HTTP or supplied by client).</summary>
    public Guid? TaskId { get; private set; }
    public Guid? DecisionId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private CommitteeActionItem() { }

    public static CommitteeActionItem Create(
        Guid meetingId,
        string title,
        string? description,
        Guid? ownerAssigneeId,
        DateTime? dueDate,
        Guid? taskId,
        Guid? decisionId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Action item title is required.");

        return new CommitteeActionItem
        {
            Id = Guid.NewGuid(),
            CommitteeMeetingId = meetingId,
            Title = title.Trim(),
            Description = description?.Trim(),
            OwnerAssigneeId = ownerAssigneeId,
            DueDate = dueDate,
            Status = CommitteeActionItemStatus.Open,
            TaskId = taskId,
            DecisionId = decisionId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void LinkTask(Guid taskId)
    {
        if (taskId == Guid.Empty)
            throw new InvalidOperationException("TaskId is required.");
        if (Status is CommitteeActionItemStatus.Completed or CommitteeActionItemStatus.Cancelled)
            throw new InvalidOperationException("Cannot link a task to a closed action item.");
        TaskId = taskId;
    }

    public void UpdateStatus(CommitteeActionItemStatus status)
    {
        if (Status is CommitteeActionItemStatus.Completed or CommitteeActionItemStatus.Cancelled)
            throw new InvalidOperationException("Closed action items are immutable.");
        Status = status;
        if (status is CommitteeActionItemStatus.Completed or CommitteeActionItemStatus.Cancelled)
            CompletedAt = DateTime.UtcNow;
    }
}

public class AgendaTemplate
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>JSON array of section titles / prompts.</summary>
    public string SectionsJson { get; set; } = "[]";
    public bool IsManagementReview { get; set; }
    public int SortOrder { get; set; }
}

public class ApprovalStep
{
    public Guid Id { get; private set; }
    public Guid ApprovalChainId { get; private set; }
    public int StepOrder { get; private set; }
    public StepMode Mode { get; private set; }
    public string AssigneeType { get; private set; } = string.Empty;
    public Guid AssigneeId { get; private set; }
    public string? ConditionExpression { get; private set; }
    public Guid? FallbackStepId { get; private set; }

    private ApprovalStep() { }

    public static ApprovalStep Create(Guid chainId, int order, StepMode mode, string assigneeType, Guid assigneeId, string? conditionExpression, Guid? fallbackStepId)
    {
        return new ApprovalStep { Id = Guid.NewGuid(), ApprovalChainId = chainId, StepOrder = order, Mode = mode, AssigneeType = assigneeType, AssigneeId = assigneeId, ConditionExpression = conditionExpression, FallbackStepId = fallbackStepId };
    }
}

public class EscalationLevel
{
    public Guid Id { get; private set; }
    public Guid EscalationChainId { get; private set; }
    public int LevelOrder { get; private set; }
    public string TargetAssigneeType { get; private set; } = string.Empty;
    public Guid? TargetAssigneeId { get; private set; }
    public int? DelayHours { get; private set; }
    public string? ConditionExpression { get; private set; }

    private EscalationLevel() { }

    public static EscalationLevel Create(Guid chainId, int levelOrder, string targetAssigneeType, Guid? targetAssigneeId, int? delayHours, string? conditionExpression)
    {
        return new EscalationLevel { Id = Guid.NewGuid(), EscalationChainId = chainId, LevelOrder = levelOrder, TargetAssigneeType = targetAssigneeType, TargetAssigneeId = targetAssigneeId, DelayHours = delayHours, ConditionExpression = conditionExpression };
    }
}

public class GovernanceTimelineEntry
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? GovernedResourceType { get; set; }
    public Guid? GovernedResourceId { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
}

public class GovernanceAuditLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class GovernanceLookupCategory
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<GovernanceLookupValue> Values { get; set; } = new List<GovernanceLookupValue>();
}

public class GovernanceLookupValue
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public GovernanceLookupCategory? Category { get; set; }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedOn { get; set; }
    public DateTime? RelayProcessedOn { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
