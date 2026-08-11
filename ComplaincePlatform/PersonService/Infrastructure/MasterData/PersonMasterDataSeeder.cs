using PersonService.Domain.Entities;
using PersonService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace PersonService.Infrastructure.MasterData;

public static class PersonMasterDataSeeder
{
    public static async Task SeedAsync(PersonDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.PersonLookupCategories.AnyAsync(cancellationToken))
            return;

        var categories = new (string Code, string Name, (string Code, string Name)[] Values)[]
        {
            ("Prefix", "Name Prefix", new[] { ("MR", "Mr."), ("MRS", "Mrs."), ("DR", "Dr."), ("PROF", "Prof.") }),
            ("Suffix", "Name Suffix", new[] { ("JR", "Jr."), ("SR", "Sr."), ("MD", "MD"), ("PHD", "PhD") }),
            ("Gender", "Gender", new[] { ("MALE", "Male"), ("FEMALE", "Female"), ("OTHER", "Other"), ("UNSPECIFIED", "Prefer not to say") }),
            ("Nationality", "Nationality", new[] { ("IN", "Indian"), ("US", "American"), ("GB", "British") }),
            ("Language", "Language", new[] { ("EN", "English"), ("HI", "Hindi"), ("TA", "Tamil") }),
            ("ContactType", "Contact Type", new[] { ("WORK", "Work"), ("PERSONAL", "Personal"), ("MOBILE", "Mobile") }),
            ("AddressType", "Address Type", new[] { ("RESIDENTIAL", "Residential"), ("OFFICE", "Office"), ("TEMPORARY", "Temporary"), ("BILLING", "Billing"), ("EMERGENCY", "Emergency") }),
            ("IdentityDocumentType", "Identity Document Type", new[] { ("PASSPORT", "Passport"), ("NATIONAL_ID", "National ID"), ("DRIVING_LICENSE", "Driving License"), ("EMPLOYEE_CARD", "Employee Card"), ("MEDICAL_REG", "Medical Registration"), ("NURSING_REG", "Nursing Registration") }),
            ("MaritalStatus", "Marital Status", new[] { ("SINGLE", "Single"), ("MARRIED", "Married"), ("DIVORCED", "Divorced"), ("WIDOWED", "Widowed") }),
            ("BloodGroup", "Blood Group", new[] { ("A_POS", "A+"), ("A_NEG", "A-"), ("B_POS", "B+"), ("B_NEG", "B-"), ("O_POS", "O+"), ("O_NEG", "O-"), ("AB_POS", "AB+"), ("AB_NEG", "AB-") }),
            ("PreferredContactMethod", "Preferred Contact Method", new[] { ("EMAIL", "Email"), ("PHONE", "Phone"), ("SMS", "SMS") }),
            ("EmergencyRelationship", "Emergency Relationship", new[] { ("SPOUSE", "Spouse"), ("PARENT", "Parent"), ("SIBLING", "Sibling"), ("FRIEND", "Friend") })
        };

        var order = 0;
        foreach (var (code, name, values) in categories)
        {
            var catId = Guid.NewGuid();
            db.PersonLookupCategories.Add(new PersonLookupCategory { Id = catId, Code = code, Name = name, IsSystem = true });
            var sort = 0;
            foreach (var (vCode, vName) in values)
            {
                db.PersonLookupValues.Add(new PersonLookupValue
                {
                    Id = Guid.NewGuid(),
                    CategoryId = catId,
                    Code = vCode,
                    Name = vName,
                    SortOrder = sort++,
                    IsActive = true
                });
            }
            order++;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
