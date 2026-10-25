using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrganizationService.Application.Services;
using OrganizationService.Infrastructure.Persistence;
using Xunit;

namespace OrganizationService.Tests;

public class OnboardingTelemetryAppServiceTests
{
    [Fact]
    public async Task RecordAsync_persists_event()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new OrganizationDbContext(options);
        var svc = new OnboardingTelemetryAppService(db, NullLogger<OnboardingTelemetryAppService>.Instance);
        var orgId = Guid.NewGuid();

        var entity = await svc.RecordAsync(
            orgId,
            Guid.NewGuid(),
            null,
            "OnboardingOpened",
            "Welcome",
            "corr-1",
            "{\"source\":\"test\"}",
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal("OnboardingOpened", entity.EventType);
        Assert.Equal(1, await db.OnboardingTelemetryEvents.CountAsync());
    }

    [Fact]
    public async Task RecordAsync_requires_event_type()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new OrganizationDbContext(options);
        var svc = new OnboardingTelemetryAppService(db, NullLogger<OnboardingTelemetryAppService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RecordAsync(Guid.NewGuid(), null, null, " ", null, null, null, CancellationToken.None));
    }
}
