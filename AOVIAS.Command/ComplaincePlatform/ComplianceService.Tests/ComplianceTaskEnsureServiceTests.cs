using ComplianceService.Application.Contracts;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Infrastructure.Domain;
using Xunit;

namespace ComplianceService.Tests;

public sealed class ComplianceTaskEnsureServiceTests : IDisposable
{
    private readonly ComplianceDbContext _db;
    private readonly IComplianceTaskEnsureService _sut;

    public ComplianceTaskEnsureServiceTests()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ComplianceDbContext(options, userContext: null);
        _sut = new ComplianceTaskEnsureService(
            _db,
            new UnitOfWork(_db),
            NullLogger<ComplianceTaskEnsureService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Ensure_creates_task_when_missing()
    {
        var (journey, oeId, userId) = await SeedGraphAsync();

        var task = await _sut.EnsureComplianceTaskForObjectiveElementAsync(journey, oeId, userId);

        var expectedStandardId = await _db.ObjectiveElements.AsNoTracking()
            .Where(o => o.Id == oeId)
            .Select(o => o.ComplianceStandardId)
            .FirstAsync();

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(journey.HospitalId, task.HospitalId);
        Assert.Equal(oeId, task.ObjectiveElementId);
        Assert.Equal(expectedStandardId, task.ComplianceStandardId);
        Assert.Equal(userId.ToString(), task.AssignedToUserId);
    }

    [Fact]
    public async Task Ensure_reuses_existing_task_and_does_not_change_assignee()
    {
        var (journey, oeId, userId) = await SeedGraphAsync();
        var otherUser = Guid.NewGuid();

        var first = await _sut.EnsureComplianceTaskForObjectiveElementAsync(journey, oeId, userId);
        var second = await _sut.EnsureComplianceTaskForObjectiveElementAsync(journey, oeId, otherUser);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(userId.ToString(), second.AssignedToUserId);
        Assert.Equal(1, await _db.ComplianceTasks.CountAsync());
    }

    [Fact]
    public async Task Ensure_backfills_empty_assignee_without_overwriting_non_empty()
    {
        var (journey, oeId, userId) = await SeedGraphAsync();
        var uploader = Guid.NewGuid();

        var standardId = await _db.ObjectiveElements.AsNoTracking()
            .Where(o => o.Id == oeId)
            .Select(o => o.ComplianceStandardId)
            .FirstAsync();

        var preloaded = new ComplianceTask
        {
            Id = Guid.NewGuid(),
            HospitalId = journey.HospitalId,
            ComplianceStandardId = standardId,
            ObjectiveElementId = oeId,
            AssignedToUserId = "   ",
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = ComplianceTaskStatus.Pending,
            Priority = ComplianceTaskPriority.Medium,
            Title = "pre",
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 2 }
        };
        _db.ComplianceTasks.Add(preloaded);
        await _db.SaveChangesAsync();

        var task = await _sut.EnsureComplianceTaskForObjectiveElementAsync(journey, oeId, uploader);

        Assert.Equal(preloaded.Id, task.Id);
        Assert.Equal(uploader.ToString(), task.AssignedToUserId);
    }

    [Fact]
    public async Task Ensure_throws_when_objective_element_not_in_framework()
    {
        var (journey, _, userId) = await SeedGraphAsync();
        var foreignOe = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.EnsureComplianceTaskForObjectiveElementAsync(journey, foreignOe, userId));
    }

    [Fact]
    public async Task Ensure_succeeds_when_no_regulation_row()
    {
        var (journey, oeId, userId) = await SeedGraphAsync();
        Assert.Equal(0, await _db.Regulations.CountAsync());

        var expectedStandardId = await _db.ObjectiveElements.AsNoTracking()
            .Where(o => o.Id == oeId)
            .Select(o => o.ComplianceStandardId)
            .FirstAsync();

        var task = await _sut.EnsureComplianceTaskForObjectiveElementAsync(journey, oeId, userId);

        Assert.Equal(expectedStandardId, task.ComplianceStandardId);
    }

    private async Task<(ComplianceJourney Journey, Guid OeId, Guid UserId)> SeedGraphAsync()
    {
        var tenantId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var frameworkId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var rv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };
        var fw = new ComplianceFramework
        {
            Id = frameworkId,
            PlatformId = "FW1",
            Name = "F",
            Authority = "A",
            Version = "1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            RowVersion = rv
        };
        var ch = new ComplianceChapter
        {
            Id = Guid.NewGuid(),
            PlatformId = "CH1",
            FrameworkId = fw.Id,
            Code = "C",
            Name = "N",
            Sequence = 1,
            CreatedAt = DateTime.UtcNow,
            RowVersion = rv
        };
        var st = new ComplianceStandard
        {
            Id = Guid.NewGuid(),
            PlatformId = "ST1",
            ChapterId = ch.Id,
            Code = "S",
            Name = "SN",
            Sequence = 1,
            CreatedAt = DateTime.UtcNow,
            RowVersion = rv
        };
        var oe = new ObjectiveElement
        {
            Id = Guid.NewGuid(),
            PlatformId = "OE1",
            ComplianceStandardId = st.Id,
            Code = "OE",
            Title = "T",
            Description = "D",
            CreatedAt = DateTime.UtcNow,
            IsApplicable = true,
            RowVersion = rv
        };
        var journey = new ComplianceJourney(
            tenantId,
            tenantId,
            hospitalId,
            frameworkId,
            validityPeriodInMonths: 12,
            minimumRequiredScore: 0);

        _db.ComplianceFrameworks.Add(fw);
        _db.ComplianceChapters.Add(ch);
        _db.ComplianceStandards.Add(st);
        _db.ObjectiveElements.Add(oe);

        _db.ComplianceJourneys.Add(journey);
        await _db.SaveChangesAsync();

        return (journey, oe.Id, userId);
    }
}
