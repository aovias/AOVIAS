using ComplianceService.Application.BackgroundJobs;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComplianceService.Tests;

public sealed class RiskRegisterTests : IDisposable
{
    private readonly ComplianceDbContext _db;
    private readonly RiskRegisterAppService _service;
    private readonly RecordingNotificationPublisher _notifications;

    public RiskRegisterTests()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ComplianceDbContext(options, userContext: null);
        _service = new RiskRegisterAppService(_db);
        _notifications = new RecordingNotificationPublisher();
    }

    public void Dispose() => _db.Dispose();

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(3, 4, 12)]
    [InlineData(5, 5, 25)]
    public void RiskScore_IsLikelihoodTimesImpact(int likelihood, int impact, int expected)
    {
        var risk = new Risk
        {
            Likelihood = likelihood,
            Impact = impact,
        };

        Assert.Equal(expected, risk.RiskScore);
    }

    [Theory]
    [InlineData(2, 3, 6)]
    [InlineData(4, 5, 20)]
    public void ResidualRiskScore_IsResidualLikelihoodTimesImpact(int rl, int ri, int expected)
    {
        var risk = new Risk
        {
            Likelihood = 5,
            Impact = 5,
            ResidualLikelihood = rl,
            ResidualImpact = ri,
        };

        Assert.Equal(expected, risk.ResidualRiskScore);
    }

    [Fact]
    public void ResidualRiskScore_NullWhenIncomplete()
    {
        var risk = new Risk { Likelihood = 3, Impact = 3, ResidualLikelihood = 2 };
        Assert.Null(risk.ResidualRiskScore);
    }

    [Fact]
    public async Task ClosedRisk_IsImmutable_OnUpdate()
    {
        var orgId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var risk = SeedRisk(orgId, hospitalId, RiskStatus.Assessed);
        risk.TransitionTo(RiskStatus.Closed);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(orgId, risk.Id, Guid.NewGuid(), new UpdateRiskRequestDto
            {
                Title = "Should fail",
            }));

        Assert.Contains("immutable", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClosedRisk_CannotTransitionAway()
    {
        var risk = new Risk { Status = RiskStatus.Closed, Likelihood = 1, Impact = 1 };
        Assert.False(risk.Status.CanTransitionTo(RiskStatus.Identified));
        Assert.Throws<InvalidOperationException>(() => risk.TransitionTo(RiskStatus.Mitigating));
    }

    [Fact]
    public async Task Create_ComputesScore_AndListReturnsIt()
    {
        var orgId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();

        var created = await _service.CreateAsync(orgId, hospitalId, Guid.NewGuid(), new CreateRiskRequestDto
        {
            Title = "Falls risk",
            Likelihood = 4,
            Impact = 5,
            Category = "Clinical",
        });

        Assert.Equal(20, created.RiskScore);
        Assert.Equal(nameof(RiskStatus.Identified), created.Status);

        var list = await _service.ListAsync(orgId, hospitalId, null, null, null, null, null, null, 1, 25);
        Assert.Equal(1, list.TotalCount);
        Assert.Equal(20, list.Items[0].RiskScore);
    }

    [Fact]
    public async Task Matrix_BucketsByLikelihoodAndImpact()
    {
        var orgId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        SeedRisk(orgId, hospitalId, RiskStatus.Identified, likelihood: 5, impact: 5);
        SeedRisk(orgId, hospitalId, RiskStatus.Mitigating, likelihood: 5, impact: 5);
        SeedRisk(orgId, hospitalId, RiskStatus.Closed, likelihood: 5, impact: 5);
        await _db.SaveChangesAsync();

        var matrix = await _service.GetMatrixAsync(orgId, hospitalId);
        var cell = matrix.Cells.Single(c => c.Likelihood == 5 && c.Impact == 5);
        Assert.Equal(2, cell.Count);
        Assert.Equal(2, matrix.TotalActiveRisks);
        Assert.Equal("Critical", cell.Band);
    }

    [Fact]
    public async Task ReviewDueJob_MovesToUnderReview_AndNotifies()
    {
        var orgId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var risk = SeedRisk(orgId, Guid.NewGuid(), RiskStatus.Mitigating);
        risk.OwnerUserId = ownerId;
        risk.ReviewDueDate = DateTime.UtcNow.AddDays(-1);
        await _db.SaveChangesAsync();

        var job = new RiskReviewDueJob(_db, _notifications, NullLogger<RiskReviewDueJob>.Instance);
        await job.ProcessReviewDueAsync();

        var updated = await _db.Risks.AsNoTracking().SingleAsync(r => r.Id == risk.Id);
        Assert.Equal(RiskStatus.UnderReview, updated.Status);
        Assert.Contains(_notifications.Messages, m => m.EventType == "RiskReviewDue");
    }

    private Risk SeedRisk(
        Guid orgId,
        Guid hospitalId,
        RiskStatus status,
        int likelihood = 3,
        int impact = 3)
    {
        var risk = new Risk
        {
            Id = Guid.NewGuid(),
            TenantId = orgId,
            HospitalId = hospitalId,
            Title = "Seed risk",
            Likelihood = likelihood,
            Impact = impact,
            Severity = ComplianceSeverity.Major,
            Status = status,
            IdentifiedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
            ClosedAt = status == RiskStatus.Closed ? DateTime.UtcNow : null,
        };
        _db.Risks.Add(risk);
        return risk;
    }

    private sealed class RecordingNotificationPublisher : IComplianceNotificationPublisher
    {
        public List<ComplianceNotificationMessage> Messages { get; } = new();

        public Task PublishAsync(ComplianceNotificationMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
