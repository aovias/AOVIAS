# Sprint 13E — Persona Experience Layer Report

**PRAMAN Compliance Platform · Compliance Copilot**  
**Date:** 2026-06-24  
**Status:** Complete

---

## Executive Summary

Sprint 13E delivers a **persona-driven Copilot experience** without adding new AI capabilities, recommendation rules, draft types, or autonomous actions. One Copilot surfaces seven role-aware experiences through filtering, prioritization, landing states, and workspace UI.

---

## Persona Catalog

| Persona | Display Name | Theme | Primary Focus |
|---------|--------------|-------|---------------|
| `AccreditationCoach` | Accreditation Coach | accreditation | Stage unlocks, readiness gaps, blocking deficiencies |
| `QualityManagerAssistant` | Quality Manager Assistant | quality | Task prioritization, evidence gaps, SLA |
| `EvidenceAdvisor` | Evidence Advisor | evidence | Missing evidence, verification backlog |
| `CapaAdvisor` | CAPA Advisor | capa | Overdue CAPAs, recurring deficiencies, effectiveness |
| `ExecutiveAdvisor` | Executive Advisor | executive | Readiness trends, governance, escalations |
| `DepartmentHeadAssistant` | Department Head Assistant | department | Department tasks, SLA warnings |
| `AuditorAssistant` | Auditor Assistant | audit | Open findings, verification, audit prep |

Definitions live in `PersonaExperienceRegistry` with objectives, default questions, priority categories, supported draft types, and supported proposal types.

---

## Routing Matrix

**Hierarchy (CopilotPersonaRouter):**

1. Manual override (`PersonaOverride` / session storage)
2. Page context map
3. Focused entity type
4. Conversation summary heuristics
5. Recent entity refs
6. Default: Accreditation Coach

| Page / Context | Auto Persona |
|----------------|--------------|
| Compliance Maturity | Accreditation Coach |
| Analytics Hub / Dashboard | Executive Advisor |
| DCC / Issues & Risks / Corrective Actions | CAPA Advisor |
| Evidence / Journey | Evidence Advisor |
| Findings / Audit / Review Queue | Auditor Assistant |
| Copilot Workspace | Accreditation Coach (overrideable) |

---

## Experience Design

### Landing States (`PersonaLandingStateBuilder`)

Headlines are built from existing analytics SSOT (deficiency counts, CAPA metrics, SLA metrics, evidence summary). No new rules.

### Persona Workspace (`/app/copilot`)

Tabs: Overview · Recommendations · Drafts · Approvals · History

Components:
- `PersonaSwitcherComponent` — view/change persona, reset to Auto, session persistence
- `PersonaHeaderComponent` — persona identity, focus, readiness, KPI, landing copy
- `CopilotWorkspaceCardsComponent` — priorities, drafts, approvals, questions, mini-activity
- `CopilotActivityFeedComponent` — read-only engagement feed

### Conversation Injection

Sidebar AI assistant prepends active persona, focus area, and objectives into `conversationSummary` on each query.

---

## Workspace Architecture

```
Gateway /api/v1/copilot/*
    └── ComplianceService CopilotExperienceController
            ├── GET  experience/catalog
            ├── GET  experience/workspace?persona=&frameworkId=
            ├── GET  activity
            ├── GET  usage-metrics
            └── POST activity

PersonaExperienceService
    ├── PersonaExperienceRegistry (definitions)
    ├── PersonaLandingStateBuilder (SSOT headlines)
    ├── ICopilotRecommendationRepository (filter + category score)
    ├── IAiDraftRepository (supported draft types)
    ├── ICopilotActionProposalRepository (supported proposal types)
    └── ICopilotActivityService (workspace view tracking)

CopilotActivityEvents table (immutable audit of engagement)
```

---

## Usage Metrics (`CopilotUsageMetrics`)

Stored server-side from activity events (no analytics dashboard yet):

- Persona usage counts
- Drafts generated
- Proposals approved / executed
- Recommendation engagement
- Workspace views
- Persona changes

---

## Validation Scenarios

| Scenario | Expected Persona |
|----------|------------------|
| Navigate to Compliance Maturity | Accreditation Coach |
| Navigate to Analytics Hub | Executive Advisor |
| Navigate to DCC / Corrective Actions | CAPA Advisor |
| Navigate to Evidence | Evidence Advisor |
| Navigate to Findings | Auditor Assistant |
| Manual persona override | Selected persona persists in session |
| Reset to Auto | Router re-evaluates from page context |
| Persona change | Recommendation ordering changes via priority categories |
| Persona change | Highlighted drafts/proposals change via supported type lists |

**Confirmed:** No new recommendation rules, draft templates, or proposal generators were added in 13E.

---

## Copilot Maturity Assessment

| Capability | Sprint | 13E Contribution |
|------------|--------|------------------|
| Core foundation | 13A | Persona router enhanced |
| Recommendations | 13B | Persona prioritization (reuse) |
| Drafts | 13C | Persona-highlighted drafts |
| Approvals | 13D | Persona-filtered proposals |
| Persona UX | 13E | Workspace, switcher, landing, activity |
| Observability | 13F (next) | Metrics foundation stored |

---

## Platform Review Readiness

- ✅ One Copilot, multiple experiences
- ✅ Auto + manual persona selection
- ✅ Copilot workspace route (`/app/copilot`) in sidebar
- ✅ Activity feed + usage metrics capture
- ✅ Gateway proxy for all experience endpoints
- ✅ EF migration `20260624170000_Sprint13E_PersonaExperienceLayer`
- ✅ Ready for Sprint 14 — External Auditor Workspace
- ✅ Ready for Sprint 13F — Observability & Effectiveness Analytics

---

## Key Files

**Backend**
- `Application/Copilot/Experience/PersonaExperienceDefinition.cs`
- `Application/Copilot/Experience/PersonaExperienceRegistry.cs`
- `Application/Copilot/Services/PersonaLandingStateBuilder.cs`
- `Application/Copilot/Services/PersonaExperienceService.cs`
- `Application/Copilot/Services/CopilotActivityService.cs`
- `Application/Copilot/Services/CopilotPersonaRouter.cs`
- `Controllers/V1/Copilot/CopilotExperienceController.cs`

**Frontend**
- `features/copilot/pages/copilot-workspace/`
- `features/copilot/components/persona-switcher/`
- `features/copilot/components/persona-header/`
- `features/copilot/components/copilot-workspace-cards/`
- `features/copilot/components/copilot-activity-feed/`
- `features/copilot/services/copilot-navigation-bridge.service.ts`
- `features/copilot/state/copilot.store.ts` (session override, workspace, activity)
