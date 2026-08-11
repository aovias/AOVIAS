namespace ComplianceService.Domain.Enums;

/// <summary>Sprint 16 — Incident category taxonomy (extensible).</summary>
public enum IncidentCategory
{
    PatientFall = 1,
    MedicationError = 2,
    HospitalAcquiredInfection = 3,
    NeedleStickInjury = 4,
    EquipmentFailure = 5,
    FireSafety = 6,
    BiomedicalWaste = 7,
    Security = 8,
    Violence = 9,
    PatientIdentification = 10,
    Consent = 11,
    BloodTransfusion = 12,
    Laboratory = 13,
    Radiology = 14,
    ClinicalCare = 15,
    NearMiss = 16,
    SentinelEvent = 17,
    Other = 99,
}
