# Phase 50 — Research: OE Execution Enterprise Compliance Wizard (Inline)

## Question answered

What do we need to know to **plan** the redesign of the Objective Element execution UI so it matches stakeholder UX (compliance wizard, execution-width layout, Resume Journey entry) without new backend fields?

---

## Current implementation (codebase)

| Area | Finding |
|------|---------|
| **Component** | `OeExecutionWizardComponent` (`app-oe-execution-wizard`) — overlay shell (`.oe-wizard`), backdrop, `cdkTrapFocus`, header with `JourneyWorkspacePrimaryHeader`, horizontal step track. |
| **Steps (code)** | `OE_WIZARD_STEP_LABELS`: Understand → Execute → Evaluate → Improve → Complete (`oe-execution-wizard.model.ts`). |
| **State** | `JourneyWorkspaceStore`: `oeWizardOpen`, `oeWizardStepIndex`, locking/completion, `OeWizardPersistedState` in sessionStorage (user + OE scoped). |
| **Entry** | `JourneyContextPanelComponent` primary button **`onStartJourney()`** — label switches to **Resume Journey** when `journeyStatus === 'in_progress'` (same handler as Start). Execution screen wires `startJourney` to open wizard. |
| **Execution host** | `execution-screen.component.html` hosts `<app-oe-execution-wizard />` in `.execution-screen__oe-wizard-host`. **`execution-screen.component.scss` sets `display: none` on the host** — planner must **enable visibility** when wizard is active (e.g. class on `execution-screen` + host rule) or remove the hide so the overlay can render. |
| **Prior phase doc** | Phase 44 spec assumed **full viewport** overlay; Phase 50 **explicitly** requires **same width** as Journey Execution workspace — layout strategy must change (inset panel vs full viewport). |

---

## UX mapping (old steps → new steps, no new fields)

Stakeholder steps:

1. **Understanding** — Explanation, Why It Matters  
2. **Compliance Guidance** — How To Comply  
3. **Evidence** — Evidence Required, drag-drop upload  
4. **Audit Checklist** — toggles/checkboxes  
5. **Review & Submit** — summary, submit  

**Implementation approach:** Re-label and restructure templates; map **existing** OE text fields (description, requirements, mock copy) and **existing** checklist/evidence UI patterns into these five panels. **Do not** invent new API properties — use current `ObjectiveElement` / VM strings and store flags already in `OeWizardPersistedState` (or equivalent), with optional **sessionStorage key migration** if step indices change semantics.

---

## Layout: “same width as Journey Execution”

- **Journey Execution** uses `.execution-screen` grid: padded page, three-column workspace on desktop.  
- **Target:** Wizard should occupy the **main content column** width (same max-width / gutters as `.execution-screen__workspace`), not `inset: 0` full screen.  
- **Pattern:** Replace full-viewport `.oe-wizard` with an **inline panel** below the execution header (or replace workspace columns when wizard open — product choice: **focus mode** hides chapter/standards/context **or** dims them with **non-modal** overlay). Minimum viable: **single column** centered to `minmax(0, 1fr)` matching **center + right** combined max width, or explicit `max-width` token shared with execution panels — **verify against** `execution-screen.component.scss` grid tokens.

---

## Responsive

- **Desktop:** Horizontal stepper (existing pattern).  
- **Mobile:** **Vertical** stepper (stakeholder requirement) — current code uses horizontal track; add breakpoint to stack steps or use `flex-direction: column` / separate mobile template region.  
- **Footer:** Full-width primary/secondary on **narrow** breakpoints; sticky footer inside scroll container.

---

## Accessibility

- Keep `role="dialog"` only if focus trap remains; **inline** wizard may behave as **region** + `aria-labelledby` instead of modal — decide with a11y: if not modal, **do not** use `aria-modal="true"`.  
- Step progress: `aria-current="step"` on active step; **percentage** progress bar with `aria-valuenow` / `aria-valuemax` (already partially in context panel patterns).

---

## Token / design system

- **Mandatory:** `DESIGN_RULES.md`, `src/styles/tokens/*` — semantic colors: info (blue), success (green), warning/in-progress (orange), attention (red) — map to existing semantic token names in project, not raw hex.  
- **Icons:** Lucide via `@ng-icons` (already in wizard).

---

## Risks

| Risk | Mitigation |
|------|------------|
| `display: none` on wizard host blocks all UI | Gate host with `execution-screen--wizard-open` class. |
| Full-screen CSS conflicts with inline | Refactor `.oe-wizard__surface` to max-height + border-radius card, not full viewport. |
| Step locking rules from Phase 44 | Re-derive **linear** gates for five new steps using existing `oeWizardCanAccessStep` logic. |

---

## RESEARCH COMPLETE

Ready for **50–01** (design spec + wireframe + component hierarchy) and **50–02** (implementation waves: shell + steps + store + polish).
