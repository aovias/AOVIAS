namespace ComplianceService.Domain.Enums;

/// <summary>Sprint 17 — Clinical indicator taxonomy (extensible catalog).</summary>
public enum IndicatorCategory
{
    MedicationSafety = 1,
    PatientFalls = 2,
    HospitalAcquiredInfection = 3,
    SurgicalSiteInfection = 4,
    PressureInjury = 5,
    MedicationErrors = 6,
    BloodTransfusionReactions = 7,
    Readmissions = 8,
    Mortality = 9,
    NeedleStickInjuries = 10,
    BiomedicalWaste = 11,
    EquipmentDowntime = 12,
    EmergencyResponse = 13,
    PatientSatisfaction = 14,
    ComplaintResolution = 15,
    LaboratoryErrors = 16,
    RadiologyErrors = 17,
    HandHygieneCompliance = 18,
    Other = 99,
}
