using System.Text.Json;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ComplianceService.Tests;

public sealed class OutboxDomainEventDispatcherTests
{
    [Fact]
    public async Task Dispatch_serializes_concrete_FrameworkEnrolled_payload_not_interface_only()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ComplianceDbContext(options, userContext: null);
        var hospital = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var framework = Guid.Parse("0fe8a9c0-9ea0-4970-9097-d4f73d45dca7");
        var dispatcher = new OutboxDomainEventDispatcher(db);

        await dispatcher.DispatchAsync(
            [new FrameworkEnrolledDomainEvent(hospital, framework, Guid.NewGuid())],
            CancellationToken.None);
        await db.SaveChangesAsync();

        var payload = (await db.OutboxMessages.SingleAsync()).Payload;
        using var doc = JsonDocument.Parse(payload);
        Assert.Equal(hospital, doc.RootElement.GetProperty("HospitalId").GetGuid());
        Assert.Equal(framework, doc.RootElement.GetProperty("FrameworkId").GetGuid());
        Assert.True(doc.RootElement.TryGetProperty("RequestedByUserId", out _));
    }
}
