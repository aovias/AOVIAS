namespace ComplianceService.Application.RoleExperience;

public interface IRoleExperienceService
{
    Task<RoleExperienceHomeDto> GetHomeAsync(
        IReadOnlyList<string> roles,
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort pack engagement counters from existing analytics (Phase 3 Sprint 6).
    /// </summary>
    Task<RoleEngagementDto> GetEngagementAsync(
        IReadOnlyList<string> roles,
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId = null,
        CancellationToken cancellationToken = default);
}
