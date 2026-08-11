using System.IO;
using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IEvidenceLibraryService
{
    Task<EvidenceSummaryDto> GetSummaryAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<EvidenceListItemDto>> GetListAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid? objectiveElementId,
        Guid? chapterId,
        Guid? standardId,
        string? status,
        Guid? departmentId,
        string? fileType,
        string? search,
        int page,
        int pageSize,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default);

    Task<EvidenceAnalyticsDto> GetAnalyticsAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceActivityItemDto>> GetRecentActivityAsync(
        Guid hospitalId,
        Guid frameworkId,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceTimelineItemDto>> GetRecentTimelineAsync(
        Guid hospitalId,
        Guid frameworkId,
        int take,
        CancellationToken cancellationToken = default);

    Task<EvidenceDashboardDto> GetDashboardAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default);

    Task<EvidenceUploadContextDto> GetUploadContextAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evidence detail for the library: validates hospital + resolved framework and an OE link in that framework, then delegates to evidence detail.
    /// </summary>
    Task<EvidenceDetailDto?> GetLibraryEvidenceDetailAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid evidenceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the current evidence file for download; same scope rules as <see cref="GetLibraryEvidenceDetailAsync"/>.
    /// </summary>
    Task<(Stream Stream, string ContentType, string FileDownloadName)?> DownloadLibraryEvidenceAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid evidenceId,
        CancellationToken cancellationToken = default);
}
