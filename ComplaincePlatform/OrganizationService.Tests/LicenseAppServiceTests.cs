using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OrganizationService.Application.Services;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;
using Xunit;

namespace OrganizationService.Tests;

public class LicenseAppServiceTests
{
    private static OrganizationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrganizationDbContext(options);
    }

    private static LicenseAppService CreateService(OrganizationDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Licensing:SigningKey"] = "TEST-LICENSE-SIGNING-KEY",
                ["Licensing:SyncCatalogFromLicense"] = "false"
            })
            .Build();
        var engine = new EntitlementEngine(config);
        return new LicenseAppService(db, config, NullLogger<LicenseAppService>.Instance, engine);
    }

    [Fact]
    public async Task IssueAsync_creates_issued_key_with_entitlements_and_expiry()
    {
        await using var db = CreateDb();
        var svc = CreateService(db);
        var orgId = Guid.NewGuid();

        var key = await svc.IssueAsync(
            organizationId: orgId,
            planId: null,
            type: LicenseType.Trial,
            maxSeats: 5,
            validDays: 30,
            graceDays: 7,
            entitlements: ["core.compliance", "ai.assist"],
            customerName: "Pilot Hospital",
            cancellationToken: CancellationToken.None);

        Assert.Equal(LicenseStatus.Issued, key.Status);
        Assert.Equal(orgId, key.OrganizationId);
        Assert.Equal(5, key.MaxSeats);
        Assert.Equal(7, key.GracePeriodDays);
        Assert.StartsWith("AOVI-", key.KeyCode);
        Assert.Contains("core.compliance", key.EntitlementsJson);
        Assert.NotNull(key.ExpiresAtUtc);
        Assert.True(key.ExpiresAtUtc > DateTime.UtcNow.AddDays(29));
    }

    [Fact]
    public async Task ActivateOnlineAsync_marks_active_and_sets_grace_end()
    {
        await using var db = CreateDb();
        var svc = CreateService(db);
        var orgId = Guid.NewGuid();

        var issued = await svc.IssueAsync(
            orgId, null, LicenseType.Subscription, 10, 365, 14, null, "Cust", CancellationToken.None);

        var status = await svc.ActivateOnlineAsync(
            issued.KeyCode, orgId, "fp-machine-1", "admin@test", CancellationToken.None);

        Assert.Equal(nameof(LicenseStatus.Active), status.Status);
        Assert.True(status.IsOperational);
        Assert.Equal(orgId, status.OrganizationId);

        var persisted = await db.LicenseKeys.SingleAsync();
        Assert.Equal(LicenseStatus.Active, persisted.Status);
        Assert.NotNull(persisted.ActivatedAtUtc);
        Assert.NotNull(persisted.GraceEndsAtUtc);
        Assert.Equal(persisted.ExpiresAtUtc!.Value.AddDays(14), persisted.GraceEndsAtUtc);

        var activation = await db.LicenseActivations.SingleAsync();
        Assert.True(activation.IsActive);
        Assert.Equal("Online", activation.ActivationMode);
        Assert.Equal("fp-machine-1", activation.MachineFingerprint);
    }

    [Fact]
    public async Task ValidateAsync_enters_grace_when_expired_but_within_grace_window()
    {
        await using var db = CreateDb();
        var svc = CreateService(db);
        var orgId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.LicenseKeys.Add(new LicenseKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            KeyCode = "AOVI-TEST-GRACE-0001",
            LicenseType = LicenseType.Subscription,
            Status = LicenseStatus.Active,
            MaxSeats = 10,
            UsedSeats = 0,
            GracePeriodDays = 14,
            IssuedAtUtc = now.AddDays(-40),
            ActivatedAtUtc = now.AddDays(-40),
            ExpiresAtUtc = now.AddDays(-2),
            GraceEndsAtUtc = now.AddDays(12),
            EntitlementsJson = "[\"core.compliance\"]",
            CreatedAtUtc = now.AddDays(-40)
        });
        await db.SaveChangesAsync();

        var status = await svc.ValidateAsync(orgId, CancellationToken.None);

        Assert.Equal(nameof(LicenseStatus.Grace), status.Status);
        Assert.True(status.IsOperational);
        Assert.Contains("grace", status.Message!, StringComparison.OrdinalIgnoreCase);

        var persisted = await db.LicenseKeys.SingleAsync();
        Assert.Equal(LicenseStatus.Grace, persisted.Status);
    }

    [Fact]
    public async Task ValidateAsync_marks_expired_after_grace_ends()
    {
        await using var db = CreateDb();
        var svc = CreateService(db);
        var orgId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.LicenseKeys.Add(new LicenseKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            KeyCode = "AOVI-TEST-EXPIRED-0001",
            LicenseType = LicenseType.Subscription,
            Status = LicenseStatus.Grace,
            MaxSeats = 10,
            UsedSeats = 0,
            GracePeriodDays = 7,
            IssuedAtUtc = now.AddDays(-60),
            ActivatedAtUtc = now.AddDays(-60),
            ExpiresAtUtc = now.AddDays(-20),
            GraceEndsAtUtc = now.AddDays(-5),
            EntitlementsJson = "[\"core.compliance\"]",
            CreatedAtUtc = now.AddDays(-60)
        });
        await db.SaveChangesAsync();

        var status = await svc.ValidateAsync(orgId, CancellationToken.None);

        Assert.Equal(nameof(LicenseStatus.Expired), status.Status);
        Assert.False(status.IsOperational);
        Assert.Contains("expired", status.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ActivateOnlineAsync_rejects_wrong_organization()
    {
        await using var db = CreateDb();
        var svc = CreateService(db);
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var issued = await svc.IssueAsync(
            orgA, null, LicenseType.Trial, 3, 30, 7, null, null, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ActivateOnlineAsync(issued.KeyCode, orgB, "fp", null, CancellationToken.None));
    }

    [Fact]
    public async Task IssueAsync_rejects_second_operational_trial()
    {
        await using var db = CreateDb();
        var svc = CreateService(db);
        var orgId = Guid.NewGuid();

        await svc.IssueAsync(orgId, null, LicenseType.Trial, 10, 30, 7, null, "A", CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.IssueAsync(orgId, null, LicenseType.Trial, 10, 30, 7, null, "B", CancellationToken.None));

        Assert.Contains("already has an operational", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.LicenseKeys.CountAsync());
    }

    [Fact]
    public async Task GetOrIssueTrialAsync_reuses_existing_operational_key()
    {
        await using var db = CreateDb();
        var svc = CreateService(db);
        var orgId = Guid.NewGuid();

        var first = await svc.IssueAsync(orgId, null, LicenseType.Trial, 10, 30, 7, null, "A", CancellationToken.None);
        var (second, reused) = await svc.GetOrIssueTrialAsync(orgId, 10, 30, 7, null, "A", null, CancellationToken.None);

        Assert.True(reused);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await db.LicenseKeys.CountAsync());
    }

    [Fact]
    public async Task GetOrIssueTrialAsync_issues_when_none_exists()
    {
        await using var db = CreateDb();
        var svc = CreateService(db);
        var orgId = Guid.NewGuid();

        var (key, reused) = await svc.GetOrIssueTrialAsync(
            orgId, 10, 30, 7, ["core.compliance"], "Cust", null, CancellationToken.None);

        Assert.False(reused);
        Assert.Equal(LicenseType.Trial, key.LicenseType);
        Assert.Equal(LicenseStatus.Issued, key.Status);
    }

    [Fact]
    public async Task AssignSeatAsync_is_idempotent_for_same_user()
    {
        await using var db = CreateDb();
        var svc = CreateService(db);
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var key = await svc.IssueAsync(orgId, null, LicenseType.Trial, 5, 30, 7, null, null, CancellationToken.None);
        await svc.ActivateOnlineAsync(key.KeyCode, orgId, "host", "admin", CancellationToken.None);

        var first = await svc.AssignSeatAsync(orgId, userId, "a@b.com", CancellationToken.None);
        var second = await svc.AssignSeatAsync(orgId, userId, "a@b.com", CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await db.LicenseSeatAssignments.CountAsync(x => x.IsActive));
        var persisted = await db.LicenseKeys.SingleAsync();
        Assert.Equal(1, persisted.UsedSeats);
    }
}
