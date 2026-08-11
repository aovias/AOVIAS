# Phase 35 — Research: Journey Module NABH UI Refactor

## RESEARCH COMPLETE

### Current implementation (WebApp)

| Finding | Detail |
|--------|--------|
| **Location** | `src/app/features/journey/pages/journey.component.ts` — single ~2.8k-line standalone component |
| **Data model** | Local interfaces: `Chapter`, **`Clause`**, `Task`, `Evidence`, `Finding`, `TimelineEvent`; **mock/static** data inlined in component |
| **UX pattern** | Clause terminology and clause checklist-style UI; chapter left nav exists with search + risk + completion (partial) |
| **API** | **No** `JourneyService` / backend binding in feature; not aligned with target `Chapter → Standards[] → ObjectiveElements[]` contract |
| **Routing** | `features/journey/routes.ts` lazy-loads one component only |

### Gap vs. target objective

1. **Structural**: Remove all **Clause** types, cards, and flat checklist flows; introduce **Standard** (expandable) + **OE** as the leaf unit.
2. **NABH semantics**: 1–5 score + labels; classification bands; core vs non-core; accreditation eligibility rules in UI.
3. **Lifecycle**: Mode switch affecting filters and edit/read-only — requires **central state** (signals/store), not local booleans.
4. **Evidence & audits**: OE-level evidence status, verification, expiry — likely **nested** APIs or sub-resources; plan assumes backend exposes or will expose structured payloads; UI types first, adapters for mock → API.
5. **Performance**: Current monolith prevents lazy rendering; need **split components** + **CDK virtual scroll** (or deferred `*ngFor` batches) for OE lists.

### Architectural decisions (recommended)

- **Feature folder**: `features/journey/` → `components/`, `models/`, `services/`, `state/`, `mappers/`, keep `pages/journey-shell.component.ts` (thin) + child routes if needed.
- **State**: Angular **signals** + optional small facade service (`JourneyWorkspaceStore`) for selection, lifecycle mode, filters — consistent with project (no NgRx).
- **Theming**: Reuse **CSS variables** from existing journey styles; extract tokens to `journey-tokens` partial or component-scoped `:host` to avoid global drift.
- **API strategy**: Phase execution should define **TypeScript models** matching the tree contract; use **mock JSON** or **HttpClient** behind `JourneyDataService` with `useMock` flag until endpoints are finalized — **no backend schema change** in this repo.

### Risks

| Risk | Mitigation |
|------|------------|
| API not ready | Typed interfaces + mock provider + swap implementation in service |
| Scope explosion | Waves in PLAN: foundation → shell → OE → drawer/actions → performance |
| Regression | E2E smoke: load journey, select chapter, expand standard, open OE drawer |

### References

- User spec: hierarchical navigation, OE detail drawer, NA + justification, action plans, accreditation panel, lifecycle toggle, evidence integration, trends.
- Project: `ComplianceContextService` / `AppContextService` for hospital + framework context (journey likely scoped by `journeyId` from org).

---

*Generated for GSD plan-phase. Consumed by `35-*-PLAN.md`.*
