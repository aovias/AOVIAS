using System;
using System.Threading;
using System.Threading.Tasks;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComplianceService.Tests;

public sealed class AssignmentRoutingServiceTests
{
    [Fact]
    public async Task ResolveForDeficiencyAsync_ruleA_returns_high_confidence_when_owner_configured()
    {
        var (options, deficiencyId, _) = await SeedRoutingScenarioAsync(
            oeDepartment: "Pharmacy",
            configureOwner: true);

        await using var db = new ComplianceDbContext(options);
        var sut = CreateSut(db);

        var result = await sut.ResolveForDeficiencyAsync(deficiencyId);

        Assert.Equal(AssignmentRoutingRuleType.ObjectiveElementDepartment, result.RuleApplied);
        Assert.Equal("PHARMACY", result.NormalizedDepartmentCode);
        Assert.Equal(AssignmentConfidence.High, result.Confidence);
        Assert.True(result.CanAutoAssign);
        Assert.Equal("chief-pharmacist", result.RecommendedAssigneeUserId);
    }

    [Fact]
    public async Task ResolveForDeficiencyAsync_ruleA_returns_medium_when_no_owner_configured()
    {
        var (options, deficiencyId, _) = await SeedRoutingScenarioAsync(
            oeDepartment: "Pharmacy",
            configureOwner: false);

        await using var db = new ComplianceDbContext(options);
        var sut = CreateSut(db);

        var result = await sut.ResolveForDeficiencyAsync(deficiencyId);

        Assert.Equal(AssignmentConfidence.Medium, result.Confidence);
        Assert.False(result.CanAutoAssign);
        Assert.Null(result.RecommendedAssigneeUserId);
    }

    [Fact]
    public async Task ResolveForDeficiencyAsync_ruleB_used_when_ruleA_low_and_root_cause_set()
    {
        var (options, deficiencyId, _) = await SeedRoutingScenarioAsync(
            oeDepartment: "Unknown Dept",
            configureOwner: true,
            rootCause: RootCauseCategory.TrainingMissing);

        await using var db = new ComplianceDbContext(options);
        var deficiency = await db.ComplianceDeficiencies.FirstAsync(d => d.Id == deficiencyId);
        var hrDept = new ComplianceDepartment
        {
            Id = Guid.NewGuid(),
            OrganizationId = deficiency.OrganizationId,
            Code = "HUMAN_RESOURCES",
            Name = "Human Resources",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };
        db.Departments.Add(hrDept);
        db.DepartmentOwnerships.Add(new DepartmentOwnership
        {
            Id = Guid.NewGuid(),
            OrganizationId = deficiency.OrganizationId,
            HospitalId = deficiency.HospitalId,
            DepartmentId = hrDept.Id,
            PrimaryOwnerUserId = "hr-manager",
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            Status = DepartmentOwnershipStatus.Active,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.ResolveForDeficiencyAsync(deficiencyId);

        Assert.Equal(AssignmentRoutingRuleType.RootCauseCategory, result.RuleApplied);
        Assert.Equal("HUMAN_RESOURCES", result.NormalizedDepartmentCode);
        Assert.Equal("hr-manager", result.RecommendedAssigneeUserId);
        Assert.False(result.CanAutoAssign);
    }

    [Fact]
    public async Task TryAutoAssignDeficiencyAsync_applies_owner_when_can_auto_assign()
    {
        var (options, deficiencyId, _) = await SeedRoutingScenarioAsync(
            oeDepartment: "Pharmacy",
            configureOwner: true);

        await using var db = new ComplianceDbContext(options);
        var ownership = new RecordingOwnershipSync(db);
        var sut = new AssignmentRoutingService(
            db,
            ownership,
            new NoOpEventEmitter(),
            new TestUnitOfWork(db),
            NullLogger<AssignmentRoutingService>.Instance);

        var result = await sut.TryAutoAssignDeficiencyAsync(deficiencyId);

        Assert.True(result.WasApplied);
        Assert.True(result.WasAutoAssigned);
        Assert.Equal("chief-pharmacist", ownership.LastOwnerUserId);
    }

    private static AssignmentRoutingService CreateSut(ComplianceDbContext db) =>
        new(
            db,
            new NoOpOwnershipSync(),
            new NoOpEventEmitter(),
            new TestUnitOfWork(db),
            NullLogger<AssignmentRoutingService>.Instance);

    private static async Task<(DbContextOptions<ComplianceDbContext> Options, Guid DeficiencyId, Guid OrgId)> SeedRoutingScenarioAsync(
        string oeDepartment,
        bool configureOwner,
        RootCauseCategory? rootCause = null)
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
        var deptId = Guid.NewGuid();
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
                RowVersion = rv,
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
                RowVersion = rv,
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
                RowVersion = rv,
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
                Department = oeDepartment,
                ResponsibleRole = "Chief Pharmacist",
                PlatformId = "poe01",
                CreatedAt = now,
                RowVersion = rv,
            });

            db.Departments.Add(new ComplianceDepartment
            {
                Id = deptId,
                OrganizationId = orgId,
                Code = "PHARMACY",
                Name = "Pharmacy",
                IsActive = true,
                CreatedAt = now,
                RowVersion = rv,
            });

            if (configureOwner)
            {
                db.DepartmentOwnerships.Add(new DepartmentOwnership
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    HospitalId = hospitalId,
                    DepartmentId = deptId,
                    PrimaryOwnerUserId = "chief-pharmacist",
                    EffectiveFrom = now.AddDays(-1),
                    Status = DepartmentOwnershipStatus.Active,
                    CreatedAt = now,
                    RowVersion = rv,
                });
            }

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
                RowVersion = rv,
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

            if (rootCause.HasValue)
            {
                deficiency.SetRootCause(rootCause.Value, "test");
            }

            db.ComplianceDeficiencies.Add(deficiency);
            deficiencyId = deficiency.Id;
            await db.SaveChangesAsync();
        }

        return (options, deficiencyId, orgId);
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly ComplianceDbContext _db;

        public TestUnitOfWork(ComplianceDbContext db) => _db = db;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangesAsync(cancellationToken);
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

    private sealed class NoOpOwnershipSync : IDeficiencyTaskOwnershipSyncService
    {
        public Task<ComplianceTask?> GetPrimaryRemediationTaskAsync(Guid deficiencyId, CancellationToken ct = default) =>
            Task.FromResult<ComplianceTask?>(null);

        public Task SyncDeficiencyOwnerFromTaskAsync(ComplianceTask task, string? updatedBy = null, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task SyncTaskOwnerFromDeficiencyAsync(ComplianceDeficiency deficiency, string? updatedBy = null, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task SetPrimaryOwnerAsync(
            Guid deficiencyId,
            string ownerUserId,
            DateTime? dueDate = null,
            string? departmentName = null,
            Guid? departmentId = null,
            string? updatedBy = null,
            CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class RecordingOwnershipSync : IDeficiencyTaskOwnershipSyncService
    {
        private readonly ComplianceDbContext _db;

        public RecordingOwnershipSync(ComplianceDbContext db) => _db = db;

        public string? LastOwnerUserId { get; private set; }

        public Task<ComplianceTask?> GetPrimaryRemediationTaskAsync(Guid deficiencyId, CancellationToken ct = default) =>
            Task.FromResult<ComplianceTask?>(null);

        public Task SyncDeficiencyOwnerFromTaskAsync(ComplianceTask task, string? updatedBy = null, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task SyncTaskOwnerFromDeficiencyAsync(ComplianceDeficiency deficiency, string? updatedBy = null, CancellationToken ct = default) =>
            Task.CompletedTask;

        public async Task SetPrimaryOwnerAsync(
            Guid deficiencyId,
            string ownerUserId,
            DateTime? dueDate = null,
            string? departmentName = null,
            Guid? departmentId = null,
            string? updatedBy = null,
            CancellationToken ct = default)
        {
            LastOwnerUserId = ownerUserId;
            var deficiency = await _db.ComplianceDeficiencies.FirstAsync(d => d.Id == deficiencyId, ct);
            deficiency.Assign(ownerUserId, dueDate, updatedBy ?? "test");
            await _db.SaveChangesAsync(ct);
        }
    }
}
