namespace ComplianceService.Application.Contracts;

/// <summary>Evidence Binder writes: delegates persistence to <see cref="IJourneyWorkspaceService"/>.</summary>
public interface IEvidenceBinderService
{
    /// <summary>Registers evidence from multipart upload; resolves active journey internally.</summary>
    Task<Guid> RegisterEvidenceFromUploadAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        Stream fileStream,
        string fileName,
        string? contentType,
        string evidenceType,
        string? displayName,
        string? title,
        string? metadata,
        CancellationToken cancellationToken = default);
}
