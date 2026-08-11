# Phase 37 — Research: PRAMAN Journey Dashboard Enterprise Redesign

## Scope understood from request

- Build a brand new Journey dashboard screen in Angular; do not reuse prior Journey UI layout/styles/components.
- Follow `DESIGN_RULES.md` strictly.
- Use only tokenized styling from `src/styles/tokens/foundation.tokens.css` and `src/styles/tokens/theme.tokens.css`.
- No inline styles, no hardcoded hex or pixel values unless introducing approved tokens centrally.
- Build top-to-bottom sections: Header, Journey Mode Selector, Recommendation Panel, Primary Action Bar, Metrics Overview, Insights & Charts.
- Keep workflow context clear: Chapter -> Standard -> OE.

## Design and architecture constraints extracted

1. **Token-only styling**
  - Spacing must use 4px scale tokens (`--space-`*).
  - Colors must use semantic theme tokens (`--color-`*).
  - Shadows/radius/typography from token files only.
2. **Theme compatibility**
  - Light/dark must switch without component-level hardcoded colors.
  - New chart colors should be centralized in token layer, then consumed by chart config.
3. **Component architecture**
  - Reusable presentational components with `@Input`/`@Output`.
  - `ChangeDetectionStrategy.OnPush` everywhere in UI layer.
  - No business logic in reusable UI components.
4. **Interaction + accessibility**
  - Keyboard focus, ARIA labels, tooltip semantics for locked states.
  - Hover and selected states represented with tokenized elevation/border changes.
5. **Performance**
  - Keep Journey feature lazy-loaded.
  - Avoid churn in chart rendering and card lists (`trackBy`, immutable inputs).

## Codebase implications

- Existing Journey module already contains many components and workspace store files.
- Requirement explicitly requests replacing UI implementation, so plan must include:
  - pruning old Journey dashboard-specific UI sections/styles,
  - introducing a new dashboard page composition with reusable component set,
  - migrating any remaining hardcoded chart colors to centralized semantic chart tokens.

## Recommended implementation strategy

- **Wave 1:** Define dashboard contracts + page shell + route wiring + remove legacy dashboard composition.
- **Wave 2:** Build reusable components (`journey-card`, `recommendation-panel`, `action-bar`, `metric-card`, `chart-card`) with OnPush and SCSS token usage.
- **Wave 3:** Chart integration, centralized chart token mapping, accessibility hardening, responsive verification, and migration sweep for hardcoded style leftovers.

## Risks and mitigations

- **Risk:** Residual old Journey layout code survives and conflicts visually.
  - **Mitigation:** Include explicit removal/refactor tasks and grep-based verification for old selectors/classes.
- **Risk:** Chart libs often encourage inline color arrays.
  - **Mitigation:** Define chart semantic tokens and a centralized chart palette mapper utility.
- **Risk:** Responsive regressions at small widths.
  - **Mitigation:** Enforce 12-column layout contract and breakpoint checks for all sections.

## Research verdict

This phase is feasible with a 3-plan execution set. The success hinge is strict token governance plus a clean separation between container/state logic and reusable presentation components.

## RESEARCH COMPLETE