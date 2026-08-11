# 04 — UI / UX Audit

**Program:** CRP v1.0  
**Assessment Date:** 26 July 2026  
**Frontend SSOT:** `V:\DevelopmentRepo\V1.0\WebApp` (`praman-web` / AOVIAS Command)  
**Method:** Static route/nav/component analysis + incomplete-signal scan (no browser session in this pass)  
**Related:** `V:\DevelopmentRepo\V1.0\docs\PRAMAN-QA-Module-Inventory.md` (10 Jul 2026)

---

## 1. Audit Criteria

Each screen evaluated for: Navigation · Loading · Validation · Error handling · Empty states · Success messages · Accessibility · Responsiveness · Consistency · Usability · Professional appearance.

Status legend: **Pass** | **Partial** | **Fail** | **Placeholder** | **Hidden**

---

## 2. Shell & Cross-Cutting UX

| Area | Finding | Status | Severity |
|------|---------|--------|----------|
| App shell | Header + sidebar + mobile bottom nav + hospital selector + operational mode | Pass | — |
| Hospital context | Required for most APIs; missing context → toast “Organization context is not available” | Partial | Medium |
| Operational modes | Self Assessment / Internal Audit / Final Audit | Pass | — |
| Surveillance mode | Nav disabled “Coming soon” | Fail | High |
| Compliance Graph nav | `SHOW_COMPLIANCE_GRAPH_NAV = false` — routes exist, nav hidden | Hidden | Medium |
| Role-based shell | Single dashboard for all roles; `roleGuard` unused | Fail | High |
| Notification bell | Present; real-time SignalR delivery incomplete | Partial | High |
| AI sidebar | Present; production AI quality uncertain | Partial | Medium |
| Theme | Theme toggle; settings theme “future release” | Partial | Low |
| Error pages | 403/404/500/maintenance | Pass | — |
| Loading | Global loading interceptor; journey long-read interceptor | Partial | Medium |
| Empty states | Inconsistent (“No data.” vs richer empty) | Partial | Medium |
| Accessibility | Material helps; no systematic a11y audit evidence | Partial | Medium |
| Responsiveness | Mobile bottom nav exists; admin tables likely dense | Partial | Medium |
| Visual consistency | Large surface area; CCC/placeholders dilute professionalism | Partial | Medium |

---

## 3. Auth & Onboarding Screens

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| `/login` | Reactive forms + validation | Pass | — |
| `/login/mfa` | MFA challenge | Pass | — |
| `/signup`, `/register` | Multi-step validation | Pass | — |
| `/forgot-password`, `/reset-password` | Standard flows | Pass | — |
| `/verify-email` | Present | Partial | Medium |
| `/onboarding` | 6-step wizard; AI assistant panel | Pass | Medium (email invites) |

---

## 4. Dashboard & Intelligence

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| `/app/dashboard` | Shared for all roles; server-driven widgets | Partial | High (no role dashboards) |
| `/app/analytics`, `/:section` | Analytics hub | Pass | — |
| `/app/compliance-maturity` | Maturity view | Pass | — |
| `/app/copilot` | Workspace; depends on stubbed LLM | Partial | High |
| `/app/control-center/*` | Command center / tasks / knowledge graph | Partial | Medium |
| Overlap | Dashboard vs Control Center vs CCC — competing “home” concepts | Fail | Medium |

---

## 5. Compliance Execution

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| `/app/journey/execution` | Primary execution screen | Pass | — |
| `/app/journey/execution/stage/:stage` | Stage navigation | Pass | — |
| `/app/journey/execution/Progress/:oeGuid` | OE wizard | Partial | Medium (prod upload flag) |
| `/app/journey/maturity` | Maturity dashboard | Pass | — |
| `/app/journey/operational` | Operational journey | Pass | — |
| Journey mock layer | `useJourneyMockWorkspace` (default false); mock data files remain | Partial | Medium |
| `/app/tasks` | Task page | Pass | — |
| `/app/evidence` | Evidence library | Pass | — |
| `/app/evidence/review-queue` | Review queue | Pass | — |
| `/app/evidence/binder` | Binder; AI suggestions empty | Partial | Medium |

---

## 6. Action Center / Findings / CAPA

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| Findings (workspace) | Live API | Pass | — |
| Deficiency Command Center | Core CAPA path | Partial | High (model duality) |
| `/app/audit/corrective-actions` | Issues & risks | Partial | Medium |
| `/app/control-center/tasks` | Alerts/tasks | Pass | — |
| Empty states | Some “No data.” | Partial | Low |

---

## 7. Audit & Auditor

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| `/app/audit` | Audit dashboard | Partial | Medium |
| `/app/audit/findings`, `/:id` | Findings; detail has “future” placeholders | Partial | Medium |
| `/app/audit/simulation` | Client mock only | Placeholder | High |
| `/app/audit/reports` | Reports | Partial | Medium |
| `/app/auditor/dashboard` | Auditor home | Partial | High (not role-gated) |
| `/app/auditor/sessions`, `/:id` | Sessions workspace | Partial | Medium |
| `/app/auditor/evidence-review` | Evidence review | Pass | — |
| `/app/auditor/observations` | Reuses sessions page | Fail | Medium |
| `/app/auditor/history` | Reuses sessions page | Fail | Medium |
| `/app/auditor/reports` | Reports | Partial | Medium |
| Surveillance header link | Disabled Coming soon | Fail | High |

---

## 8. Documentation (QDMS)

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| `/app/documentation/dashboard` | Dashboard | Pass | — |
| Register / policies / sops / manuals / forms | Typed lists | Pass | — |
| Authoring studio | Authoring home + editor | Pass | — |
| Explorer / graph | Navigation aids | Pass | — |
| Review calendar / acknowledgements | Lifecycle support | Pass | — |
| Analytics / templates / settings | Admin surfaces | Partial | Low |

**Professional appearance:** Strong relative to other modules — treat as launch showcase.

---

## 9. Patient Safety

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| Dashboard / command-center | Implemented | Pass | — |
| Investigations / RCA | Workspace screens | Pass | — |
| Near-misses / sentinel / feedback / lessons | List+workflow | Pass | — |
| Analytics / settings | Present | Partial | Low |
| Forms | Often template-driven `ngModel` + HTML required (weaker validation) | Partial | Medium |

---

## 10. Quality Intelligence

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| Dashboard / indicators / scorecards | Implemented | Pass | — |
| Trends / benchmarking / thresholds | Present | Partial | Medium |
| Investigations / analytics / executive | Present | Partial | Medium |
| Settings | Present | Partial | Low |

---

## 11. Compliance Graph

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| Viewer / search / entity / traceability / executive / analytics | Routes live | Partial | Medium |
| Discoverability | Hidden from header mega-menu | Fail | Medium |

---

## 12. Reporting

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| `/app/reports` landing | Hub | Pass | — |
| compliance / audit / tasks / evidence / readiness | Typed reports | Pass | — |
| `/app/reports/custom` | Hardcoded NABH/JCI/chapter filters risk | Partial | Medium |
| Gap analysis `/app/gap-analysis/:id` | Workflow-driven | Pass | — |

---

## 13. CCC Placeholders

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| `/app/ccc/regulatory-governance` | Placeholder | Placeholder | Medium |
| `organizational-control` | Placeholder | Placeholder | Medium |
| `access-accountability` | Placeholder | Placeholder | Medium |
| `compliance-infrastructure` | Placeholder | Placeholder | Medium |
| `risk-escalation` | Placeholder | Placeholder | Medium |
| `monitoring-traceability` | Placeholder | Placeholder | Medium |
| `integration-system` | Placeholder | Placeholder | Medium |

**Recommendation:** Remove from customer nav until implemented — damages enterprise trust.

---

## 14. People / Workforce / Professional

| Route | Evaluation | Status | Severity |
|-------|------------|--------|----------|
| `/app/persons` directory/detail | Implemented | Pass | — |
| `/app/persons/merge` | Explicit placeholder | Placeholder | Low |
| Workforce directory / org-chart / manager | Implemented | Pass | — |
| Transfer / promote wizards | Present | Pass | — |
| Professional renewals / detail / timeline | Present | Pass | — |

---

## 15. Access Governance / Governance / Security

| Area | Evaluation | Status | Severity |
|------|------------|--------|----------|
| Access governance routes | Deep-link admin; simulator present | Partial | Medium |
| Governance dashboard / committees / meetings | Thin meeting forms | Partial | Medium |
| Security dashboard / devices / sessions / risk | Present | Partial | Medium |
| Permission UX consistency | Mix of permissionGuard + coarse codes | Partial | High |

---

## 16. Settings Administration

| Route / area | Evaluation | Status | Severity |
|--------------|------------|--------|----------|
| Settings home | Present | Pass | — |
| Org settings / branding | Dirty guard; branding mock previews not runtime-themed | Partial | Medium |
| Identity users | Bulk import/export disabled Coming soon | Partial | Medium |
| Password management | Permission gated | Pass | — |
| AI admin | Many Coming soon actions (models enable/disable) | Partial | Medium |
| Notifications admin | Stats often `—` placeholders | Partial | Medium |
| Workforce admin / intelligence / directory | Broad; some future cards | Partial | Medium |
| Integrations | Diagnostics charts Coming soon | Partial | Low |
| Platform compliance defaults | Coming Soon placeholder page | Placeholder | Low |
| System audit | Export Coming soon | Partial | Medium |
| Account / theme | Future release copy | Partial | Low |
| About | Present | Pass | — |

---

## 17. Navigation Model Issues

| Issue | Impact | Severity |
|-------|--------|----------|
| Multiple “command centers” (Dashboard, Control Center, CCC, Task CC) | Cognitive overload | Medium |
| Auditor area not role-isolated | Security/UX failure for external assessors | High |
| Hidden graph vs sold “traceability” story | Demo inconsistency | Medium |
| Disabled Surveillance still visible | Looks unfinished | High |
| Deep-link-only Access/Governance | Admins may never find critical tools | Medium |
| Legacy redirects | Good for bookmarks; increases test surface | Low |

---

## 18. Validation & Error Patterns

| Pattern | Where | Assessment |
|---------|-------|------------|
| Reactive Forms + Validators | Auth, org settings, branding | Strong |
| Template-driven + HTML required | Governance, auditor, patient-safety creates | Weak |
| API errors via interceptor/toasts | Global | Adequate but inconsistent field-level mapping |
| Permission denied → `/error/403` | permissionGuard routes | Good |
| Missing hospital context toast | Many pages | Usable but abrupt |

---

## 19. Accessibility & Professional Appearance

| Check | Result |
|-------|--------|
| Keyboard / ARIA systematic audit | **Not evidenced** |
| Color contrast / theme | Material defaults; branding indigo `#4F46E5` hardcode |
| Professional enterprise look | Strongest: Documentation, Journey, Evidence; Weakest: CCC placeholders, Coming soon clusters in Settings/AI |
| Empty states | Uneven — some sparse |
| Success feedback | Toast-driven; not uniformly confirmed for long workflows |

---

## 20. Screen Risk Register (UX)

| ID | Screen/Area | Gap | Severity |
|----|-------------|-----|----------|
| UX-001 | Shared dashboard | No role-specific experience | High |
| UX-002 | Auditor shell | Not role-gated | High |
| UX-003 | Surveillance nav | Disabled unfinished feature | High |
| UX-004 | CCC ×7 | Placeholder routes | Medium |
| UX-005 | Audit simulation | Mock sold as route | High |
| UX-006 | Copilot | Stub LLM undermines trust | High |
| UX-007 | Branding settings | Mock previews | Medium |
| UX-008 | Identity bulk ops | Coming soon | Medium |
| UX-009 | Auditor observations/history | Duplicate/reuse wrong page | Medium |
| UX-010 | Compliance graph | Hidden nav | Medium |
| UX-011 | Notification admin stats | Placeholder dashes | Medium |
| UX-012 | Empty states | Inconsistent | Low |
| UX-013 | Form validation pattern split | Data quality risk | Medium |
| UX-014 | Competing home surfaces | Navigation confusion | Medium |
| UX-015 | Person merge | Placeholder | Low |

---

## 21. UX Launch Recommendations

1. **Hide** CCC, Audit Simulation, Surveillance, Person Merge, Compliance Defaults from all customer-facing nav.  
2. **Ship role shells** (or at least auditor-only layout) before commercial pilots.  
3. **Unify empty/error/success** components across Tier-1 modules.  
4. **Promote Documentation + Journey + Evidence** as the polished demo path.  
5. **Do not demo Copilot** until real LLM adapter is wired.  
6. Run a focused **WCAG 2.1 AA** pass on login, onboarding, journey OE wizard, evidence review, documentation acknowledge.  
7. Fix **API base URL double-prefix** risks (`IntegrationApiService`, `WorkforceIntelligenceApiService`) before UAT.

---

## 22. Demo Screen Path (Recommended)

Use only these for sales demos until gaps close:

1. Login → Onboarding (or seeded hospital)  
2. Dashboard  
3. Journey execution → OE evidence  
4. Evidence review  
5. Findings → Deficiency CAPA  
6. Documentation publish → acknowledge  
7. Patient safety incident → RCA  
8. Readiness report  

Explicitly **exclude**: CCC, Surveillance, Audit Simulation, Copilot autonomy, AI model marketplace.
