using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record ListKnowledgeBaseFrameworksQuery(string ApiVersion) : IRequest<IActionResult>;

public record GetKnowledgeBaseFrameworkQuery(Guid Id, string ApiVersion) : IRequest<IActionResult>;

public record ListKnowledgeBaseNodesQuery(Guid FrameworkId, string ApiVersion) : IRequest<IActionResult>;

public record GetKnowledgeBaseNodeQuery(Guid Id, string ApiVersion) : IRequest<IActionResult>;

public record GetKnowledgeBaseNodeChildrenQuery(Guid Id, string ApiVersion) : IRequest<IActionResult>;

public record GetKnowledgeBaseNodeDetailsQuery(Guid Id, string ApiVersion) : IRequest<IActionResult>;

public record GetKnowledgeBaseNodeDetailsByCodeQuery(string NodeCode, string ApiVersion) : IRequest<IActionResult>;

public record GetKnowledgeBaseNodeDecisionsQuery(Guid NodeId, string ApiVersion) : IRequest<IActionResult>;

public record GetKnowledgeBaseKnowledgeByNodeQuery(Guid NodeId, string ApiVersion) : IRequest<IActionResult>;

public record GetKnowledgeBaseKnowledgeByNodeCodeQuery(string NodeCode, string ApiVersion) : IRequest<IActionResult>;

public record SearchKnowledgeBaseKnowledgeQuery(string? Q, Guid? FrameworkId, string ApiVersion) : IRequest<IActionResult>;

public record GetKnowledgeBaseAssessmentSummaryQuery(Guid Id, string ApiVersion) : IRequest<IActionResult>;

public record GetKnowledgeBaseGapAnalysisByAssessmentQuery(Guid AssessmentId, string ApiVersion) : IRequest<IActionResult>;
