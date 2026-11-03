# AccredaOne Design System v1.0

**Status: CLOSED — APPROVED BASELINE**

Central visual foundation for AccredaOne. **All future UI work must consume the design tokens defined here.** New hardcoded colours, spacing, or typography require an explicit design-system decision.

Phase 2 shell components (header, sidebar) build on this baseline; see `ACCREDAONE_APPLICATION_SHELL_V1.md`.

## Visual direction

- Deep teal and professional navy brand identity
- Muted teal and turquoise accents
- Warm white and subtle neutral surfaces
- Editorial serif typography for headings (Georgia stack)
- Modern sans-serif typography for application UI (Inter / system stack)
- Restrained borders, shadows, and rounded corners
- Professional enterprise SaaS appearance

Light mode only in v1.0 (no dark theme).

## Canonical tokens

Authoritative definitions live in `application/frontend/app/globals.css` under `:root`. Do not duplicate hex values elsewhere.

### Brand

| Token | Value |
| ----- | ----- |
| `--ao-brand-primary` | `#126E70` |
| `--ao-brand-primary-hover` | `#0F5D60` |
| `--ao-brand-primary-active` | `#0B4F52` |
| `--ao-brand-deep` | `#123B46` |
| `--ao-brand-muted` | `#527078` |
| `--ao-brand-soft` | `#D2E5E6` |

### Surfaces

| Token | Value |
| ----- | ----- |
| `--ao-surface-page` | `#F4F7F6` |
| `--ao-surface-sidebar` | `#F1F5F4` |
| `--ao-surface-card` | `#FFFFFF` |
| `--ao-surface-subtle` | `#E5F0EE` |
| `--ao-surface-inverse` | `#123B46` |

### Text

| Token | Value |
| ----- | ----- |
| `--ao-text-primary` | `#183B45` |
| `--ao-text-secondary` | `#536B70` |
| `--ao-text-muted` | `#566D71` |
| `--ao-text-inverse` | `#FFFFFF` |
| `--ao-text-link` | `#126E70` |

### Borders

| Token | Value |
| ----- | ----- |
| `--ao-border-default` | `#D5E1DE` |
| `--ao-border-strong` | `#BCCDC9` |
| `--ao-border-focus` | `#126E70` |

### Semantic status

| Token | Role |
| ----- | ---- |
| `--ao-status-success` / `--ao-status-success-bg` | Success text / soft background |
| `--ao-status-warning` / `--ao-status-warning-bg` | Warning |
| `--ao-status-error` / `--ao-status-error-bg` | Error / destructive |
| `--ao-status-info` / `--ao-status-info-bg` | Informational |

### Typography

- **Headings:** `var(--ao-font-heading)` — page titles, major section headings, brand title
- **Body / UI:** `var(--ao-font-body)` — navigation, buttons, forms, tables, metadata

Scale: `--ao-text-xs` through `--ao-text-display` (see `globals.css`).

No required network font loading; Inter is listed first in the body stack and falls back to system UI fonts.

### Spacing

4px base, rem-based: `--ao-space-1` (4px) through `--ao-space-12` (48px).

### Radius and elevation

- Radius: `--ao-radius-sm` … `--ao-radius-full`
- Shadows: `--ao-shadow-sm`, `--ao-shadow-md`, `--ao-shadow-lg`

### Layout

| Token | Default |
| ----- | ------- |
| `--ao-header-height` | 64px |
| `--ao-sidebar-width` | 248px |
| `--ao-sidebar-collapsed-width` | 72px |
| `--ao-content-max-width` | 1440px |

### Motion

- `--ao-transition-fast` (150ms)
- `--ao-transition-normal` (200ms)

Respect `prefers-reduced-motion` (global override in `globals.css`).

## Tailwind CSS v4 integration

Tailwind is enabled via `@import "tailwindcss"` and `@tailwindcss/postcss` in `postcss.config.mjs`.

Semantic utilities are mapped in `@theme inline` to the same CSS variables (for example `bg-ao-brand-primary`, `text-ao-text-secondary`, `border-ao-border-default`, `shadow-ao-md`, `rounded-ao-lg`, `p-ao-4`, `font-heading`, `font-body`).

**CSS custom properties remain canonical.** Tailwind theme entries reference `var(--ao-…)`; do not define a second palette in JavaScript config.

### Token usage requirements

1. Use `:root` tokens or legacy aliases that resolve to them — never new raw hex in components.
2. Prefer canonical names (`--ao-text-muted`) over deprecated aliases (`--ao-muted`) in new code.
3. Tailwind utilities are optional sugar; changing a token in `:root` must update all consumers.
4. Shell, discovery, and presentation UI must stay on v1.0 tokens through Phase 2+.

## Component standards

Shared classes in `globals.css`:

| Pattern | Classes |
| ------- | ------- |
| Buttons | `.ao-btn`, `.ao-btn-primary`, `.ao-btn-secondary`, `.ao-btn-tertiary`, `.ao-btn-destructive` |
| Badges | `.ao-badge`, `.ao-badge-success`, `.ao-badge-warning`, `.ao-badge-error`, `.ao-badge-info` |
| Links | `.ao-link` |
| Forms | `.ao-input` (+ discovery field styles aligned to tokens) |
| Shell | `.ao-header`, `.ao-sidebar`, `.ao-main`, `.ao-card`, `.ao-panel` |

Existing discovery and presentation components use legacy `.ao-*` names backed by **legacy alias variables** (`--ao-accent`, `--ao-ink`, etc.) that resolve to v1.0 tokens.

## Accessibility (WCAG 2.2 AA target)

- Focus: 2px `--ao-border-focus` outline on interactive controls
- Minimum 44px touch targets on primary discovery actions
- Disabled controls: reduced opacity + `not-allowed` cursor
- Use semantic HTML; status communicated with colour plus text

### Final contrast validation (v1.0 closure)

| Pair | Ratio | WCAG 2.2 AA (normal text) |
| ---- | ----- | ------------------------- |
| `--ao-text-primary` on `--ao-surface-page` | 11.13:1 | Pass |
| `--ao-text-secondary` on `--ao-surface-page` | 5.26:1 | Pass |
| `--ao-text-muted` on `--ao-surface-page` | **5.10:1** | **Pass** |
| `--ao-text-muted` on `--ao-surface-sidebar` | **5.00:1** | **Pass** |
| `--ao-text-inverse` on `--ao-brand-primary` | 6.02:1 | Pass |
| `--ao-text-link` on `--ao-surface-page` | 5.58:1 | Pass |
| `--ao-brand-primary` on `--ao-brand-soft` | 4.61:1 | Pass |
| `--ao-status-error` on `--ao-status-error-bg` | 4.58:1 | Pass |

**Closure note:** `--ao-text-muted` was updated from `#71868A` to **`#566D71`** (Phase 2A) to meet 4.5:1 on light surfaces. All other approved brand colours unchanged.

## Extending the theme

1. Propose new token names under `:root` (do not hardcode hex in components).
2. Add matching `@theme inline` entries if Tailwind utilities are needed.
3. Update this document; major changes require a new design-system version, not silent drift.

## Legacy aliases (deprecated)

These remain for discovery and older shell class names:

`--ao-ink`, `--ao-accent`, `--ao-canvas`, `--ao-line`, `--ao-surface`, `--ao-surface-muted`, `--ao-header`, `--ao-sidebar`, `--ao-radius`, `--ao-shadow`, etc.

New features should use canonical `--ao-*` names.
