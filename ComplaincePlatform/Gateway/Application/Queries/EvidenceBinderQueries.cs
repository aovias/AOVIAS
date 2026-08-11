using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record GetEvidenceBinderSummaryQuery(string ApiVersion, Guid FrameworkId) : IRequest<IActionResult>;

public sealed record GetEvidenceBinderChaptersQuery(string ApiVersion, Guid FrameworkId) : IRequest<IActionResult>;

public sealed record GetEvidenceBinderObjectivesQuery(string ApiVersion, Guid FrameworkId, Guid ChapterId) : IRequest<IActionResult>;

public sealed record GetEvidenceBinderOverviewQuery(string ApiVersion, Guid FrameworkId, Guid ObjectiveElementId) : IRequest<IActionResult>;

public sealed record GetEvidenceBinderEvidencesQuery(
    string ApiVersion,
    Guid FrameworkId,
    Guid ObjectiveElementId,
    int Page,
    int PageSize) : IRequest<IActionResult>;

public sealed record GetEvidenceBinderChecklistQuery(string ApiVersion, Guid FrameworkId, Guid ObjectiveElementId) : IRequest<IActionResult>;

public sealed record GetEvidenceBinderAiMappingQuery(string ApiVersion, Guid FrameworkId, Guid ObjectiveElementId) : IRequest<IActionResult>;

public sealed record GetEvidenceBinderHistoryQuery(string ApiVersion, Guid FrameworkId, Guid ObjectiveElementId) : IRequest<IActionResult>;
