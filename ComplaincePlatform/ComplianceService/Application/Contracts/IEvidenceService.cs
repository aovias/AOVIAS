using ComplianceService.Application.DTOs;
using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts
{
	public interface IEvidenceService
	{
		Task<Evidence> CreateEvidenceAsync(Evidence evidence, CancellationToken cancellationToken = default);

		/// <summary>Full detail for evidence linked to the objective element, including version history.</summary>
		Task<EvidenceDetailDto?> GetEvidenceDetailAsync(Guid objectiveElementId, Guid evidenceId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Appends a new file version: new <see cref="EvidenceVersion"/> row and updates <see cref="Evidence.FileUrl"/> / <see cref="Evidence.CurrentVersion"/>.
		/// </summary>
		Task<Evidence> AppendEvidenceVersionAsync(Guid evidenceId, string newFileUrl, Guid userId, string? notes, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<Evidence>> GetEvidenceForObjectiveElementAsync(
			Guid objectiveElementId,
			Evidence.EvidenceArtifactType? artifactType = null,
			string? department = null,
			Evidence.EvidenceLifecycleStatus? status = null,
			CancellationToken cancellationToken = default);

		Task SetVerificationAsync(Guid objectiveElementId, Guid evidenceId, Guid auditorId, bool accepted, string? remarks, CancellationToken cancellationToken = default);
		Task<EvidenceApprovalResultDto> ApplyApprovalAsync(
			Guid evidenceId,
			Guid auditorId,
			EvidenceApprovalAction action,
			string? reviewComment,
			string? expectedRowVersionBase64 = null,
			Guid? objectiveElementId = null,
			CancellationToken cancellationToken = default);

		/// <summary>Removes the OE–evidence link and deletes the evidence row when no other links exist.</summary>
		Task RemoveEvidenceFromObjectiveElementAsync(Guid objectiveElementId, Guid evidenceId, CancellationToken cancellationToken = default);
	}
}

