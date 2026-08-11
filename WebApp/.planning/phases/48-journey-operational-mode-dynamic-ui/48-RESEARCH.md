# Phase 48 — Research: OperationalMode-dynamic Journey UI

## RESEARCH COMPLETE

### Domain alignment

- **`OperationalMode`** (numeric enum 0–4) lives in `src/app/features/journey/models/nabh.enums.ts` and is already used by `JourneyWorkspaceStore.lifecycleMode`, workspace mapping (`normOperationalMode`), and filters (`filterOesByLifecycle`, `mapLifecycleToNabhAssessmentPhase`).
- **Shell** `OperationalMode` in `shell-operational-mode.service.ts` is a **different** string union (sidebar UX only); Phase 48 scope is **journey feature** enum only—avoid name collisions by importing from `nabh.enums` with explicit aliases if needed.

### Surfaces to touch (no new components)

| Area | Primary files |
|------|----------------|
| Dashboard — phase cards, progress, metrics, actions | `journey.component.ts/html`, `journey-card.*`, `accreditation-progress-card` (bindings), `action-bar.*`, `metric-card` usage |
| Execution — chapters | `execution-chapter-panel.*`, `execution-screen.component.*` VM |
| Execution — standards / OE rows | `standard-card.*`, execution OE row templates / VM in `execution-screen.component.ts` |
| Right context | `journey-context-panel.*`, `JourneyContextPanelVm` / store computeds |
| OE wizard | `oe-execution-wizard.component.*`, `oe-execution-wizard.model.ts`, store wizard signals |

### Data gaps (mock until API)

Progression rules (e.g. “Internal enabled after Self threshold”) require **either**:

- New **optional** fields on `JourneyWorkspaceSnapshot` / overview (API-backed later), or  
- **Deterministic mock** thresholds in a small `journey-mode-progression.util.ts` (documented as mock-only).

Per phase constraint **“no new fields”** in product UI: prefer **mock derivation from existing** `overview` / chapter aggregates already on snapshot; if impossible, add **internal-only** optional snapshot fields gated as “mock contract” in one place—not new user-visible controls.

### Centralized policy shape

A single **pure** module (e.g. `journey-operational-mode.policy.ts`) should expose:

- Booleans: `showHighRiskAction`, metric visibility flags, `progressBarKind`, `chapterIssueBadge`, etc.
- Wizard: which steps are **enabled** vs **locked** vs **read-only**
- Context panel: evidence/tasks/upload visibility and `disabled` reasons (strings for tooltips / `aria-disabled`)

Consumers: `computed(() => policy(store.lifecycleMode(), …ctx))` in store or thin component computeds—**no** scattered `if (mode === …)` in templates beyond binding to policy fields.

### Accessibility

- When actions are hidden, avoid leaving keyboard traps; when disabled, set `aria-disabled` + title/tooltip from policy `reason` string.

### References

- `DESIGN_RULES.md`, `src/styles/tokens/*` — only visibility/state changes; no new layout tokens required.
- Prior related phases: 35 (lifecycle), 41–42 (execution + context), 44 (wizard).
