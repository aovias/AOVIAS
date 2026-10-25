using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OrganizationService.Application.Services;
using OrganizationService.Configuration;
using OrganizationService.Domain;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Email;
using Xunit;

namespace OrganizationService.Tests;

public class TrialLifecycleEngineTests
{
    private sealed class CapturingEmail : IEmailService
    {
        public List<(string To, string Subject)> Sent { get; } = new();
        public Task SendAsync(string to, string subject, string html)
        {
            Sent.Add((to, subject));
            return Task.CompletedTask;
        }
    }

    private static OrganizationDbContext Db()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrganizationDbContext(options);
    }

    private static async Task<(OrganizationDbContext db, Guid orgId, Guid keyId)> SeedTrialAsync(
        OrganizationDbContext db,
        LicenseStatus status,
        DateTime expiresAt,
        int extensionCount = 0)
    {
        var orgId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = orgId,
            Name = "Acme",
            Type = "Hospital",
            Address = "1 St",
            PlatformId = "ORG5",
            OrganizationCode = "ACME5",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            RowVersion = Array.Empty<byte>()
        });
        db.Members.Add(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            UserId = Guid.NewGuid(),
            Role = "ORG_ADMIN",
            Email = "admin@acme.test",
            FirstName = "Ada",
            CreatedAt = DateTime.UtcNow,
            RowVersion = Array.Empty<byte>()
        });
        var keyId = Guid.NewGuid();
        db.LicenseKeys.Add(new LicenseKey
        {
            Id = keyId,
            OrganizationId = orgId,
            KeyCode = "TRIAL-LIFE-001",
            LicenseType = LicenseType.Trial,
            Status = status,
            MaxSeats = 10,
            UsedSeats = 1,
            GracePeriodDays = 7,
            IssuedAtUtc = DateTime.UtcNow.AddDays(-20),
            ActivatedAtUtc = DateTime.UtcNow.AddDays(-20),
            ExpiresAtUtc = expiresAt,
            ExtensionCount = extensionCount,
            EntitlementsJson = "[]",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return (db, orgId, keyId);
    }

    private static TrialLifecycleEngine Engine(OrganizationDbContext db, CapturingEmail email, TrialLifecycleOptions? opts = null)
    {
        var config = new ConfigurationBuilder().Build();
        var licenses = new LicenseAppService(db, config, NullLogger<LicenseAppService>.Instance, new EntitlementEngine(config));
        var audit = new CommercialAdminAuditAppService(db, NullLogger<CommercialAdminAuditAppService>.Instance);
        return new TrialLifecycleEngine(
            db,
            licenses,
            audit,
            email,
            Options.Create(opts ?? new TrialLifecycleOptions { ReminderDays = [7, 3, 1] }),
            NullLogger<TrialLifecycleEngine>.Instance);
    }

    [Fact]
    public async Task Evaluate_sends_7day_reminder_once()
    {
        await using var db = Db();
        var email = new CapturingEmail();
        await SeedTrialAsync(db, LicenseStatus.Active, DateTime.UtcNow.AddDays(7).AddHours(-2));
        var engine = Engine(db, email);

        var r1 = await engine.EvaluateAllAsync("Manual");
        Assert.True(r1.ReminderCount >= 1);
        Assert.Equal(1, email.Sent.Count);

        var r2 = await engine.EvaluateAllAsync("Manual");
        Assert.Equal(0, r2.ReminderCount);
        Assert.Equal(1, email.Sent.Count);
        Assert.Equal(1, await db.TrialLifecycleNotificationLogs.CountAsync());
    }

    [Fact]
    public async Task Evaluate_transitions_to_grace_and_notifies()
    {
        await using var db = Db();
        var email = new CapturingEmail();
        var (_, _, keyId) = await SeedTrialAsync(db, LicenseStatus.Active, DateTime.UtcNow.AddMinutes(-5));
        var engine = Engine(db, email);

        var result = await engine.EvaluateAllAsync("Manual");
        Assert.True(result.TransitionCount >= 1);
        var key = await db.LicenseKeys.FirstAsync(k => k.Id == keyId);
        Assert.Equal(LicenseStatus.Grace, key.Status);
        Assert.Contains(email.Sent, s => s.Subject.Contains("grace", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Health_summary_reports_counts()
    {
        await using var db = Db();
        var email = new CapturingEmail();
        await SeedTrialAsync(db, LicenseStatus.Active, DateTime.UtcNow.AddDays(3));
        var engine = Engine(db, email);
        await engine.EvaluateAllAsync("Manual");
        var health = await engine.GetHealthSummaryAsync();
        Assert.NotNull(health);
    }
}
