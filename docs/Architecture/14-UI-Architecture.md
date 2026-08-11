# UI Architecture

[← README](./README.md) · [UX Guidelines](./22-UX-Guidelines.md)

Angular 21 standalone app at `WebApp/`. Gateway is sole API origin in production path.

---

## Shell Layout

| Component | Role |
|-----------|------|
| `ShellComponent` | Header + sidebar + main + footer |
| `SidebarComponent` | Module navigation (`sidebar-nav.model.ts`) |
| `HeaderComponent` | Context, reports/audit/knowledge mega-menu |
| `BottomNavComponent` | Mobile shortcuts |

**Main content background:** `var(--bg-secondary)` — Copilot aligns to same tokens.

---

## Navigation Sections (Sidebar)

| Section | Routes |
|---------|--------|
| Dashboard | `/app/dashboard`, `/app/analytics`, `/app/compliance-maturity`, `/app/copilot` |
| Compliance Execution | `/app/journey`, `/app/evidence`, `/app/tasks` |
| Action Center | `/app/audit/findings`, corrective actions, control-center tasks |

---

## Screen Catalog

| Screen | Route | Store / state | Key APIs |
|--------|-------|---------------|----------|
| Dashboard | `/app/dashboard` | Services | `/dashboard`, snapshots |
| Analytics Hub | `/app/analytics/:section?` | `AnalyticsApiService` | `/analytics` |
| Compliance Maturity | `/app/compliance-maturity` | Component state | maturity-dashboard |
| **Copilot Workspace** | `/app/copilot` | `CopilotStore` | `/copilot/*` |
| Journey Maturity | `/app/journey/maturity` | `JourneyWorkspaceStore` | journeys |
| Journey Execution | `/app/journey/execution` | `JourneyWorkspaceStore` | journeys, tasks |
| OE Execution | `/app/journey/execution/Progress/:oeGuid` | Wizard state | OE wizard APIs |
| Evidence Library | `/app/evidence` | — | `/evidence` |
| Review Queue | `/app/evidence/review-queue` | — | review-queue |
| Evidence Binder | `/app/evidence/binder` | `EvidenceBinderStore` | `/evidence-binder` |
| Tasks | `/app/tasks` | `TaskStore` (scoped) | `/tasks` |
| Findings | `/app/audit/findings` | — | `/findings/workspace` |
| Finding Detail | `/app/audit/findings/:id` | — | finding by id |
| Control Center | `/app/control-center` | — | control-center APIs |
| Onboarding | `/onboarding` | `OnboardingStore` | onboarding wizard |

---

## Copilot Workspace Tabs

| Tab | Component | APIs |
|-----|-----------|------|
| Overview | `CopilotWorkspaceCardsComponent` | experience/workspace, recommendations |
| Recommendations | `RecommendationListComponent` | recommendations, refresh |
| Drafts | `DraftCardComponent`, `DraftViewerComponent` | drafts |
| Approvals | `ProposalListComponent` | proposals |
| History | `CopilotActivityFeedComponent` | activity |
| Analytics | `CopilotAnalyticsComponent` | analytics/* |

---

## State Management

- **Signals + injectable stores** — no NgRx.
- Root stores: `CopilotStore`, `JourneyWorkspaceStore`, `GapAnalysisStore`, `OnboardingStore`.
- Component-scoped: `TaskStore` on `TaskPageComponent`.

---

## Interceptors (order)

`loadingInterceptor` → `authInterceptor` → `journeyLongReadInterceptor` → `apiResponseInterceptor`

---

## Deep Links

- Copilot recommendations may include `deepLink` → `router.navigateByUrl`.
- Journey OE routes: `/app/journey/execution/Progress/{oeGuid}`.

---

## Responsive Behavior

- Sidebar collapses to overlay (< 768px).
- Bottom nav on mobile.
- Shell uses `100dvh` with scroll in `main-content`.

---

## AI UX Surfaces

| Surface | Location | Type |
|---------|----------|------|
| Compliance Copilot | `/app/copilot` | Structured workspace |
| Sidebar AI Assistant | Shell sidebar tab | Conversational |
| Control Center AI panel | `/app/control-center` | Embedded copilot panel |
| Onboarding AI | `/onboarding` | Wizard assistant |

---

## Future UI

- External auditor portal (**planned**)
- Unified analytics + Copilot nav (**TD-H04**)
