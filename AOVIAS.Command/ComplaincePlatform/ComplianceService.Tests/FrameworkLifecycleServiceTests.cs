using ComplianceService.Application.Contracts;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComplianceService.Tests;

public sealed class FrameworkLifecycleServiceTests : IDisposable
{
    private readonly ComplianceDbContext _db;
    private readonly RecordingNotificationPublisher _notifications;
    private readonly FrameworkLifecycleService _sut;

    public FrameworkLifecycleServiceTests()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ComplianceDbContext(options, userContext: null);
        _notifications = new RecordingNotificationPublisher();
        _sut = new FrameworkLifecycleService(
            _db,
            NullLogger<FrameworkLifecycleService>.Instance,
            _notifications);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Activate_RetiresSiblingActiveVersion_SameName()
    {
        var active = SeedFramework("NABH", "5.0", FrameworkCatalogStatus.Active);
        var published = SeedFramework("NABH", "6.0", FrameworkCatalogStatus.Published);
        await _db.SaveChangesAsync();

        var result = await _sut.ActivateAsync(published.Id);

        Assert.Equal(nameof(FrameworkCatalogStatus.Active), result.Status);
        Assert.True(result.IsActive);

        var sibling = await _db.ComplianceFrameworks.AsNoTracking().SingleAsync(f => f.Id == active.Id);
        Assert.Equal(FrameworkCatalogStatus.Retired, sibling.Status);
        Assert.False(sibling.IsActive);

        Assert.Contains(_notifications.Messages, m => m.EventType == "framework.activated");
    }

    [Fact]
    public async Task Archive_MakesFrameworkImmutable()
    {
        var retired = SeedFramework("NABH", "4.0", FrameworkCatalogStatus.Retired);
        await _db.SaveChangesAsync();

        await _sut.ArchiveAsync(retired.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PublishAsync(retired.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ActivateAsync(retired.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RetireAsync(retired.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ArchiveAsync(retired.Id));
    }

    private ComplianceFramework SeedFramework(string name, string version, FrameworkCatalogStatus status)
    {
        var framework = new ComplianceFramework
        {
            Id = Guid.NewGuid(),
            PlatformId = Guid.NewGuid().ToString("N")[..10],
            Name = name,
            Authority = "QCI",
            Version = version,
            Status = status,
            IsActive = status == FrameworkCatalogStatus.Active,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };
        _db.ComplianceFrameworks.Add(framework);
        return framework;
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
