namespace ComplianceService.Domain;

/// <summary>Seed catalog for 503 OE department labels (Sprint 9B Phase 1).</summary>
public static class DepartmentLabelCatalog
{
    public static readonly IReadOnlyList<(string Label, string Code, string DefaultRole)> Mappings =
    [
        ("Clinical / Nursing", "CLINICAL_NURSING", "Chief Medical Officer"),
        ("Front Office / Clinical", "FRONT_OFFICE_CLINICAL", "Admission In-charge"),
        ("Pharmacy", "PHARMACY", "Chief Pharmacist"),
        ("Facility / Engineering", "FACILITY_ENGINEERING", "Facility Manager"),
        ("Management", "MANAGEMENT", "Administrator"),
        ("Infection Control", "INFECTION_CONTROL", "Infection Control Officer"),
        ("Quality Management", "QUALITY_MANAGEMENT", "Quality Head"),
        ("Human Resources", "HUMAN_RESOURCES", "HR Manager"),
        ("Quality / Patient Relations", "QUALITY_PATIENT_RELATIONS", "Quality Manager"),
        ("IT / Medical Records", "IT_MEDICAL_RECORDS", "IT Manager"),
    ];
}
