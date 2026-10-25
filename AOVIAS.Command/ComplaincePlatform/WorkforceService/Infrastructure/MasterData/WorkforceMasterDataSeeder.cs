using WorkforceService.Infrastructure.Persistence;
using WorkforceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace WorkforceService.Infrastructure.MasterData;

public static class WorkforceMasterDataSeeder
{
    public static async Task SeedAsync(WorkforceDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.WorkforceLookupCategories.AnyAsync(cancellationToken))
            return;

        var categories = new (string Code, string Name, (string Code, string Name)[] Values)[]
        {
            ("EmploymentType", "Employment Type", new[] { ("PERMANENT", "Permanent"), ("CONTRACT", "Contract"), ("CONSULTANT", "Consultant"), ("RESIDENT", "Resident"), ("VISITING", "Visiting Consultant"), ("INTERN", "Intern"), ("VOLUNTEER", "Volunteer"), ("OUTSOURCED", "Outsourced"), ("TEMPORARY", "Temporary") }),
            ("EmploymentStatus", "Employment Status", new[] { ("DRAFT", "Draft"), ("INVITED", "Invited"), ("JOINED", "Joined"), ("PROBATION", "Probation"), ("CONFIRMED", "Confirmed"), ("ACTIVE", "Active"), ("ON_LEAVE", "On Leave"), ("SUSPENDED", "Suspended"), ("RESIGNED", "Resigned"), ("TERMINATED", "Terminated"), ("RETIRED", "Retired"), ("ARCHIVED", "Archived") }),
            ("PositionLevel", "Position Level", new[] { ("ENTRY", "Entry"), ("MID", "Mid"), ("SENIOR", "Senior"), ("EXECUTIVE", "Executive") }),
            ("Grade", "Grade", new[] { ("G1", "Grade 1"), ("G2", "Grade 2"), ("G3", "Grade 3") }),
            ("Band", "Band", new[] { ("B1", "Band 1"), ("B2", "Band 2"), ("B3", "Band 3") }),
            ("ContractType", "Contract Type", new[] { ("FIXED", "Fixed Term"), ("INDEFINITE", "Indefinite"), ("PROBATION", "Probation") }),
            ("TransferReason", "Transfer Reason", new[] { ("REORG", "Reorganization"), ("REQUEST", "Employee Request"), ("PROMOTION_PATH", "Career Path") }),
            ("PromotionReason", "Promotion Reason", new[] { ("MERIT", "Merit"), ("VACANCY", "Vacancy"), ("RESTRUCTURE", "Restructure") }),
            ("DepartmentType", "Department Type", new[] { ("CLINICAL", "Clinical"), ("ADMIN", "Administrative"), ("SUPPORT", "Support") }),
            ("ReportingRelationshipType", "Reporting Relationship Type", new[] { ("FUNCTIONAL", "Functional Manager"), ("ADMIN", "Administrative Manager"), ("CLINICAL", "Clinical Supervisor"), ("QUALITY", "Quality Reporting"), ("ACTING", "Acting Manager") })
        };

        foreach (var (code, name, values) in categories)
        {
            var catId = Guid.NewGuid();
            db.WorkforceLookupCategories.Add(new WorkforceLookupCategory { Id = catId, Code = code, Name = name, IsSystem = true });
            var sort = 0;
            foreach (var (vCode, vName) in values)
                db.WorkforceLookupValues.Add(new WorkforceLookupValue { Id = Guid.NewGuid(), CategoryId = catId, Code = vCode, Name = vName, SortOrder = sort++, IsActive = true });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedTemplatePositionsAsync(WorkforceDbContext db, Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (await db.Positions.AnyAsync(p => p.OrganizationId == organizationId, cancellationToken))
            return;

        var templates = new[] { ("MS", "Medical Superintendent", true, false, true), ("QH", "Quality Head", false, true, true), ("CONS", "Consultant Cardiologist", true, false, false), ("NURSE", "Nurse", true, false, false), ("TECH", "Technician", true, false, false), ("RECEP", "Receptionist", false, true, false), ("BME", "Biomedical Engineer", false, true, false) };
        foreach (var (code, name, clinical, admin, leadership) in templates)
        {
            db.Positions.Add(new Domain.Aggregates.Position
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                PositionCode = code,
                PositionName = name,
                IsClinical = clinical,
                IsAdministrative = admin,
                IsLeadership = leadership,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
