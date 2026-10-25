using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using AppScoreResult = ComplianceService.Application.ComplianceScoring.ComplianceScoreResult;
using AppScoringService = ComplianceService.Application.Contracts.IComplianceScoringService;

namespace ComplianceService.Tests;

/// <summary>
/// Integration tests for the Compliance Maturity Dashboard aggregation pipeline.
/// Uses in-memory EF + mock IComplianceScoringService to verify end-to-end DTO assembly.
/// </summary>
public sealed class ComplianceMaturityDashboardIntegrationTests
{
    private readonly Guid _orgId      = Guid.NewGuid();
    private readonly Guid _hospitalId = Guid.NewGuid();
    private readonly Guid _frameworkId = Guid.NewGuid();

    private ComplianceDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ComplianceDbContext(options, userContext: null);
    }

    private async Task SeedAsync(ComplianceDbContext db)
    {
        var framework = new ComplianceFramework
        {
            Id         = _frameworkId,
            Name       = "NABH",
            Authority  = "QCI",
            Version    = "Entry Level",
            IsActive   = true,
            PlatformId = "NABH-01",
            RowVersion = Array.Empty<byte>(),
        };

        db.ComplianceFrameworks.Add(framework);

        var journey = new ComplianceJourney(
            _orgId, _orgId, _hospitalId, _frameworkId,
            validityPeriodInMonths: 12,
            minimumRequiredScore: 0);

        db.ComplianceJourneys.Add(journey);
        await db.SaveChangesAsync();
    }

    private static StubComplianceScoringService BuildScoringMock(decimal overallScore = 65m) =>
        new(overallScore);

    [Fact]
    public async Task GetDashboardAsync_ReturnsDto_WithCorrectOrganizationAndFramework()
    {
        await using var db = CreateDb(nameof(GetDashboardAsync_ReturnsDto_WithCorrectOrganizationAndFramework));
        await SeedAsync(db);

        var sut = BuildSut(db);
        var result = await sut.GetDashboardAsync(_hospitalId, _orgId);

        Assert.Equal(_orgId,      result.OrganizationId);
        Assert.Equal(_frameworkId, result.FrameworkId);
        Assert.Equal("NABH",      result.FrameworkName);
    }

    [Fact]
    public async Task GetDashboardAsync_CMI_IsCalculatedFromCoreScore()
    {
        await using var db = CreateDb(nameof(GetDashboardAsync_CMI_IsCalculatedFromCoreScore));
        await SeedAsync(db);

        var sut = BuildSut(db, overallScore: 80m);
        var result = await sut.GetDashboardAsync(_hospitalId, _orgId);

        // CMI = 80×0.40 + 0×0.30 + 0×0.20 + 0×0.10 = 32
        Assert.Equal(32m, result.ComplianceMaturityIndex.Value);
        Assert.Equal(80m, result.ComplianceMaturityIndex.CoreScore);
    }

    [Fact]
    public async Task GetDashboardAsync_Stage_IsCoreWhenScoreBelowThreshold()
    {
        await using var db = CreateDb(nameof(GetDashboardAsync_Stage_IsCoreWhenScoreBelowThreshold));
        await SeedAsync(db);

        var sut = BuildSut(db, overallScore: 60m);
        var result = await sut.GetDashboardAsync(_hospitalId, _orgId);

        Assert.Equal(MaturityStage.Core, result.CurrentStage);
    }

    [Fact]
    public async Task GetDashboardAsync_Stage_IsCommitmentWhenScoreAtThreshold()
    {
        await using var db = CreateDb(nameof(GetDashboardAsync_Stage_IsCommitmentWhenScoreAtThreshold));
        await SeedAsync(db);

        var sut = BuildSut(db, overallScore: 70m);
        var result = await sut.GetDashboardAsync(_hospitalId, _orgId);

        Assert.Equal(MaturityStage.Commitment, result.CurrentStage);
    }

    [Fact]
    public async Task GetDashboardAsync_FourStagesReturned()
    {
        await using var db = CreateDb(nameof(GetDashboardAsync_FourStagesReturned));
        await SeedAsync(db);

        var sut = BuildSut(db);
        var result = await sut.GetDashboardAsync(_hospitalId, _orgId);

        Assert.Equal(4, result.Stages.Count);
        Assert.Contains(result.Stages, s => s.Stage == MaturityStage.Core);
        Assert.Contains(result.Stages, s => s.Stage == MaturityStage.Commitment);
        Assert.Contains(result.Stages, s => s.Stage == MaturityStage.Achievement);
        Assert.Contains(result.Stages, s => s.Stage == MaturityStage.Excellence);
    }

    [Fact]
    public async Task GetDashboardAsync_CommitmentSummary_IsLocked_WhenScoreBelowThreshold()
    {
        await using var db = CreateDb(nameof(GetDashboardAsync_CommitmentSummary_IsLocked_WhenScoreBelowThreshold));
        await SeedAsync(db);

        var sut = BuildSut(db, overallScore: 50m);
        var result = await sut.GetDashboardAsync(_hospitalId, _orgId);

        Assert.True(result.CommitmentSummary.IsLocked);
        Assert.True(result.AchievementSummary.IsLocked);
        Assert.True(result.ExcellenceSummary.IsLocked);
    }

    [Fact]
    public async Task GetDashboardAsync_NextActions_AreNotEmpty()
    {
        await using var db = CreateDb(nameof(GetDashboardAsync_NextActions_AreNotEmpty));
        await SeedAsync(db);

        var sut = BuildSut(db);
        var result = await sut.GetDashboardAsync(_hospitalId, _orgId);

        Assert.NotEmpty(result.NextActions);
        Assert.True(result.NextActions.Count <= 5);
    }

    [Fact]
    public async Task GetDashboardAsync_ApiResponseShape_HasAllRequiredFields()
    {
        await using var db = CreateDb(nameof(GetDashboardAsync_ApiResponseShape_HasAllRequiredFields));
        await SeedAsync(db);

        var sut = BuildSut(db);
        var result = await sut.GetDashboardAsync(_hospitalId, _orgId);

        Assert.NotNull(result.ComplianceMaturityIndex);
        Assert.NotNull(result.CoreSummary);
        Assert.NotNull(result.CommitmentSummary);
        Assert.NotNull(result.AchievementSummary);
        Assert.NotNull(result.ExcellenceSummary);
        Assert.NotNull(result.NextActions);
        Assert.NotNull(result.Stages);
    }

    // ── Builder ───────────────────────────────────────────────────────────────

    private ComplianceMaturityAggregationService BuildSut(
        ComplianceDbContext db,
        decimal overallScore = 65m)
    {
        return new ComplianceMaturityAggregationService(
            db,
            BuildScoringMock(overallScore),
            new StubComplianceScoreService(),
            new ComplianceMaturityIndexService(),
            new PredictedGapEngineService(),
            new StubStageUnlockingService(),
            new StubEscalationService(),
            new StubCapaQueryService(),
            new StubAnalyticsAggregationService(),
            NullLogger<ComplianceMaturityAggregationService>.Instance);
    }
}

internal sealed class StubCapaQueryService : ICapaQueryService
{
    public Task<DeficiencyCapaDto?> GetCapaAsync(Guid deficiencyId, CancellationToken ct = default) =>
        Task.FromResult<DeficiencyCapaDto?>(null);

    public Task<CapaMetricsDto> GetMetricsAsync(
        Guid organizationId,
        Guid? hospitalId = null,
        Guid? frameworkId = null,
        CancellationToken ct = default) =>
        Task.FromResult(new CapaMetricsDto());

    public CapaActionStatus ResolveActionStatus(CapaActionStatus stored, DateTime? targetDate, DateTime? completedDate) =>
        stored;
}

internal sealed class StubEscalationService : IEscalationService
{
    public Task<EscalationEvaluationResultDto> EvaluateAsync(
        Guid deficiencyId, SlaEvaluationResultDto sla, bool persist = true, CancellationToken ct = default) =>
        Task.FromResult(new EscalationEvaluationResultDto { DeficiencyId = deficiencyId });

    public Task<IReadOnlyList<DeficiencyEscalationHistoryDto>> GetHistoryAsync(Guid deficiencyId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DeficiencyEscalationHistoryDto>>(Array.Empty<DeficiencyEscalationHistoryDto>());

    public Task<DeficiencyEscalationHistoryDto?> AcknowledgeAsync(
        Guid deficiencyId, Guid escalationId, string? acknowledgedBy, CancellationToken ct = default) =>
        Task.FromResult<DeficiencyEscalationHistoryDto?>(null);

    public Task<SlaEscalationMetricsDto> GetMetricsAsync(Guid organizationId, CancellationToken ct = default) =>
        Task.FromResult(new SlaEscalationMetricsDto());

    public Task<SlaEscalationMetricsDto> GetMetricsForScopeAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default) =>
        Task.FromResult(new SlaEscalationMetricsDto());
}

internal sealed class StubAnalyticsAggregationService : IAnalyticsAggregationService
{
    public Task<DeficiencyMetricCountsDto> GetDeficiencyCountsAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        Task.FromResult(new DeficiencyMetricCountsDto());

    public Task<CapaMetricsDto> GetCapaMetricsAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        Task.FromResult(new CapaMetricsDto());

    public Task<SlaEscalationMetricsDto> GetSlaMetricsAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        Task.FromResult(new SlaEscalationMetricsDto());

    public Task<AnalyticsReadinessDto> GetReadinessAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        Task.FromResult(new AnalyticsReadinessDto());

    public Task<AnalyticsGovernanceDto> GetGovernanceMetricsAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        Task.FromResult(new AnalyticsGovernanceDto());
}

internal sealed class StubStageUnlockingService : IStageUnlockingService
{
    public Task<IReadOnlyList<StageUnlockEvaluationResult>> EvaluateAndPersistAsync(
        Guid journeyId,
        string triggerSource,
        Guid? triggeredByUserId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StageUnlockEvaluationResult>>(Array.Empty<StageUnlockEvaluationResult>());

    public Task<IReadOnlyList<StageUnlockEvaluationResult>> GetStageEvaluationsAsync(
        Guid hospitalId,
        Guid organizationId,
        Guid frameworkId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StageUnlockEvaluationResult>>(Array.Empty<StageUnlockEvaluationResult>());

    public Task<bool> IsStageAccessibleAsync(
        Guid journeyId,
        Domain.ObjectiveElementClassification stage,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(stage == Domain.ObjectiveElementClassification.Core);

    public Task<StageUnlockEvaluationResult?> GetStageEvaluationAsync(
        Guid journeyId,
        Domain.ObjectiveElementClassification stage,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<StageUnlockEvaluationResult?>(null);
}

// ── Test doubles ──────────────────────────────────────────────────────────────

internal sealed class StubComplianceScoringService : AppScoringService
{
    private readonly decimal _score;

    public StubComplianceScoringService(decimal score) => _score = score;

    public Task<AppScoreResult> CalculateHospitalScoreAsync(Guid hospitalId) =>
        Task.FromResult(new AppScoreResult
        {
            HospitalId    = hospitalId,
            OverallScore  = _score,
            CriticalCount = 2,
            MajorCount    = 5,
            MinorCount    = 3,
        });

    public Task<AppScoreResult> CalculateAndPersistHospitalScoreAsync(Guid hospitalId) =>
        CalculateHospitalScoreAsync(hospitalId);

    public Task<AppScoreResult> CalculateFrameworkScoreAsync(Guid hospitalId, Guid frameworkId) =>
        Task.FromResult(new AppScoreResult
        {
            HospitalId    = hospitalId,
            OverallScore  = _score,
            CriticalCount = 2,
            MajorCount    = 5,
            MinorCount    = 3,
        });
}

internal sealed class StubComplianceScoreService : IComplianceScoreService
{
    public Task<decimal> GetJourneyScoreAsync(
        Guid frameworkId,
        Guid journeyId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(0m);

    public Task<AppScoreResult> GetFrameworkScoreAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AppScoreResult { HospitalId = hospitalId, OverallScore = 0m });

    public Task<AppScoreResult> GetHospitalScoreAsync(
        Guid hospitalId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AppScoreResult { HospitalId = hospitalId, OverallScore = 0m });

    public Task<decimal> GetStageScoreAsync(
        Guid journeyId,
        ObjectiveElementClassification stage,
        CancellationToken cancellationToken = default)
        => Task.FromResult(0m);
}
