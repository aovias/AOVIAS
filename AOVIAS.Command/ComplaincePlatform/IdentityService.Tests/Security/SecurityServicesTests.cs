using IdentityService.Application.Contracts.Security;
using IdentityService.Application.Services.Security;
using IdentityService.Domain;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdentityService.Tests.Security;

public class RiskScoringServiceTests
{
    private static (RiskScoringService service, IdentityDbContext db, SqliteConnection connection) CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(connection).Options;
        var db = new IdentityDbContext(options);
        db.Database.EnsureCreated();

        db.UserSecurityPolicies.Add(new UserSecurityPolicy
        {
            Id = Guid.NewGuid(), Name = "Default", IsActive = true, CreatedAt = DateTime.UtcNow,
            RiskThresholdMedium = 40, RiskThresholdHigh = 70
        });
        db.SaveChanges();

        var policyMock = new Mock<Application.Contracts.ISecurityPolicyService>();
        policyMock.Setup(p => p.GetEffectivePolicyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(db.UserSecurityPolicies.First());

        var eventsMock = new Mock<ISecurityEventService>();
        eventsMock.Setup(e => e.WriteOutboxAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var store = new Mock<IUserStore<AppUser>>();
        var userManager = new UserManager<AppUser>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var service = new RiskScoringService(db, policyMock.Object, eventsMock.Object, userManager);
        return (service, db, connection);
    }

    [Fact]
    public async Task CalculateAndPersistAsync_SameInputs_ProducesConsistentLevel()
    {
        var (service, db, connection) = CreateFixture();
        await using (connection)
        await using (db)
        {
            var userId = Guid.NewGuid();
            var first = await service.CalculateAndPersistAsync(userId);
            var second = await service.CalculateAndPersistAsync(userId);
            Assert.Equal(first.Level, second.Level);
            Assert.Equal(first.Score, second.Score);
        }
    }

    [Fact]
    public void ScoreToLevel_UsesPolicyThresholds()
    {
        var (service, db, connection) = CreateFixture();
        using (connection)
        using (db)
        {
            var policy = db.UserSecurityPolicies.First();
            Assert.Equal(RiskLevel.Low, service.ScoreToLevel(10, policy));
            Assert.Equal(RiskLevel.High, service.ScoreToLevel(50, policy));
            Assert.Equal(RiskLevel.Critical, service.ScoreToLevel(80, policy));
        }
    }
}

public class ThreatDetectionServiceTests
{
    [Fact]
    public async Task EvaluateRefreshReuseAsync_CreatesIncidentAndEvents()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(connection).Options;
        var db = new IdentityDbContext(options);
        db.Database.EnsureCreated();

        var eventsMock = new Mock<ISecurityEventService>();
        eventsMock.Setup(e => e.RecordAsync(It.IsAny<string>(), It.IsAny<SecurityEventSeverity>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        eventsMock.Setup(e => e.WriteOutboxAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var policyMock = new Mock<Application.Contracts.ISecurityPolicyService>();
        policyMock.Setup(p => p.GetEffectivePolicyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSecurityPolicy { Id = Guid.NewGuid(), Name = "Default", IsActive = true, CreatedAt = DateTime.UtcNow });

        var riskMock = new Mock<IRiskScoringService>();
        riskMock.Setup(r => r.CalculateAndPersistAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRiskScoreDto { UserId = Guid.NewGuid(), Score = 0, Level = "Low", FactorsJson = "[]", CalculatedAt = DateTime.UtcNow });

        var service = new ThreatDetectionService(db, eventsMock.Object, policyMock.Object, riskMock.Object);
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await service.EvaluateRefreshReuseAsync(userId, sessionId);

        Assert.Single(db.SecurityIncidents);
        Assert.Equal("SessionCompromised", db.SecurityIncidents.First().IncidentType);
        await using (connection)
        await using (db) { }
    }
}

public class SecurityTimelineServiceTests
{
    [Fact]
    public async Task GetTimelineAsync_ReturnsDescendingOrder()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();

        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.SecurityEvents.AddRange(
            new SecurityEvent { Id = Guid.NewGuid(), EventType = "A", Severity = SecurityEventSeverity.Info, UserId = userId, Summary = "older", OccurredAt = now.AddHours(-2) },
            new SecurityEvent { Id = Guid.NewGuid(), EventType = "B", Severity = SecurityEventSeverity.Info, UserId = userId, Summary = "newer", OccurredAt = now });
        await db.SaveChangesAsync();

        var service = new SecurityTimelineService(db);
        var timeline = await service.GetTimelineAsync(userId, 10);

        Assert.Equal("newer", timeline.First().Summary);
        await using (connection)
        await using (db) { }
    }
}
