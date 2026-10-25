using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OrganizationService.Domain;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Development;

namespace OrganizationService.Infrastructure.MasterData;

public class OrganizationMasterDataBootstrapHostedService(IServiceProvider sp) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = sp.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OrganizationMasterDataBootstrapHostedService>>();

        await SeedLookupCategoriesAsync(db, logger, cancellationToken);

        if (NonProductionEnvironment.AllowDevelopmentSeeding(env))
        {
            var count = await OrganizationMasterDepartmentSeeder.SeedAsync(db, cancellationToken);
            logger.LogInformation("Master department seeding completed ({Count} records upserted).", count);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedLookupCategoriesAsync(
        OrganizationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var membershipType = await EnsureCategoryAsync(db, "MembershipType", "Membership Type", cancellationToken);
        var unitType = await EnsureCategoryAsync(db, "UnitType", "Organizational Unit Type", cancellationToken);
        var deptType = await EnsureCategoryAsync(db, "DepartmentType", "Department Type", cancellationToken);

        await EnsureValuesAsync(db, membershipType.Id, new (string Code, string Name, int Sort)[]
        {
            ("Employee", "Employee", 1),
            ("Consultant", "Consultant", 2),
            ("Vendor", "Vendor", 3),
            ("Auditor", "Auditor", 4),
        }, cancellationToken);

        var unitTypes = OrganizationalUnitHierarchyPolicy.AllUnitTypeCodes
            .Select((code, i) => (code, code, i + 1))
            .ToArray();
        await EnsureValuesAsync(db, unitType.Id, unitTypes, cancellationToken);

        await EnsureValuesAsync(db, deptType.Id, new (string Code, string Name, int Sort)[]
        {
            ("Clinical", "Clinical", 1),
            ("Administrative", "Administrative", 2),
            ("Quality", "Quality", 3),
        }, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Organization lookup categories/values upserted (UnitType count={Count}).",
            OrganizationalUnitHierarchyPolicy.AllUnitTypeCodes.Count);
    }

    private static async Task<OrganizationLookupCategory> EnsureCategoryAsync(
        OrganizationDbContext db, string code, string name, CancellationToken ct)
    {
        var existing = await db.LookupCategories.FirstOrDefaultAsync(c => c.Code == code, ct);
        if (existing is not null) return existing;

        var created = new OrganizationLookupCategory { Id = Guid.NewGuid(), Code = code, Name = name };
        db.LookupCategories.Add(created);
        return created;
    }

    private static async Task EnsureValuesAsync(
        OrganizationDbContext db,
        Guid categoryId,
        IReadOnlyList<(string Code, string Name, int Sort)> values,
        CancellationToken ct)
    {
        var existing = await db.LookupValues
            .Where(v => v.CategoryId == categoryId)
            .ToListAsync(ct);
        var byCode = existing.ToDictionary(v => v.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var (code, name, sort) in values)
        {
            if (byCode.TryGetValue(code, out var row))
            {
                row.Name = name;
                row.SortOrder = sort;
                continue;
            }

            db.LookupValues.Add(new OrganizationLookupValue
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                Code = code,
                Name = name,
                SortOrder = sort
            });
        }
    }
}
