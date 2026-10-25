using GovernanceService.Application.Contracts;

using GovernanceService.Domain.Aggregates;

using GovernanceService.Domain.Entities;

using GovernanceService.Domain.Enums;

using GovernanceService.Infrastructure.Clients;

using GovernanceService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shared.Communication;

using System.Text.Json;



namespace GovernanceService.Application.Services;



public class GovernanceTimelineProjector(GovernanceDbContext db) : IGovernanceTimelineService

{

    public async Task AppendAsync(string eventType, string summary, string? resourceType, Guid? resourceId, Guid? actorUserId, Guid? organizationId, object? payload = null, CancellationToken ct = default)

    {

        db.GovernanceTimeline.Add(new GovernanceTimelineEntry

        {

            Id = Guid.NewGuid(),

            EventType = eventType,

            Summary = summary,

            GovernedResourceType = resourceType,

            GovernedResourceId = resourceId,

            ActorUserId = actorUserId,

            OrganizationId = organizationId,

            OccurredAt = DateTime.UtcNow,

            PayloadJson = payload == null ? "{}" : JsonSerializer.Serialize(payload)

        });

        await db.SaveChangesAsync(ct);

    }



    public async Task<IReadOnlyList<GovernanceTimelineDto>> SearchAsync(string? resourceType, Guid? resourceId, Guid? organizationId, CancellationToken ct = default)

    {

        var q = db.GovernanceTimeline.AsQueryable();

        if (!string.IsNullOrEmpty(resourceType)) q = q.Where(t => t.GovernedResourceType == resourceType);

        if (resourceId.HasValue) q = q.Where(t => t.GovernedResourceId == resourceId);

        if (organizationId.HasValue) q = q.Where(t => t.OrganizationId == organizationId);

        return await q.OrderByDescending(t => t.OccurredAt).Take(200)

            .Select(t => new GovernanceTimelineDto

            {

                Id = t.Id, EventType = t.EventType, Summary = t.Summary, OccurredAt = t.OccurredAt,

                ActorUserId = t.ActorUserId, PayloadJson = t.PayloadJson

            })

            .ToListAsync(ct);

    }



    public async Task<OwnerLookupResultDto> GetOwnersAsync(string resourceType, Guid resourceId, string? responsibilityTypeCode, CancellationToken ct = default)

    {

        var now = DateTime.UtcNow;

        var respQuery = db.Responsibilities.Where(r => !r.IsDeleted && r.GovernedResourceType == resourceType && r.GovernedResourceId == resourceId);

        if (!string.IsNullOrEmpty(responsibilityTypeCode))

        {

            respQuery = respQuery.Where(r => db.LookupValues.Any(v => v.Id == r.ResponsibilityTypeValueId && v.Code == responsibilityTypeCode));

        }

        var respIds = await respQuery.Select(r => r.Id).ToListAsync(ct);

        var assignments = await db.GovernanceAssignments

            .Where(a => respIds.Contains(a.ResponsibilityId) && !a.IsDeleted && a.Status == GovernanceAssignmentStatus.Active)

            .ToListAsync(ct);

        var active = assignments.Where(a => a.IsActiveAt(now)).Select(a => new GovernanceAssignmentDto

        {

            Id = a.Id, ResponsibilityId = a.ResponsibilityId, AssigneeType = a.AssigneeType, AssigneeId = a.AssigneeId, Priority = a.Priority, Status = a.Status.ToString()

        }).ToList();

        return new OwnerLookupResultDto { Owners = active };

    }

}



public class ResponsibilityAppService(

    GovernanceDbContext db,

    IGovernanceTimelineService timeline,

    IGovernanceAuditWriter audit,

    IAssigneeValidationService assigneeValidator) : IResponsibilityAppService

{

    public async Task<ResponsibilityDto> CreateAsync(CreateResponsibilityRequest request, Guid actorUserId, CancellationToken ct = default)

    {

        if (await db.Responsibilities.AnyAsync(r => r.ResponsibilityCode == request.ResponsibilityCode, ct))

            throw new InvalidOperationException("Responsibility code already exists.");

        var r = Responsibility.Create(request.ResponsibilityCode, request.ResponsibilityTypeValueId, request.GovernedResourceType, request.GovernedResourceId, request.OrganizationId, request.HospitalId, request.RequiresPrimaryOwner);

        db.Responsibilities.Add(r);

        WriteOutbox("ResponsibilityAssigned", r.Id, new { r.Id, r.ResponsibilityCode });

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync("Responsibility", r.Id, "Created", actorUserId, ct);

        if (request.InitialAssignment != null)

            await AssignAsync(r.Id, request.InitialAssignment, actorUserId, ct);

        else

            await timeline.AppendAsync("ResponsibilityAssigned", $"Created {r.ResponsibilityCode}", r.GovernedResourceType, r.GovernedResourceId, actorUserId, request.OrganizationId, new { responsibilityId = r.Id }, ct);

        return Map(r);

    }



    public async Task<ResponsibilityDto?> GetByIdAsync(Guid id, CancellationToken ct = default)

    {

        var r = await db.Responsibilities.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        return r == null ? null : Map(r);

    }



    public async Task<PagedResultDto<ResponsibilityDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)

    {

        var q = db.Responsibilities.Where(r => !r.IsDeleted);

        var total = await q.CountAsync(ct);

        var items = await q.OrderBy(r => r.ResponsibilityCode).Skip((page - 1) * pageSize).Take(pageSize).Select(r => Map(r)).ToListAsync(ct);

        return new PagedResultDto<ResponsibilityDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };

    }



    public async Task UpdateAsync(Guid id, UpdateResponsibilityRequest request, CancellationToken ct = default)

    {

        var r = await db.Responsibilities.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Not found.");

        r.Update(request.ResponsibilityTypeValueId, request.RequiresPrimaryOwner);

        await db.SaveChangesAsync(ct);

    }



    public async Task DeleteAsync(Guid id, Guid actorUserId, CancellationToken ct = default)

    {

        var r = await db.Responsibilities.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Not found.");

        r.SoftDelete();

        WriteOutbox("ResponsibilityRevoked", id, new { id });

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync("Responsibility", id, "Revoked", actorUserId, ct);

        await timeline.AppendAsync("ResponsibilityRevoked", $"Revoked {r.ResponsibilityCode}", r.GovernedResourceType, r.GovernedResourceId, actorUserId, r.OrganizationId, new { responsibilityId = id }, ct);

    }



    public async Task<GovernanceAssignmentDto> AssignAsync(Guid responsibilityId, AssignGovernanceRequest request, Guid actorUserId, CancellationToken ct = default)

    {

        await assigneeValidator.ValidateAsync(request.AssigneeType, request.AssigneeId, ct);

        var resp = await db.Responsibilities.FirstOrDefaultAsync(r => r.Id == responsibilityId && !r.IsDeleted, ct) ?? throw new InvalidOperationException("Responsibility not found.");

        var now = DateTime.UtcNow;

        GovernanceAssignment? previousPrimary = null;

        if (request.Priority.Equals("Primary", StringComparison.OrdinalIgnoreCase) && resp.RequiresPrimaryOwner)

        {

            previousPrimary = await db.GovernanceAssignments.FirstOrDefaultAsync(a =>

                a.ResponsibilityId == responsibilityId && !a.IsDeleted && a.Status == GovernanceAssignmentStatus.Active &&

                a.Priority == "Primary" && a.EffectiveFrom <= now && (!a.EffectiveTo.HasValue || a.EffectiveTo > now), ct);

            if (previousPrimary != null && previousPrimary.AssigneeId == request.AssigneeId && previousPrimary.AssigneeType == request.AssigneeType)

                throw new InvalidOperationException("Primary owner already assigned to this assignee.");

        }

        var overlap = await db.GovernanceAssignments.AnyAsync(a =>

            a.ResponsibilityId == responsibilityId && a.AssigneeType == request.AssigneeType && a.AssigneeId == request.AssigneeId &&

            !a.IsDeleted && a.Status == GovernanceAssignmentStatus.Active &&

            a.EffectiveFrom <= (request.EffectiveTo ?? DateTime.MaxValue) &&

            (a.EffectiveTo == null || a.EffectiveTo > request.EffectiveFrom), ct);

        if (overlap) throw new InvalidOperationException("Overlapping active assignment.");



        if (previousPrimary != null)

        {

            previousPrimary.Revoke();

            WriteOutbox("OwnershipChanged", resp.Id, new { responsibilityId, previousAssigneeId = previousPrimary.AssigneeId, newAssigneeId = request.AssigneeId });

        }



        var assignment = GovernanceAssignment.Create(responsibilityId, request.AssigneeType, request.AssigneeId, request.Priority, request.AssignmentMode, request.EffectiveFrom, request.EffectiveTo, null, request.Reason, actorUserId);

        db.GovernanceAssignments.Add(assignment);

        WriteOutbox("ResponsibilityAssigned", assignment.Id, new { assignmentId = assignment.Id, responsibilityId, request.AssigneeType, request.AssigneeId, request.Priority });

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync("GovernanceAssignment", assignment.Id, "Assigned", actorUserId, ct);

        await timeline.AppendAsync(previousPrimary != null ? "OwnershipChanged" : "ResponsibilityAssigned",

            $"Assigned {request.AssigneeType} ({request.Priority})", resp.GovernedResourceType, resp.GovernedResourceId, actorUserId, resp.OrganizationId,

            new { assignmentId = assignment.Id, responsibilityId, request.Priority }, ct);

        return new GovernanceAssignmentDto { Id = assignment.Id, ResponsibilityId = responsibilityId, AssigneeType = assignment.AssigneeType, AssigneeId = assignment.AssigneeId, Priority = assignment.Priority, Status = assignment.Status.ToString() };

    }



    private void WriteOutbox(string type, Guid aggregateId, object payload) =>

        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = aggregateId, OccurredOn = DateTime.UtcNow, Type = type, Payload = JsonSerializer.Serialize(payload) });



    private static ResponsibilityDto Map(Responsibility r) => new()

    {

        Id = r.Id, ResponsibilityCode = r.ResponsibilityCode, ResponsibilityTypeValueId = r.ResponsibilityTypeValueId,

        GovernedResourceType = r.GovernedResourceType, GovernedResourceId = r.GovernedResourceId, RequiresPrimaryOwner = r.RequiresPrimaryOwner

    };

}



public partial class CommitteeAppService(GovernanceDbContext db, IGovernanceTimelineService timeline, IGovernanceAuditWriter audit) : ICommitteeAppService

{

    public async Task<CommitteeDto> CreateAsync(CreateCommitteeRequest request, Guid actorUserId, CancellationToken ct = default)

    {

        if (await db.Committees.AnyAsync(c => c.CommitteeCode == request.CommitteeCode, ct))

            throw new InvalidOperationException("Committee code exists.");

        var c = Committee.Create(request.CommitteeCode, request.CommitteeName, request.CommitteeTypeValueId, request.Charter, request.OrganizationId, request.HospitalId, request.TermStart, request.TermEnd, request.ChairpersonAssigneeId, request.SecretaryAssigneeId);

        db.Committees.Add(c);

        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = c.Id, OccurredOn = DateTime.UtcNow, Type = "CommitteeCreated", Payload = JsonSerializer.Serialize(new { committeeId = c.Id }) });

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync("Committee", c.Id, "Created", actorUserId, ct);

        await timeline.AppendAsync("CommitteeCreated", c.CommitteeName, "Committee", c.Id, actorUserId, request.OrganizationId, new { committeeId = c.Id }, ct);

        return Map(c);

    }



    public async Task<CommitteeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)

    {

        var c = await db.Committees.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        return c == null ? null : Map(c);

    }



    public async Task UpdateAsync(Guid id, UpdateCommitteeRequest request, Guid actorUserId, CancellationToken ct = default)

    {

        var c = await db.Committees.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Committee not found.");

        c.Update(request.CommitteeName, request.CommitteeTypeValueId, request.Charter, request.TermStart, request.TermEnd, request.ChairpersonAssigneeId, request.SecretaryAssigneeId);

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync("Committee", id, "Updated", actorUserId, ct);

    }



    public async Task<IReadOnlyList<CommitteeDto>> ListAsync(CancellationToken ct = default)

    {

        var rows = await db.Committees.Where(c => !c.IsDeleted).ToListAsync(ct);

        return rows.Select(Map).ToList();

    }



    public async Task AddMemberAsync(Guid committeeId, AddCommitteeMemberRequest request, Guid actorUserId, CancellationToken ct = default)

    {

        var committee = await db.Committees.Include(c => c.Memberships).FirstOrDefaultAsync(c => c.Id == committeeId && !c.IsDeleted, ct) ?? throw new InvalidOperationException("Committee not found.");

        committee.AddMember(request.MemberAssigneeId, request.CommitteeRoleValueId, request.TermStart, request.TermEnd, request.VotingRights);

        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = committeeId, OccurredOn = DateTime.UtcNow, Type = "CommitteeMemberAdded", Payload = JsonSerializer.Serialize(new { committeeId, request.MemberAssigneeId }) });

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync("Committee", committeeId, "MemberAdded", actorUserId, ct);

        await timeline.AppendAsync("CommitteeJoined", "Member added", "Committee", committeeId, actorUserId, committee.OrganizationId, new { memberAssigneeId = request.MemberAssigneeId }, ct);

    }



    public async Task CompleteMeetingAsync(Guid committeeId, Guid meetingId, CompleteMeetingRequest request, Guid actorUserId, CancellationToken ct = default)

    {

        var committee = await db.Committees.FirstOrDefaultAsync(c => c.Id == committeeId && !c.IsDeleted, ct) ?? throw new InvalidOperationException("Committee not found.");

        var meeting = await db.CommitteeMeetings.FirstOrDefaultAsync(m => m.Id == meetingId && m.CommitteeId == committeeId, ct) ?? throw new InvalidOperationException("Meeting not found.");

        meeting.Complete(request.Minutes, request.AttendanceJson);

        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = meetingId, OccurredOn = DateTime.UtcNow, Type = "CommitteeMeetingCompleted", Payload = JsonSerializer.Serialize(new { committeeId, meetingId }) });

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync("CommitteeMeeting", meetingId, "Completed", actorUserId, ct);

        await timeline.AppendAsync("CommitteeMeetingCompleted", "Meeting completed", "Committee", committeeId, actorUserId, committee.OrganizationId, new { meetingId }, ct);

    }



    private static CommitteeDto Map(Committee c) => new()

    {

        Id = c.Id, CommitteeCode = c.CommitteeCode, CommitteeName = c.CommitteeName, Status = c.Status.ToString(),

        ChairpersonAssigneeId = c.ChairpersonAssigneeId, SecretaryAssigneeId = c.SecretaryAssigneeId, OrganizationId = c.OrganizationId

    };

}



public class ApprovalChainAppService(GovernanceDbContext db, IGovernanceAuditWriter audit) : IApprovalChainAppService

{

    public async Task<ApprovalChainDto> CreateAsync(CreateApprovalChainRequest request, CancellationToken ct = default)

    {

        if (await db.ApprovalChains.AnyAsync(c => c.ChainCode == request.ChainCode, ct))

            throw new InvalidOperationException("Chain code exists.");

        var chain = ApprovalChain.Create(request.ChainCode, request.ChainName, request.ApprovalTypeValueId, request.OrganizationId);

        foreach (var s in request.Steps.OrderBy(x => x.StepOrder))

        {

            var mode = Enum.TryParse<StepMode>(s.Mode, true, out var m) ? m : StepMode.Sequential;

            chain.AddStep(s.StepOrder, mode, s.AssigneeType, s.AssigneeId, s.ConditionExpression, null);

        }

        db.ApprovalChains.Add(chain);

        await db.SaveChangesAsync(ct);

        return new ApprovalChainDto { Id = chain.Id, ChainCode = chain.ChainCode, ChainName = chain.ChainName, Status = chain.Status.ToString() };

    }



    public async Task PublishAsync(Guid id, CancellationToken ct = default)

    {

        var chain = await db.ApprovalChains.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct) ?? throw new InvalidOperationException("Not found.");

        chain.Publish();

        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = id, OccurredOn = DateTime.UtcNow, Type = "ApprovalChainPublished", Payload = JsonSerializer.Serialize(new { chainId = id }) });

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync("ApprovalChain", id, "Published", Guid.Empty, ct);

    }



    public async Task<IReadOnlyList<ApprovalChainDto>> ListAsync(CancellationToken ct = default)

        => await db.ApprovalChains.Where(c => !c.IsDeleted).Select(c => new ApprovalChainDto { Id = c.Id, ChainCode = c.ChainCode, ChainName = c.ChainName, Status = c.Status.ToString() }).ToListAsync(ct);

}



public class EscalationChainAppService(GovernanceDbContext db) : IEscalationChainAppService

{

    public async Task<EscalationChainDto> CreateAsync(CreateEscalationChainRequest request, CancellationToken ct = default)

    {

        if (await db.EscalationChains.AnyAsync(c => c.ChainCode == request.ChainCode, ct))

            throw new InvalidOperationException("Chain code exists.");

        ValidateNoEscalationLoop(request.Levels);

        var chain = EscalationChain.Create(request.ChainCode, request.ChainName, request.EscalationTypeValueId, request.OrganizationId);

        foreach (var l in request.Levels.OrderBy(x => x.LevelOrder))

            chain.AddLevel(l.LevelOrder, l.TargetAssigneeType, l.TargetAssigneeId, l.DelayHours, l.ConditionExpression);

        db.EscalationChains.Add(chain);

        await db.SaveChangesAsync(ct);

        return new EscalationChainDto { Id = chain.Id, ChainCode = chain.ChainCode, ChainName = chain.ChainName, Status = chain.Status.ToString() };

    }



    public async Task PublishAsync(Guid id, CancellationToken ct = default)

    {

        var chain = await db.EscalationChains.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct) ?? throw new InvalidOperationException("Not found.");

        chain.Publish();

        await db.SaveChangesAsync(ct);

    }



    public async Task<EscalationChainDto?> GetByIdAsync(Guid id, CancellationToken ct = default)

    {

        var c = await db.EscalationChains.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        return c == null ? null : new EscalationChainDto { Id = c.Id, ChainCode = c.ChainCode, ChainName = c.ChainName, Status = c.Status.ToString() };

    }



    public async Task<IReadOnlyList<EscalationChainDto>> ListAsync(CancellationToken ct = default)

        => await db.EscalationChains.Where(c => !c.IsDeleted).Select(c => new EscalationChainDto { Id = c.Id, ChainCode = c.ChainCode, ChainName = c.ChainName, Status = c.Status.ToString() }).ToListAsync(ct);



    private static void ValidateNoEscalationLoop(List<CreateEscalationLevelRequest> levels)

    {

        var targets = levels.Where(l => l.TargetAssigneeId.HasValue).Select(l => $"{l.TargetAssigneeType}:{l.TargetAssigneeId}").ToList();

        if (targets.Distinct().Count() != targets.Count)

            throw new InvalidOperationException("Escalation loop detected — duplicate target at multiple levels.");

    }

}



public class ApprovalWorkflowEvaluatorService(GovernanceDbContext db) : IApprovalWorkflowEvaluatorService

{

    public async Task<ApprovalDecisionDto> EvaluateAsync(ApprovalEvaluateRequest request, CancellationToken ct = default)

    {

        var chain = await db.ApprovalChains.Include(c => c.Steps).FirstOrDefaultAsync(c => c.Id == request.ApprovalChainId && c.Status == ChainStatus.Published && !c.IsDeleted, ct);

        if (chain == null) return new ApprovalDecisionDto { Reason = "Approval chain not found or not published." };



        if (request.RejectedStepOrder.HasValue)

            return new ApprovalDecisionDto { Completed = true, Approved = false, Reason = "Rejected", CurrentStep = request.RejectedStepOrder };



        var approved = ParseApprovedSteps(request);

        var applicable = chain.Steps

            .Where(s => s.Mode != StepMode.Conditional || MatchesCondition(s.ConditionExpression, request.Context))

            .OrderBy(s => s.StepOrder)

            .ToList();



        if (applicable.Count == 0)

            return new ApprovalDecisionDto { Completed = true, Approved = false, Reason = "No applicable steps" };



        var matched = new List<string>();

        var pending = new List<string>();

        var stepGroups = applicable.GroupBy(s => s.StepOrder).OrderBy(g => g.Key);



        foreach (var group in stepGroups)

        {

            var steps = group.ToList();

            var allApproved = steps.All(s => approved.Contains(s.StepOrder));

            if (!allApproved)

            {

                foreach (var step in steps.Where(s => !approved.Contains(s.StepOrder)))

                {

                    matched.Add($"Step{step.StepOrder}:{step.AssigneeType}");

                    pending.Add($"{step.AssigneeType}:{step.AssigneeId}");

                }

                var mode = steps[0].Mode;

                return new ApprovalDecisionDto

                {

                    Completed = false,

                    Approved = false,

                    Reason = "Pending approval",

                    CurrentStep = group.Key,

                    MatchedSteps = matched,

                    PendingApprovers = pending

                };

            }

            foreach (var step in steps)

                matched.Add($"Step{step.StepOrder}:{step.AssigneeType}");

            if (steps.Any(s => s.Mode == StepMode.Sequential))

                break;

        }



        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = chain.Id, OccurredOn = DateTime.UtcNow, Type = "ApprovalCompleted", Payload = JsonSerializer.Serialize(new { chainId = chain.Id }) });

        await db.SaveChangesAsync(ct);

        return new ApprovalDecisionDto { Completed = true, Approved = true, Reason = "All steps approved", MatchedSteps = matched, PendingApprovers = Array.Empty<string>() };

    }



    private static HashSet<int> ParseApprovedSteps(ApprovalEvaluateRequest request)

    {

        var raw = request.ApprovedSteps;

        if (string.IsNullOrWhiteSpace(raw) && request.Context.TryGetValue("approvedSteps", out var ctx))

            raw = ctx;

        if (string.IsNullOrWhiteSpace(raw)) return new HashSet<int>();

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)

            .Select(s => int.TryParse(s, out var n) ? n : -1).Where(n => n > 0).ToHashSet();

    }



    private static bool MatchesCondition(string? expression, Dictionary<string, string> context)

    {

        if (string.IsNullOrEmpty(expression)) return true;

        if (!context.TryGetValue(expression, out var val)) return false;

        return val.Equals("true", StringComparison.OrdinalIgnoreCase);

    }

}



public class EscalationResolverService(

    GovernanceDbContext db,

    IGovernanceTimelineService timeline,

    IWorkforceServiceClient workforceClient,

    ICommunicationClient communicationClient,

    IIdentityContactClient identityContactClient) : IEscalationResolverService

{

    private const int MaxDepth = 10;



    public async Task<EscalationResolutionDto> ResolveAsync(EscalationResolveRequest request, CancellationToken ct = default)

    {

        if (request.CurrentLevel >= MaxDepth)

            return new EscalationResolutionDto { ChainId = request.EscalationChainId, Reason = "Max escalation depth reached", EscalationComplete = true };



        var chain = await db.EscalationChains.Include(c => c.Levels).FirstOrDefaultAsync(c => c.Id == request.EscalationChainId && c.Status == ChainStatus.Published && !c.IsDeleted, ct);

        if (chain == null) return new EscalationResolutionDto { ChainId = request.EscalationChainId, Reason = "Chain not found" };



        var candidates = chain.Levels.Where(l => l.LevelOrder > request.CurrentLevel).OrderBy(l => l.LevelOrder).ToList();

        foreach (var level in candidates)

        {

            if (level.DelayHours.HasValue && request.HoursSinceOpen.HasValue && request.HoursSinceOpen < level.DelayHours)

                continue;

            if (!string.IsNullOrEmpty(level.ConditionExpression))

            {

                if (!request.Context.TryGetValue(level.ConditionExpression, out var val) || val != "true")

                    continue;

            }

            if (!string.IsNullOrEmpty(request.Severity) && !string.IsNullOrEmpty(level.ConditionExpression) &&

                level.ConditionExpression.StartsWith("severity:", StringComparison.OrdinalIgnoreCase))

            {

                var required = level.ConditionExpression["severity:".Length..];

                if (!string.Equals(required, request.Severity, StringComparison.OrdinalIgnoreCase))

                    continue;

            }



            var assigneeType = level.TargetAssigneeType;

            var assigneeId = level.TargetAssigneeId;

            if (assigneeType.Equals("ManagerChain", StringComparison.OrdinalIgnoreCase) && request.CurrentEmploymentId.HasValue)

            {

                var managerId = await workforceClient.GetManagerEmploymentIdAsync(request.CurrentEmploymentId.Value, ct);

                if (!managerId.HasValue)

                    return new EscalationResolutionDto { ChainId = chain.Id, Reason = "No manager in chain", EscalationComplete = true };

                assigneeType = "Employment";

                assigneeId = managerId;

            }



            await timeline.AppendAsync("EscalationTriggered", $"Level {level.LevelOrder}", null, null, request.CurrentAssigneeUserId, null, new { level = level.LevelOrder }, ct);

            db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = chain.Id, OccurredOn = DateTime.UtcNow, Type = "EscalationTriggered", Payload = JsonSerializer.Serialize(new { level = level.LevelOrder }) });

            await db.SaveChangesAsync(ct);

            if (request.CurrentAssigneeUserId.HasValue)
            {
                try
                {
                    var (personId, email) = await identityContactClient.GetUserContactAsync(request.CurrentAssigneeUserId.Value, ct);
                    await communicationClient.SendAsync(new CommunicationRequestDto
                    {
                        ModuleCode = "Governance",
                        EventType = "EscalationTriggered",
                        PersonId = personId,
                        RecipientEmail = email,
                        TemplateCode = "governance-escalation",
                        Critical = true,
                        Variables = new Dictionary<string, string> { ["level"] = level.LevelOrder.ToString(), ["message"] = $"Escalation level {level.LevelOrder}" },
                        DedupeKey = $"EscalationTriggered:{chain.Id}:{level.LevelOrder}:{request.CurrentAssigneeUserId}"
                    }, ct);
                }
                catch { /* communication must not block escalation state */ }
            }

            return new EscalationResolutionDto

            {

                ChainId = chain.Id,

                ResolvedLevel = level.LevelOrder,

                NextAssigneeType = assigneeType,

                NextAssigneeId = assigneeId,

                Reason = "Escalated to next level",

                EscalationComplete = false

            };

        }



        return new EscalationResolutionDto { ChainId = request.EscalationChainId, Reason = "No further levels", EscalationComplete = true };

    }

}



public class GovernanceAnalyticsService(GovernanceDbContext db) : IGovernanceAnalyticsService, IGovernanceKpiService

{

    public async Task<GovernanceAnalyticsDto> GetSummaryAsync(CancellationToken ct = default)

    {

        var now = DateTime.UtcNow;

        var mrTypeIds = await db.LookupValues.Include(v => v.Category)

            .Where(v => v.Category!.Code == "CommitteeType" && v.Code == "ManagementReview")

            .Select(v => v.Id)

            .ToListAsync(ct);

        return new GovernanceAnalyticsDto

        {

            ActiveResponsibilities = await db.Responsibilities.CountAsync(r => !r.IsDeleted, ct),

            ActiveCommittees = await db.Committees.CountAsync(c => !c.IsDeleted && c.Status == CommitteeStatus.Active, ct),

            PublishedApprovalChains = await db.ApprovalChains.CountAsync(c => !c.IsDeleted && c.Status == ChainStatus.Published, ct),

            ActiveAssignments = await db.GovernanceAssignments.CountAsync(a => !a.IsDeleted && a.Status == GovernanceAssignmentStatus.Active && a.EffectiveFrom <= now && (a.EffectiveTo == null || a.EffectiveTo > now), ct),

            ScheduledMeetings = await db.CommitteeMeetings.CountAsync(m => m.Status == MeetingStatus.Scheduled, ct),

            CompletedMeetings = await db.CommitteeMeetings.CountAsync(m => m.Status == MeetingStatus.Completed, ct),

            MeetingsPendingMinutesApproval = await db.CommitteeMeetings.CountAsync(m => m.Status == MeetingStatus.Completed && !m.MinutesApproved, ct),

            OpenActionItems = await db.CommitteeActionItems.CountAsync(a => a.Status == CommitteeActionItemStatus.Open || a.Status == CommitteeActionItemStatus.InProgress, ct),

            ActionItemsWithLinkedTasks = await db.CommitteeActionItems.CountAsync(a => a.TaskId != null, ct),

            ManagementReviewCommittees = mrTypeIds.Count == 0

                ? 0

                : await db.Committees.CountAsync(c => !c.IsDeleted && mrTypeIds.Contains(c.CommitteeTypeValueId), ct)

        };

    }

    public async Task<GovernanceKpisDto> GetKpisAsync(CancellationToken ct = default)

    {

        var summary = await GetSummaryAsync(ct);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new GovernanceKpisDto

        {

            ActiveCommittees = summary.ActiveCommittees,

            OpenActionItems = summary.OpenActionItems,

            MeetingsThisMonth = await db.CommitteeMeetings.CountAsync(m => m.ScheduledAt >= monthStart, ct),

            PendingMinutesApprovals = summary.MeetingsPendingMinutesApproval,

            DocumentsEffective = 0,

            TrainingOverdue = 0,

            OpenIncidents = 0,

            OpenRisks = 0,

            SourceNotes = "Committee/meeting/action KPIs from GovernanceService. Documents/training/incidents/risks enriched by Gateway when Compliance clients respond."

        };

    }

}



public class LookupAppService(GovernanceDbContext db)

{

    public async Task<IReadOnlyList<LookupValueDto>> GetByCategoryAsync(string category, CancellationToken ct = default)

        => await db.LookupValues.Include(v => v.Category).Where(v => v.Category!.Code == category)

            .OrderBy(v => v.SortOrder).Select(v => new LookupValueDto { Id = v.Id, Code = v.Code, Name = v.Name }).ToListAsync(ct);

}


