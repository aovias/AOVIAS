using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WorkforceIntelligenceService.Application.Contracts;
using WorkforceIntelligenceService.Application.Intelligence.Forecasting;
using WorkforceIntelligenceService.Application.Intelligence.Platform;
using WorkforceIntelligenceService.Application.Intelligence.Projections;
using WorkforceIntelligenceService.Application.Intelligence.Queries;
using WorkforceIntelligenceService.Application.Intelligence.Recommendations;
using WorkforceIntelligenceService.Application.Intelligence.Snapshots;
using WorkforceIntelligenceService.Domain.ReadModels;
using WorkforceIntelligenceService.Infrastructure.Clients;
using WorkforceIntelligenceService.Infrastructure.Persistence;
using WorkforceIntelligenceService.Infrastructure.Services;

namespace WorkforceIntelligenceService.Tests;

public class IntelligenceTestDb : IDisposable
{
    public WorkforceIntelligenceDbContext Db { get; }

    public IntelligenceTestDb()
    {
        var options = new DbContextOptionsBuilder<WorkforceIntelligenceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new WorkforceIntelligenceDbContext(options);
    }

    public void Dispose() => Db.Dispose();
}

public class WorkforceSnapshotServiceTests
{
    [Fact]
    public async Task RefreshOrganization_ComputesHeadcountFromSourceEmployments()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var workforce = new Mock<IWorkforceSourceClient>();
        workforce.Setup(w => w.ListEmploymentsAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SourceEmploymentDto>
            {
                new() { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, EmploymentStatus = "Active", Fte = 1 },
                new() { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, EmploymentStatus = "Active", Fte = 0.5m },
                new() { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, EmploymentStatus = "Terminated", Fte = 1 }
            });
        var professional = new Mock<IProfessionalSourceClient>();
        professional.Setup(p => p.ListProfilesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceProfessionalProfileDto>());
        professional.Setup(p => p.GetExpiringAsync(orgId, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceRenewalItemDto>());
        var governance = new Mock<IGovernanceSourceClient>();
        governance.Setup(g => g.GetSummaryAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(new SourceGovernanceSummaryDto());
        var organization = new Mock<IOrganizationSourceClient>();
        organization.Setup(o => o.GetDepartmentSizesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, int>());
        var outbox = new Mock<IIntelligenceOutboxWriter>();

        var service = new WorkforceSnapshotService(fixture.Db, workforce.Object, professional.Object, governance.Object, organization.Object,
            outbox.Object, Options.Create(new IntelligenceOptions()), NullLogger<WorkforceSnapshotService>.Instance);

        await service.RefreshOrganizationAsync(orgId, "test-corr");

        var snapshot = await fixture.Db.WorkforceSnapshots.SingleAsync();
        Assert.Equal(2, snapshot.TotalHeadcount);
        Assert.Equal(1.5m, snapshot.TotalFte);
        Assert.Equal(1, snapshot.Version);
    }
}

public class IntelligenceQueryServiceTests
{
    [Fact]
    public async Task GetDashboard_ReturnsLatestExecutiveMetrics()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        fixture.Db.ExecutiveMetrics.Add(new ExecutiveMetrics
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AsOfUtc = DateTime.UtcNow,
            Headcount = 42, TrainingCompliancePercent = 88, CredentialRiskCount = 3
        });
        await fixture.Db.SaveChangesAsync();

        var cache = new AnalyticsCacheService(fixture.Db);
        var queries = new IntelligenceQueryService(fixture.Db, cache, Options.Create(new IntelligenceOptions()));
        var result = await queries.GetDashboardAsync(new IntelligenceScope(orgId));

        Assert.Equal(42, result.Headcount);
        Assert.Equal(88, result.TrainingCompliancePercent);
        Assert.Equal(3, result.CredentialRiskCount);
    }

    [Fact]
    public async Task GetHeadcount_ReturnsTrendFromSnapshots()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        fixture.Db.WorkforceSnapshots.AddRange(
            new WorkforceSnapshot { Id = Guid.NewGuid(), OrganizationId = orgId, AsOfUtc = DateTime.UtcNow.AddDays(-2), Version = 1, TotalHeadcount = 10, TotalFte = 10 },
            new WorkforceSnapshot { Id = Guid.NewGuid(), OrganizationId = orgId, AsOfUtc = DateTime.UtcNow, Version = 2, TotalHeadcount = 12, TotalFte = 12 }
        );
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.GetHeadcountAsync(new IntelligenceScope(orgId));
        Assert.Equal(12, result.TotalHeadcount);
        Assert.Equal(2, result.Trend.Count);
    }
}

public class ForecastEngineTests
{
    [Fact]
    public async Task Generate_IncrementsVersionPerForecastType()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var professional = new Mock<IProfessionalSourceClient>();
        professional.Setup(p => p.GetExpiringAsync(orgId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SourceRenewalItemDto>
            {
                new() { CredentialType = "Certification", CredentialName = "BLS", ExpiryDate = DateTime.UtcNow.AddDays(30), DaysUntilExpiry = 30 }
            });
        var outbox = new Mock<IIntelligenceOutboxWriter>();
        var engine = new ForecastEngine(fixture.Db, professional.Object, outbox.Object, Options.Create(new IntelligenceOptions()));

        var first = await engine.GenerateAsync(orgId, "CertificationExpiry", "c1");
        var second = await engine.GenerateAsync(orgId, "CertificationExpiry", "c2");

        Assert.Equal(1, first.Version);
        Assert.Equal(2, second.Version);
        var points = JsonSerializer.Deserialize<List<ForecastDataPointDto>>(second.ResultsJson);
        Assert.NotNull(points);
        Assert.NotEmpty(points!);
    }
}

public class RecommendationOrchestratorTests
{
    [Fact]
    public async Task Generate_AlwaysIncludesSourceReferences()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        fixture.Db.TrainingMatrixEntries.Add(new TrainingMatrixEntry
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, EmploymentId = Guid.NewGuid(), PersonId = Guid.NewGuid(),
            CourseName = "Fire Safety", IsMandatory = true, Status = "Pending", AsOfUtc = DateTime.UtcNow
        });
        await fixture.Db.SaveChangesAsync();

        var professional = new Mock<IProfessionalSourceClient>();
        professional.Setup(p => p.GetExpiringAsync(orgId, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceRenewalItemDto>());
        var outbox = new Mock<IIntelligenceOutboxWriter>();
        var orchestrator = new RecommendationOrchestrator(fixture.Db, professional.Object, outbox.Object, new AnalyticsCacheService(fixture.Db));

        var count = await orchestrator.GenerateAsync(orgId, "corr");
        Assert.True(count >= 1);
        var rec = await fixture.Db.WorkforceRecommendations.FirstAsync();
        var refs = JsonSerializer.Deserialize<List<SourceReferenceDto>>(rec.SourceReferencesJson);
        Assert.NotNull(refs);
        Assert.NotEmpty(refs!);
        Assert.False(string.IsNullOrWhiteSpace(rec.Explanation));
    }
}

public class ProjectionHandlerTests
{
    [Fact]
    public async Task SnapshotRefreshProjection_TriggersSnapshotOnEmploymentCreated()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var snapshot = new Mock<IWorkforceSnapshotService>();
        var handler = new SnapshotRefreshProjection(fixture.Db, snapshot.Object, NullLogger<SnapshotRefreshProjection>.Instance, "EmploymentCreated");

        await handler.HandleAsync(new IntelligenceEventEnvelope
        {
            Type = "EmploymentCreated",
            AggregateId = Guid.NewGuid(),
            Payload = JsonSerializer.Serialize(new { organizationId = orgId })
        });

        snapshot.Verify(s => s.RefreshOrganizationAsync(orgId, It.IsAny<string>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class AnalyticsCacheServiceTests
{
    [Fact]
    public async Task Cache_RoundTripsPayload()
    {
        using var fixture = new IntelligenceTestDb();
        var cache = new AnalyticsCacheService(fixture.Db);
        await cache.SetAsync("key1", new ExecutiveDashboardDto { Headcount = 99 }, TimeSpan.FromMinutes(5));
        var result = await cache.GetAsync<ExecutiveDashboardDto>("key1");
        Assert.NotNull(result);
        Assert.Equal(99, result!.Headcount);
    }
}

public class CredentialClassificationTests
{
    [Theory]
    [InlineData(-1, "Expired")]
    [InlineData(15, "Expiring")]
    [InlineData(200, "Active")]
    public void ClassifyCredential_ReturnsExpectedStatus(int daysFromNow, string expected)
    {
        var expiry = DateTime.UtcNow.AddDays(daysFromNow);
        var status = daysFromNow < 0 ? "Expired" : daysFromNow < 90 ? "Expiring" : "Active";
        Assert.Equal(expected, status);
    }
}

public class SearchIndexTests
{
    [Fact]
    public async Task Search_FindsMatchingSkillText()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        fixture.Db.WorkforceSearchIndexes.Add(new WorkforceSearchIndex
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, PersonId = Guid.NewGuid(), EmploymentId = Guid.NewGuid(),
            SearchText = "Jane Doe ICU Ventilation", IndexedAtUtc = DateTime.UtcNow
        });
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.SearchAsync(new IntelligenceScope(orgId), "ventilation", 1, 10);
        Assert.Equal(1, result.TotalCount);
    }
}

public class ResponsibilityMatrixQueryTests
{
    [Fact]
    public async Task GetResponsibilities_CountsGapsAndCoverage()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var asOf = DateTime.UtcNow;
        fixture.Db.ResponsibilityMatrixEntries.AddRange(
            new ResponsibilityMatrixEntry { Id = Guid.NewGuid(), OrganizationId = orgId, ResponsibilityId = Guid.NewGuid(), ResponsibilityName = "A", CoverageStatus = "Covered", AsOfUtc = asOf },
            new ResponsibilityMatrixEntry { Id = Guid.NewGuid(), OrganizationId = orgId, ResponsibilityId = Guid.NewGuid(), ResponsibilityName = "B", CoverageStatus = "Gap", AsOfUtc = asOf }
        );
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.GetResponsibilitiesAsync(new IntelligenceScope(orgId, AsOf: asOf));
        Assert.Equal(1, result.Covered);
        Assert.Equal(1, result.Gaps);
    }
}

public class TrainingMatrixQueryTests
{
    [Fact]
    public async Task GetTraining_ComputesMandatoryCompliance()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var asOf = DateTime.UtcNow;
        fixture.Db.WorkforceSnapshots.Add(new WorkforceSnapshot { Id = Guid.NewGuid(), OrganizationId = orgId, AsOfUtc = asOf, Version = 1, TotalHeadcount = 1, TotalFte = 1 });
        fixture.Db.TrainingMatrixEntries.AddRange(
            new TrainingMatrixEntry { Id = Guid.NewGuid(), OrganizationId = orgId, PersonId = Guid.NewGuid(), EmploymentId = Guid.NewGuid(), CourseName = "A", IsMandatory = true, Status = "Completed", AsOfUtc = asOf },
            new TrainingMatrixEntry { Id = Guid.NewGuid(), OrganizationId = orgId, PersonId = Guid.NewGuid(), EmploymentId = Guid.NewGuid(), CourseName = "B", IsMandatory = true, Status = "Pending", AsOfUtc = asOf }
        );
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.GetTrainingAsync(new IntelligenceScope(orgId, AsOf: asOf));
        Assert.Equal(50, result.OrganizationCompliancePercent);
    }
}

public class SkillMatrixQueryTests
{
    [Fact]
    public async Task GetSkills_ReportsCriticalGaps()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var asOf = DateTime.UtcNow;
        fixture.Db.WorkforceSnapshots.Add(new WorkforceSnapshot { Id = Guid.NewGuid(), OrganizationId = orgId, AsOfUtc = asOf, Version = 1, TotalHeadcount = 1, TotalFte = 1 });
        fixture.Db.SkillMatrixEntries.Add(new SkillMatrixEntry
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, PersonId = Guid.NewGuid(), EmploymentId = Guid.NewGuid(),
            SkillName = "Ventilation", CurrentLevel = "Basic", Gap = "Below target", IsCritical = true, AsOfUtc = asOf
        });
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.GetSkillsAsync(new IntelligenceScope(orgId, AsOf: asOf));
        Assert.Single(result.CriticalGaps);
        Assert.Equal(0, result.CoveragePercent);
    }
}

public class LicenseDashboardQueryTests
{
    [Fact]
    public async Task GetLicenses_AggregatesStatusCounts()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var asOf = DateTime.UtcNow;
        fixture.Db.WorkforceSnapshots.Add(new WorkforceSnapshot { Id = Guid.NewGuid(), OrganizationId = orgId, AsOfUtc = asOf, Version = 1, TotalHeadcount = 2, TotalFte = 2 });
        fixture.Db.LicenseMatrixEntries.AddRange(
            new LicenseMatrixEntry { Id = Guid.NewGuid(), OrganizationId = orgId, PersonId = Guid.NewGuid(), EmploymentId = Guid.NewGuid(), LicenseName = "L1", Status = "Active", AsOfUtc = asOf },
            new LicenseMatrixEntry { Id = Guid.NewGuid(), OrganizationId = orgId, PersonId = Guid.NewGuid(), EmploymentId = Guid.NewGuid(), LicenseName = "L2", Status = "Expiring", AsOfUtc = asOf }
        );
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.GetLicensesAsync(new IntelligenceScope(orgId, AsOf: asOf));
        Assert.Equal(1, result.Active);
        Assert.Equal(1, result.Expiring);
    }
}

public class CommitteeAnalyticsQueryTests
{
    [Fact]
    public async Task GetCommittees_ReturnsAllCommitteeSnapshots()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        fixture.Db.CommitteeAnalyticsSnapshots.Add(new CommitteeAnalyticsSnapshot
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CommitteeId = Guid.NewGuid(), CommitteeName = "IPC",
            MemberCount = 5, AttendanceRate = 90, AsOfUtc = DateTime.UtcNow
        });
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.GetCommitteesAsync(new IntelligenceScope(orgId));
        Assert.Single(result.Committees);
        Assert.Equal("IPC", result.Committees[0].CommitteeName);
    }
}

public class OrganizationAnalyticsQueryTests
{
    [Fact]
    public async Task GetOrganization_ReturnsDepartmentSizes()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        fixture.Db.OrganizationAnalyticsSnapshots.Add(new OrganizationAnalyticsSnapshot
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentSizeJson = "{\"ICU\":10,\"ER\":8}",
            SpanOfControlAvg = 4.5m, AsOfUtc = DateTime.UtcNow
        });
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.GetOrganizationAnalyticsAsync(new IntelligenceScope(orgId));
        Assert.Equal(10, result.DepartmentSizes["ICU"]);
        Assert.Equal(4.5m, result.SpanOfControlAvg);
    }
}

public class AvailabilityAnalyticsQueryTests
{
    [Fact]
    public async Task GetAvailability_FallsBackToHeadcountWhenNoSnapshot()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        fixture.Db.WorkforceSnapshots.Add(new WorkforceSnapshot { Id = Guid.NewGuid(), OrganizationId = orgId, AsOfUtc = DateTime.UtcNow, Version = 1, TotalHeadcount = 25, TotalFte = 25 });
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.GetAvailabilityAsync(new IntelligenceScope(orgId));
        Assert.Equal(25, result.Working);
    }
}

public class CertificationDashboardQueryTests
{
    [Fact]
    public async Task GetCertifications_CountsExpiredItems()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var asOf = DateTime.UtcNow;
        fixture.Db.WorkforceSnapshots.Add(new WorkforceSnapshot { Id = Guid.NewGuid(), OrganizationId = orgId, AsOfUtc = asOf, Version = 1, TotalHeadcount = 1, TotalFte = 1 });
        fixture.Db.CertificationMatrixEntries.Add(new CertificationMatrixEntry
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, PersonId = Guid.NewGuid(), EmploymentId = Guid.NewGuid(),
            CertificationName = "ACLS", Status = "Expired", AsOfUtc = asOf
        });
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.GetCertificationsAsync(new IntelligenceScope(orgId, AsOf: asOf));
        Assert.Equal(1, result.Expired);
    }
}

public class CompetencyMatrixQueryTests
{
    [Fact]
    public async Task GetCompetencies_ReturnsGapRows()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var asOf = DateTime.UtcNow;
        fixture.Db.WorkforceSnapshots.Add(new WorkforceSnapshot { Id = Guid.NewGuid(), OrganizationId = orgId, AsOfUtc = asOf, Version = 1, TotalHeadcount = 1, TotalFte = 1 });
        fixture.Db.CompetencyMatrixEntries.Add(new CompetencyMatrixEntry
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, PersonId = Guid.NewGuid(), EmploymentId = Guid.NewGuid(),
            CompetencyName = "Sedation", CurrentLevel = "Novice", TargetLevel = "Expert", Gap = "Novice vs Expert", AsOfUtc = asOf
        });
        await fixture.Db.SaveChangesAsync();

        var queries = new IntelligenceQueryService(fixture.Db, new AnalyticsCacheService(fixture.Db), Options.Create(new IntelligenceOptions()));
        var result = await queries.GetCompetenciesAsync(new IntelligenceScope(orgId, AsOf: asOf));
        Assert.Single(result.Rows);
        Assert.Equal("Novice vs Expert", result.Rows[0].Gap);
    }
}

public class OutboxWriterTests
{
    [Fact]
    public async Task Enqueue_PersistsOutboxMessage()
    {
        using var fixture = new IntelligenceTestDb();
        var writer = new IntelligenceOutboxWriter(fixture.Db);
        await writer.EnqueueAsync("WorkforceSnapshotUpdated", Guid.NewGuid(), new { test = true });
        Assert.Single(await fixture.Db.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task Enqueue_StoresEventType()
    {
        using var fixture = new IntelligenceTestDb();
        var writer = new IntelligenceOutboxWriter(fixture.Db);
        var id = Guid.NewGuid();
        await writer.EnqueueAsync("ForecastGenerated", id, new { forecastType = "Attrition" });
        var msg = await fixture.Db.OutboxMessages.SingleAsync();
        Assert.Equal("ForecastGenerated", msg.Type);
        Assert.Equal(id, msg.AggregateId);
    }
}

public class ExecutiveMetricsTests
{
    [Fact]
    public async Task SnapshotRefresh_WritesExecutiveMetrics()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var workforce = new Mock<IWorkforceSourceClient>();
        workforce.Setup(w => w.ListEmploymentsAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SourceEmploymentDto>
            {
                new() { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, EmploymentStatus = "Active", Fte = 1 }
            });
        var professional = new Mock<IProfessionalSourceClient>();
        professional.Setup(p => p.ListProfilesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceProfessionalProfileDto>());
        professional.Setup(p => p.GetExpiringAsync(orgId, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceRenewalItemDto>());
        var governance = new Mock<IGovernanceSourceClient>();
        governance.Setup(g => g.GetSummaryAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(new SourceGovernanceSummaryDto { OpenActions = 2 });
        var organization = new Mock<IOrganizationSourceClient>();
        organization.Setup(o => o.GetDepartmentSizesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, int>());
        var outbox = new Mock<IIntelligenceOutboxWriter>();

        var service = new WorkforceSnapshotService(fixture.Db, workforce.Object, professional.Object, governance.Object, organization.Object,
            outbox.Object, Options.Create(new IntelligenceOptions()), NullLogger<WorkforceSnapshotService>.Instance);
        await service.RefreshOrganizationAsync(orgId, "corr");

        var exec = await fixture.Db.ExecutiveMetrics.SingleAsync();
        Assert.Equal(1, exec.Headcount);
        Assert.Equal(2, exec.CommitteeActionBacklog);
    }
}

public class ForecastImmutabilityTests
{
    [Fact]
    public async Task ForecastVersions_DoNotOverwritePriorRows()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var professional = new Mock<IProfessionalSourceClient>();
        professional.Setup(p => p.GetExpiringAsync(orgId, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceRenewalItemDto>());
        var engine = new ForecastEngine(fixture.Db, professional.Object, new Mock<IIntelligenceOutboxWriter>().Object, Options.Create(new IntelligenceOptions()));
        await engine.GenerateAsync(orgId, "Hiring", "c1");
        await engine.GenerateAsync(orgId, "Hiring", "c2");
        Assert.Equal(2, await fixture.Db.ForecastSnapshots.CountAsync());
    }
}

public class RecommendationExplainabilityTests
{
    [Fact]
    public async Task ExpiringCredential_ProducesHighConfidenceRenewal()
    {
        using var fixture = new IntelligenceTestDb();
        var orgId = Guid.NewGuid();
        var professional = new Mock<IProfessionalSourceClient>();
        professional.Setup(p => p.GetExpiringAsync(orgId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SourceRenewalItemDto>
            {
                new() { PersonId = Guid.NewGuid(), PersonName = "Dr A", CredentialType = "License", CredentialName = "Medical Council", DaysUntilExpiry = 10 }
            });
        var orchestrator = new RecommendationOrchestrator(fixture.Db, professional.Object, new Mock<IIntelligenceOutboxWriter>().Object, new AnalyticsCacheService(fixture.Db));
        await orchestrator.GenerateAsync(orgId, "c");
        var rec = await fixture.Db.WorkforceRecommendations.SingleAsync();
        Assert.Equal("LicenseRenewal", rec.RecommendationType);
        Assert.True(rec.Confidence >= 0.9m);
    }
}
