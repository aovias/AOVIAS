# Phase 39 — Research: Journey Enterprise UI Polish

## RESEARCH COMPLETE

### Question answered

What must execution know to implement **visual-only** enterprise polish across Journey **without** new features, while updating **`DESIGN_RULES.md`** as the contract for future work?

---

### Relationship to prior phases

| Phase | What it did | Phase 39 builds on it |
|-------|-------------|------------------------|
| **37 / 37.1 / 37.2** | Journey dashboard + execution layout + reference polish | Refines density, borders, hierarchy — **no** IA or route changes |
| **38** | Token compliance (`var(--*)` only, no raw hex in feature CSS) | Keeps token discipline; adds **semantic density** (hairline border, card compact padding, badge/icon rules) via **documented** aliases |

---

### Source of truth for implementation

- **`DESIGN_RULES.md`** — must gain explicit sections: border weight, elevation tiers, **Journey card density** (compact vs normal), badge + icon tokens, spacing enforcement, button hierarchy. Phase 38 already has a “Journey implementation mapping” table; Phase 39 **extends** it with polish-specific rows.
- **`src/styles/tokens/foundation.tokens.css`** — add **aliases only** (e.g. `--border-width-hairline`, `--card-padding-compact`, `--icon-size-sm|md`) that chain to `rem` / existing `--space-*` / `--radius-*`, not new arbitrary colors.
- **`src/styles/tokens/theme.tokens.css`** — optional **Journey-only elevation** token if `--shadow-sm` is still too strong for stacked cards; prefer `color-mix` or lighter rgba **only** inside theme file (same pattern as existing shadows).

---

### Key components to touch (inventory)

**Execution workspace**

- `execution-screen.component.ts` (+ html/scss) — shell gaps, column separation by spacing not heavy borders.
- `execution-chapter-panel`, `chapter-navigation-panel`, `chapter-navigation`, `chapter-item` — reduce chapter card height, tighten progress + label alignment.
- `execution-standards-panel`, `standards-panel`, `standard-card` — **already** has `expanded` input + `standardToggle` output: wire default/header so **accordion** uses **existing** API only (no new buttons beyond styling existing chevron/header).
- `oe-table`, `oe-row` — row height, column alignment, divider weight.
- `execution-action-panel`, `accreditation-decision-panel`, `progress-card` — card padding, primary vs secondary button tiers.

**Journey dashboard / list**

- `journey.component.*`, `journey-card`, `metric-card`, `chart-card`, `recommendation-panel`, `action-bar`, `journey-nabh-metrics-bar`, `lifecycle-mode-switch` — unify card shell, reduce blue overuse on non-primary surfaces.

**Overlays**

- `oe-detail-drawer` and related tabs — match new border/elevation/badge rules; no new tabs or fields.

---

### Risk controls

- **No new controls:** collapsible standard sections must use **`StandardCardComponent`**’s existing `expanded` / `standardToggle` — verify parent (`execution-standards-panel` or equivalent) already toggles; only adjust layout/CSS and ensure expanded state is visually obvious.
- **Icons:** use existing icon set already in Journey (e.g. Lucide/ng-icons); add **decorative** icons only where they clarify status/action — no new business logic.
- **Verification:** grep Journey for raw `border: 1px` / `2px` after change; spot-check light + dark theme.

---

### References

- `DESIGN_RULES.md` — current Control Center + Journey mapping.
- `.planning/phases/38-journey-design-rules-token-compliance/38-01-PLAN.md` — prior token sweep scope.
