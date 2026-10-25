# Phase 41 — Research

## RESEARCH COMPLETE

### Goal

Plan an incremental refactor of the Journey Execution screen (`ExecutionScreenComponent` + `execution-*-panel` + `standard-card`) toward **Standards → OE → Start Journey**, without a from-scratch redesign.

### Current codebase anchors

- **Shell:** `src/app/features/journey/pages/execution-screen.component.{ts,html,scss}` — 2-column workspace (chapter rail + standards center); right column may be absent or placeholder depending on branch state.
- **State:** `JourneyWorkspaceStore` — chapter/standard/OE selection, filtered OEs, department filter.
- **VM:** `execution-screen-vm.model.ts`, `standard-card.model.ts` — `StandardModel`, `StandardCardOeModel`, `actionClicked` on `app-standard-card`.
- **OE status:** `execution-oe-vm.util.ts` — `oeRowStatus`, labels/tone for chips.
- **Tokens:** `DESIGN_RULES.md`, `src/styles/tokens/*` — mandatory for new visuals.

### Domain gaps (to close in plan)

1. **Journey lifecycle per OE** — Product needs a tri-state **Journey Status**: Not Started | In Progress | Completed. Source options: (a) derive from existing `ObjectiveElement` / workspace DTO fields if present (e.g. workflow step, last activity); (b) **mock** in store until API; (c) document contract for backend `GET/ PATCH` later. Plan must pick one and isolate behind a small mapper.

2. **“Start Journey Panel”** — User asked row click → open panel. Map to: (1) select OE in store (already `selectOe`), (2) **right “Journey Context Panel”** visible state with instructions vs detail; (3) optional **overlay/drawer** only if product confirms — default to **inline right column** to match Phase 37.1 mission-control layout.

3. **Right panel content** — Requires VM fields: department, role, frequency, score, evidence count, last updated — confirm against `objective-element.model.ts` and journey API DTOs; use `—` or mock when missing.

4. **Quick filter chips** — Optional wave: filter OEs by compliance tone / classification / risk; may compose with existing department tabs — avoid duplicate filter semantics.

### UX / technical constraints

- **OnPush**, `trackBy` on `@for` OE lists (already pattern in `standard-card`).
- **Accessibility:** `aria-expanded` on expandable rows/cards; table/grid roles; keyboard: listbox/grid pattern vs arrow navigation — document tradeoff (full roving tabindex is higher effort).
- **Performance:** Lazy render expanded OE block already partially addressed; keep expanded body in DOM with CSS grid `0fr`/`1fr` animation (existing pattern).

### References

- Phase **37.1** / **37.2** plans and VERIFICATION for execution screen scope.
- `40-journey-dashboard-reference-parity` for token/discipline consistency.

---

## Synthesis for planning

- Ship in **3 waves**: (1) data + store + VM for journey status + selection contract; (2) `standard-card` + OE row + right panel UI; (3) responsive breakpoints, title wrapping, quick filters, a11y pass, verification doc.
