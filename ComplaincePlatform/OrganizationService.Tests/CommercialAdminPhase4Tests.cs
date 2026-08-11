using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OrganizationService.Application.Services;
using OrganizationService.Domain;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;
using Xunit;

namespace OrganizationService.Tests;

public class CommercialAdminPhase4Tests
{
    private static OrganizationDbContext Db()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrganizationDbContext(options);
    }

    [Fact]
    public async Task Audit_requires_reason()
    {
        await using var db = Db();
        var svc = new CommercialAdminAuditAppService(db, NullLogger<CommercialAdminAuditAppService>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RecordAsync(Guid.NewGuid(), null, "op@test", "Org", "1", "Suspend", null, null, " ", null));
    }

    [Fact]
    public async Task Audit_persists()
    {
        await using var db = Db();
        var svc = new CommercialAdminAuditAppService(db, NullLogger<CommercialAdminAuditAppService>.Instance);
        var e = await svc.RecordAsync(Guid.NewGuid(), Guid.NewGuid(), "op@test", "Org", "1", "Suspend", "{}", "{}", "Customer request", "corr");
        Assert.Equal(1, await db.CommercialAdminAuditEvents.CountAsync());
        Assert.Equal("Suspend", e.Action);
    }

    [Fact]
    public async Task Org_status_suspend_and_restore()
    {
        await using var db = Db();
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Acme",
            Type = "Hospital",
            Address = "1 St",
            PlatformId = "ORG1",
            OrganizationCode = "ORG1CODE",
            Status = OrganizationStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            RowVersion = Array.Empty<byte>()
        };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var svc = new OrganizationCommercialAdminAppService(db, NullLogger<OrganizationCommercialAdminAppService>.Instance);
        await svc.SetStatusAsync(org.Id, OrganizationStatuses.Suspended);
        Assert.Equal(OrganizationStatuses.Suspended, (await svc.GetAsync(org.Id))!.Status);
        await svc.SetStatusAsync(org.Id, OrganizationStatuses.Active);
        Assert.Equal(OrganizationStatuses.Active, (await svc.GetAsync(org.Id))!.Status);
    }

    [Fact]
    public async Task Extend_trial_respects_max_extensions()
    {
        await using var db = Db();
        var orgId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = orgId,
            Name = "T",
            Type = "H",
            Address = "A",
            PlatformId = "ORG2",
            OrganizationCode = "C2",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            RowVersion = Array.Empty<byte>()
        });
        db.LicenseKeys.Add(new LicenseKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            KeyCode = "TRIAL-TEST-KEY-01",
            LicenseType = LicenseType.Trial,
            Status = LicenseStatus.Active,
            MaxSeats = 10,
            IssuedAtUtc = DateTime.UtcNow,
            ActivatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(10),
            ExtensionCount = 2,
            EntitlementsJson = "[]",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var licenses = new LicenseAppService(db, config, NullLogger<LicenseAppService>.Instance, new EntitlementEngine(config));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            licenses.ExtendTrialAsync(orgId, 15, maxExtensions: 2));
    }
}
