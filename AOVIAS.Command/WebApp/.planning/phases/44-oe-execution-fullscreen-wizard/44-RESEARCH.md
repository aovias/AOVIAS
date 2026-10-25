# Phase 44 — Research: OE Full-Screen Wizard Modal

## What planners need to know

### Full-screen overlay in Angular enterprise apps

- **CDK Overlay** or a **route-level fullscreen layout** are common patterns. Modal “full screen” often uses **fixed inset 0** + **high z-index** + **backdrop** (`--overlay-backdrop`, `--z-modal`) so the shell matches PRAMAN tokens. Focus **must** move into the overlay on open (**focus trap**) and restore on close.
- **Angular Material** provides `MatDialog` with `panelClass` + `maxWidth`/`height` overrides, or **custom overlay** for full viewport. Choice affects animation and focus management — document trade-offs in implementation plans, not in the UX spec.

### Wizard / stepper UX

- **Linear vs gated steps:** This phase requires **explicit locking** (Execute after Understand; Evaluate after minimum execution; Improve **conditional** on non-compliance / open gaps; Complete only when resolved). The stepper UI should **visually distinguish** locked, active, completed, and **conditionally skipped** (e.g. Improve when already compliant).
- **Stepper placement:** Below header or integrated — both valid; integrated saves vertical space but must stay readable on tablet/mobile.

### State persistence (“resume”)

- **Session vs server:** Resume can use **sessionStorage** namespaced by `userId` + `oeId` (align with Phase 28 isolation patterns) until API exists; planner should call out **conflict** with multi-tab / multi-device expectations.

### Accessibility

- **WAI-ARIA:** Wizard container should expose **progress** (`aria-valuenow` / `aria-valuemax` or `aria-current="step"` on the active step). Step titles must be **headings** or labeled regions. **Keyboard:** arrow keys vs Tab-only — define minimum: Tab order through primary actions, **Escape** closes (with confirm if dirty).

### Responsive

- **Context panel:** Desktop fixed column; tablet **slide-over** or **drawer**; mobile **bottom sheet** or **collapsible** — match existing Journey patterns (`--shadow-elevated-panel`, `--bp-*`) where possible.

### Theming

- All surfaces must reference **`DESIGN_RULES.md`** and **`src/styles/tokens/*`** — no new hex in feature CSS; reuse scrollbar utilities (`praman-scrollbar`) inside scroll regions.

---

## RESEARCH COMPLETE

Sufficient to author **44-DESIGN-SPEC.md** and phased **PLAN.md** files for design-first deliverables and subsequent implementation waves.
