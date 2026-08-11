using GovernanceService.Domain.Entities;
using GovernanceService.Domain.Enums;

namespace GovernanceService.Domain.Aggregates;

public class Responsibility
{
    public Guid Id { get; private set; }
    public string ResponsibilityCode { get; private set; } = string.Empty;
    public Guid ResponsibilityTypeValueId { get; private set; }
    public string GovernedResourceType { get; private set; } = string.Empty;
    public Guid? GovernedResourceId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Guid? HospitalId { get; private set; }
    public bool RequiresPrimaryOwner { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private Responsibility() { }

    public static Responsibility Create(string code, Guid responsibilityTypeValueId, string governedResourceType, Guid? governedResourceId, Guid? organizationId, Guid? hospitalId, bool requiresPrimaryOwner)
    {
        return new Responsibility
        {
            Id = Guid.NewGuid(),
            ResponsibilityCode = code.Trim(),
            ResponsibilityTypeValueId = responsibilityTypeValueId,
            GovernedResourceType = governedResourceType.Trim(),
            GovernedResourceId = governedResourceId,
            OrganizationId = organizationId,
            HospitalId = hospitalId,
            RequiresPrimaryOwner = requiresPrimaryOwner,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(Guid responsibilityTypeValueId, bool requiresPrimaryOwner)
    {
        ResponsibilityTypeValueId = responsibilityTypeValueId;
        RequiresPrimaryOwner = requiresPrimaryOwner;
    }

    public void SoftDelete() => IsDeleted = true;
}

public class GovernanceAssignment
{
    public Guid Id { get; private set; }
    public Guid ResponsibilityId { get; private set; }
    public string AssigneeType { get; private set; } = string.Empty;
    public Guid AssigneeId { get; private set; }
    public string Priority { get; private set; } = "Secondary";
    public string AssignmentMode { get; private set; } = "Permanent";
    public GovernanceAssignmentStatus Status { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public Guid? DelegatedFromAssignmentId { get; private set; }
    public string? Reason { get; private set; }
    public Guid AssignedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private GovernanceAssignment() { }

    public static GovernanceAssignment Create(Guid responsibilityId, string assigneeType, Guid assigneeId, string priority, string assignmentMode, DateTime effectiveFrom, DateTime? effectiveTo, Guid? delegatedFrom, string? reason, Guid assignedByUserId)
    {
        if (effectiveTo.HasValue && effectiveTo <= effectiveFrom)
            throw new InvalidOperationException("EffectiveTo must be after EffectiveFrom.");
        return new GovernanceAssignment
        {
            Id = Guid.NewGuid(),
            ResponsibilityId = responsibilityId,
            AssigneeType = assigneeType,
            AssigneeId = assigneeId,
            Priority = priority,
            AssignmentMode = assignmentMode,
            Status = GovernanceAssignmentStatus.Active,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            DelegatedFromAssignmentId = delegatedFrom,
            Reason = reason,
            AssignedByUserId = assignedByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke() { Status = GovernanceAssignmentStatus.Revoked; IsDeleted = true; }

    public bool IsActiveAt(DateTime utcNow) =>
        !IsDeleted && Status == GovernanceAssignmentStatus.Active &&
        EffectiveFrom <= utcNow && (!EffectiveTo.HasValue || EffectiveTo > utcNow);
}

public class Committee
{
    public Guid Id { get; private set; }
    public string CommitteeCode { get; private set; } = string.Empty;
    public string CommitteeName { get; private set; } = string.Empty;
    public Guid CommitteeTypeValueId { get; private set; }
    public string? Charter { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Guid? HospitalId { get; private set; }
    public Guid? ChairpersonAssigneeId { get; private set; }
    public Guid? SecretaryAssigneeId { get; private set; }
    public DateTime? TermStart { get; private set; }
    public DateTime? TermEnd { get; private set; }
    public CommitteeStatus Status { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private readonly List<CommitteeMembership> _memberships = new();
    private readonly List<CommitteeMeeting> _meetings = new();
    public IReadOnlyCollection<CommitteeMembership> Memberships => _memberships.AsReadOnly();
    public IReadOnlyCollection<CommitteeMeeting> Meetings => _meetings.AsReadOnly();

    private Committee() { }

    public static Committee Create(string code, string name, Guid committeeTypeValueId, string? charter, Guid? organizationId, Guid? hospitalId, DateTime? termStart, DateTime? termEnd, Guid? chairpersonAssigneeId = null, Guid? secretaryAssigneeId = null)
    {
        if (termEnd.HasValue && termStart.HasValue && termEnd <= termStart)
            throw new InvalidOperationException("Committee term end must be after term start.");
        return new Committee
        {
            Id = Guid.NewGuid(),
            CommitteeCode = code.Trim(),
            CommitteeName = name.Trim(),
            CommitteeTypeValueId = committeeTypeValueId,
            Charter = charter,
            OrganizationId = organizationId,
            HospitalId = hospitalId,
            ChairpersonAssigneeId = chairpersonAssigneeId,
            SecretaryAssigneeId = secretaryAssigneeId,
            TermStart = termStart,
            TermEnd = termEnd,
            Status = CommitteeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, Guid committeeTypeValueId, string? charter, DateTime? termStart, DateTime? termEnd, Guid? chairpersonAssigneeId, Guid? secretaryAssigneeId)
    {
        if (termEnd.HasValue && termStart.HasValue && termEnd <= termStart)
            throw new InvalidOperationException("Committee term end must be after term start.");
        CommitteeName = name.Trim();
        CommitteeTypeValueId = committeeTypeValueId;
        Charter = charter;
        TermStart = termStart;
        TermEnd = termEnd;
        ChairpersonAssigneeId = chairpersonAssigneeId;
        SecretaryAssigneeId = secretaryAssigneeId;
    }

    public CommitteeMeeting? FindMeeting(Guid meetingId) => _meetings.FirstOrDefault(m => m.Id == meetingId);

    public CommitteeMembership AddMember(Guid memberAssigneeId, Guid committeeRoleValueId, DateTime termStart, DateTime? termEnd, bool votingRights)
    {
        var m = CommitteeMembership.Create(Id, memberAssigneeId, committeeRoleValueId, termStart, termEnd, votingRights);
        _memberships.Add(m);
        return m;
    }

    public CommitteeMeeting ScheduleMeeting(DateTime scheduledAt, Guid meetingTypeValueId, string? agenda, string? agendaTemplateCode = null)
    {
        var meeting = CommitteeMeeting.Create(Id, scheduledAt, meetingTypeValueId, agenda, agendaTemplateCode);
        _meetings.Add(meeting);
        return meeting;
    }

    public void SoftDelete() { IsDeleted = true; Status = CommitteeStatus.Dissolved; }
}

public class ApprovalChain
{
    public Guid Id { get; private set; }
    public string ChainCode { get; private set; } = string.Empty;
    public string ChainName { get; private set; } = string.Empty;
    public Guid ApprovalTypeValueId { get; private set; }
    public ChainStatus Status { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private readonly List<ApprovalStep> _steps = new();
    public IReadOnlyCollection<ApprovalStep> Steps => _steps.AsReadOnly();

    private ApprovalChain() { }

    public static ApprovalChain Create(string code, string name, Guid approvalTypeValueId, Guid? organizationId)
    {
        return new ApprovalChain
        {
            Id = Guid.NewGuid(),
            ChainCode = code.Trim(),
            ChainName = name.Trim(),
            ApprovalTypeValueId = approvalTypeValueId,
            OrganizationId = organizationId,
            Status = ChainStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public ApprovalStep AddStep(int order, StepMode mode, string assigneeType, Guid assigneeId, string? conditionExpression, Guid? fallbackStepId)
    {
        var step = ApprovalStep.Create(Id, order, mode, assigneeType, assigneeId, conditionExpression, fallbackStepId);
        _steps.Add(step);
        return step;
    }

    public void Publish()
    {
        if (Status != ChainStatus.Draft) throw new InvalidOperationException("Only draft chains can be published.");
        Status = ChainStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void SoftDelete() => IsDeleted = true;
}

public class EscalationChain
{
    public Guid Id { get; private set; }
    public string ChainCode { get; private set; } = string.Empty;
    public string ChainName { get; private set; } = string.Empty;
    public Guid EscalationTypeValueId { get; private set; }
    public ChainStatus Status { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private readonly List<EscalationLevel> _levels = new();
    public IReadOnlyCollection<EscalationLevel> Levels => _levels.AsReadOnly();

    private EscalationChain() { }

    public static EscalationChain Create(string code, string name, Guid escalationTypeValueId, Guid? organizationId)
    {
        return new EscalationChain
        {
            Id = Guid.NewGuid(),
            ChainCode = code.Trim(),
            ChainName = name.Trim(),
            EscalationTypeValueId = escalationTypeValueId,
            OrganizationId = organizationId,
            Status = ChainStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public EscalationLevel AddLevel(int levelOrder, string targetAssigneeType, Guid? targetAssigneeId, int? delayHours, string? conditionExpression)
    {
        var level = EscalationLevel.Create(Id, levelOrder, targetAssigneeType, targetAssigneeId, delayHours, conditionExpression);
        _levels.Add(level);
        return level;
    }

    public void Publish()
    {
        if (Status != ChainStatus.Draft) throw new InvalidOperationException("Only draft chains can be published.");
        Status = ChainStatus.Published;
    }

    public void SoftDelete() => IsDeleted = true;
}
