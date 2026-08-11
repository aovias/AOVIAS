using Microsoft.EntityFrameworkCore;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Infrastructure.MasterData;

/// <summary>
/// Seeds platform master departments required for onboarding and user provisioning.
/// Idempotent — safe to run on every OrganizationService startup in non-production environments.
/// </summary>
public static class OrganizationMasterDepartmentSeeder
{
    private static readonly (Guid Id, string Name, string Code, string Category, int DisplayOrder)[] Departments =
    [
        (new Guid("a0000001-0001-4000-8000-000000000001"), "Medical", "MED", "Clinical", 1),
        (new Guid("a0000001-0001-4000-8000-000000000002"), "Nursing", "NUR", "Clinical", 2),
        (new Guid("a0000001-0001-4000-8000-000000000003"), "Radiology", "RAD", "Clinical", 3),
        (new Guid("a0000001-0001-4000-8000-000000000004"), "Laboratory", "LAB", "Clinical", 4),
        (new Guid("a0000001-0001-4000-8000-000000000005"), "Pharmacy", "PHR", "Clinical", 5),
        (new Guid("a0000001-0001-4000-8000-000000000006"), "Administration", "ADM", "Administrative", 6),
        (new Guid("a0000001-0001-4000-8000-000000000007"), "Human Resources", "HR", "Administrative", 7),
        (new Guid("a0000001-0001-4000-8000-000000000008"), "Finance", "FIN", "Administrative", 8),
        (new Guid("a0000001-0001-4000-8000-000000000009"), "Quality", "QLT", "Administrative", 9),
        (new Guid("a0000001-0001-4000-8000-000000000010"), "Operations", "OPS", "Administrative", 10),
    ];

    public static async Task<int> SeedAsync(OrganizationDbContext db, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var changed = 0;

        foreach (var (id, name, code, category, displayOrder) in Departments)
        {
            var existing = await db.MasterDepartments
                .FirstOrDefaultAsync(d => d.Id == id || d.Code == code, cancellationToken);

            if (existing is null)
            {
                db.MasterDepartments.Add(new MasterDepartment
                {
                    Id = id,
                    Name = name,
                    Code = code,
                    Category = category,
                    Description = "QA / development master department",
                    DisplayOrder = displayOrder,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                changed++;
                continue;
            }

            existing.Name = name;
            existing.Code = code;
            existing.Category = category;
            existing.DisplayOrder = displayOrder;
            existing.IsActive = true;
            existing.UpdatedAt = now;
            if (string.IsNullOrWhiteSpace(existing.Description))
                existing.Description = "QA / development master department";
            changed++;
        }

        if (changed > 0)
            await db.SaveChangesAsync(cancellationToken);

        return changed;
    }
}
