using ComplianceService.Application.DTOs;

using ComplianceService.Domain;



namespace ComplianceService.Application.Contracts;



/// <summary>

/// Sprint 7B Phase 1 — promotion eligibility rules (discovery domain only).

/// </summary>

public interface IFindingPromotionEligibilityService

{

    Task<FindingPromotionEligibilityDto> EvaluateAsync(

        Finding finding,

        CancellationToken ct = default);

}


