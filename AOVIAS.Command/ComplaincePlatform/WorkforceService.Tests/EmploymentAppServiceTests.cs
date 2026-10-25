using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using WorkforceService.Application.Contracts;
using WorkforceService.Application.DTOs;
using WorkforceService.Application.Services;
using WorkforceService.Domain.Aggregates;
using WorkforceService.Domain.Entities;
using WorkforceService.Infrastructure.Persistence;
using Shared.Infrastructure.Services;
using Shared.Logging.Audit;
using Xunit;

namespace WorkforceService.Tests;

public class EmploymentAppServiceTests
{
    private static WorkforceDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new WorkforceDbContext(options);
    }

    [Fact]
    public async Task AssignManager_RejectsSelfReference()
    {
        await using var db = CreateDb();
        var employment = new Employment
        {
            Id = Guid.NewGuid(),
            EmploymentNumber = "EMP000001",
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            HospitalId = Guid.NewGuid(),
            PersonSearchName = "TEST USER",
            RowVersion = new byte[8],
            CreatedAt = DateTime.UtcNow
        };
        db.Employments.Add(employment);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignManagerAsync(employment.Id, new AssignManagerRequest
            {
                ManagerEmploymentId = employment.Id,
                EffectiveFrom = DateTime.UtcNow
            }));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesEmployment()
    {
        await using var db = CreateDb();
        var employment = new Employment
        {
            Id = Guid.NewGuid(),
            EmploymentNumber = "EMP000002",
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            HospitalId = Guid.NewGuid(),
            PersonSearchName = "USER",
            RowVersion = new byte[8],
            CreatedAt = DateTime.UtcNow
        };
        db.Employments.Add(employment);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var deleted = await service.DeleteAsync(employment.Id, Guid.NewGuid());
        Assert.True(deleted);
        var stored = await db.Employments.FirstAsync(e => e.Id == employment.Id);
        Assert.True(stored.IsDeleted);
        Assert.DoesNotContain(db.OutboxMessages, o => o.Type == "EmploymentTerminated");
    }

    [Fact]
    public async Task CreateAsync_SucceedsWithMockClients()
    {
        await using var db = CreateDb();
        var personId = Guid.NewGuid();
        var service = BuildService(db, personId);
        idGen.Setup(x => x.GenerateAsync("EMP", It.IsAny<CancellationToken>())).ReturnsAsync("EMP000099");

        var created = await service.CreateAsync(new CreateEmploymentRequest
        {
            PersonId = personId,
            OrganizationId = Guid.NewGuid(),
            HospitalId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid()
        });

        Assert.Equal("EMP000099", created.EmploymentNumber);
        Assert.Equal("TEST", created.PersonSearchName);
        var createdOutbox = Assert.Single(db.OutboxMessages, o => o.Type == "EmploymentCreated");
        Assert.Equal($"{{\"employmentId\":\"{created.Id}\"}}", createdOutbox.Payload);
    }

    [Fact]
    public async Task UpdateAsync_PreservesEmploymentNumber()
    {
        await using var db = CreateDb();
        var employment = new Employment
        {
            Id = Guid.NewGuid(),
            EmploymentNumber = "EMP000003",
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            HospitalId = Guid.NewGuid(),
            PersonSearchName = "USER",
            RowVersion = new byte[8],
            CreatedAt = DateTime.UtcNow
        };
        db.Employments.Add(employment);
        await db.SaveChangesAsync();

        var service = BuildService(db, employment.PersonId);
        var updated = await service.UpdateAsync(employment.Id, new UpdateEmploymentRequest { NoticePeriodDays = 30 });
        Assert.NotNull(updated);
        Assert.Equal("EMP000003", updated!.EmploymentNumber);
    }

    [Fact]
    public async Task AssignPosition_OnlyOneActivePrimary()
    {
        await using var db = CreateDb();
        var orgId = Guid.NewGuid();
        var pos1 = new Position { Id = Guid.NewGuid(), OrganizationId = orgId, PositionCode = "P1", PositionName = "Role1", RowVersion = new byte[8], CreatedAt = DateTime.UtcNow };
        var pos2 = new Position { Id = Guid.NewGuid(), OrganizationId = orgId, PositionCode = "P2", PositionName = "Role2", RowVersion = new byte[8], CreatedAt = DateTime.UtcNow };
        var employment = new Employment
        {
            Id = Guid.NewGuid(),
            EmploymentNumber = "EMP000004",
            PersonId = Guid.NewGuid(),
            OrganizationId = orgId,
            HospitalId = Guid.NewGuid(),
            PersonSearchName = "USER",
            RowVersion = new byte[8],
            CreatedAt = DateTime.UtcNow
        };
        db.Positions.AddRange(pos1, pos2);
        db.Employments.Add(employment);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var effective = DateTime.UtcNow.AddDays(-1);
        await service.AssignPositionAsync(employment.Id, new AssignPositionRequest { PositionId = pos1.Id, EffectiveFrom = effective, IsPrimary = true });
        await service.AssignPositionAsync(employment.Id, new AssignPositionRequest { PositionId = pos2.Id, EffectiveFrom = DateTime.UtcNow, IsPrimary = true });

        var primaries = db.PositionAssignments.Where(p => p.EmploymentId == employment.Id && p.IsPrimary).ToList();
        Assert.Equal(2, primaries.Count);
        Assert.Single(primaries, p => p.IsActive());
    }

    [Fact]
    public async Task AssignManager_RejectsReportingCycle()
    {
        await using var db = CreateDb();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();
        db.Employments.AddRange(
            new Employment { Id = empA, EmploymentNumber = "EMP000005", PersonId = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), HospitalId = Guid.NewGuid(), PersonSearchName = "A", RowVersion = new byte[8], CreatedAt = DateTime.UtcNow },
            new Employment { Id = empB, EmploymentNumber = "EMP000006", PersonId = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), HospitalId = Guid.NewGuid(), PersonSearchName = "B", RowVersion = new byte[8], CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = BuildService(db);
        await service.AssignManagerAsync(empA, new AssignManagerRequest { ManagerEmploymentId = empB, EffectiveFrom = DateTime.UtcNow });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignManagerAsync(empB, new AssignManagerRequest { ManagerEmploymentId = empA, EffectiveFrom = DateTime.UtcNow }));
    }

    [Fact]
    public async Task TransferAsync_WritesHistoryAndTimeline()
    {
        await using var db = CreateDb();
        var employment = new Employment
        {
            Id = Guid.NewGuid(),
            EmploymentNumber = "EMP000007",
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            HospitalId = Guid.NewGuid(),
            PersonSearchName = "USER",
            RowVersion = new byte[8],
            CreatedAt = DateTime.UtcNow
        };
        db.Employments.Add(employment);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        await service.TransferAsync(employment.Id, new TransferEmploymentRequest
        {
            TransferKind = "Department",
            EffectiveDate = DateTime.UtcNow,
            Notes = "Moved to ICU"
        });

        Assert.Contains(db.EmploymentHistories, h => h.EmploymentId == employment.Id && h.EventType == "Transferred");
        Assert.Contains(db.WorkforceTimeline, t => t.EmploymentId == employment.Id && t.EventType == "Transferred");
        var transferOutbox = Assert.Single(db.OutboxMessages, o => o.Type == "EmploymentTransferred");
        Assert.Equal("{}", transferOutbox.Payload);
    }

    private static readonly Mock<IPlatformIdGenerator> idGen = new();

    private static EmploymentAppService BuildService(WorkforceDbContext db, Guid? personId = null)
    {
        idGen.Setup(x => x.GenerateAsync("EMP", It.IsAny<CancellationToken>())).ReturnsAsync("EMP000001");
        var audit = new WorkforceAuditService(db, Mock.Of<IAuditLogger>());
        var person = new Mock<IPersonServiceClient>();
        var org = new Mock<IOrganizationServiceClient>();
        org.Setup(x => x.ValidateHospitalAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var pid = personId ?? Guid.NewGuid();
        person.Setup(x => x.GetPersonAsync(pid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonRefDto { PersonId = pid, SearchName = "TEST" });
        person.Setup(x => x.GetPersonAsync(It.Is<Guid>(g => personId == null || g == personId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonRefDto { PersonId = pid, SearchName = "TEST" });
        var http = new Mock<IHttpContextAccessor>();
        return new EmploymentAppService(db, idGen.Object, audit, person.Object, org.Object, http.Object);
    }
}
