using ComplianceService.Application.Contracts;
using ComplianceService.Application.Documentation;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

/// <summary>
/// Seeds built-in QDMS document templates (Sprint 15 Phase 9).
/// </summary>
public static class DocumentationTemplateSeeder
{
    private static readonly (string Name, string Category, ControlledDocumentType Type, string Outline)[] BuiltIn =
    [
        ("Hand Hygiene SOP", "Infection Control", ControlledDocumentType.Sop,
            "# Hand Hygiene SOP\n\n## Purpose\nDefine hand hygiene practices.\n\n## Scope\nAll clinical areas.\n\n## Procedure\n1. Wash hands with soap and water.\n2. Use alcohol-based hand rub when indicated.\n\n## Responsibilities\n- Staff: comply\n- IPC team: audit"),
        ("Medication Management Policy", "Clinical", ControlledDocumentType.Policy,
            "# Medication Management Policy\n\n## Purpose\nSafe medication practices.\n\n## Scope\nAll medication-related activities.\n\n## Key Requirements\n- Prescription verification\n- High-alert medication protocols\n- Adverse drug reaction reporting"),
        ("Fire Safety Manual", "Safety", ControlledDocumentType.Manual,
            "# Fire Safety Manual\n\n## Evacuation routes\n## Assembly points\n## Fire drill schedule\n## Equipment inspection"),
        ("Biomedical Waste (BMW) SOP", "Environment", ControlledDocumentType.Sop,
            "# BMW Management SOP\n\n## Segregation\n## Storage\n## Transportation\n## Documentation"),
        ("Admission SOP", "Patient Care", ControlledDocumentType.Sop,
            "# Admission SOP\n\n## Initial assessment\n## Consent\n## Documentation\n## Bed allocation"),
        ("Discharge SOP", "Patient Care", ControlledDocumentType.Sop,
            "# Discharge SOP\n\n## Discharge planning\n## Medication reconciliation\n## Follow-up instructions"),
        ("Infection Control Policy", "Infection Control", ControlledDocumentType.Policy,
            "# Infection Control Policy\n\n## Surveillance\n## Outbreak management\n## Isolation precautions"),
        ("Patient Rights Charter", "Governance", ControlledDocumentType.Policy,
            "# Patient Rights\n\n## Dignity\n## Information\n## Consent\n## Complaints"),
        ("Emergency Preparedness Plan", "Safety", ControlledDocumentType.Protocol,
            "# Emergency Preparedness\n\n## Code protocols\n## Disaster management\n## Business continuity"),
    ];

    public static async Task SeedAsync(ComplianceDbContext db, CancellationToken ct = default)
    {
        if (await db.DocumentTemplates.AnyAsync(t => t.IsBuiltIn, ct))
            return;

        foreach (var (name, category, type, outline) in BuiltIn)
        {
            db.DocumentTemplates.Add(new DocumentTemplate
            {
                Id = Guid.NewGuid(),
                Name = name,
                Category = category,
                DocumentType = type,
                ContentTemplate = outline,
                AuthoringSchemaKey = AuthoringSchemaRegistry.ResolveSchemaKey(type),
                Description = $"Built-in {category} template",
                IsBuiltIn = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
