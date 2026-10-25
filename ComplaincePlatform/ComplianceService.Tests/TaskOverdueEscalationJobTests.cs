using ComplianceService.Application.Authorization;
using ComplianceService.Application.BackgroundJobs;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Dashboard.Projection;
using ComplianceService.Application.Dashboard.Summary;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComplianceService.Tests;

public sealed class TaskOverdueEscalationJobTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Hospital = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Framework = Guid.Parse("0fe8a9c0-9ea0-4970-9097-d4f73d45dca7");

    [Fact]
    public async Task TryEmitOverdue_uses_organization_as_envelope_tenant_and_keeps_hospital_in_payload()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ComplianceDbContext(options, userContext: null);
        db.ComplianceJourneys.Add(new ComplianceJourney(Org, Org, Hospital, Framework, 12, 0));
        var chapterId = Guid.NewGuid();
        var standardId = Guid.NewGuid();
        db.ComplianceFrameworks.Add(new ComplianceFramework
        {
            Id = Framework,
            PlatformId = "NABH",
            Name = "NABH",
            Authority = "QCI",
            Version = "5",
            IsActive = true,
            RowVersion = new byte[] { 1 },
            CreatedAt = DateTime.UtcNow
        });
        db.ComplianceChapters.Add(new ComplianceChapter
        {
            Id = chapterId,
            FrameworkId = Framework,
            PlatformId = "AAC",
            Code = "AAC",
            Name = "AAC",
            Sequence = 1,
            RowVersion = new byte[] { 1 },
            CreatedAt = DateTime.UtcNow
        });
        db.ComplianceStandards.Add(new ComplianceStandard
        {
            Id = standardId,
            ChapterId = chapterId,
            PlatformId = "AAC1",
            Code = "AAC.1",
            Name = "Std",
            Sequence = 1,
            RowVersion = new byte[] { 1 },
            CreatedAt = DateTime.UtcNow
        });
        var task = new ComplianceTask
        {
            Id = Guid.NewGuid(),
            HospitalId = Hospital,
            ComplianceStandardId = standardId,
            ObjectiveElementId = Guid.NewGuid(),
            DueDate = DateTime.UtcNow.AddDays(-1),
            Status = ComplianceTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 1 }
        };
        db.ComplianceTasks.Add(task);
        await db.SaveChangesAsync();

        var emitter = new CapturingEmitter();
        var job = new TaskOverdueEscalationJob(
            db,
            emitter,
            new NoopUow(db),
            CreateProcessor(db),
            new EmptyDirectory(),
            NullLogger<TaskOverdueEscalationJob>.Instance);

        Assert.True(await job.TryEmitOverdueAsync(task));
        Assert.Equal(Org, emitter.TenantId);
        Assert.NotEqual(Hospital, emitter.TenantId);
        var json = System.Text.Json.JsonSerializer.Serialize(emitter.Payload);
        Assert.Contains(Hospital.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Framework.ToString(), json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TryEmitOverdue_uses_journey_organization_id_when_tenant_id_differs()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ComplianceDbContext(options, userContext: null);
        db.ComplianceJourneys.Add(new ComplianceJourney(Hospital, Org, Hospital, Framework, 12, 0));
        var task = new ComplianceTask
        {
            Id = Guid.NewGuid(),
            HospitalId = Hospital,
            ComplianceStandardId = Guid.NewGuid(),
            ObjectiveElementId = Guid.NewGuid(),
            DueDate = DateTime.UtcNow.AddDays(-1),
            Status = ComplianceTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 1 }
        };
        db.ComplianceTasks.Add(task);
        await db.SaveChangesAsync();

        var emitter = new CapturingEmitter();
        var job = new TaskOverdueEscalationJob(
            db,
            emitter,
            new NoopUow(db),
            CreateProcessor(db),
            new EmptyDirectory(),
            NullLogger<TaskOverdueEscalationJob>.Instance);

        Assert.True(await job.TryEmitOverdueAsync(task));
        Assert.Equal(Org, emitter.TenantId);
        Assert.NotEqual(Hospital, emitter.TenantId);
    }

    private static InProcessOutboxProcessor CreateProcessor(ComplianceDbContext db) =>
        new(db, new NoopScore(), new NoopQueue(), new NoopRemediation(), new NoopCache(), new RecordingEngine());

    private sealed class CapturingEmitter : IComplianceEventEmitter
    {
        public Guid TenantId { get; private set; }
        public object? Payload { get; private set; }

        public Task EmitAsync(
            ComplianceEventType eventType,
            Guid entityId,
            string entityType,
            Guid tenantId,
            Guid? userId,
            object payload,
            CancellationToken cancellationToken = default)
        {
            TenantId = tenantId;
            Payload = payload;
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyDirectory : IHospitalDirectory
    {
        public Task<HospitalLookupResult> GetHospitalAsync(Guid hospitalId, CancellationToken cancellationToken = default)
            => Task.FromResult(HospitalLookupResult.NotFound());
    }

    private sealed class NoopUow(ComplianceDbContext db) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
    }

    private sealed class RecordingEngine : IDashboardProjectionEngine
    {
        public Task ProcessOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RebuildAsync(DashboardProjectionScope scope, string triggerEventType, Guid? outboxMessageId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<DashboardProjection?> TryGetProjectionAsync(DashboardProjectionScope scope, CancellationToken cancellationToken = default) => Task.FromResult<DashboardProjection?>(null);
    }

    private sealed class NoopScore : IComplianceProjectionHandler
    {
        public Task HandleAsync(ComplianceScoreUpdatedDomainEvent domainEvent, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NoopQueue : IComplianceSnapshotRecalculationQueue
    {
        public void EnqueueHospitalFramework(Guid hospitalId, Guid frameworkId) { }
        public void EnqueueMaturityRecalc(Guid hospitalId, Guid organizationId) { }
    }

    private sealed class NoopRemediation : IDeficiencyRemediationTaskService
    {
        public Task<Guid?> EnsureRemediationTaskAsync(Guid deficiencyId, CancellationToken cancellationToken = default, bool runAutoRouting = true)
            => Task.FromResult<Guid?>(null);
    }

    private sealed class NoopCache : IDashboardSummaryCache
    {
        public Task<DashboardSummaryPayload?> TryGetAsync(Guid tenantId, Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
            => Task.FromResult<DashboardSummaryPayload?>(null);

        public Task SetAsync(Guid tenantId, Guid? hospitalId, Guid? frameworkId, DashboardSummaryPayload payload, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Invalidate(Guid tenantId, Guid? hospitalId = null, Guid? frameworkId = null) { }
    }
}
