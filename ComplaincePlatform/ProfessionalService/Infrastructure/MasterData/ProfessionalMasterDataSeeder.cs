using ProfessionalService.Domain.Entities;
using ProfessionalService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ProfessionalService.Infrastructure.MasterData;

public static class ProfessionalMasterDataSeeder
{
    public static async Task SeedAsync(ProfessionalDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.ProfessionalLookupCategories.AnyAsync(cancellationToken))
            return;

        var categories = new (string Code, string Name, (string Code, string Name)[] Values)[]
        {
            ("ProfessionalCategory", "Professional Category", new[] { ("DOCTOR", "Doctor"), ("NURSE", "Nurse"), ("TECHNICIAN", "Technician"), ("PHARMACIST", "Pharmacist"), ("PHYSIOTHERAPIST", "Physiotherapist"), ("DIETICIAN", "Dietician"), ("RADIOGRAPHER", "Radiographer"), ("BME", "Biomedical Engineer"), ("PSYCHOLOGIST", "Psychologist"), ("COUNSELLOR", "Counsellor"), ("QUALITY", "Quality Professional"), ("ADMIN", "Administrator"), ("CONSULTANT", "Consultant"), ("VENDOR", "Vendor"), ("AUDITOR", "Auditor") }),
            ("QualificationType", "Qualification Type", new[] { ("DEGREE", "Degree"), ("DIPLOMA", "Diploma"), ("CERTIFICATE", "Certificate"), ("FELLOWSHIP", "Fellowship") }),
            ("RegistrationAuthority", "Registration Authority", new[] { ("MCI", "Medical Council"), ("NMC", "Nursing Council"), ("PCI", "Pharmacy Council"), ("PARAMEDICAL", "Paramedical Council"), ("LAB", "Laboratory Council") }),
            ("LicenseType", "License Type", new[] { ("MEDICAL", "Medical Practice"), ("NURSING", "Nursing Practice"), ("PHARMACY", "Pharmacy Practice"), ("RADIOLOGY", "Radiology Practice") }),
            ("CertificationType", "Certification Type", new[] { ("BLS", "BLS"), ("ACLS", "ACLS"), ("NABH", "NABH Assessor"), ("JCI", "JCI Surveyor"), ("ISO", "ISO Lead Auditor") }),
            ("CompetencyLevel", "Competency Level", new[] { ("NOVICE", "Novice"), ("COMPETENT", "Competent"), ("PROFICIENT", "Proficient"), ("EXPERT", "Expert") }),
            ("SkillCategory", "Skill Category", new[] { ("CLINICAL", "Clinical"), ("QUALITY", "Quality"), ("LEADERSHIP", "Leadership"), ("SAFETY", "Patient Safety") }),
            ("Specialty", "Specialty", new[] { ("EMERGENCY", "Emergency Medicine"), ("ICU", "ICU"), ("CARDIOLOGY", "Cardiology"), ("ONCOLOGY", "Oncology"), ("INFECTION", "Infection Control") }),
            ("CredentialStatus", "Credential Status", new[] { ("ACTIVE", "Active"), ("PENDING", "Pending"), ("EXPIRED", "Expired"), ("SUSPENDED", "Suspended") }),
            ("PrivilegeType", "Privilege Type", new[] { ("ICU", "Operate ICU"), ("SURGERY", "Perform Surgery"), ("PRESCRIBE", "Prescribe Medication"), ("RADIOLOGY", "Radiology Reporting"), ("BLOODBANK", "Blood Bank Approval") }),
            ("TrainingType", "Training Type", new[] { ("MANDATORY", "Mandatory"), ("OPTIONAL", "Optional"), ("INDUCTION", "Induction"), ("REFRESHER", "Refresher") }),
            ("MembershipOrganization", "Membership Organization", new[] { ("IMA", "IMA"), ("NNA", "NNA"), ("ISQUA", "ISQua"), ("OTHER", "Other Society") })
        };

        foreach (var (code, name, values) in categories)
        {
            var catId = Guid.NewGuid();
            db.ProfessionalLookupCategories.Add(new ProfessionalLookupCategory { Id = catId, Code = code, Name = name, IsSystem = true });
            var sort = 0;
            foreach (var (vCode, vName) in values)
                db.ProfessionalLookupValues.Add(new ProfessionalLookupValue { Id = Guid.NewGuid(), CategoryId = catId, Code = vCode, Name = vName, SortOrder = sort++, IsActive = true });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
