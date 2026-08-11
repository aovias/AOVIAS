using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrganizationService.Application.DTOs;
using OrganizationService.Application.Services;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;
using OrganizationService.Infrastructure.Storage;
using Shared.Infrastructure.Domain;
using Xunit;

namespace OrganizationService.Tests;

public class OrganizationSettingsAppServiceTests
{
    private static OrganizationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrganizationDbContext(options);
    }

    [Fact]
    public async Task Update_rejects_duplicate_organization_code()
    {
        await using var db = CreateDb();
        var orgA = new Organization { Id = Guid.NewGuid(), Name = "A", Type = "Hospital", Address = "A", PlatformId = "ORG001", OrganizationCode = "ALPHA", CreatedAt = DateTime.UtcNow, RowVersion = new byte[8] };
        var orgB = new Organization { Id = Guid.NewGuid(), Name = "B", Type = "Hospital", Address = "B", PlatformId = "ORG002", OrganizationCode = "BETA", CreatedAt = DateTime.UtcNow, RowVersion = new byte[8] };
        db.Organizations.AddRange(orgA, orgB);
        db.OrganizationSettings.Add(new OrganizationSettings { OrganizationId = orgA.Id, RowVersion = new byte[8] });
        db.OrganizationSettings.Add(new OrganizationSettings { OrganizationId = orgB.Id, RowVersion = new byte[8] });
        await db.SaveChangesAsync();

        var timeline = new OrganizationTimelineProjector(db);
        var storage = new LocalFileStorageService(new TestHostEnvironment());
        var svc = new OrganizationSettingsAppService(db, storage, timeline, Options.Create(new OrganizationSettingsOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateAsync(
            orgB.Id,
            new UpdateOrganizationSettingsRequest
            {
                OrganizationName = "B",
                OrganizationCode = "ALPHA",
                TimeZoneId = "Asia/Kolkata",
                LanguageCode = "en",
                WorkingDays = ["Monday"],
                WorkingHours = new WorkingHoursDto { Start = "09:00", End = "18:00" }
            },
            Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_writes_timeline_audit_payload()
    {
        await using var db = CreateDb();
        var org = new Organization { Id = Guid.NewGuid(), Name = "A", Type = "Hospital", Address = "A", PlatformId = "ORG001", OrganizationCode = "ALPHA", CreatedAt = DateTime.UtcNow, RowVersion = new byte[8] };
        db.Organizations.Add(org);
        db.OrganizationSettings.Add(new OrganizationSettings { OrganizationId = org.Id, RowVersion = new byte[8] });
        await db.SaveChangesAsync();

        var timeline = new OrganizationTimelineProjector(db);
        var storage = new LocalFileStorageService(new TestHostEnvironment());
        var svc = new OrganizationSettingsAppService(db, storage, timeline, Options.Create(new OrganizationSettingsOptions()));
        var actor = Guid.NewGuid();

        await svc.UpdateAsync(
            org.Id,
            new UpdateOrganizationSettingsRequest
            {
                OrganizationName = "Renamed Org",
                OrganizationCode = "ALPHA",
                TimeZoneId = "UTC",
                LanguageCode = "en",
                WorkingDays = ["Monday", "Friday"],
                WorkingHours = new WorkingHoursDto { Start = "08:00", End = "17:00" }
            },
            actor);

        var entry = await db.OrganizationTimeline.SingleAsync();
        Assert.Equal("OrganizationSettings.Updated", entry.EventType);
        Assert.Contains("OrganizationName", entry.PayloadJson);
        Assert.Contains("Renamed Org", entry.PayloadJson);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
