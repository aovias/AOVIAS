using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

/// <summary>Gateway MediatR query/command records for Copilot (Sprint 13A + 13B).</summary>

// ── Sprint 13A: read-only recommendations ────────────────────────────────────
public sealed record GetCopilotRecommendationsQuery(string ApiVersion, string? QueryString) : IRequest<IActionResult>;
public sealed record GetCopilotRecommendationsByPersonaQuery(string ApiVersion, string Persona, string? QueryString) : IRequest<IActionResult>;
public sealed record GetCopilotRecommendationQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;

// ── Sprint 13B: rule-engine refresh ──────────────────────────────────────────
public sealed record RefreshCopilotRecommendationsCommand(string ApiVersion, string? Body) : IRequest<IActionResult>;

// ── Sprint 13A + 13C: drafts ──────────────────────────────────────────────────
public sealed record GetCopilotDraftsQuery(string ApiVersion, string? QueryString) : IRequest<IActionResult>;
public sealed record GetCopilotDraftQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record GetCopilotDraftVersionsQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record GenerateCopilotDraftCommand(string ApiVersion, string? Body) : IRequest<IActionResult>;
public sealed record RegenerateCopilotDraftCommand(string ApiVersion, Guid Id) : IRequest<IActionResult>;

// ── Sprint 13A: personas ──────────────────────────────────────────────────────
public sealed record GetCopilotPersonasQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetCopilotActivePersonaQuery(string ApiVersion, string? QueryString) : IRequest<IActionResult>;

// ── Sprint 13D: action proposals ──────────────────────────────────────────────
public sealed record GetCopilotProposalsQuery(string ApiVersion, string? QueryString) : IRequest<IActionResult>;
public sealed record GetCopilotPendingProposalsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetCopilotProposalQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record GetCopilotProposalAuditTrailQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record CreateCopilotProposalCommand(string ApiVersion, string? Body) : IRequest<IActionResult>;
public sealed record GenerateCopilotProposalFromRecommendationCommand(string ApiVersion, Guid RecommendationId, string? Body) : IRequest<IActionResult>;
public sealed record GenerateCopilotProposalFromDraftCommand(string ApiVersion, Guid DraftId, string? Body) : IRequest<IActionResult>;
public sealed record ApproveCopilotProposalCommand(string ApiVersion, Guid Id, string? Body) : IRequest<IActionResult>;
public sealed record RejectCopilotProposalCommand(string ApiVersion, Guid Id, string? Body) : IRequest<IActionResult>;
public sealed record ExecuteCopilotProposalCommand(string ApiVersion, Guid Id) : IRequest<IActionResult>;

// ── Sprint 13E: persona experience layer ─────────────────────────────────────
public sealed record GetCopilotExperienceCatalogQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetCopilotExperienceWorkspaceQuery(string ApiVersion, string? QueryString) : IRequest<IActionResult>;
public sealed record GetCopilotActivityQuery(string ApiVersion, string? QueryString) : IRequest<IActionResult>;
public sealed record GetCopilotUsageMetricsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record RecordCopilotActivityCommand(string ApiVersion, string? Body) : IRequest<IActionResult>;

// ── Sprint 13F: observability analytics ─────────────────────────────────────
public sealed record GetCopilotAnalyticsOverviewQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetCopilotAnalyticsRecommendationsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetCopilotAnalyticsDraftsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetCopilotAnalyticsProposalsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetCopilotAnalyticsPersonasQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetCopilotAnalyticsTrendsQuery(string ApiVersion, string? QueryString) : IRequest<IActionResult>;
public sealed record GetCopilotAnalyticsImpactQuery(string ApiVersion, string? QueryString) : IRequest<IActionResult>;
