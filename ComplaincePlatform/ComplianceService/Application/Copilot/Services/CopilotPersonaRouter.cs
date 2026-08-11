using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.RoleExperience;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Rule-based deterministic persona router. No LLM required.
///
/// Decision hierarchy:
///   1. Manual override  (context.PersonaOverride)
///   2. Page context map (matches AiPageContextService values)
///   3. Focused entity type
///   4. Conversation context heuristics
///   5. JWT role → RoleExperiencePackRegistry persona
///   6. Default: AccreditationCoach
/// </summary>
public sealed class CopilotPersonaRouter : ICopilotPersonaRouter
{
    private static readonly Dictionary<string, CopilotPersona> PageContextMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Dcc",                    CopilotPersona.CapaAdvisor          },
        { "DeficiencyCommandCenter",CopilotPersona.CapaAdvisor          },
        { "IssuesAndRisks",         CopilotPersona.CapaAdvisor          },
        { "CorrectiveActions",      CopilotPersona.CapaAdvisor          },
        { "ComplianceMaturity",     CopilotPersona.AccreditationCoach   },
        { "Journey",                CopilotPersona.EvidenceAdvisor      },
        { "Evidence",               CopilotPersona.EvidenceAdvisor      },
        { "EvidenceBinder",         CopilotPersona.EvidenceAdvisor      },
        { "ReviewQueue",            CopilotPersona.AuditorAssistant     },
        { "Audit",                  CopilotPersona.AuditorAssistant     },
        { "Findings",               CopilotPersona.AuditorAssistant     },
        { "AnalyticsHub",           CopilotPersona.ExecutiveAdvisor     },
        { "Analytics",              CopilotPersona.ExecutiveAdvisor     },
        { "Dashboard",              CopilotPersona.ExecutiveAdvisor     },
        { "QualityIntelligence",    CopilotPersona.QualityManagerAssistant },
        { "PatientSafety",          CopilotPersona.QualityManagerAssistant },
        { "ComplianceGraph",        CopilotPersona.AccreditationCoach   },
        { "Copilot",                CopilotPersona.AccreditationCoach   },
    };

    private static readonly Dictionary<string, CopilotPersona> EntityTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ComplianceDeficiency",  CopilotPersona.CapaAdvisor          },
        { "Deficiency",            CopilotPersona.CapaAdvisor          },
        { "Capa",                  CopilotPersona.CapaAdvisor          },
        { "CapaAction",            CopilotPersona.CapaAdvisor          },
        { "Evidence",              CopilotPersona.EvidenceAdvisor      },
        { "ObjectiveElement",      CopilotPersona.AccreditationCoach   },
        { "Finding",               CopilotPersona.AuditorAssistant     },
        { "AuditFinding",          CopilotPersona.AuditorAssistant     },
        { "ClinicalIndicator",     CopilotPersona.QualityManagerAssistant },
        { "IndicatorRegistry",     CopilotPersona.QualityManagerAssistant },
    };

    public CopilotPersona DeterminePersona(CopilotContext context)
    {
        if (context.PersonaOverride.HasValue)
            return context.PersonaOverride.Value;

        if (!string.IsNullOrWhiteSpace(context.PageContext) &&
            PageContextMap.TryGetValue(context.PageContext, out var pagePersona))
            return pagePersona;

        if (!string.IsNullOrWhiteSpace(context.FocusedEntityType) &&
            EntityTypeMap.TryGetValue(context.FocusedEntityType, out var entityPersona))
            return entityPersona;

        if (!string.IsNullOrWhiteSpace(context.ConversationSummary))
        {
            var persona = InferFromConversation(context.ConversationSummary);
            if (persona.HasValue)
                return persona.Value;
        }

        if (context.RecentEntityRefs.Count > 0)
        {
            foreach (var ref_ in context.RecentEntityRefs)
            {
                var type = ref_.Split(':')[0];
                if (EntityTypeMap.TryGetValue(type, out var refPersona))
                    return refPersona;
            }
        }

        // Phase 3 Sprint 1 — JWT role → Copilot persona when persona query / page signals are absent.
        if (context.UserRoles.Count > 0
            && RoleExperiencePackRegistry.TryResolvePersona(context.UserRoles, out var rolePersona))
        {
            return rolePersona;
        }

        return CopilotPersona.AccreditationCoach;
    }

    private static CopilotPersona? InferFromConversation(string summary)
    {
        var text = summary.ToLowerInvariant();

        if (text.Contains("capa") || text.Contains("corrective action") || text.Contains("deficiency"))
            return CopilotPersona.CapaAdvisor;

        if (text.Contains("evidence") || text.Contains("verification") || text.Contains("obligation"))
            return CopilotPersona.EvidenceAdvisor;

        if (text.Contains("finding") || text.Contains("audit observation"))
            return CopilotPersona.AuditorAssistant;

        if (text.Contains("executive") || text.Contains("leadership") || text.Contains("governance")
            || text.Contains("readiness trend") || text.Contains("risk exposure"))
            return CopilotPersona.ExecutiveAdvisor;

        if (text.Contains("accreditation") || text.Contains("maturity stage") || text.Contains("stage unlock"))
            return CopilotPersona.AccreditationCoach;

        if (text.Contains("department") || text.Contains("team task") || text.Contains("sla"))
            return CopilotPersona.DepartmentHeadAssistant;

        return null;
    }
}
