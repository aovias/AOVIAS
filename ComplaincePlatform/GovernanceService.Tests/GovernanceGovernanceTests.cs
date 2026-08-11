using GovernanceService.Application.Contracts;
using GovernanceService.Application.Services;
using GovernanceService.Domain.Aggregates;
using GovernanceService.Domain.Enums;
using GovernanceService.Infrastructure.Clients;
using GovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Communication;
using Xunit;

namespace GovernanceService.Tests;

public class GovernanceGovernanceTests
{
    private sealed class NoOpAssigneeValidator : IAssigneeValidationService
    {
        public Task ValidateAsync(string assigneeType, Guid assigneeId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoOpWorkforceClient : IWorkforceServiceClient
    {
        public Task<bool> EmploymentExistsAsync(Guid employmentId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<Guid?> GetManagerEmploymentIdAsync(Guid employmentId, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
    }

    private static GovernanceDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<GovernanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GovernanceDbContext(options);
    }

    private static (ResponsibilityAppService Resp, GovernanceTimelineProjector Timeline, GovernanceDbContext Db) CreateResponsibilityStack()
    {
        var db = CreateDb();
        var timeline = new GovernanceTimelineProjector(db);
        var audit = new GovernanceAuditWriter(db);
        var svc = new ResponsibilityAppService(db, timeline, audit, new NoOpAssigneeValidator());
        return (svc, timeline, db);
    }

    private static CreateResponsibilityRequest BaseResponsibility(string code = "RESP-1") => new()
    {
        ResponsibilityCode = code,
        ResponsibilityTypeValueId = Guid.NewGuid(),
        GovernedResourceType = "ComplianceJourney",
        GovernedResourceId = Guid.NewGuid(),
        RequiresPrimaryOwner = true
    };

    private static AssignGovernanceRequest Assign(Guid assigneeId, string priority = "Primary") => new()
    {
        AssigneeType = "Position",
        AssigneeId = assigneeId,
        Priority = priority,
        EffectiveFrom = DateTime.UtcNow.AddDays(-1)
    };

    [Fact]
    public async Task Primary_reassignment_emits_ownership_changed()
    {
        var (svc, _, db) = CreateResponsibilityStack();
        var actor = Guid.NewGuid();
        var resp = await svc.CreateAsync(BaseResponsibility(), actor);
        await svc.AssignAsync(resp.Id, Assign(Guid.NewGuid(), "Primary"), actor);
        await svc.AssignAsync(resp.Id, Assign(Guid.NewGuid(), "Primary"), actor);
        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Type == "OwnershipChanged"));
    }

    [Fact]
    public async Task Overlapping_active_assignment_rejected()
    {
        var (svc, _, _) = CreateResponsibilityStack();
        var actor = Guid.NewGuid();
        var assignee = Guid.NewGuid();
        var resp = await svc.CreateAsync(BaseResponsibility(), actor);
        await svc.AssignAsync(resp.Id, Assign(assignee, "Secondary"), actor);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.AssignAsync(resp.Id, Assign(assignee, "Secondary"), actor));
    }

    [Fact]
    public void EffectiveTo_before_EffectiveFrom_rejected()
    {
        var from = DateTime.UtcNow;
        Assert.Throws<InvalidOperationException>(() =>
            GovernanceAssignment.Create(Guid.NewGuid(), "Person", Guid.NewGuid(), "Secondary", "Permanent", from, from.AddDays(-1), null, null, Guid.NewGuid()));
    }

    [Fact]
    public void Committee_term_end_before_start_rejected()
    {
        var start = DateTime.UtcNow;
        Assert.Throws<InvalidOperationException>(() =>
            Committee.Create("C1", "Committee 1", Guid.NewGuid(), null, null, null, start, start.AddDays(-1)));
    }

    [Fact]
    public async Task Approval_evaluate_includes_matched_steps()
    {
        await using var db = CreateDb();
        var chainSvc = new ApprovalChainAppService(db, new GovernanceAuditWriter(db));
        var evaluator = new ApprovalWorkflowEvaluatorService(db);
        var chain = await chainSvc.CreateAsync(new CreateApprovalChainRequest
        {
            ChainCode = "APPR-1",
            ChainName = "Sequential",
            ApprovalTypeValueId = Guid.NewGuid(),
            Steps =
            [
                new CreateApprovalStepRequest { StepOrder = 1, Mode = "Sequential", AssigneeType = "Person", AssigneeId = Guid.NewGuid() },
                new CreateApprovalStepRequest { StepOrder = 2, Mode = "Sequential", AssigneeType = "Person", AssigneeId = Guid.NewGuid() }
            ]
        });
        await chainSvc.PublishAsync(chain.Id);
        var decision = await evaluator.EvaluateAsync(new ApprovalEvaluateRequest { ApprovalChainId = chain.Id });
        Assert.False(decision.Completed);
        Assert.NotEmpty(decision.MatchedSteps);
        Assert.Single(decision.MatchedSteps);
        Assert.Contains("Step1", decision.MatchedSteps[0]);
    }

    [Fact]
    public async Task Parallel_approval_requires_all_steps_at_order()
    {
        await using var db = CreateDb();
        var chainSvc = new ApprovalChainAppService(db, new GovernanceAuditWriter(db));
        var evaluator = new ApprovalWorkflowEvaluatorService(db);
        var assignee1 = Guid.NewGuid();
        var assignee2 = Guid.NewGuid();
        var chain = await chainSvc.CreateAsync(new CreateApprovalChainRequest
        {
            ChainCode = "APPR-PAR",
            ChainName = "Parallel",
            ApprovalTypeValueId = Guid.NewGuid(),
            Steps =
            [
                new CreateApprovalStepRequest { StepOrder = 1, Mode = "Parallel", AssigneeType = "Person", AssigneeId = assignee1 },
                new CreateApprovalStepRequest { StepOrder = 1, Mode = "Parallel", AssigneeType = "Person", AssigneeId = assignee2 }
            ]
        });
        await chainSvc.PublishAsync(chain.Id);
        var partial = await evaluator.EvaluateAsync(new ApprovalEvaluateRequest { ApprovalChainId = chain.Id });
        Assert.False(partial.Completed);
        Assert.Equal(2, partial.PendingApprovers.Count);

        var complete = await evaluator.EvaluateAsync(new ApprovalEvaluateRequest { ApprovalChainId = chain.Id, ApprovedSteps = "1" });
        Assert.True(complete.Completed);
        Assert.True(complete.Approved);
        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Type == "ApprovalCompleted"));
    }

    [Fact]
    public async Task Escalation_loop_duplicate_target_rejected()
    {
        await using var db = CreateDb();
        var svc = new EscalationChainAppService(db);
        var target = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(new CreateEscalationChainRequest
        {
            ChainCode = "ESC-1",
            ChainName = "Loop",
            EscalationTypeValueId = Guid.NewGuid(),
            Levels =
            [
                new CreateEscalationLevelRequest { LevelOrder = 1, TargetAssigneeType = "User", TargetAssigneeId = target },
                new CreateEscalationLevelRequest { LevelOrder = 2, TargetAssigneeType = "User", TargetAssigneeId = target }
            ]
        }));
    }

    [Fact]
    public async Task Expired_assignment_excluded_from_owner_lookup()
    {
        var (svc, timeline, db) = CreateResponsibilityStack();
        var actor = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var resp = await svc.CreateAsync(new CreateResponsibilityRequest
        {
            ResponsibilityCode = "RESP-EXP",
            ResponsibilityTypeValueId = Guid.NewGuid(),
            GovernedResourceType = "ComplianceJourney",
            GovernedResourceId = resourceId,
            RequiresPrimaryOwner = false
        }, actor);
        await svc.AssignAsync(resp.Id, new AssignGovernanceRequest
        {
            AssigneeType = "Position",
            AssigneeId = Guid.NewGuid(),
            Priority = "Secondary",
            EffectiveFrom = DateTime.UtcNow.AddDays(-10),
            EffectiveTo = DateTime.UtcNow.AddDays(-1)
        }, actor);

        var owners = await timeline.GetOwnersAsync("ComplianceJourney", resourceId, null);
        Assert.Empty(owners.Owners);
    }

    [Fact]
    public async Task Assignment_creates_timeline_entry_and_outbox()
    {
        var (svc, timeline, db) = CreateResponsibilityStack();
        var actor = Guid.NewGuid();
        var resp = await svc.CreateAsync(BaseResponsibility("RESP-TIMELINE"), actor);
        await svc.AssignAsync(resp.Id, Assign(Guid.NewGuid(), "Secondary"), actor);

        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Type == "ResponsibilityAssigned"));
        var events = await timeline.SearchAsync(resp.GovernedResourceType, resp.GovernedResourceId, null);
        Assert.Contains(events, e => e.EventType == "ResponsibilityAssigned");
    }

    [Fact]
    public async Task Revoked_assignment_excluded_from_owner_lookup()
    {
        var (svc, timeline, db) = CreateResponsibilityStack();
        var actor = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var resp = await svc.CreateAsync(new CreateResponsibilityRequest
        {
            ResponsibilityCode = "RESP-REV",
            ResponsibilityTypeValueId = Guid.NewGuid(),
            GovernedResourceType = "ComplianceJourney",
            GovernedResourceId = resourceId,
            RequiresPrimaryOwner = false
        }, actor);
        var assignment = await svc.AssignAsync(resp.Id, Assign(Guid.NewGuid(), "Secondary"), actor);
        var entity = await db.GovernanceAssignments.FirstAsync(a => a.Id == assignment.Id);
        entity.Revoke();
        await db.SaveChangesAsync();

        var owners = await timeline.GetOwnersAsync("ComplianceJourney", resourceId, null);
        Assert.Empty(owners.Owners);
    }

    [Fact]
    public async Task Escalation_resolve_advances_level()
    {
        await using var db = CreateDb();
        var timeline = new GovernanceTimelineProjector(db);
        var chainSvc = new EscalationChainAppService(db);
        var resolver = new EscalationResolverService(db, timeline, new NoOpWorkforceClient(), Mock.Of<ICommunicationClient>(), Mock.Of<IIdentityContactClient>());
        var target1 = Guid.NewGuid();
        var target2 = Guid.NewGuid();
        var chain = await chainSvc.CreateAsync(new CreateEscalationChainRequest
        {
            ChainCode = "ESC-ADV",
            ChainName = "Advance",
            EscalationTypeValueId = Guid.NewGuid(),
            Levels =
            [
                new CreateEscalationLevelRequest { LevelOrder = 1, TargetAssigneeType = "User", TargetAssigneeId = target1, DelayHours = 0 },
                new CreateEscalationLevelRequest { LevelOrder = 2, TargetAssigneeType = "User", TargetAssigneeId = target2, DelayHours = 24 }
            ]
        });
        await chainSvc.PublishAsync(chain.Id);
        var result = await resolver.ResolveAsync(new EscalationResolveRequest { EscalationChainId = chain.Id, CurrentLevel = 0, HoursSinceOpen = 0 });
        Assert.Equal(1, result.ResolvedLevel);
        Assert.Equal(target1, result.NextAssigneeId);
    }

    [Fact]
    public async Task Escalation_skips_level_when_delay_not_met()
    {
        await using var db = CreateDb();
        var timeline = new GovernanceTimelineProjector(db);
        var chainSvc = new EscalationChainAppService(db);
        var resolver = new EscalationResolverService(db, timeline, new NoOpWorkforceClient(), Mock.Of<ICommunicationClient>(), Mock.Of<IIdentityContactClient>());
        var target2 = Guid.NewGuid();
        var chain = await chainSvc.CreateAsync(new CreateEscalationChainRequest
        {
            ChainCode = "ESC-DELAY",
            ChainName = "Delay",
            EscalationTypeValueId = Guid.NewGuid(),
            Levels =
            [
                new CreateEscalationLevelRequest { LevelOrder = 1, TargetAssigneeType = "User", TargetAssigneeId = Guid.NewGuid(), DelayHours = 48 },
                new CreateEscalationLevelRequest { LevelOrder = 2, TargetAssigneeType = "User", TargetAssigneeId = target2, DelayHours = 0 }
            ]
        });
        await chainSvc.PublishAsync(chain.Id);
        var result = await resolver.ResolveAsync(new EscalationResolveRequest { EscalationChainId = chain.Id, CurrentLevel = 0, HoursSinceOpen = 10 });
        Assert.Equal(2, result.ResolvedLevel);
        Assert.Equal(target2, result.NextAssigneeId);
    }

    [Fact]
    public async Task Committee_meeting_completion_emits_outbox()
    {
        await using var db = CreateDb();
        var timeline = new GovernanceTimelineProjector(db);
        var audit = new GovernanceAuditWriter(db);
        var svc = new CommitteeAppService(db, timeline, audit);
        var actor = Guid.NewGuid();
        var committee = await svc.CreateAsync(new CreateCommitteeRequest
        {
            CommitteeCode = "QC-1",
            CommitteeName = "Quality",
            CommitteeTypeValueId = Guid.NewGuid()
        }, actor);
        var meeting = await svc.ScheduleMeetingAsync(committee.Id, new ScheduleMeetingRequest
        {
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            MeetingTypeValueId = Guid.NewGuid()
        }, actor);
        Assert.NotEqual(Guid.Empty, meeting.Id);
        await svc.CompleteMeetingAsync(committee.Id, meeting.Id, new CompleteMeetingRequest { Minutes = "Done" }, actor);
        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Type == "CommitteeMeetingCompleted"));
    }

    [Fact]
    public async Task Committee_action_item_links_task_and_blocks_after_minutes_approval()
    {
        await using var db = CreateDb();
        var timeline = new GovernanceTimelineProjector(db);
        var audit = new GovernanceAuditWriter(db);
        var svc = new CommitteeAppService(db, timeline, audit);
        var actor = Guid.NewGuid();
        var committee = await svc.CreateAsync(new CreateCommitteeRequest
        {
            CommitteeCode = "MR-1",
            CommitteeName = "Management Review",
            CommitteeTypeValueId = Guid.NewGuid()
        }, actor);
        var meeting = await svc.ScheduleMeetingAsync(committee.Id, new ScheduleMeetingRequest
        {
            ScheduledAt = DateTime.UtcNow,
            MeetingTypeValueId = Guid.NewGuid(),
            AgendaTemplateCode = "ManagementReview",
            Agenda = "1. CAPA"
        }, actor);

        var action = await svc.CreateActionItemAsync(committee.Id, meeting.Id, new CreateActionItemRequest
        {
            Title = "Close open CAPA",
            DueDate = DateTime.UtcNow.AddDays(14)
        }, actor);
        Assert.Equal("Open", action.Status);
        Assert.Null(action.TaskId);

        var taskId = Guid.NewGuid();
        await svc.LinkActionItemTaskAsync(committee.Id, meeting.Id, action.Id, new LinkActionItemTaskRequest { TaskId = taskId }, actor);
        var linked = await db.CommitteeActionItems.FirstAsync(a => a.Id == action.Id);
        Assert.Equal(taskId, linked.TaskId);

        var decision = await svc.RecordDecisionAsync(committee.Id, meeting.Id, new RecordDecisionRequest
        {
            DecisionTypeValueId = Guid.NewGuid(),
            Summary = "Approve remediation",
            DeficiencyId = Guid.NewGuid(),
            RiskId = Guid.NewGuid(),
            TaskId = taskId
        }, actor);
        Assert.NotNull(decision.DeficiencyId);
        Assert.NotNull(decision.RiskId);
        Assert.Equal(taskId, decision.TaskId);

        await svc.CompleteMeetingAsync(committee.Id, meeting.Id, new CompleteMeetingRequest { Minutes = "MR held" }, actor);
        await svc.ApproveMinutesAsync(committee.Id, meeting.Id, actor);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateActionItemAsync(committee.Id, meeting.Id, new CreateActionItemRequest { Title = "Too late" }, actor));
        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Type == "CommitteeActionItemCreated"));
        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Type == "CommitteeMinutesApproved"));
    }

    [Fact]
    public async Task Governance_analytics_kpis_smoke()
    {
        await using var db = CreateDb();
        var timeline = new GovernanceTimelineProjector(db);
        var audit = new GovernanceAuditWriter(db);
        var committees = new CommitteeAppService(db, timeline, audit);
        var actor = Guid.NewGuid();
        var c = await committees.CreateAsync(new CreateCommitteeRequest
        {
            CommitteeCode = "KPI-1",
            CommitteeName = "KPI Committee",
            CommitteeTypeValueId = Guid.NewGuid()
        }, actor);
        var meeting = await committees.ScheduleMeetingAsync(c.Id, new ScheduleMeetingRequest
        {
            ScheduledAt = DateTime.UtcNow,
            MeetingTypeValueId = Guid.NewGuid()
        }, actor);
        await committees.CreateActionItemAsync(c.Id, meeting.Id, new CreateActionItemRequest { Title = "Open action" }, actor);

        var analytics = new GovernanceAnalyticsService(db);
        var summary = await analytics.GetSummaryAsync();
        Assert.True(summary.ActiveCommittees >= 1);
        Assert.True(summary.OpenActionItems >= 1);
        Assert.True(summary.ScheduledMeetings >= 1);

        var kpis = await analytics.GetKpisAsync();
        Assert.True(kpis.ActiveCommittees >= 1);
        Assert.True(kpis.OpenActionItems >= 1);
        Assert.True(kpis.MeetingsThisMonth >= 1);
        Assert.Contains("GovernanceService", kpis.SourceNotes);
    }
}
