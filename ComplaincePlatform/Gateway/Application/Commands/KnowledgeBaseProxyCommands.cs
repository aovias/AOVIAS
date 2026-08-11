using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record PostKnowledgeBaseAiAskCommand(JsonElement Body, string ApiVersion) : IRequest<IActionResult>;

public record PostKnowledgeBaseDecisionCommand(JsonElement Body, string ApiVersion) : IRequest<IActionResult>;

public record PostKnowledgeBaseAssessmentCommand(JsonElement Body, string ApiVersion) : IRequest<IActionResult>;

public record PutKnowledgeBaseAssessmentItemCommand(Guid AssessmentId, Guid NodeId, JsonElement Body, string ApiVersion) : IRequest<IActionResult>;

public record PostKnowledgeBaseGapAnalysisCommand(Guid AssessmentId, string ApiVersion) : IRequest<IActionResult>;
