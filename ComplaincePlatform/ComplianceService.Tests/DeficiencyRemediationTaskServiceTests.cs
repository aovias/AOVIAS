using System;
using System.Threading;
using System.Threading.Tasks;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComplianceService.Tests;

public sealed class DeficiencyRemediationTaskServiceTests
{
    [Fact]
    public async Task EnsureRemediationTaskAsync_creates_task_when_none_exists()
    {
        var (options, deficiencyId, _) = await SeedDeficiencyAsync();
        await using var db = new ComplianceDbContext(options);
        var sut = CreateSut(db);

        var taskId = await sut.EnsureRemediationTaskAsync(deficiencyId);

        Assert.NotNull(taskId);
        var task = await db.ComplianceTasks.FirstAsync(t => t.Id == taskId);
        Assert.Equal(deficiencyId, task.SourceDeficiencyId);
        Assert.Contains("Upload evidence", task.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnsureRemediationTaskAsync_is_idempotent_for_same_deficiency()
    {
        var (options, deficiencyId, _) = await SeedDeficiencyAsync();
        await using var db = new ComplianceDbContext(options);
        var sut = CreateSut(db);

        var first = await sut.EnsureRemediationTaskAsync(deficiencyId);
        var second = await sut.EnsureRemediationTaskAsync(deficiencyId);

        Assert.Equal(first, second);
        Assert.Equal(1, await db.ComplianceTasks.CountAsync(t => t.SourceDeficiencyId == deficiencyId));
    }

    [Fact]
    public async Task EnsureRemediationTaskAsync_links_existing_open_oe_task()
    {
        var (options, deficiencyId, oeId) = await SeedDeficiencyAsync(includeOpenOeTask: true);
        await using var db = new ComplianceDbContext(options);
        var sut = CreateSut(db);

        var taskId = await sut.EnsureRemediationTaskAsync(deficiencyId);

        Assert.NotNull(taskId);
        var task = await db.ComplianceTasks.FirstAsync(t => t.Id == taskId);
        Assert.Equal(deficiencyId, task.SourceDeficiencyId);
        Assert.Equal(oeId, task.ObjectiveElementId);
        Assert.Equal(1, await db.ComplianceTasks.CountAsync());
    }

    private static DeficiencyRemediationTaskService CreateSut(ComplianceDbContext db)
    {
        var repo = new DeficiencyRepository(db);
        return new DeficiencyRemediationTaskService(
            db,
            repo,
            new NoOpLifecycle(),
            new NoOpRouting(),
            new NoOpEventEmitter(),
            new TestUnitOfWork(db),
            NullLogger<DeficiencyRemediationTaskService>.Instance);
    }

    private static async Task<(DbContextOptions<ComplianceDbContext> Options, Guid DeficiencyId, Guid OeId)> SeedDeficiencyAsync(
        bool includeOpenOeTask = false)
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var fwId = Guid.NewGuid();
        var chId = Guid.NewGuid();
        var stdId = Guid.NewGuid();
        var oeId = Guid.NewGuid();
        var obligationId = Guid.NewGuid();
        var deficiencyId = Guid.Empty;
        var now = DateTime.UtcNow;
        var rv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

        await using (var db = new ComplianceDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            db.ComplianceFrameworks.Add(new ComplianceFramework
            {
                Id = fwId,
                Name = "NABH",
                Authority = "QCI",
                Version = "6",
                IsActive = true,
                PlatformId = "pfw01",
                CreatedAt = now,
                RowVersion = rv
            });

            db.ComplianceChapters.Add(new ComplianceChapter
            {
                Id = chId,
                FrameworkId = fwId,
                Code = "PRE",
                Name = "Patient Rights",
                Sequence = 1,
                PlatformId = "pch01",
                CreatedAt = now,
                RowVersion = rv
            });

            db.ComplianceStandards.Add(new ComplianceStandard
            {
                Id = stdId,
                ChapterId = chId,
                Code = "PRE.1",
                Name = "Medication Storage",
                Sequence = 1,
                PlatformId = "pst01",
                CreatedAt = now,
                RowVersion = rv
            });

            db.ObjectiveElements.Add(new ObjectiveElement
            {
                Id = oeId,
                ComplianceStandardId = stdId,
                Code = "PRE.1.1",
                Title = "Medication Storage SOP",
                Description = "SOP required",
                Classification = ObjectiveElementClassification.Core,
                IsApplicable = true,
                PlatformId = "poe01",
                CreatedAt = now,
                RowVersion = rv
            });

            db.OrganizationComplianceObligations.Add(new OrganizationComplianceObligation
            {
                Id = obligationId,
                TenantId = tenantId,
                OrganizationId = orgId,
                HospitalId = hospitalId,
                FrameworkId = fwId,
                ChapterId = chId,
                StandardId = stdId,
                ObjectiveElementId = oeId,
                Code = "PRE.1.1",
                Title = "Medication Storage SOP",
                MaturityLevel = ObjectiveElementClassification.Core,
                EvidenceStatus = EvidenceStatus.Missing,
                VerificationStatus = VerificationStatus.NotVerified,
                CreatedAt = now,
                RowVersion = rv
            });

            var deficiency = ComplianceDeficiency.Create(
                tenantId,
                orgId,
                hospitalId,
                fwId,
                oeId,
                obligationId,
                "DEF-001",
                "Medication Storage SOP Missing",
                "No SOP on file",
                DeficiencyType.Evidence,
                DeficiencySeverity.Critical,
                ObjectiveElementClassification.Core,
                riskCategory: null);

            db.ComplianceDeficiencies.Add(deficiency);
            deficiencyId = deficiency.Id;

            if (includeOpenOeTask)
            {
                db.ComplianceTasks.Add(new ComplianceTask
                {
                    Id = Guid.NewGuid(),
                    HospitalId = hospitalId,
                    ComplianceStandardId = stdId,
                    ObjectiveElementId = oeId,
                    AssignedToUserId = "owner",
                    DueDate = now.AddDays(14),
                    Status = ComplianceTaskStatus.Pending,
                    Title = "Generic OE task",
                    Priority = ComplianceTaskPriority.Medium,
                    CreatedAt = now,
                    RowVersion = rv
                });
            }

            await db.SaveChangesAsync();
        }

        return (options, deficiencyId, oeId);
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly ComplianceDbContext _db;

        public TestUnitOfWork(ComplianceDbContext db) => _db = db;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangesAsync(cancellationToken);
    }

    private sealed class NoOpLifecycle : IDeficiencyTaskLifecycleService
    {
        public Task SyncFromTaskAsync(ComplianceTask task, ComplianceTaskStatus? previousStatus, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SyncFromEvidenceAsync(Guid taskId, EvidenceLifecycleTrigger trigger, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpEventEmitter : IComplianceEventEmitter
    {
        public Task EmitAsync(
            ComplianceEventType eventType,
            Guid entityId,
            string entityType,
            Guid tenantId,
            Guid? userId,
            object payload,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpRouting : IAssignmentRoutingService
    {
        public Task<AssignmentRoutingApplyResultDto> TryAutoAssignDeficiencyAsync(
            Guid deficiencyId,
            bool forceAutoAssign = false,
            string? updatedBy = null,
            CancellationToken ct = default) =>
            Task.FromResult(new AssignmentRoutingApplyResultDto());

        public Task<AssignmentRoutingResultDto> ResolveForDeficiencyAsync(Guid deficiencyId, CancellationToken ct = default) =>
            Task.FromResult(new AssignmentRoutingResultDto());

        public Task<AssignmentRoutingResultDto> ResolveForObjectiveElementAsync(
            Guid organizationId,
            Guid hospitalId,
            Guid objectiveElementId,
            CancellationToken ct = default) =>
            Task.FromResult(new AssignmentRoutingResultDto());

        public Task<AssignmentRoutingResultDto> ResolveForFindingAsync(
            Guid findingId,
            Guid organizationId,
            Guid hospitalId,
            CancellationToken ct = default) =>
            Task.FromResult(new AssignmentRoutingResultDto());

        public Task<AssignmentRoutingMetricsDto> GetMetricsAsync(Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(new AssignmentRoutingMetricsDto());
    }
}
