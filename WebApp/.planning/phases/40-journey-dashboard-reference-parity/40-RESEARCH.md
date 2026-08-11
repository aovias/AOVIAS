# Phase 40 — Research: Journey Dashboard Reference Parity

## RESEARCH COMPLETE

### Source documents

- **Design Gap Analysis: Reference vs. Implemented** (stakeholder) — sections 1–10: mode cards, recommendation, action bar, metrics, charts, page layout, typography, interactive states, effects, spacing.
- **Reference screenshot** (Journey Mode dashboard) — dark theme, left-aligned “Select Your Journey Mode” + subtitle, four mode cards in a row, recommendation card, three action buttons, metric row, chart row.

### Current implementation (baseline)

| Area | Location | Notes |
|------|-----------|--------|
| Journey page shell | `journey.component.html` / `.scss` | No page title block; mode grid uses 12-col `span 3` (four cards per row when wide enough). |
| Mode cards | `journey-card` | `icon` input is **first letter** of label (`mode.label.charAt(0)`). Status string from VM. |
| Icons elsewhere | `execution-action-panel` | Uses `@ng-icons/lucide` — **reuse pattern** for journey cards + action bar. |
| Recommendation | `recommendation-panel` | Copy from `buildRecommendationVm()`; `highRiskCount`/`lastActivity` props; gap doc wants specific CTA + warning line format. |
| Action bar | `action-bar` | Text-only or minimal icons — reference wants **visible** play / clock / warning icons. |
| Metrics / charts | `metric-card`, `chart-card` | Token-based; gap doc asks for bolder typography, darker surfaces, stronger metric hierarchy. |

### Resolved decisions for planning

1. **Header copy (mandatory)**  
   - **Title:** `Select Your Journey Mode`  
   - **Subtitle:** `Choose the audit phase to focus on` (stakeholder doc spelling; not “Chose”).

2. **Header placement**  
   - **Content-only** block at top of `journey-page` — **not** a revival of the removed global `journey-header` (Phase 39); simple left-aligned `h1` + `p` with tokens.

3. **Mode card icons**  
   - Map each mode to a **Lucide** icon (e.g. Self Assessment → check, Internal Audit → search, Final Audit / Surveillance → lock or similar) via `journey-card` template + inputs, or pass icon **name** from `journey.component` / VM.

4. **Layout**  
   - **Desktop:** explicit **4-column** mode for mode cards at `min-width` breakpoints (e.g. `grid-template-columns: repeat(4, minmax(0, 1fr))` or keep 12-col with `span 3` but verify **no** premature collapse).  
   - **Tablet/mobile:** stack or 2-col per existing breakpoints — document in plan.

5. **Active state**  
   - Reference **blue glow** — implement with **`box-shadow`** + `border-color` using `var(--color-brand-primary)` / `color-mix` per `DESIGN_RULES.md` (no raw hex in feature CSS).

6. **Copy / data**  
   - Recommendation **primary line** can use **derived** text from snapshot (e.g. next OE / compliance target) when available; otherwise **tighten** copy to match reference *tone* without inventing fake API fields.

---

### Out of scope (unless gap doc explicitly requires API)

- Top app chrome (breadcrumbs, PLANNING/IMPLEMENTATION tabs) from **second** reference image — **not** in current Journey route; note as **future** shell work.

---

### Files likely touched

- `src/app/features/journey/pages/journey.component.{html,scss,ts}`
- `src/app/features/journey/components/journey-card/*`
- `src/app/features/journey/components/recommendation-panel/*`
- `src/app/features/journey/components/action-bar/*`
- `src/app/features/journey/components/metric-card/*`
- `src/app/features/journey/components/chart-card/*`
- Optional: `src/app/features/journey/models/journey-dashboard-vm.model.ts` for icon keys / recommendation strings
