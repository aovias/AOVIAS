using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComplianceService.Tests;

public sealed class ObligationCanonicalIdentityTests
{
    private readonly Guid _org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _hospitalA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _hospitalB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _framework = Guid.Parse("0fe8a9c0-9ea0-4970-9097-d4f73d45dca7");
    private readonly Guid _oe = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static ComplianceDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ComplianceDbContext(options, userContext: null);
    }

    private OrganizationComplianceObligation Obligation(Guid hospitalId, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        TenantId = _org,
        OrganizationId = _org,
        HospitalId = hospitalId,
        FrameworkId = _framework,
        ChapterId = Guid.NewGuid(),
        StandardId = Guid.NewGuid(),
        ObjectiveElementId = _oe,
        Code = "OE-1",
        Title = "Element",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        RowVersion = new byte[] { 1 }
    };

    [Fact]
    public async Task UpsertBatch_skips_existing_hospital_framework_oe()
    {
        await using var db = CreateDb();
        var repo = new ObligationRepository(db, NullLogger<ObligationRepository>.Instance);
        var first = Obligation(_hospitalA);
        await repo.UpsertBatchAsync(new[] { first });
        await repo.UpsertBatchAsync(new[] { Obligation(_hospitalA) });

        Assert.Equal(1, await db.OrganizationComplianceObligations.CountAsync());
        Assert.Equal(first.Id, (await db.OrganizationComplianceObligations.SingleAsync()).Id);
    }

    [Fact]
    public async Task UpsertBatch_allows_same_framework_oe_for_two_hospitals()
    {
        await using var db = CreateDb();
        var repo = new ObligationRepository(db, NullLogger<ObligationRepository>.Instance);
        await repo.UpsertBatchAsync(new[] { Obligation(_hospitalA), Obligation(_hospitalB) });

        Assert.Equal(2, await db.OrganizationComplianceObligations.CountAsync());
    }

    [Fact]
    public async Task GetByKey_uses_hospital_framework_oe()
    {
        await using var db = CreateDb();
        var repo = new ObligationRepository(db, NullLogger<ObligationRepository>.Instance);
        var a = Obligation(_hospitalA);
        var b = Obligation(_hospitalB);
        db.OrganizationComplianceObligations.AddRange(a, b);
        await db.SaveChangesAsync();

        var found = await repo.GetByKeyAsync(_hospitalA, _framework, _oe);
        Assert.NotNull(found);
        Assert.Equal(a.Id, found!.Id);
    }

    [Fact]
    public async Task Sync_is_idempotent_per_hospital_and_does_not_block_second_hospital()
    {
        await using var db = CreateDb();
        await SeedCatalogAsync(db);
        var repo = new ObligationRepository(db, NullLogger<ObligationRepository>.Instance);
        var sut = new ObligationSynchronizationService(db, repo, NullLogger<ObligationSynchronizationService>.Instance);

        await sut.SyncForOrganizationAsync(_org, _hospitalA, _org, _framework);
        await sut.SyncForOrganizationAsync(_org, _hospitalA, _org, _framework);
        Assert.Equal(1, await db.OrganizationComplianceObligations.CountAsync(o => o.HospitalId == _hospitalA));

        await sut.SyncForOrganizationAsync(_org, _hospitalB, _org, _framework);
        Assert.Equal(1, await db.OrganizationComplianceObligations.CountAsync(o => o.HospitalId == _hospitalB));
        Assert.Equal(2, await db.OrganizationComplianceObligations.CountAsync());
    }

    private async Task SeedCatalogAsync(ComplianceDbContext db)
    {
        var chapterId = Guid.NewGuid();
        var standardId = Guid.NewGuid();
        db.ComplianceFrameworks.Add(new ComplianceFramework
        {
            Id = _framework,
            Name = "NABH",
            Authority = "QCI",
            Version = "1",
            IsActive = true,
            PlatformId = "NABH-01",
            RowVersion = new byte[] { 1 }
        });
        db.ComplianceChapters.Add(new ComplianceChapter
        {
            Id = chapterId,
            FrameworkId = _framework,
            Code = "CH",
            Name = "Chapter",
            Sequence = 1,
            PlatformId = "CH-01",
            RowVersion = new byte[] { 1 }
        });
        db.ComplianceStandards.Add(new ComplianceStandard
        {
            Id = standardId,
            ChapterId = chapterId,
            Code = "ST",
            Name = "Standard",
            Sequence = 1,
            PlatformId = "ST-01",
            RowVersion = new byte[] { 1 }
        });
        db.ObjectiveElements.Add(new ObjectiveElement
        {
            Id = _oe,
            ComplianceStandardId = standardId,
            Code = "OE-1",
            Title = "Element",
            Description = "d",
            Classification = ObjectiveElementClassification.Core,
            PlatformId = "OE-01",
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync();
    }
}
