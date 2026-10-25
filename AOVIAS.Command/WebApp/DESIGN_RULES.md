# PRAMAN UI Design Rules

## Constitution Reference

This design guidance is subordinate to:

- `V:\DevelopmentRepo\V1.0\docs\PRAMAN-Architecture-Constitution.md`

If guidance here conflicts with the constitution, the constitution takes precedence.

Design documentation for the WebApp.

> **Read this first.** The **AOVIAS COMMAND UI System** section immediately below is
> the current standard for all new and refactored screens. The Control Center /
> Journey sections that follow it document the *observed literals* those modules
> were built from; they remain accurate as history and as a reference for
> extending those specific screens, but where they disagree with the system
> below, **the system below wins**.

---

# AOVIAS COMMAND UI System

One visual language for every screen. Implemented in three files:

| File | Role |
|------|------|
| `src/styles/tokens/foundation.tokens.css` | Theme-independent primitives: type scale, spacing, radius, control sizing, motion, elevation aliases |
| `src/styles/tokens/theme.tokens.css` | Theme-scoped values: colour, status, interaction states, shadows |
| `src/styles/enterprise-ui.css` | Base normalization, `.aov-*` layout/component primitives, Angular Material harmonization |

**Rule:** feature CSS uses tokens and primitives. Raw hex, px, and `box-shadow`
literals in component styles are defects, not style choices.

---

## UI-1 — Typography scale

Every page uses the same roles. The page title is deliberately ~20% smaller than
the previous 28px heading so chrome yields vertical space to content.

| Role | Token | Size | Weight |
|------|-------|------|--------|
| Page title (H1) | `--font-size-page-title` | 22px | 600 |
| Page title, mobile | `--font-size-page-title-compact` | 20px | 600 |
| Section title (H2) | `--font-size-section-title` | 18px | 600 |
| Card title (H3) | `--font-size-card-title` | 16px | 600 |
| Subtitle / description | `--font-size-subtitle` | 14px | 400 |
| Body | `--font-size-body` | 14px | 400 |
| Dense body, table cells, buttons | `--font-size-body-sm` | 13px | 400–600 |
| Form + control labels | `--font-size-label` | 13px | 500 |
| Caption, hint, validation | `--font-size-caption` | 12px | 400 |
| Eyebrow, table header, badge | `--font-size-micro` | 11px | 600 + `--letter-spacing-wide`, uppercase |
| Metric / display numeral | `--font-size-display-lg` | 32px | 700 |

Supporting tokens: `--line-height-heading` (1.3), `--line-height-normal` (1.5),
`--line-height-relaxed` (1.6), `--letter-spacing-tight` (headings),
`--font-variant-numeric-tabular` (numbers, dates, IDs, table columns),
`--font-family-mono` (code).

**Exactly one H1 per page.** Marketing-style heroes outside the app shell
(onboarding welcome/completion) use `--font-size-display-lg`.

**Colour inherits.** The base `h1`–`h6`, `p`, and `a` rules set size, weight, and
rhythm but *not* colour, because these elements also appear on inverted and
tinted surfaces. Colour comes from the `.aov-*` primitive that owns the surface.
Text links opt in with **`.aov-link`**; bare anchors stay structural.

---

## UI-2 — Page layout template

Every routed page resolves to the same horizontal position. No page shifts
left or right relative to another.

```css
.aov-page {
  inline-size: 100%;               /* fluid — fills the shell, never centres */
  padding: var(--page-padding);    /* 25px on all four sides, every viewport */
  gap: var(--page-section-gap);
}
```

| Token | Value | Use |
|-------|-------|-----|
| `--page-padding` | **25px** | All four sides of every page container, at every screen size |
| `--page-padding-inline` / `-inline-compact` | → `--page-padding` | Aliases; kept so existing rules resolve to the one value |
| `--page-padding-block-start` / `-end` | → `--page-padding` | Aliases |
| `--page-max-width` | `none` | Pages are fluid; there is no content cap |
| `--page-header-gap` | 16px | Title block ↔ actions |
| `--page-header-margin-block-end` | 24px | Header ↔ first section |
| `--page-section-gap` | 24px | Between page sections |
| `--page-card-gap` | 16px | Between sibling cards |

`--page-padding` is deliberately off the 4px grid: it is a product decision,
declared once as a token so it never becomes a literal in feature CSS. Change
the page gutter in one place by editing that token.

### The page container is fluid — never capped, never centred

Page components **must not** declare `max-width` / `max-inline-size` +
`margin-inline: auto` on their root container. That is how caps drifted to
1400 / 1440 / 1680 / none and pages stopped lining up. The container always
fills the width the shell gives it and takes `--page-padding` on all four sides,
so every screen starts and ends at the same place at every viewport size.

Narrower *content* columns inside a page (forms, settings panels) are still
fine — they set their own `max-inline-size` and stay left-aligned, so the page's
own start position is unaffected.

> **Do not express page geometry as a child selector in the shell component.**
> Routed components are created by `router-outlet`, so their host elements never
> carry the shell's `_ngcontent` attribute. A rule like `.main-content > *` in
> `shell.component.ts` matches `router-outlet` but *not* the routed page — it
> fails silently and pushes the page into an unintended grid track.
> `.main-content` is the scroll region and nothing more.

**Primitives:** `.aov-page`, `.aov-page--flush` (page owns its own scroll
regions), `.aov-page__header`, `__titles`, `__eyebrow`, `__title`, `__subtitle`,
`__actions`, `__section`, `.aov-section-title`, `.aov-grid`,
`.aov-module-nav` (sticky module tab bar, aligned to page inline padding),
`.aov-metric-grid` / `.aov-metric-card` (KPI tiles).

---

## UI-3 — Control sizing and density

| Token | Value | Use |
|-------|-------|-----|
| `--control-height-sm` | 32px | Dense toolbars, table row actions |
| `--control-height-md` | 36px | Default enterprise density (buttons, menu items) |
| `--control-height-lg` | 40px | Primary CTAs, form inputs, primary tabs |
| `--table-cell-padding-block` / `-inline` | 12px / 16px | Table cells |
| `--table-header-min-height` / `--table-row-min-height` | 40px / 44px | Table rows |
| `--table-header-font-size` | `--font-size-micro`, uppercase | Table headers |
| `--table-body-font-size` | `--font-size-body-sm` | Table body |

> **Never give a `<table>` a single-word class that Tailwind also emits** —
> `grid`, `flex`, `block`, `table`, `contents`. Tailwind's global
> `.grid { display: grid }` beats an un-namespaced component rule, the element
> stops being a table, and `<thead>` sizes independently of `<tbody>` so headers
> no longer align with their columns. Four grids hit this. Use `.aov-table` /
> `.aov-table-wrap`; `enterprise-ui.css` also carries a `table.grid { display: table }`
> guard so the failure cannot recur silently.
>
> Wide grids add `.aov-table--wide` (56rem floor) or set their own
> `min-inline-size` when they carry many columns; the wrapper scrolls.
> Row states use `.is-selectable` + `aria-selected`, not bespoke classes.

**Primitives:** `.aov-btn` (`--primary`, `--secondary`, `--outlined`, `--text`,
`--danger`, `--icon`, `--sm`, `--lg`, `--loading`), `.aov-input`, `.aov-select`,
`.aov-textarea`, `.aov-search`, `.aov-check`, `.aov-field`, `.aov-label`,
`.aov-hint`, `.aov-error`, `.aov-table` / `.aov-table-wrap`, `.aov-toolbar`,
`.aov-chip` / `.aov-badge`, `.aov-alert`, `.aov-empty`, `.aov-skeleton`,
`.aov-progress`, `.aov-kv`, `.aov-metric`, `.aov-num` / `.aov-date`.

---

## UI-4 — Page navigation

**One component:** `app-page-tabs`
(`src/app/shared/ui/navigation/page-tabs/page-tabs.component.ts`). No custom tab
implementations.

| Variant | Use |
|---------|-----|
| `primary` | Module-level tabs — underline indicator on a divider rule |
| `secondary` | Segmented control for sub-views inside a page or panel |
| `section` | Quiet inline navigation inside a card or side panel |

Two modes, chosen by the items: **navigation** (items carry `route` → renders
`<nav><a routerLink>`, router owns active state) and **selection** (no route →
`role="tablist"`, roving tabindex, Arrow/Home/End keys, `aria-selected`, emits
`tabSelected`). Both modes give identical height, indicator, hover animation,
spacing, and typography; the strip scrolls horizontally rather than wrapping.

Icons are optional Lucide names — the **host** registers them via `provideIcons`
in its `viewProviders`.

**Module bars** use `.aov-module-nav` + `[inset]="true"`. The bar and the tab
strip span the **full page width**, so the active-tab divider runs edge to edge,
while `inset` pads the tabs by `--page-padding` so the first tab lines up with
the page content beneath it. Every module shell (risks, journey, documentation,
patient-safety, quality-intelligence, compliance-graph) uses this exact pairing.

---

## UI-5 — Colour

**AOVIAS Primary Blue** (`--color-primary`, `#2f6bff`) is the single interaction
accent: buttons, links, selection controls, focus rings, active tabs, selected
menu items, chips, pagination, progress. Angular Material's M3 primary/tertiary
tokens are bound to `--color-primary` in `material-aovias-palette.css` and
`enterprise-ui.css`.

### Customer branding vs. the interaction accent

Organization branding is **identity only** and must never touch the interaction
accent.

| Purpose | Tokens | Who writes them |
|---------|--------|-----------------|
| Interaction accent | `--color-primary*`, `--primary-color`, `--color-brand-primary*`, `--mat-sys-primary` | Stylesheets only. Always AOVIAS Blue. |
| Customer identity | `--color-identity-primary` / `-secondary` / `-accent` / `-soft` / `-border` | `OrganizationBrandingThemeService` at runtime |

`OrganizationBrandingThemeService` writes **inline styles on `documentElement`**,
which outrank every stylesheet. Historically it set `--color-primary` and
friends, so an organization whose brand colour was purple repainted every
button, chip, active nav item, and focus ring in the product — a whole-app
regression invisible to any source grep. It now writes only `--color-identity-*`.

**Never add an interaction token to that service.**

**No shell chrome consumes the identity tokens.** The header separator briefly
did, and it immediately put a non-AOVIAS hue back into the top bar — the exact
problem the split was meant to solve. The application interface is AOVIAS Blue
everywhere; a customer's brand colour belongs to the logo and to server-rendered
artefacts (reports, exports, emails), not to in-app chrome.

The `--color-identity-*` tokens and `applyColors()` are kept so branding data
has a safe, non-interactive destination and cannot leak into controls. They
default to AOVIAS Blue when an organization has no branding, and `resetColors()`
clears them on org switch. If a genuinely customer-owned surface is added later
(a branded report preview, a login brand panel), that is what it should read —
never page or control chrome.

When auditing colour, check **both** the source *and* the computed runtime value:

```bash
getComputedStyle(document.documentElement).getPropertyValue('--color-primary')
```

Status colours are **never** interaction accents. Use the `-fg` variants for
text — they are contrast-corrected per theme:

| Role | Fill / dot | Text | Soft bg | Border |
|------|-----------|------|---------|--------|
| Success | `--color-status-success` | `--color-status-success-fg` | `--color-status-success-soft` | `--color-status-success-border` |
| Warning | `--color-status-warning` | `--color-status-warning-fg` | `--color-status-warning-soft` | `--color-status-warning-border` |
| Danger | `--color-status-danger` | `--color-status-danger-fg` | `--color-status-danger-soft` | `--color-status-danger-border` |
| Info | `--color-status-info` | `--color-status-info-fg` | `--color-status-info-soft` | `--color-status-info-border` |
| Neutral | `--color-text-muted` | `--color-status-neutral-fg` | `--color-status-neutral-soft` | `--color-border-subtle` |

---

## UI-7 — Spacing

4px grid via `--space-1` … `--space-12`. Role tokens replace ad-hoc values:

`--card-padding` / `--card-padding-compact` · `--dialog-padding` /
`--dialog-padding-compact` · `--toolbar-padding-block` / `-inline` ·
`--toolbar-gap` · `--form-field-gap` (label ↔ control) · `--form-row-gap`
(field ↔ field) · `--form-group-gap` (fieldset ↔ fieldset) · `--grid-gap` ·
`--button-gap` · `--icon-gap` · `--table-cell-padding-*`.

Prefer `gap` on flex/grid over margin hacks.

---

## UI-8 — Border radius

| Token | Value | Applies to |
|-------|-------|-----------|
| `--radius-control` | 6px | Buttons, inputs, selects, small controls |
| `--radius-surface` | 12px | Cards, panels, table shells |
| `--radius-menu` | 8px | Menus, dropdowns, popovers, tooltips |
| `--radius-dialog` | 16px | Dialogs, drawers, sheets |
| `--radius-chip` | 999px | Chips, tags, pills, badges |

Material components are bound to these via `--mdc-*-container-shape` /
`--mat-*-container-shape` in `enterprise-ui.css`. No component declares its own
px radius.

---

## UI-9 — Iconography

| Token | Value | Use |
|-------|-------|-----|
| `--icon-size-xs` | 10px | Inline with micro text |
| `--icon-size-sm` | 12px | Meta, chips, dense rails |
| `--icon-size-md` | 16px | Default inline / button / row action |
| `--icon-size-lg` | 20px | Page header + toolbar actions |
| `--icon-size-xl` | 24px | Empty states, feature tiles |
| `--icon-size-nav` | 18px | Sidebar + bottom nav rail |
| `--icon-stroke-width` | 1.75 | Lucide stroke weight |
| `--icon-gap` | 8px | Icon ↔ label |

Align icon baselines with the adjacent text token.

---

## UI-10 — Interaction states

One vocabulary, one curve, one duration family.

| Token | Meaning |
|-------|---------|
| `--state-hover-bg` | Hover surface |
| `--state-pressed-bg` | Pressed / active surface |
| `--state-selected-bg` / `--state-selected-fg` / `--state-selected-border` | Selected |
| `--state-focus-ring-color` | Focus indicator |
| `--state-disabled-fg` / `-bg` / `-border`, `--state-disabled-opacity` | Disabled |
| `--state-skeleton-bg` / `--state-skeleton-shine` | Loading placeholder |

Motion: `--motion-duration-instant|fast|base|slow` (80/120/180/240ms) with
`--motion-easing-standard`. `--transition-control` is the shared transition for
colour, background, border, and shadow. All animated primitives collapse under
`prefers-reduced-motion: reduce`.

Loading buttons use `.aov-btn--loading` — identical spinner geometry on every
variant.

---

## UI-11 — Elevation

Limited scale, addressed by role. Feature CSS does not declare `box-shadow`.

| Token | Resolves to |
|-------|-------------|
| `--elevation-flat` | `none` |
| `--elevation-card` | `--shadow-xs` |
| `--elevation-card-hover` / `--elevation-raised` | `--shadow-sm` |
| `--elevation-menu` / `--elevation-popover` | `--shadow-md` |
| `--elevation-dialog` / `--elevation-fab` | `--shadow-lg` |
| `--elevation-drawer` | `--shadow-elevated-panel` |

---

## UI-12 — Responsive

Breakpoints from `foundation.tokens.css` (`--bp-mobile-max` … `--bp-wide-min`).

- Page padding drops to `--page-padding-inline-compact` at ≤767px; H1 drops to
  `--font-size-page-title-compact`; `.aov-page__header` stacks.
- Content is capped at `--page-max-width` and centred on wide monitors.
- **The page body never scrolls horizontally.** Wide content (tables, matrices,
  tab strips, diagrams) scrolls inside its own `overflow-x: auto` container,
  paired with the `praman-scrollbar` utilities.
- Flex/grid children that hold overflowing content set `min-inline-size: 0`.

---

## UI-13 — Accessibility

- **Focus:** one indicator everywhere — `--focus-ring-width` solid
  `--state-focus-ring-color`, offset `--focus-ring-offset`. Defined at zero
  specificity in `enterprise-ui.css` and re-asserted in
  `material-m3-button-overrides.css` (loads last).
- **Contrast:** status *text* uses the `-fg` variants, which are darkened in
  light theme and lightened in dark theme for WCAG AA.
- **Touch targets:** `--touch-target-min` (44px) applied to buttons and tabs
  under `@media (pointer: coarse)`.
- **Labels:** every control has a `<label for>` or `aria-label`; icons inside
  labelled controls are `aria-hidden`.
- **Tables:** `<th scope="col">`, a `<caption>` (visually hidden is fine), and
  `aria-sort` on sortable headers — the sort affordance is CSS-generated from
  `aria-sort`, so state and indicator cannot drift apart.
- **Disabled:** `aria-disabled` or `disabled` plus `--state-disabled-opacity`;
  never colour alone.

---

## Migration guidance

When touching a screen, convert it rather than matching its current literals:

1. Wrap the page in `.aov-page` and use `.aov-page__header` / `__title` /
   `__subtitle`.
2. Replace px font sizes with UI-1 role tokens.
3. Replace bespoke tabs with `app-page-tabs`.
4. Replace hex/px/shadow literals with UI-5 / UI-7 / UI-8 / UI-11 tokens.
5. Verify light **and** dark theme, and that nothing scrolls the page sideways.

Reference implementation: the **Risks** module
(`src/app/features/risks/pages/`) — shell, register table, matrix heatmap, and
form are all built entirely on this system.

---

## Source of Truth (Control Center — historical)

- **Primary reference:** the **Compliance Command Center** screen — `src/app/features/control-center/pages/compliance-command-center.component.ts` — and the components it composes:
  - `ai-intelligence-panel`, `executive-kpi-bar`, `risk-command-center`, `action-center`, `compliance-analytics`, `audit-readiness`, `activity-stream`, `smart-alerts`, `ai-copilot`
- **Broader Control Center feature:** other routes under `src/app/features/control-center/` (e.g. task command center, knowledge graph) use the **same CSS variable set** and add screen-specific literals (breakpoints, one-off dimensions). Treat them as extensions of the same module, not a second design language.
- **No arbitrary values:** numbers and token names in the sections below were **taken from Control Center source** (inline `styles` in the feature’s `.component.ts` files) or from **global definitions those styles rely on** (`src/styles/tokens/theme.tokens.css`).
- **Legacy vs semantic names:** components reference **legacy** custom properties (`--bg-primary`, `--primary-color`, …). Those aliases are assigned in `theme.tokens.css` and resolve to semantic `--color-*` tokens. Prefer **reusing the legacy names** when matching this UI; migrating call sites to `--color-*` only is a separate refactor.

---

## Colors (CSS variables used in Control Center)

These variables appear in `src/app/features/control-center/**/*.component.ts` styles. **Resolved colors** (light/dark) live in `src/styles/tokens/theme.tokens.css` under `:root[data-theme='light']` and `:root[data-theme='dark']` (legacy aliases block).

| Token | Typical usage in Control Center |
|--------|----------------------------------|
| `--bg-primary` | Page background (e.g. Compliance shell, task/knowledge pages). |
| `--bg-secondary` | Secondary surfaces (e.g. segmented areas in task UI). |
| `--bg-tertiary` | Muted panels, tracks, table-like rows. |
| `--card-bg` | Card and elevated panel backgrounds. |
| `--border-color` | Borders and dividers. |
| `--text-primary` | Primary headings and body emphasis. |
| `--text-secondary` | Subtitles, meta, secondary labels. |
| `--primary-color` | Primary actions, links, accents. |
| `--primary-hover` | Hover state for primary controls. |
| `--primary-bg-light` | Soft primary-tinted backgrounds (chips, highlights). |
| `--success-color` / `--warning-color` / `--danger-color` | Status, severity, and semantic accents. |
| `--hover-bg` | Generic hover background for rows and tiles. |
| `--edge-color` | **Knowledge graph only** — graph edge stroke (fallback in code: `rgba(100, 100, 100, 0.3)`). |

**Resolution:** e.g. `--bg-primary` → `var(--color-bg-page)`; `--primary-color` → `var(--color-brand-primary)`; `--border-color` → `var(--color-border-subtle)`. Do not document new hex values in feature code; **read them from `theme.tokens.css`** for the active theme.

---

## Typography (observed in Control Center)

Sizes and weights are **pixel literals in component styles** (not separate `--font-size-*` tokens in this feature). Common values seen across Compliance and its children:

| Scale (px) | Weight | Typical roles |
|-------------|--------|----------------|
| 10–11 | 600–700 | Micro labels, badges |
| 12–13 | 500–600 | Secondary labels, table/meta text, buttons |
| 14 | 500–600 | Body, subtitles, primary buttons (e.g. Compliance header) |
| 15–16 | 600 | Section titles, KPI labels |
| 18–20 | 600–700 | Card titles, panel headings |
| 24–28 | 600–700 | Large metrics, page title (Compliance uses **28px** for `page-title`; **24px** under `max-width: 768px`) |
| 32–58 | 700 | Donut/chart numerals, empty states (component-specific) |

**Line height:** `1`, `1.4`, `1.5` appear where multiline readability matters.

If a new screen must match this module and a size is not listed, use: **Value derived from Control Center UI – maintain consistency.**

---

## Spacing system (observed)

Control Center uses **literal `px` gaps and padding**, mostly on a **4px grid** (4, 8, 12, 16, 20, 24, 28 …). Recurring patterns:

- **Page shell (Compliance):** `padding: 24px`; section column `gap: 24px`; header actions `gap: 16px`.
- **Cards:** often `padding: 20px`–`28px`; inter-card `gap: 16px`–`20px`.
- **Tight UI:** `4px`, `6px`, `8px`, `10px`, `12px`, `14px` for chips, icon gaps, compact rows.

There is **no** single shared `--spacing-*` token family in this feature’s components; reuse **these magnitudes** or variables from global tokens if/when the app introduces shared spacing custom properties.

---

## Border radius (observed)

Common values: **2px, 3px, 4px, 5px, 6px, 8px, 10px, 12px, 20px**, plus **50%** for avatars/dots. **Compliance page** header controls use **6px** (`status-indicator`, `refresh-button`). Heavier cards often use **12px** — **`--radius-card`** in `foundation.tokens.css` encodes **12px** for shared card surfaces.

**Suggested convention (documentation only):** treat **6px–8px** as the default control/card radius family for this module unless a specific component uses another value above.

---

## Shadows / elevation

Control Center references global shadow tokens:

| Token | Usage |
|--------|--------|
| `--shadow-xs` | **Journey / stacked cards** — very soft resting elevation (defined in `theme.tokens.css` light + dark) |
| `--shadow-sm` | Default cards, panels, KPI tiles |
| `--shadow-md` | Hover elevation, emphasis (e.g. KPI hover, alerts) |
| `--shadow-lg` | AI copilot FAB and panel, prominent overlays |

Some screens use **hardcoded** `box-shadow` with `rgba(...)` (e.g. knowledge graph, task command center). Prefer `--shadow-*` for new work in this module; if you must match an existing screen, copy the **exact** declaration from that component.

---

## Icon and touch sizes (observed)

Inline SVG / icon boxes use sizes such as **12px, 16px, 20px, 24px, 28px**; avatar-style boxes **32px–48px**; Compliance **refresh** icon **16px** (`width`/`height` on `svg`).

**Phase 39 token preference (Journey):** use **`--icon-size-sm`** (12px) for meta, chips, and dense rails; **`--icon-size-md`** (16px) for row actions and default inline icons. Align icon baselines with adjacent **`--font-size-body`** / **`--font-size-caption`** text.

---

## Border system (Phase 39 — Journey)

- **Width:** use **`--border-width-hairline`** (`1px`) and **`--border-width-default`** (alias to hairline) for card shells, dividers, and table rules. **Do not** use 2px+ outlines for default surfaces — that reads as heavy and “boxed.”
- **Color:** `border: var(--border-width-hairline) solid var(--border-color)` (legacy) or `var(--color-border-subtle)` in semantic form. Separation can come from **background step** (`--card-bg` on `--bg-primary`) or **`--shadow-xs`** instead of a second border.
- **When to prefer shadow over border:** stacked cards on the same background → **`--shadow-xs`** or **`--shadow-sm`**; full-bleed panels already on contrasting `--card-bg` → often **no** border, only radius.

---

## Color discipline (Phase 39 — Journey)

- **Neutral first:** page **`--bg-primary`**, surfaces **`--card-bg`** / **`--bg-tertiary`** for structure. Most labels and chrome use **`--text-primary`** / **`--text-secondary`**.
- **Brand / `--primary-color`:** reserve for **one primary CTA per region**, critical links, and focused interactive affordances. Avoid large filled primary areas for decoration.
- **Status:** success / warning / danger / neutral semantics via **`--success-color`**, **`--warning-color`**, **`--danger-color`**, and execution status tokens in `theme.tokens.css` — not brand blue for non-primary states.

---

## Card density (Phase 39 — Journey)

| Mode | Token | Use for |
|------|--------|--------|
| **Compact** | `--card-padding-compact` (`var(--space-3)`) | Chapter rail cards, dense lists, OE table wrappers |
| **Normal** | `--card-padding-normal` / `--card-padding` (`var(--space-5)`) | Hero metrics, dashboard cards, primary panels |

---

## Badges & chips (Phase 39 — Journey)

- **Radius:** `var(--radius-sm)` or `var(--border-radius-standard)` (6px) for pills; match **status** chips to execution tokens where applicable.
- **Padding:** target **`var(--space-1)`** vertical and **`var(--space-2)`–`var(--space-3)`** horizontal for compact badges; keep **font** at **`--font-size-caption`** with **`--font-weight-semibold`** where emphasis is needed.
- **Colors:** semantic backgrounds via **`color-mix()`** with status colors (see execution status rows in `theme.tokens.css`); **neutral** meta uses **`--text-secondary`** on **`--bg-tertiary`** or subtle border only.

---

## Spacing — section rhythm (Phase 39 — Journey)

- **Related controls** (label + field, icon + text): **`--space-2`**–**`--space-3`** gap.
- **Blocks inside a card:** **`--space-3`**–**`--space-4`** vertical gap.
- **Between major sections** (header vs content vs footer regions): **`--section-gap-y`** (`var(--space-4)`) or **`--section-gap-y-loose`** (`var(--space-6)`) for page-level stacks.
- Prefer **gap** on flex/grid over margin hacks; align **start** to the same horizontal edge within a column.

---

## Button hierarchy (Phase 39 — Journey)

- **Primary:** solid fill **`var(--primary-color)`**, text **`var(--color-text-inverse)`** or guaranteed contrast per theme; height **`var(--button-height)`**; **`--font-weight-semibold`**; **`--border-radius-standard`**.
- **Secondary:** outline **`1px solid var(--border-color)`** (hairline) + **`var(--card-bg)`** or transparent; text **`--text-primary`**; hover **`--hover-bg`**.
- **Tertiary / ghost:** text-only or minimal border; **`--text-secondary`** default, **`--primary-color`** on hover if it is the single action in a row.

---

## Journey implementation mapping (Phase 38 + 39)

| Name | Resolves to |
|------|-------------|
| `--spacing-xs` / `--spacing-sm` / `--spacing-md` / `--spacing-lg` | `var(--space-1)` … `var(--space-6)` via `foundation.tokens.css` |
| `--border-radius-standard` | `var(--radius-sm)` (6px) |
| `--radius-card` | `0.75rem` (12px) — cards/panels aligned with Journey surfaces |
| `--button-height` / `--input-height` | `2.5rem` — shared control height |
| `--card-padding` / `--card-padding-normal` | `var(--space-5)` — default card density |
| `--card-padding-compact` | `var(--space-3)` — rails, dense lists |
| `--border-width-hairline` / `--border-width-default` | `1px` — default structural borders |
| `--section-gap-y` / `--section-gap-y-loose` | `var(--space-4)` / `var(--space-6)` — vertical section rhythm |
| `--icon-size-sm` / `--icon-size-md` | `0.75rem` / `1rem` — inline icons |
| `--shadow-xs` | Theme — very soft resting elevation for stacked Journey cards |
| Semantic colors in SCSS | Prefer `--color-*` from `theme.tokens.css`; inline/legacy components may use `--text-primary`, `--card-bg`, `--border-color`, etc. |
| Overlays | `--overlay-backdrop` → `--color-overlay-backdrop`; drawer elevation: `--shadow-elevated-panel`; focus ring: `--shadow-focus-ring-primary` |

Journey feature styles under `src/app/features/journey/` should use these tokens and `color-mix()` with `var(--primary-color)` / status colors **instead of** raw hex/rgba in component CSS.

---

## Journey dashboard (mode selection) — Phase 40

- **Page intro:** `h1` **Select Your Journey Mode** + subtitle **Choose the audit phase to focus on** in `journey.component` (`.journey-page-intro`), not in the app shell header.
- **Mode grid:** Desktop uses **four equal columns** (`.journey-mode-grid`: `repeat(4, minmax(0, 1fr))`); **two columns** from `max-width: 64rem`, **one column** from `max-width: 40rem`.
- **Active mode card:** Primary border plus soft glow via **`box-shadow`** + `color-mix()` on **`var(--color-brand-primary)`** (see `journey-card.component.scss`).
- **Mode icons:** Lucide via **`NgIcon`** + `provideIcons` on `JourneyCardComponent`; map lifecycle modes to **`lucideCheck`**, **`lucideSearch`**, **`lucideLock`**, **`lucideVideo`**.
- **Recommendation footer:** Pattern “**N** High Risk OE(s) need attention · Last activity: …” with count from **`overview.oesAtOrBelowThreeCount`** and relative time from optional **`snapshot.lastSyncedAt`** (ISO).

---

## Journey Execution — Context Panel (Phase 42)

- **BEM root:** `.journey-context` on `app-journey-context-panel`; inner scroll shell **`.journey-context__shell`** (gradient + `--shadow-sm`, `--radius-lg`).
- **Cards:** **`.journey-context__card`** — nested surfaces with `--shadow-xs`; three blocks: **OE** (`.journey-context__card--oe`), **Journey Progress** (`--progress`), **actions** (`--actions`).
- **Chips:** Department uses **`color-mix`** on **`--color-brand-primary`**; Role uses **`--color-chart-chapter-trend`** (purple family in theme); Frequency uses **`--color-status-success`**. No new chip-specific CSS variables required.
- **Progress bar:** Track `--color-bg-surface-alt`; fill gradient from **`--color-brand-primary`** / **`--color-status-warning`** via `linear-gradient` + `color-mix`.
- **Layout:** Right column host **`.execution-screen__panel--right`** is a flex column with **`min-block-size: 0`** so inner **`overflow-y: auto`** scrolls; quick actions grid **3 columns** desktop, **2 columns** at `max-width: 40rem`.
- **Icons:** Lucide via **`NgIcon`**; primary/secondary actions and quick tiles use **`--font-size-body`** / **`--font-size-heading-sm`** for icon size alignment.

---

## Scrollbar system (Phase 43)

**Source of truth:** `src/styles/tokens/foundation.tokens.css` (width, radius, motion, opacity scale) and `src/styles/tokens/theme.tokens.css` (thumb/track for light/dark + panel/dense/timeline variants). **Global rules:** `src/styles/scrollbar.css` (imported from `src/styles.css`).

### Principles

- **Thin and rounded:** use **`--scrollbar-width`** / **`--scrollbar-width-touch`** and **`--scrollbar-radius`** — no thick default bars or square thumbs.
- **Token-only:** scrollbars reference **`--scrollbar-track`**, **`--scrollbar-thumb`**, **`--scrollbar-thumb-hover`**, **`--scrollbar-thumb-active`**, **`--scrollbar-thumb-focus`**, and variant tokens (`--scrollbar-thumb-panel*`, `--scrollbar-thumb-dense*`, `--scrollbar-thumb-timeline*`). Do **not** set scrollbar colors with raw hex/rgb in feature SCSS.
- **One system:** only **context variants** differ (primary vs panel vs dense vs timeline), not per-module one-offs.
- **Native scroll:** prefer CSS-only enhancement; do **not** add JS scrollbar libraries for default app behavior.

### Utility classes

| Class | When to use |
|-------|-------------|
| **`praman-scrollbar`** | Any scrollable container (`overflow: auto` / `scroll`) that should use themed scrollbars on tablet/desktop. Sets `min-height: 0` / `min-width: 0` for flex children. |
| **`praman-scrollbar--panel`** | Secondary rails: side panels, filters, chapter lists, Journey context column. |
| **`praman-scrollbar--dense`** | Tables, OE lists, standards/OE stacks — slightly stronger thumb for scanability; pair row padding so actions are not obscured. |
| **`praman-scrollbar--timeline`** | Horizontal or mixed scroll regions; defaults to primary thumb tokens unless overridden later. |

**Focus:** `:focus-visible` on a **`praman-scrollbar`** host uses **`--shadow-focus-ring-primary`** for keyboard visibility.

### Responsive

- **≥ tablet (`640px`):** custom thin/touch widths per foundation breakpoints; **≤ mobile (`639px`)** and **coarse pointer on narrow viewports:** native scrollbars (`scrollbar-width` / `scrollbar-color` reset) so touch UX is not degraded.

### Forbidden

- Thick scrollbars, bright non-semantic thumb colors, sharp thumb geometry, ad-hoc scrollbar styling outside the utilities above.

---

## Journey — OE Execution Full-Screen Wizard (Phase 44)

**Authoritative UX:** `.planning/phases/44-oe-execution-fullscreen-wizard/44-DESIGN-SPEC.md` — structure, five steps, locking, context panel, responsive, accessibility.

### Entry

- Opened from **`Start Journey`** / **Execute Journey** on **`app-journey-context-panel`** when an OE is selected (`JourneyWorkspaceStore`).

### Shell (not a small dialog)

- **Full-viewport overlay workspace** — `position: fixed`, **`inset: 0`**, **`z-index`** from **`--z-modal`** (see `foundation.tokens.css`), not a centered **`max-width`** dialog.
- **Backdrop:** **`--overlay-backdrop`** (legacy **`--overlay-backdrop`** / semantic **`--color-overlay-backdrop`**).
- **Surface:** **`--card-bg`** / **`--color-bg-surface`** for the inner shell; borders and shadows per existing Journey card rules.

### Four regions

1. **Header** — breadcrumb (Framework → Chapter → Standard → OE), OE code + title, status badge, close (**unsaved** → confirm before dismiss).
2. **Stepper** — **Understand → Execute → Evaluate → Improve → Complete**; completed steps show check affordance; locked steps are not navigable until unlocked.
3. **Main** — step body only; scroll with **`praman-scrollbar`** utilities.
4. **Right context** — OE summary, progress, **Next action** copy, quick actions (Upload evidence, Add task, View timeline).

### Step locking (plain language)

- **Execute** after **Understand** is acknowledged.
- **Evaluate** after **minimum execution** gate (tasks/evidence rules — product-defined).
- **Improve** only when evaluation indicates **gaps** (e.g. partial / non-compliant); compliant path may **skip** straight to **Complete** from **Evaluate**.
- **Complete** when prerequisites are met (no open blocking issues when Improve applies).

### Responsive

- **Desktop:** main + fixed right context (two-column body under stepper).
- **Tablet:** context may **collapse** to slide-over / drawer; stepper stays visible — use **`--bp-tablet-min`** / **`--bp-desktop-min`** from foundation tokens.
- **Mobile:** stacked layout; stepper **horizontal scroll** or compact; context **bottom sheet** or collapsible — avoid hiding **Next action** without a toggle.

### Accessibility (non-negotiables)

- **Focus trap** inside the overlay while open; restore focus to the invoking control on close (**CDK** `cdkTrapFocus` or equivalent).
- **Escape** requests close — same **unsaved** confirm as the close button.
- **Wizard / stepper:** logical heading order; **`aria-current`** on the active step; progress **`role="progressbar"`** or equivalent where appropriate.

### Forbidden

- Small centered modal for this flow; hardcoded **hex/px** in feature SCSS; one-off stepper styling outside tokens and this section.

---

## Journey — OE Execution Wizard — Evidence step (Phase 58)

Applies to **`app-oe-execution-wizard`** step **Evidence** (inline compliance wizard, Phase 50 shell).

### Uploaded Evidence (server-backed)

- **Data:** Rows come from **`ObjectiveElement.evidence`** on the selected OE (workspace snapshot), sorted by **`uploadedAt`** descending. **Byte size** is not shown in the list until Gateway exposes it on list items; the UI shows a **type label** derived from the filename (PDF / Excel / Word / Document).
- **Layout:** Inside the evidence card, **Uploaded Evidence** appears **above** the dashed **drop zone**. Each row: **file-kind icon** (Lucide via `NgIcon`), **filename** + **upload timestamp** (`DatePipe`), **type label**, **Download** (calls **`GET …/evidence/{id}/download`** and saves the blob with the row’s **`name`**).
- **Pending selection:** Files chosen but not yet POSTed use the **Ready to upload** label and the existing bordered row + remove pattern — visually separate from server-backed rows.

### Read-only audit phases

- When upload is disabled (**readonly** / **allStepsReadOnly**), the drop zone and pending queue stay non-interactive as before; **Download** on already-uploaded rows remains available for review.

### Styling

- **Token-only** classes in `oe-execution-wizard.component.scss` (`oe-wizard__uploaded-*`); no new product hex in feature CSS.

---

## Component dimensions (patterns)

`foundation.tokens.css` defines **`--button-height`** and **`--input-height`** (2.5rem) for shared controls. Documented **patterns from Compliance Command Center** (literal px reference):

- **Primary button (header refresh):** `padding: 10px 18px`, `font-size: 14px`, `font-weight: 600`, `border-radius: 6px`, icon **16px**.
- **Status pill:** `padding: 8px 14px`, `font-size: 13px`, `border-radius: 6px`, status dot **6px**.

Other routes (tasks, knowledge graph, copilot) define their own heights; reuse their literals when extending those screens.

---

## Responsive behavior and tablets

Breakpoints **defined in this feature** (from `@media` in `src/app/features/control-center/`):

| Breakpoint | Files (examples) |
|-------------|-------------------|
| `max-width: 1400px` | `compliance-command-center.component.ts` — bottom grid stacks |
| `max-width: 1200px` | `task-command-center.component.ts` |
| `max-width: 1024px` | `compliance-analytics`, `risk-command-center`, `knowledge-graph` |
| `max-width: 768px` | Compliance page, `audit-readiness`, `action-center`, `executive-kpi-bar`, `knowledge-graph`, `task-command-center` |
| `max-width: 480px` | `ai-copilot.component.ts` — panel sizing |

**Tablet rule:** layouts must remain usable on **all common tablet widths** (portrait and landscape). Use the breakpoints above and flex/grid patterns already present in Control Center components; **avoid fixed widths** unless overridden by a matching `@media` rule. Where a breakpoint is not defined for a new pattern: **Value derived from Control Center UI – maintain consistency.**

---

## Global token files (resolution only)

- `src/styles/tokens/foundation.tokens.css` — base scales where referenced by the build.
- `src/styles/tokens/theme.tokens.css` — **semantic `--color-*`, `--shadow-*`, scrollbar tokens (`--scrollbar-*`), and legacy aliases** used by Control Center `var(...)` references.
- `src/styles/scrollbar.css` — themed scrollbar rules (Firefox + WebKit); depends on tokens above.
- `src/styles/design-tokens.scss` — SCSS mirror where components import SCSS variables.

New Control Center–aligned UI should **reuse the legacy variables listed above** (or the semantic tokens they map to) so theme switching stays correct.

---

## Usage Rules

1. **Reuse** the CSS variables and spacing/radius magnitudes observed in Control Center before introducing new literals.
2. **Do not** introduce new colors or spacing in isolation for Journey/Compliance-aligned surfaces without checking **this document** and the **Compliance Command Center** implementation.
3. **Maintain visual consistency** across modules by aligning to these tokens and patterns.
4. **Theme:** light/dark behavior comes from `data-theme` and `theme.tokens.css`; do not hardcode colors that ignore theme.
5. **Responsive:** follow existing breakpoints and stacking behavior; test tablet widths.

---

## Architecture and engineering (unchanged expectations)

- **Atomic design** (atoms → templates) and **reusable** Angular components remain the goal.
- **No inline CSS** in new code where a shared stylesheet or token is available; Control Center currently uses inline `styles` in components — **new shared abstractions** should prefer external SCSS/CSS referencing the same variables.
- **State, auth, i18n, performance, security** rules from the broader app still apply.

---

## Migration note (optional follow-up)

A future refactor may replace legacy `var(--bg-primary)` etc. with direct `--color-*` usage in Control Center components. After such a change, **re-run an inventory** of `src/app/features/control-center/` and update this file.

---

## App shell — left sidebar (operational command center)

**Implementation:** `sidebar.component.ts`, `sidebar-nav.model.ts`, `sidebar-icons.registry.ts`, `bottom-nav.component.ts` (mobile), `shell-operational-mode.service.ts`.

### Structure

- **Brand strip:** hospital/org summary + **collapse** control (persists `localStorage` key `praman-sidebar-collapsed`).
- **Context card:** operational mode (session: `praman-operational-mode`), optional **compliance progress** (placeholder until API).
- **Navigation (icon-first):** **`matRipple`** on rows; **icons** sized with **`--icon-size-nav`** (foundation); **labels** use class **`.nav-label`**. Registry: **`getSidebarMenuSections()`**; SVG snippets: **`sidebar-icons.registry.ts`**.
- **Collapsed rail:** **`:host(.sidebar-host--collapsed)`** → **72px** rail (labels hidden, **`matTooltip`** enabled). Expand/collapse is **toggle only** (no hover-to-open).
- **Quick actions:** **Task / Evidence / Finding** icon tiles (`role="toolbar"`, **`aria-label="Quick actions"`**) above the scrolling nav; same routes as main list; registry icons + **`routerLinkActive`** (`.quick-action--active`).
- **Mobile bottom bar:** **`getBottomNavItems()`** (same routes as sidebar subset), fixed **`bottom: 48px`**, **`z-index: var(--z-shell-bottom-nav)`**; **`main`** gets **`padding-bottom: calc(var(--shell-bottom-nav-height) + env(safe-area-inset-bottom))`**.

### Visual / tokens

- Context shell uses **`--color-bg-surface`**, **`--color-border-subtle`**, **`--shadow-xs`** — no ad-hoc chrome.
- **Journey** row uses **`.menu-item--primary`** (border + soft brand fill).
- **Action Center** badges: **`navBadgeKey`** (`findings` | `issues` | `alerts`) → **`resolveNavBadge()`**; variants **`.nav-badge--neutral` | `--warning` | `--critical`**.
- **Touch:** **`--touch-target-min`** on nav rows / bottom tabs (**≥ ~44px**).
- **Active** state: existing **left accent** + **`--color-brand-primary-soft`** (unchanged pattern).

### Tooltips

- **`matTooltipDisabled`** when labels are visible: **expanded** sidebar or **mobile overlay** drawer (full labels).

### Motion

- Width/transition respects **`prefers-reduced-motion: reduce`** for sidebar width.

### Accessibility

- **Landmarks:** single `aside` for shell nav; **`<nav aria-label="Application">`** for module list; bottom **`app-bottom-nav`** has `aria-label` on `<nav>`.
- **Focus trap:** **`cdkTrapFocus`** on mobile overlay when drawer open (`ShellSidebarService`).
- Nav links: **`aria-label`** from item (`ariaLabel` or `label`).
- Disabled rows: **`aria-disabled`**, **`title`** / **`aria-description`** for reason.

### Extending navigation

- Add or reorder entries in **`sidebar-nav.model.ts`**; keep **`section.id`** stable; prefer **≤ ~8 items** per section before splitting. For mobile shortcuts, update **`getBottomNavItems()`** (keep **≤ 5** tabs).

## Top header navigation overflow

- The **header top nav** now keeps a single row on narrow desktop widths by measuring each module trigger and moving overflowed sections into a reusable **“More” dropdown** (`src/app/core/layout/header-top-nav.component.ts`). The inline row still uses the same module order so cognitive load stays constant.
- The overflow panel reuses `header-top-nav__link` styles and the same disabled/link semantics, so keyboard users and assistive tech see identical content regardless of whether a section is inline or inside More.
- This mechanism relies on measured widths from the hidden `.header-top-nav__measurement` container, so avoid adding non-layout-affecting content to the module triggers unless the measurement clones are updated to match.

---

## Verification checklist (Tasks 1–4)

- [x] Inventory: `var(--*)` and key literals gathered from `src/app/features/control-center/**/*.component.ts`.
- [x] Tokens listed match usage (see tables above); `--edge-color` scoped to knowledge graph.
- [x] Breakpoints copied from existing `@media` rules.
- [x] No standalone hex values presented as Compliance-only constants except via **theme.tokens.css** resolution path.
