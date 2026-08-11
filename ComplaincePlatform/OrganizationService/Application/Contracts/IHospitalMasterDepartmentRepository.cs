namespace OrganizationService.Application.Contracts;

/// <summary>
/// Persistence for hospital ↔ master department selections (onboarding wizard).
/// </summary>
public interface IHospitalMasterDepartmentRepository
{
    Task ReplaceHospitalSelectionsAsync(Guid hospitalId, IReadOnlyList<Guid> masterDepartmentIds, CancellationToken cancellationToken = default);
}
