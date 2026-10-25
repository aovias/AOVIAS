using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Manages active persona resolution and user overrides.
/// Delegates routing logic to ICopilotPersonaRouter.
/// </summary>
public sealed class CopilotPersonaService : ICopilotPersonaService
{
    private readonly ICopilotPersonaRouter _router;

    private static readonly IReadOnlyList<PersonaResponseDto> PersonaCatalog = new[]
    {
        new PersonaResponseDto
        {
            Id           = nameof(CopilotPersona.AccreditationCoach),
            DisplayName  = "Accreditation Coach",
            Description  = "Guides the hospital through NABH accreditation readiness, maturity stages, and framework compliance.",
            PageContexts = new[] { "ComplianceMaturity", "Journey", "ComplianceGraph", "General" },
        },
        new PersonaResponseDto
        {
            Id           = nameof(CopilotPersona.QualityManagerAssistant),
            DisplayName  = "Quality Manager Assistant",
            Description  = "Supports quality managers with task prioritization, evidence gaps, and compliance planning.",
            PageContexts = new[] { "Journey", "QualityIntelligence", "PatientSafety", "General" },
        },
        new PersonaResponseDto
        {
            Id           = nameof(CopilotPersona.EvidenceAdvisor),
            DisplayName  = "Evidence Advisor",
            Description  = "Helps with evidence collection, document review, and linking evidence to objective elements.",
            PageContexts = new[] { "Evidence", "EvidenceBinder", "ReviewQueue" },
        },
        new PersonaResponseDto
        {
            Id           = nameof(CopilotPersona.CapaAdvisor),
            DisplayName  = "CAPA Advisor",
            Description  = "Advises on deficiency remediation, corrective and preventive actions, and RCA.",
            PageContexts = new[] { "Dcc", "DeficiencyCommandCenter" },
        },
        new PersonaResponseDto
        {
            Id           = nameof(CopilotPersona.ExecutiveAdvisor),
            DisplayName  = "Executive Advisor",
            Description  = "Provides high-level compliance analytics summaries, risk overviews, and executive briefings.",
            PageContexts = new[] { "AnalyticsHub", "Dashboard", "QualityIntelligence" },
        },
        new PersonaResponseDto
        {
            Id           = nameof(CopilotPersona.DepartmentHeadAssistant),
            DisplayName  = "Department Head Assistant",
            Description  = "Focused on department-level compliance tasks, SLA adherence, and team assignments.",
            PageContexts = new[] { "General" },
        },
        new PersonaResponseDto
        {
            Id           = nameof(CopilotPersona.AuditorAssistant),
            DisplayName  = "Auditor Assistant",
            Description  = "Supports auditors with finding review, observation drafting, and audit trail queries.",
            PageContexts = new[] { "Audit", "ReviewQueue" },
        },
    };

    public CopilotPersonaService(ICopilotPersonaRouter router)
    {
        _router = router;
    }

    public CopilotPersona GetActivePersona(CopilotContext context) =>
        _router.DeterminePersona(context);

    public CopilotPersona OverridePersona(CopilotContext context, CopilotPersona override_)
    {
        // Override is applied by creating a new context with PersonaOverride set.
        // The caller is responsible for persisting the override in session state (frontend store).
        var overrideContext = new CopilotContext
        {
            OrganizationId     = context.OrganizationId,
            PageContext        = context.PageContext,
            FocusedEntityId    = context.FocusedEntityId,
            FocusedEntityType  = context.FocusedEntityType,
            ConversationSummary = context.ConversationSummary,
            RecentEntityRefs   = context.RecentEntityRefs,
            PersonaOverride    = override_,
            CurrentStage       = context.CurrentStage,
            ReadinessScore     = context.ReadinessScore,
            AnalyticsSummary   = context.AnalyticsSummary,
        };
        return _router.DeterminePersona(overrideContext);
    }

    public CopilotPersona ResetPersona(CopilotContext context)
    {
        // Reset by evaluating context without override
        var resetContext = new CopilotContext
        {
            OrganizationId     = context.OrganizationId,
            PageContext        = context.PageContext,
            FocusedEntityId    = context.FocusedEntityId,
            FocusedEntityType  = context.FocusedEntityType,
            ConversationSummary = context.ConversationSummary,
            RecentEntityRefs   = context.RecentEntityRefs,
            PersonaOverride    = null,
            CurrentStage       = context.CurrentStage,
            ReadinessScore     = context.ReadinessScore,
            AnalyticsSummary   = context.AnalyticsSummary,
        };
        return _router.DeterminePersona(resetContext);
    }

    public IReadOnlyList<PersonaResponseDto> GetPersonaCatalog() => PersonaCatalog;
}
