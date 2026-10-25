namespace ComplianceService.Application.Commands;

/// <summary>
/// Rebuilds OrganizationComplianceObligation state from saved wizard assessment answers.
///
/// Idempotent — safe to run multiple times. Existing obligation state is overwritten only
/// when a matching answer exists; obligations without answers are not touched.
///
/// Scope filters (all default to null = process all):
///   OrganizationId — restrict to one organization
///   HospitalId     — restrict to one hospital
///   FrameworkId    — restrict to one framework (applies to each hospital)
///
/// DryRun = true — resolves IDs and simulates the mapping without writing to the database.
/// </summary>
public sealed record RebuildObligationAssessmentCommand(
    Guid?  OrganizationId = null,
    Guid?  HospitalId     = null,
    Guid?  FrameworkId    = null,
    bool   DryRun         = false,
    string Source         = "AdminRebuild");
