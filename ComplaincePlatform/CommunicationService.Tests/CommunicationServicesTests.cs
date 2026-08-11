using CommunicationService.Application.Services;
using CommunicationService.Domain.Entities;
using CommunicationService.Domain.Enums;
using CommunicationService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunicationService.Tests;

public class TemplateRendererTests
{
    [Fact]
    public async Task RenderAsync_SubstitutesVariables()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new CommunicationDbContext(new DbContextOptionsBuilder<CommunicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        var templateId = Guid.NewGuid();
        db.CommunicationTemplates.Add(new CommunicationTemplate { Id = templateId, Code = "test", Name = "Test", Channel = CommunicationChannelType.Email, ModuleCode = "Test", Status = TemplateStatus.Published, CurrentVersion = 1, CreatedAt = DateTime.UtcNow });
        db.TemplateVersions.Add(new TemplateVersion { Id = Guid.NewGuid(), TemplateId = templateId, Version = 1, Locale = "en-IN", Subject = "Hello {{name}}", Body = "Body {{name}}", PublishedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var renderer = new TemplateRenderer(db);
        var result = await renderer.RenderAsync("test", new Dictionary<string, string> { ["name"] = "PRAMAN" });
        Assert.Equal("Hello PRAMAN", result!.Value.subject);
        await connection.DisposeAsync();
        await db.DisposeAsync();
    }
}

public class QuietHoursPreferenceTests
{
    [Fact]
    public async Task IsQuietHoursAsync_RespectsConfiguredWindow()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new CommunicationDbContext(new DbContextOptionsBuilder<CommunicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        var personId = Guid.NewGuid();
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        db.NotificationPreferences.Add(new NotificationPreference
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Channel = CommunicationChannelType.InApp,
            Enabled = true,
            QuietHoursStart = now.AddMinutes(-30),
            QuietHoursEnd = now.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var engine = new AvailabilityEngine(db);
        Assert.True(await engine.IsQuietHoursAsync(personId));
        await connection.DisposeAsync();
        await db.DisposeAsync();
    }
}

public class AvailabilityEngineTests
{
    [Fact]
    public async Task CanDeliverNowAsync_CriticalBypassesVacation()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new CommunicationDbContext(new DbContextOptionsBuilder<CommunicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        var personId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        db.AvailabilityProfiles.Add(new AvailabilityProfile { Id = profileId, PersonId = personId, Status = AvailabilityStatus.Vacation, CreatedAt = DateTime.UtcNow });
        db.VacationEntries.Add(new VacationEntry { Id = Guid.NewGuid(), AvailabilityProfileId = profileId, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) });
        await db.SaveChangesAsync();

        var engine = new AvailabilityEngine(db);
        Assert.True(await engine.CanDeliverNowAsync(personId, critical: true));
        Assert.False(await engine.CanDeliverNowAsync(personId, critical: false));
        await connection.DisposeAsync();
        await db.DisposeAsync();
    }
}

public class DeduplicationTests
{
    [Fact]
    public async Task SendAsync_SameDedupeKey_ReturnsExisting()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new CommunicationDbContext(new DbContextOptionsBuilder<CommunicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        var existingId = Guid.NewGuid();
        db.OutboundMessages.Add(new OutboundMessage { Id = existingId, PersonId = Guid.Empty, ModuleCode = "Test", EventType = "Test", Subject = "s", Body = "b", DedupeKey = "key1", Status = OutboundMessageStatus.Completed, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new CommunicationRequestService(db, null!, new AvailabilityEngine(db), new TemplateRenderer(db));
        var result = await service.SendAsync(new Application.Contracts.SendCommunicationRequest
        {
            ModuleCode = "Test", EventType = "Test", RecipientEmail = "a@b.com", DedupeKey = "key1"
        });
        Assert.Equal(existingId, result.OutboundMessageId);
        await connection.DisposeAsync();
        await db.DisposeAsync();
    }
}

public class TimelineOrderingTests
{
    [Fact]
    public async Task Timeline_ReturnsNewestFirst()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new CommunicationDbContext(new DbContextOptionsBuilder<CommunicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        var personId = Guid.NewGuid();
        db.CommunicationTimeline.AddRange(
            new CommunicationTimelineEntry { Id = Guid.NewGuid(), PersonId = personId, Source = "Test", EventType = "A", Summary = "older", OccurredAt = DateTime.UtcNow.AddHours(-2) },
            new CommunicationTimelineEntry { Id = Guid.NewGuid(), PersonId = personId, Source = "Test", EventType = "B", Summary = "newer", OccurredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var timeline = new CommunicationTimelineService(db);
        var items = await timeline.GetAsync(personId, 10);
        Assert.Equal("newer", items[0].Summary);
        await connection.DisposeAsync();
        await db.DisposeAsync();
    }
}
