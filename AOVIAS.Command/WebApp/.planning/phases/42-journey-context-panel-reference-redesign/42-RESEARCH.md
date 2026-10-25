# Phase 42 — Research: Journey Context Panel (Reference Redesign)

## RESEARCH COMPLETE

### Current implementation

| Area | Location | Notes |
|------|----------|--------|
| Right column host | `ExecutionScreenComponent` template | `app-journey-context-panel.execution-screen__panel--right` |
| Panel component | `src/app/features/journey/components/journey-context-panel/` | OnPush, single `vm` input (`JourneyContextPanelVm`), outputs: `startJourney`, `uploadEvidence`, `addTask`, `viewTimeline` |
| Data | `JourneyWorkspaceStore.journeyContextPanelVm` | Builds from `selectedOeId` + `findOe()`; uses `oeRowStatus`, `oeJourneyStatus`, `oeFrequencyLabel`, evidence array for counts/dates |
| Shell layout | `execution-screen.component.scss` | 3-column grid desktop; stacks ≤64rem; panel `min-block-size: 0`, `overflow: hidden` |

### Gaps vs. stakeholder spec

1. **Structure**: Spec requires three **card sections** (OE Context, Journey Progress, Quick Actions) with token **gap** between them; current UI is a **flat** section stack inside one surface.
2. **Panel container**: Spec asks elevated **gradient/dark surface**, **large radius**, **soft shadow** on the **column wrapper**; styling may live on `host` / wrapper inside `journey-context-panel` or a dedicated `execution-screen__panel--right` inner shell — avoid duplicating with left/center panels.
3. **Chips**: Department / Role / Frequency should be **pill chips** with **variant tints** (neutral/blue, purple, green) from **tokens** — map to `--color-*` or `color-mix()` on existing semantic tokens; extend `DESIGN_RULES.md` only if a token alias is missing.
4. **Progress**: Spec wants labeled **“Journey Progress”** card, **horizontal bar** with **gradient fill** (warning/orange for partial), numeric **Current Score** row, **Evidence** row, **Last Updated** row (formatted date). VM has `scoreLabel` (`3/5`) but no **0–100 bar width** — derive **`progressPercent`** from `ObjectiveElement.score` (1–5) in store VM or component: `(score / 5) * 100`.
5. **Primary / secondary CTAs**: Spec: **Start Journey** (primary gradient) + **View History** (outline) — only primary exists today; add **View History** output (`viewHistory`) and stub handler in `ExecutionScreenComponent`.
6. **Quick actions**: Spec: **3-column grid** of square **icon cards**; desktop 3-up, **mobile 2 columns**; align with existing outputs; optional single `quickAction` output with literal union for analytics — or keep separate outputs for minimal churn.
7. **States**: **Empty** copy should align (“No OE selected”); **Loading** skeletons when `store.loading()` or future async OE detail — parent may pass `loading` input or read store via `inject` (prefer **input** for testability).
8. **Disabled / locked**: `JourneyWorkspaceStore.readOnly` (lifecycle) — disable primary/secondary/quick actions when `readOnly()` is true; surface `disabledReason` optional in VM later if needed.
9. **Icons**: Continue **Lucide** via `@ng-icons`; add **history/clock** for View History if missing from `provideIcons`.
10. **API contract**: Prefer **extending `JourneyContextPanelVm`** (and store computed) vs. many `@Input`s on the component — matches existing pattern and avoids duplicate sources of truth.

### Token / design constraints

- **DESIGN_RULES.md** + `src/styles/tokens/*` only; no hex/rgba literals in feature SCSS.
- Use `--space-*`, `--radius-*`, `--color-*`, `--shadow-*`, `--font-*`, `--border-width-*`.
- Responsive breakpoints: align with execution screen (**64rem**, **40rem**) unless a documented panel breakpoint exists.

### References

- Phase 37.1 / 37.2 / 39 / 41 verification docs (execution layout).
- `JourneyContextPanelVm` in `execution-screen-vm.model.ts`.
