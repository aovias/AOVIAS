# Phase 38 — Research: Journey UI ↔ DESIGN_RULES token alignment

## RESEARCH COMPLETE

### Question

What must be true to refactor all Journey-related UI to follow `DESIGN_RULES.md` without inventing arbitrary values?

### Findings

1. **`DESIGN_RULES.md` (current)**  
   - Grounds Control Center in **legacy** CSS variables (`--bg-primary`, `--primary-color`, `--card-bg`, …) that resolve via `src/styles/tokens/theme.tokens.css`.  
   - States that **typography and spacing are often literal px** in Control Center sources; **no** `--spacing-xs` / `--font-size-xs` family is required there today.  
   - Points to **`foundation.tokens.css`** for a **4px spacing system** as `--space-1` … `--space-12` (rem-based), typography scale as `--font-size-body`, `--font-size-caption`, `--font-weight-*`, radii as `--radius-xs` … `--radius-lg`.

2. **Global tokens already available** (`foundation.tokens.css`)  
   - Spacing: `--space-0` … `--space-12`.  
   - Type: `--font-size-display-lg`, `--font-size-heading-md`, `--font-size-heading-sm`, `--font-size-body`, `--font-size-caption`, `--line-height-*`, `--font-weight-regular|medium|semibold|bold`.  
   - Radius: `--radius-xs` … `--radius-lg`.  
   - Layout: `--header-height`, `--content-max-width`, breakpoints as custom properties.

3. **Semantic / theme colors** (`theme.tokens.css`)  
   - Prefer **`var(--color-*)`** for new or refactored declarations (e.g. `--color-bg-page`, `--color-text-primary`, `--color-brand-primary`, `--color-border-subtle`).  
   - **Legacy aliases** (`--text-primary`, `--card-bg`, …) remain valid and match Control Center; either style is acceptable if consistently applied and theme-safe.

4. **Gap vs stakeholder prompt**  
   The requirements mention `--spacing-xs/sm/md/lg`, `--color-primary`, `--border-radius-standard`, `--button-height`, `--input-height`. These names **do not all exist** as-is in the repo. **Do not invent hex.** Options:  
   - **Map** to existing tokens: e.g. spacing → `--space-*`; radius “standard” → `--radius-sm` or `--radius-md` (pick one project-wide for Journey).  
   - **Optional prerequisite sub-wave:** add **aliases** in `foundation.tokens.css` only if product wants literal names (`--spacing-sm: var(--space-2)`), documented in `DESIGN_RULES.md`.

5. **Journey scope (implementation inventory)**  
   Target tree: `src/app/features/journey/` — pages (list, execution), stepper, OE detail sections, dialogs/drawers/side panels, shared journey components. Prior phases **37 / 37.1 / 37.2** already aimed at token discipline; this phase is a **full sweep** and **regression pass** for hardcoded values.

6. **Validation**  
   - Grep for `#`, `rgb`, `rgba` in Journey SCSS/HTML (allow `none`, `transparent`, SVG filters only where unavoidable).  
   - Grep for raw `\d+px` after refactor (should trend to zero in favor of `var(--space-*)`, `var(--radius-*)`, etc.).  
   - Build + visual smoke in light/dark.

### Recommendation for planning

- **Wave A (optional):** Token alias additions + `DESIGN_RULES.md` one-page mapping table (names used in code → canonical `var`).  
- **Wave B:** Inventory + file list.  
- **Wave C–E:** Refactor by surface (list → execution → stepper/OE → overlays).  
- **Wave F:** Verification checklist + fix regressions.
