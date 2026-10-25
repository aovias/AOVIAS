using System.Text.Json;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ComplianceService.Tests;

public sealed class DashboardProjectionPropagationTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Hospital = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Framework = Guid.Parse("0fe8a9c0-9ea0-4970-9097-d4f73d45dca7");

    [Fact]
    public async Task FrameworkEnrolled_is_processed_and_rebuilds_projection()
    {
        await using var db = CreateDb();
        db.ComplianceJourneys.Add(new ComplianceJourney(Org, Org, Hospital, Framework, 12, 0));
        var enroll = new FrameworkEnrolledDomainEvent(Hospital, Framework, Guid.NewGuid());
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = Hospital,
            OccurredOn = DateTime.UtcNow,
            Type = typeof(FrameworkEnrolledDomainEvent).FullName!,
            Payload = JsonSerializer.Serialize(enroll),
            RetryCount = 0
        };
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        var engineCalls = new RecordingProjectionEngine();
        var processor = CreateProcessor(db, engineCalls);
        await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Single(engineCalls.Processed);
        Assert.Equal(message.Id, engineCalls.Processed[0].Id);
        Assert.NotNull((await db.OutboxMessages.SingleAsync()).ProjectionProcessedOn);
    }

    [Fact]
    public async Task FrameworkEnrolled_full_rebuild_is_idempotent_for_same_outbox_id()
    {
        await using var db = CreateDb();
        db.ComplianceJourneys.Add(new ComplianceJourney(Org, Org, Hospital, Framework, 12, 0));
        await db.SaveChangesAsync();

        var builder = new StubSnapshotBuilder();
        var engine = CreateEngine(db, builder);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = Hospital,
            OccurredOn = DateTime.UtcNow,
            Type = typeof(FrameworkEnrolledDomainEvent).FullName!,
            Payload = JsonSerializer.Serialize(new FrameworkEnrolledDomainEvent(Hospital, Framework, Guid.NewGuid()))
        };

        await engine.ProcessOutboxMessageAsync(message);
        await engine.ProcessOutboxMessageAsync(message);

        Assert.Equal(1, builder.BuildCount);
        var projection = await db.DashboardProjections.SingleAsync();
        Assert.Equal(1, projection.TotalJourneysCount);
        Assert.Equal(Org, projection.OrganizationId);
        Assert.Equal(Hospital, projection.HospitalId);
        Assert.Equal(Framework, projection.FrameworkId);
        Assert.Equal(message.Id, projection.LastProcessedOutboxMessageId);
        Assert.Contains("FrameworkEnrolled", projection.LastEventType);
    }

    [Fact]
    public async Task FrameworkEnrolled_projection_key_uses_journey_organization_id_not_hospital_id()
    {
        await using var db = CreateDb();
        db.ComplianceJourneys.Add(new ComplianceJourney(Hospital, Org, Hospital, Framework, 12, 0));
        await db.SaveChangesAsync();

        var engine = CreateEngine(db, new StubSnapshotBuilder());
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = Hospital,
            OccurredOn = DateTime.UtcNow,
            Type = typeof(FrameworkEnrolledDomainEvent).FullName!,
            Payload = JsonSerializer.Serialize(new FrameworkEnrolledDomainEvent(Hospital, Framework, Guid.NewGuid()))
        };

        await engine.ProcessOutboxMessageAsync(message);

        var projection = await db.DashboardProjections.SingleAsync();
        Assert.Equal(Org, projection.OrganizationId);
        Assert.Equal(Hospital, projection.HospitalId);
        Assert.Equal(Framework, projection.FrameworkId);
        Assert.NotEqual(projection.HospitalId, projection.OrganizationId);
    }

    [Fact]
    public async Task TaskOverdue_increments_overdue_count_once_per_message()
    {
        await using var db = CreateDb();
        SeedCatalog(db, out var standardId, out var oeId);
        db.ComplianceTasks.Add(new ComplianceTask
        {
            Id = Guid.NewGuid(),
            HospitalId = Hospital,
            ComplianceStandardId = standardId,
            ObjectiveElementId = oeId,
            DueDate = DateTime.UtcNow.AddDays(-2),
            Status = ComplianceTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync();

        var engine = CreateEngine(db, new StubSnapshotBuilder());
        var envelope = new ComplianceEventEnvelope
        {
            EventId = Guid.NewGuid(),
            EntityId = Guid.NewGuid(),
            EntityType = nameof(ComplianceTask),
            TenantId = Org,
            EventType = ComplianceEventType.TaskOverdue,
            Payload = JsonSerializer.Serialize(new { HospitalId = Hospital, FrameworkId = Framework })
        };
        var message = new OutboxMessage
        {
            Id = envelope.EventId,
            AggregateId = envelope.EntityId,
            OccurredOn = DateTime.UtcNow,
            Type = "ComplianceEvent.TaskOverdue",
            Payload = JsonSerializer.Serialize(envelope)
        };

        await engine.ProcessOutboxMessageAsync(message);
        await engine.ProcessOutboxMessageAsync(message);

        var projection = await db.DashboardProjections.SingleAsync();
        Assert.Equal(1, projection.OverdueTasksCount);
        Assert.Equal(Org, projection.OrganizationId);
        Assert.Equal(Hospital, projection.HospitalId);
        Assert.Equal(Framework, projection.FrameworkId);
        Assert.Equal(message.Id, projection.LastProcessedOutboxMessageId);
    }

    [Fact]
    public async Task InProcessProcessor_drains_TaskOverdue_candidates()
    {
        await using var db = CreateDb();
        var envelope = new ComplianceEventEnvelope
        {
            EventId = Guid.NewGuid(),
            EntityId = Guid.NewGuid(),
            EntityType = nameof(ComplianceTask),
            TenantId = Org,
            EventType = ComplianceEventType.TaskOverdue,
            Payload = JsonSerializer.Serialize(new { HospitalId = Hospital })
        };
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = envelope.EventId,
            AggregateId = envelope.EntityId,
            OccurredOn = DateTime.UtcNow,
            Type = "ComplianceEvent.TaskOverdue",
            Payload = JsonSerializer.Serialize(envelope)
        });
        await db.SaveChangesAsync();

        var engine = new RecordingProjectionEngine();
        await CreateProcessor(db, engine).ProcessPendingAsync(CancellationToken.None);

        Assert.Single(engine.Processed);
        Assert.NotNull((await db.OutboxMessages.SingleAsync()).ProjectionProcessedOn);
    }

    [Fact]
    public async Task TaskOverdue_without_catalog_graph_does_not_throw_and_counts_zero_in_memory()
    {
        await using var db = CreateDb();
        db.ComplianceTasks.Add(new ComplianceTask
        {
            Id = Guid.NewGuid(),
            HospitalId = Hospital,
            ComplianceStandardId = Guid.NewGuid(),
            ObjectiveElementId = Guid.NewGuid(),
            DueDate = DateTime.UtcNow.AddDays(-2),
            Status = ComplianceTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync();

        var engine = CreateEngine(db, new StubSnapshotBuilder());
        var envelope = new ComplianceEventEnvelope
        {
            EventId = Guid.NewGuid(),
            EntityId = Guid.NewGuid(),
            EntityType = nameof(ComplianceTask),
            TenantId = Org,
            EventType = ComplianceEventType.TaskOverdue,
            Payload = JsonSerializer.Serialize(new { HospitalId = Hospital })
        };
        var message = new OutboxMessage
        {
            Id = envelope.EventId,
            AggregateId = envelope.EntityId,
            OccurredOn = DateTime.UtcNow,
            Type = "ComplianceEvent.TaskOverdue",
            Payload = JsonSerializer.Serialize(envelope)
        };

        await engine.ProcessOutboxMessageAsync(message);

        var projection = await db.DashboardProjections.SingleAsync();
        Assert.Equal(0, projection.OverdueTasksCount);
        Assert.Equal(message.Id, projection.LastProcessedOutboxMessageId);
    }

    [Fact]
    public async Task FrameworkEnrolled_payload_without_hospital_is_not_marked_projection_processed()
    {
        await using var db = CreateDb();
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            Type = typeof(FrameworkEnrolledDomainEvent).FullName!,
            Payload = """{"OccurredOn":"2026-04-06T11:36:20.6239447Z"}""",
            RetryCount = 0
        });
        await db.SaveChangesAsync();

        var processor = CreateProcessor(db, new RecordingProjectionEngine());
        await processor.ProcessPendingAsync(CancellationToken.None);

        var stored = await db.OutboxMessages.SingleAsync();
        Assert.Null(stored.ProjectionProcessedOn);
        Assert.Equal(1, stored.RetryCount);
        Assert.Contains("HospitalId", stored.Error);
    }

    private static ComplianceDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ComplianceDbContext(options, userContext: null);
    }

    private static void SeedCatalog(ComplianceDbContext db, out Guid standardId, out Guid oeId)
    {
        var chapterId = Guid.NewGuid();
        standardId = Guid.NewGuid();
        oeId = Guid.NewGuid();
        var rv = new byte[] { 1 };
        db.ComplianceFrameworks.Add(new ComplianceFramework
        {
            Id = Framework,
            PlatformId = "NABH",
            Name = "NABH",
            Authority = "QCI",
            Version = "5",
            IsActive = true,
            RowVersion = rv,
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
            RowVersion = rv,
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
            RowVersion = rv,
            CreatedAt = DateTime.UtcNow
        });
        db.ObjectiveElements.Add(new ObjectiveElement
        {
            Id = oeId,
            ComplianceStandardId = standardId,
            PlatformId = "AAC1A",
            Code = "AAC.1.a",
            Title = "OE",
            Description = "OE",
            RowVersion = rv,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static InProcessOutboxProcessor CreateProcessor(ComplianceDbContext db, IDashboardProjectionEngine engine) =>
        new(db, new NoopScoreHandler(), new NoopQueue(), new NoopRemediation(), new NoopCache(), engine);

    private static DashboardProjectionEngine CreateEngine(ComplianceDbContext db, IDashboardSnapshotBuilder builder)
    {
        var scopes = new SnapshotScopeFactory(db, builder);
        return new DashboardProjectionEngine(
            new DashboardProjectionRepository(db),
            new DashboardProjectionUpdater(new DashboardProjectionRepository(db), db, NullLogger<DashboardProjectionUpdater>.Instance),
            scopes,
            Options.Create(new DashboardProjectionOptions { Enabled = true }),
            NullLogger<DashboardProjectionEngine>.Instance);
    }

    private sealed class RecordingProjectionEngine : IDashboardProjectionEngine
    {
        public List<OutboxMessage> Processed { get; } = [];

        public Task ProcessOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            Processed.Add(message);
            return Task.CompletedTask;
        }

        public Task RebuildAsync(DashboardProjectionScope scope, string triggerEventType, Guid? outboxMessageId = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<DashboardProjection?> TryGetProjectionAsync(DashboardProjectionScope scope, CancellationToken cancellationToken = default)
            => Task.FromResult<DashboardProjection?>(null);
    }

    private sealed class StubSnapshotBuilder : IDashboardSnapshotBuilder
    {
        public int BuildCount { get; private set; }

        public Task<DashboardSnapshotDto> BuildLiveAsync(Guid tenantId, Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        {
            BuildCount++;
            return Task.FromResult(new DashboardSnapshotDto
            {
                Summary = new DashboardSnapshotSummaryDto { TotalJourneys = 1, ActiveJourneys = 1 },
                TasksSummary = new TasksSummaryDto()
            });
        }
    }

    private sealed class SnapshotScopeFactory(ComplianceDbContext db, IDashboardSnapshotBuilder builder)
        : IServiceScopeFactory, IServiceScope, IServiceProvider
    {
        public IServiceScope CreateScope() => this;
        public IServiceProvider ServiceProvider => this;
        public void Dispose() { }
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IDashboardSnapshotBuilder) ? builder
            : serviceType == typeof(ComplianceDbContext) ? db
            : null;
    }

    private sealed class NoopScoreHandler : IComplianceProjectionHandler
    {
        public Task HandleAsync(ComplianceScoreUpdatedDomainEvent domainEvent, CancellationToken ct)
            => Task.CompletedTask;
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
