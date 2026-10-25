using IdentityService.Application.Services;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using IdentityService.Application.Contracts;
using IdentityService.Application.Contracts.Security;

namespace IdentityService.Tests;

public class SessionServiceTests
{
    private static (SessionService service, IdentityDbContext db, SqliteConnection connection) CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new IdentityDbContext(options);
        db.Database.EnsureCreated();

        var policy = new UserSecurityPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            RefreshTokenAbsoluteExpirationDays = 7,
            RefreshTokenSlidingExpirationMinutes = 1440,
            MaxConcurrentSessions = 10,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        db.UserSecurityPolicies.Add(policy);
        db.SaveChanges();

        var policyMock = new Mock<ISecurityPolicyService>();
        policyMock.Setup(p => p.GetEffectivePolicyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var auditMock = new Mock<IIdentityAuditService>();
        auditMock.Setup(a => a.RecordFailureAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var threatMock = new Mock<IThreatDetectionService>();
        threatMock.Setup(t => t.EvaluateRefreshReuseAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var historyMock = new Mock<ISessionHistoryService>();
        historyMock.Setup(h => h.AppendAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Domain.Enums.SessionHistoryAction>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new SessionService(db, policyMock.Object, auditMock.Object, threatMock.Object, historyMock.Object);
        return (service, db, connection);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_SecondUseOfSameToken_FailsAfterRotation()
    {
        var (service, db, connection) = CreateFixture();
        await using (connection)
        await using (db)
        {
        var userId = Guid.NewGuid();
        var refreshToken = TokenHasher.GenerateOpaqueToken();
        var hash = TokenHasher.Hash(refreshToken);
        var now = DateTime.UtcNow;

        db.UserSessions.Add(new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = hash,
            TokenFamilyId = Guid.NewGuid(),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7),
            LastActivityAt = now,
            Status = SessionStatus.Active
        });
        await db.SaveChangesAsync();

        var first = await service.RotateRefreshTokenAsync(refreshToken, CancellationToken.None);
        Assert.NotNull(first);

        var second = await service.RotateRefreshTokenAsync(refreshToken, CancellationToken.None);
        Assert.Null(second);
        }
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ReuseOfRevokedToken_RevokesFamily()
    {
        var (service, db, connection) = CreateFixture();
        await using (connection)
        await using (db)
        {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var refreshToken = TokenHasher.GenerateOpaqueToken();
        var hash = TokenHasher.Hash(refreshToken);
        var now = DateTime.UtcNow;

        db.UserSessions.Add(new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = hash,
            TokenFamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(7),
            LastActivityAt = now,
            Status = SessionStatus.Revoked,
            RevokedAt = now
        });

        var activeSession = new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = TokenHasher.Hash(TokenHasher.GenerateOpaqueToken()),
            TokenFamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(7),
            LastActivityAt = now,
            Status = SessionStatus.Active
        };
        db.UserSessions.Add(activeSession);
        await db.SaveChangesAsync();

        var result = await service.RotateRefreshTokenAsync(refreshToken, CancellationToken.None);
        Assert.Null(result);

        var updated = await db.UserSessions.FindAsync(activeSession.SessionId);
        Assert.Equal(SessionStatus.ReuseDetected, updated!.Status);
        }
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ConcurrentRequests_OnlyOneSucceeds()
    {
        var (service, db, connection) = CreateFixture();
        try
        {
        var userId = Guid.NewGuid();
        var refreshToken = TokenHasher.GenerateOpaqueToken();
        var hash = TokenHasher.Hash(refreshToken);
        var now = DateTime.UtcNow;

        db.UserSessions.Add(new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = hash,
            TokenFamilyId = Guid.NewGuid(),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7),
            LastActivityAt = now,
            Status = SessionStatus.Active
        });
        await db.SaveChangesAsync();

        var policyMock = new Mock<ISecurityPolicyService>();
        policyMock.Setup(p => p.GetEffectivePolicyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(db.UserSecurityPolicies.First());

        var auditMock = new Mock<IIdentityAuditService>();
        auditMock.Setup(a => a.RecordFailureAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var threatMock = new Mock<IThreatDetectionService>();
        threatMock.Setup(t => t.EvaluateRefreshReuseAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var historyMock = new Mock<ISessionHistoryService>();
        historyMock.Setup(h => h.AppendAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Domain.Enums.SessionHistoryAction>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dbOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(connection)
            .Options;

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                await using var taskDb = new IdentityDbContext(dbOptions);
                var taskService = new SessionService(taskDb, policyMock.Object, auditMock.Object, threatMock.Object, historyMock.Object);
                return await taskService.RotateRefreshTokenAsync(refreshToken, CancellationToken.None);
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var successes = results.Count(r => r != null);

        Assert.Equal(1, successes);
        }
        finally
        {
            await db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
