using ComplianceService.Application.Aggregation;
using ComplianceService.Application.Aggregation.Models;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Policies;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Infrastructure.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

/// <summary>
/// Verifies the canonical single source of truth: the journey-, framework- and hospital-level
/// scores and the legacy <see cref="ComplianceScoringService"/> all return the identical value
/// for the same hospital/framework/journey, proving Dashboard, Sidebar, Journey and Reports agree.
/// </summary>
public class ComplianceScoreServiceParityTests
{
    private const decimal CanonicalPercent = 73.5m;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _hospitalId = Guid.NewGuid();
    private readonly Guid _frameworkId = Guid.NewGuid();

    [Fact]
    public async Task All_score_levels_return_identical_canonical_value()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ComplianceDbContext(options, userContext: null);
        await db.Database.EnsureCreatedAsync();

        var journey = new ComplianceJourney(
            _tenantId, _orgId, _hospitalId, _frameworkId,
            validityPeriodInMonths: 12, minimumRequiredScore: 0);
        db.ComplianceJourneys.Add(journey);
        db.Set<Regulation>().Add(new Regulation
        {
            Id = Guid.NewGuid(),
            HospitalId = _hospitalId,
            ComplianceFrameworkId = _frameworkId,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            PlatformId = "REG1",
            RowVersion = Array.Empty<byte>()
        });
        await db.SaveChangesAsync();

        var aggregation = new StubAggregationService(CanonicalPercent);
        var classification = new StubClassificationPolicy();
        var sut = new ComplianceScoreService(db, aggregation, classification, NullLogger<ComplianceScoreService>.Instance);

        var journeyScore = await sut.GetJourneyScoreAsync(_frameworkId, journey.Id);
        var frameworkResult = await sut.GetFrameworkScoreAsync(_hospitalId, _frameworkId);
        var hospitalResult = await sut.GetHospitalScoreAsync(_hospitalId);

        Assert.Equal(CanonicalPercent, journeyScore);
        Assert.Equal(CanonicalPercent, frameworkResult.OverallScore);
        Assert.Equal(CanonicalPercent, hospitalResult.OverallScore);

        // The legacy engine that feeds Dashboard + Reports must inherit the same number.
        var scoringService = new ComplianceScoringService(
            db,
            NullLogger<ComplianceScoringService>.Instance,
            new NoopSnapshotRecalculationService(),
            sut);

        var dashboardResult = await scoringService.CalculateFrameworkScoreAsync(_hospitalId, _frameworkId);
        var reportsHospitalResult = await scoringService.CalculateHospitalScoreAsync(_hospitalId);

        Assert.Equal(CanonicalPercent, dashboardResult.OverallScore);
        Assert.Equal(CanonicalPercent, reportsHospitalResult.OverallScore);
    }

    private sealed class StubAggregationService : IAggregationService
    {
        private readonly decimal _percent;
        public StubAggregationService(decimal percent) => _percent = percent;

        public Task<FrameworkAggregateResult> ComputeFrameworkAsync(
            Guid frameworkId,
            Guid journeyId,
            IReadOnlyCollection<ObjectiveElementClassification> includedClassifications,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new FrameworkAggregateResult
            {
                FrameworkId = frameworkId,
                CompliancePercent = _percent,
                ChapterCount = 1,
                Chapters = new List<ChapterAggregateResult>
                {
                    new() { ChapterId = Guid.NewGuid(), CompliancePercent = _percent, StandardCount = 1 }
                }
            });

        public Task<FrameworkAggregateResult> ComputeFrameworkAsync(
            Guid frameworkId,
            Guid journeyId,
            IReadOnlyList<ComplianceChapter> preloadedChapters,
            IReadOnlyList<ComplianceStandard> preloadedStandards,
            IReadOnlyList<ObjectiveElement> preloadedObjectiveElements,
            CancellationToken cancellationToken = default)
            => ComputeFrameworkAsync(frameworkId, journeyId, Array.Empty<ObjectiveElementClassification>(), cancellationToken);
    }

    private sealed class StubClassificationPolicy : IOperationalModeClassificationPolicy
    {
        public IReadOnlyList<ObjectiveElementClassification> GetClassificationsForWorkspace(OperationalMode mode)
            => Array.Empty<ObjectiveElementClassification>();

        public IReadOnlyList<ObjectiveElementClassification> GetClassificationsForDecisionRules(OperationalMode mode)
            => Array.Empty<ObjectiveElementClassification>();

        public bool IsClassificationEditable(OperationalMode mode, ObjectiveElementClassification classification)
            => false;
    }

    private sealed class NoopSnapshotRecalculationService : ComplianceService.Application.Contracts.IComplianceFrameworkSnapshotRecalculationService
    {
        public Task<bool> RecalculateAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
