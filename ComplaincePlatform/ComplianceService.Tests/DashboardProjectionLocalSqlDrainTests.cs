using ComplianceService.Application.Contracts;
using ComplianceService.Application.Dashboard.Projection;
using ComplianceService.Application.Dashboard.Summary;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ComplianceService.Tests;

/// <summary>
/// Local SQL Express drain for Phase 3G. Opt-in: PHASE3G_DRAIN=1.
/// Does not start Hangfire. Does not delete outbox rows.
/// </summary>
public sealed class DashboardProjectionLocalSqlDrainTests
{
    private const string LocalSql =
        @"Server=localhost\SQLEXPRESS;Database=Compliance;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task Drain_TaskOverdue_backlog_on_local_express()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("PHASE3G_DRAIN"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tenancy:MandatoryQueryFilters"] = "false"
            })
            .Build();

        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseSqlServer(LocalSql)
            .Options;

        await using var db = new ComplianceDbContext(options, userContext: null, executionContext: null, configuration: config);

        var enrollPending = await db.OutboxMessages.CountAsync(m =>
            m.Type.Contains("FrameworkEnrolled") && m.ProjectionProcessedOn == null);
        var overduePending = await db.OutboxMessages.CountAsync(m =>
            m.Type == "ComplianceEvent.TaskOverdue" && m.ProjectionProcessedOn == null && m.RetryCount < 5);

        Assert.True(enrollPending >= 0);
        Assert.True(overduePending >= 0);

        var engine = CreateEngine(db);
        var processor = new InProcessOutboxProcessor(
            db,
            new NoopScoreHandler(),
            new NoopQueue(),
            new NoopRemediation(),
            new NoopCache(),
            engine);

        await processor.ProcessPendingAsync(CancellationToken.None);

        var enrollProcessed = await db.OutboxMessages.CountAsync(m =>
            m.Type.Contains("FrameworkEnrolled") && m.ProjectionProcessedOn != null);
        var enrollFailed = await db.OutboxMessages.CountAsync(m =>
            m.Type.Contains("FrameworkEnrolled") && m.ProjectionProcessedOn == null && m.RetryCount > 0);
        var overdueProcessed = await db.OutboxMessages.CountAsync(m =>
            m.Type == "ComplianceEvent.TaskOverdue" && m.ProjectionProcessedOn != null);
        var overdueStillPending = await db.OutboxMessages.CountAsync(m =>
            m.Type == "ComplianceEvent.TaskOverdue" && m.ProjectionProcessedOn == null && m.RetryCount < 5);

        Assert.Equal(0, enrollProcessed);
        Assert.Equal(enrollPending, enrollFailed);
        Assert.Equal(0, overdueStillPending);
        Assert.True(overdueProcessed >= overduePending);

        var processedOverdue = await db.OutboxMessages
            .Where(m => m.Type == "ComplianceEvent.TaskOverdue" && m.ProjectionProcessedOn != null)
            .OrderByDescending(m => m.ProjectionProcessedOn)
            .FirstAsync();

        var beforeVersion = await db.DashboardProjections
            .Where(p => p.LastProcessedOutboxMessageId == processedOverdue.Id)
            .Select(p => p.DataVersion)
            .FirstOrDefaultAsync();

        await engine.ProcessOutboxMessageAsync(processedOverdue);
        await engine.ProcessOutboxMessageAsync(processedOverdue);

        var afterVersion = await db.DashboardProjections
            .Where(p => p.LastProcessedOutboxMessageId == processedOverdue.Id)
            .Select(p => p.DataVersion)
            .FirstOrDefaultAsync();

        Assert.Equal(beforeVersion, afterVersion);
    }

    private static DashboardProjectionEngine CreateEngine(ComplianceDbContext db)
    {
        var scopes = new SnapshotScopeFactory(db, new SqlSnapshotBuilder(db));
        return new DashboardProjectionEngine(
            new DashboardProjectionRepository(db),
            new DashboardProjectionUpdater(new DashboardProjectionRepository(db), db, NullLogger<DashboardProjectionUpdater>.Instance),
            scopes,
            Options.Create(new DashboardProjectionOptions { Enabled = true }),
            NullLogger<DashboardProjectionEngine>.Instance);
    }

    private sealed class SqlSnapshotBuilder(ComplianceDbContext db) : IDashboardSnapshotBuilder
    {
        public async Task<DashboardSnapshotDto> BuildLiveAsync(
            Guid tenantId,
            Guid? hospitalId,
            Guid? frameworkId,
            CancellationToken cancellationToken = default)
        {
            var journeys = await db.ComplianceJourneys.AsNoTracking()
                .Where(j => j.TenantId == tenantId
                            && (!hospitalId.HasValue || j.HospitalId == hospitalId)
                            && (!frameworkId.HasValue || frameworkId == Guid.Empty || j.FrameworkId == frameworkId))
                .CountAsync(cancellationToken);

            return new DashboardSnapshotDto
            {
                Summary = new DashboardSnapshotSummaryDto
                {
                    TotalJourneys = journeys,
                    ActiveJourneys = journeys
                },
                TasksSummary = new TasksSummaryDto()
            };
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
