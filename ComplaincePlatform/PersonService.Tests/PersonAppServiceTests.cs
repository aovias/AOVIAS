using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using PersonService.Application.Services;
using PersonService.Domain.Aggregates;
using PersonService.Domain.Entities;
using PersonService.Domain.Enums;
using PersonService.Infrastructure.Persistence;
using Shared.Infrastructure.Services;
using Shared.Logging.Audit;
using Xunit;

namespace PersonService.Tests;

public class PersonAppServiceTests
{
    private static PersonDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PersonDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PersonDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateEmail()
    {
        await using var db = CreateDb();
        var existing = new Person
        {
            Id = Guid.NewGuid(),
            PlatformId = "PRS000001",
            Status = PersonStatus.Active,
            SearchName = "EXISTING",
            RowVersion = new byte[8],
            CreatedAt = DateTime.UtcNow
        };
        existing.Contacts.Add(new PersonContact
        {
            Id = Guid.NewGuid(),
            PersonId = existing.Id,
            ContactKind = "Email",
            Value = "dup@example.com",
            NormalizedValue = "dup@example.com",
            IsPrimary = true,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        });
        db.Persons.Add(existing);
        await db.SaveChangesAsync();

        var idGen = new Mock<IPlatformIdGenerator>();
        idGen.Setup(x => x.GenerateAsync("PRS", It.IsAny<CancellationToken>())).ReturnsAsync("PRS000002");
        var audit = new Mock<PersonService.Application.Contracts.IPersonAuditService>();
        var http = new Mock<IHttpContextAccessor>();
        var env = new Mock<IWebHostEnvironment>();

        var service = new PersonAppService(db, idGen.Object, audit.Object, http.Object, env.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new PersonService.Application.DTOs.CreatePersonRequest { Email = "dup@example.com" }));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesPerson()
    {
        await using var db = CreateDb();
        var person = new Person
        {
            Id = Guid.NewGuid(),
            PlatformId = "PRS000003",
            Status = PersonStatus.Active,
            SearchName = "TEST",
            RowVersion = new byte[8],
            CreatedAt = DateTime.UtcNow
        };
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var idGen = new Mock<IPlatformIdGenerator>();
        var audit = new PersonAuditService(db, Mock.Of<IAuditLogger>());
        var http = new Mock<IHttpContextAccessor>();
        var env = new Mock<IWebHostEnvironment>();
        var service = new PersonAppService(db, idGen.Object, audit, http.Object, env.Object);

        var deleted = await service.DeleteAsync(person.Id, Guid.NewGuid());
        Assert.True(deleted);
        var stored = await db.Persons.FirstAsync(p => p.Id == person.Id);
        Assert.True(stored.IsDeleted);
        Assert.DoesNotContain(db.OutboxMessages, o => o.Type == "PersonDeleted" || o.Type == "PersonUpdated");
    }

    [Fact]
    public async Task UpdateAsync_DoesNotWritePersonUpdatedOutbox()
    {
        await using var db = CreateDb();
        var person = new Person
        {
            Id = Guid.NewGuid(),
            PlatformId = "PRS000004",
            Status = PersonStatus.Active,
            SearchName = "TEST",
            LegalFirstName = "Ada",
            RowVersion = new byte[8],
            CreatedAt = DateTime.UtcNow
        };
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var idGen = new Mock<IPlatformIdGenerator>();
        var audit = new PersonAuditService(db, Mock.Of<IAuditLogger>());
        var http = new Mock<IHttpContextAccessor>();
        var env = new Mock<IWebHostEnvironment>();
        var service = new PersonAppService(db, idGen.Object, audit, http.Object, env.Object);

        await service.UpdateAsync(person.Id, new PersonService.Application.DTOs.UpdatePersonRequest
        {
            LegalFirstName = "Grace"
        });

        Assert.DoesNotContain(db.OutboxMessages, o => o.Type == "PersonUpdated");
    }
}
