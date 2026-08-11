using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record GetEvidenceSummaryQuery(string ApiVersion, Guid FrameworkId) : IRequest<IActionResult>;

public sealed record GetEvidenceListQuery(
    string ApiVersion,
    Guid FrameworkId,
    Guid? ObjectiveElementId,
    Guid? ChapterId,
    Guid? StandardId,
    string? Status,
    Guid? DepartmentId,
    string? FileType,
    string? Search,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortOrder) : IRequest<IActionResult>;

public sealed record GetEvidenceAnalyticsQuery(string ApiVersion, Guid FrameworkId) : IRequest<IActionResult>;

public sealed record GetEvidenceRecentActivityQuery(string ApiVersion, Guid FrameworkId, int Take) : IRequest<IActionResult>;

public sealed record GetEvidenceDashboardQuery(string ApiVersion, Guid FrameworkId) : IRequest<IActionResult>;

public sealed record GetEvidenceUploadContextQuery(string ApiVersion, Guid FrameworkId) : IRequest<IActionResult>;

public sealed record GetLibraryEvidenceDetailQuery(string ApiVersion, Guid FrameworkId, Guid EvidenceId) : IRequest<IActionResult>;

public sealed record GetLibraryEvidenceDownloadQuery(string ApiVersion, Guid FrameworkId, Guid EvidenceId) : IRequest<IActionResult>;

public sealed record GetEvidenceReviewQueueQuery(
    string ApiVersion,
    Guid FrameworkId,
    string? Status,
    string? Stage,
    Guid? DepartmentId,
    Guid? OwnerId,
    string? EvidenceType,
    string? Search,
    int Page,
    int PageSize) : IRequest<IActionResult>;

public sealed record GetEvidenceAuditTrailQuery(
    string ApiVersion,
    Guid FrameworkId,
    Guid EvidenceId,
    Guid? ObjectiveElementId) : IRequest<IActionResult>;
