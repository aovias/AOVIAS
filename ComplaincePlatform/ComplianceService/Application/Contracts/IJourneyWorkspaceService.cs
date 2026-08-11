using ComplianceService.Application.DTOs;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Contracts;

public interface IJourneyWorkspaceService
{
    Task<JourneyWorkspaceSnapshotDto> GetWorkspaceAsync(
        Guid journeyId,
        Guid tenantId,
        OperationalMode? operationalMode,
        CancellationToken cancellationToken = default);

    Task<JourneySessionDto> GetSessionAsync(
        Guid journeyId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken = default);

    Task<JourneyDashboardDto> GetDashboardAsync(
        Guid journeyId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken = default);

    Task<JourneyChaptersIndexDto> GetChaptersIndexAsync(
        Guid journeyId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        string? search,
        string? sort,
        string? classificationFilter = null,
        CancellationToken cancellationToken = default);

    Task<ObjectiveElementDto> GetObjectiveElementDetailAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComplianceStandardDto>> GetChapterStandardsAsync(
        Guid journeyId,
        Guid chapterId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        int page,
        int pageSize,
        string? classificationFilter = null,
        CancellationToken cancellationToken = default);

    Task<PatchObjectiveElementResponseDto> PatchObjectiveElementAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        IReadOnlyList<string> roles,
        PatchObjectiveElementRequest request,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken = default);

    Task<JourneySessionDto> PatchOperationalModeAsync(
        Guid journeyId,
        Guid tenantId,
        int operationalMode,
        CancellationToken cancellationToken = default);

    Task DeleteObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OeEvidenceItemDto>> GetObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Guid> RegisterObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        RegisterOeEvidenceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers evidence from an uploaded file (multipart). Persists bytes under ContentRoot and stores a local reference in FileUrl.
    /// </summary>
    Task<Guid> RegisterObjectiveElementEvidenceFromUploadAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        Stream fileStream,
        string fileName,
        string? contentType,
        string evidenceType,
        string? displayName = null,
        string? title = null,
        string? metadata = null,
        CancellationToken cancellationToken = default,
        string? metadataJsonOverride = null,
        bool suppressSnapshotRecalc = false);

    Task<IReadOnlyList<OeHistoryEventDto>> GetObjectiveElementHistoryAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<EvidenceDetailDto?> GetObjectiveElementEvidenceDetailAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Opens the current file for download; caller must dispose the stream.</summary>
    Task<(Stream Stream, string ContentType, string FileDownloadName)> DownloadObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task VerifyObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        Guid tenantId,
        Guid userId,
        bool accepted,
        string? remarks,
        CancellationToken cancellationToken = default);

    Task<Guid> UploadObjectiveElementEvidenceVersionAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        Guid tenantId,
        Guid userId,
        Stream fileStream,
        string fileName,
        string? contentType,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<BulkEvidenceUploadResponseDto> BulkUploadObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        IReadOnlyList<BulkUploadIncomingFile> files,
        string evidenceType,
        string? sharedMetadata,
        CancellationToken cancellationToken = default);
}
