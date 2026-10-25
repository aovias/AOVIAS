# Responsive Audit — Sprint R1.UI.7

**Date:** 2026-07-24  
**Scope:** AOVIAS Website (`apps/website`) — production readiness across 320px → 3440px  
**Goal:** Intentional layouts on every modern device (not merely “responsive”)

---

## Summary

Mobile navigation was redesigned as a portaled drawer with overlay, ESC, click-outside, body scroll lock, focus trap, and 44px touch targets. Site-wide gutters, footer collapse, hero CTAs, button/input hit areas, and shared responsive utilities were tightened to remove overlap, clipping, and awkward stacking.

---

## Findings by severity

### Critical

| Finding | Resolution |
| --- | --- |
| Mobile nav nested under fixed header (`z-50`) caused stacking / overlap with page chrome | Drawer now portals to `document.body` at `z-[100]` with full-screen backdrop |
| Weak body scroll lock (`overflow: hidden` only) allowed iOS background scroll | New `useBodyScrollLock` — fixed body, scrollbar gap, restore scroll position |
| Sub-44px chrome controls (theme, search, hamburger, social) failed touch a11y | Raised to `min-h-11` / `size-11` across header, drawer, footer, search overlay |

### High

| Finding | Resolution |
| --- | --- |
| Footer dumped all 6 link columns into a single mobile column | Progressive grid: 1 → `sm:2` → `md:3` → `lg:7` with brand spanning early breakpoints |
| Logo crowded hamburger/theme on ≤360px | Fluid logo max-widths (`8.25rem` → `10.5rem` → `15rem`) |
| Hero CTAs wrapped awkwardly on narrow phones | Full-width CTAs below `sm`, auto width from `sm` up |
| Container gutters tight at 320px | `px-3` below 360px, then `px-4` / `sm:px-6` / `lg:px-8` |
| Desktop nav hit areas short (`h-9`) | Primary + mega-menu triggers use `h-11` + `whitespace-nowrap` |

### Medium

| Finding | Resolution |
| --- | --- |
| Decorative footer glow used fixed `42rem` width | Capped with `w-[min(42rem,100vw)]` |
| Card titles fixed at `1.5rem` felt large on small phones | Fluid `--font-size-card-title` clamp |
| Healthcare demo cards over-padded on mobile | `p-4 sm:p-6` |
| Search overlay close control below 44px | `size-11` close button |
| Duplicated responsive patterns | Shared utilities: `.touch-target`, `.responsive-media`, `.responsive-embed`, `.section-pad-x`, `.break-words-safe` |

### Low

| Finding | Resolution / note |
| --- | --- |
| Comparison tables remain horizontally scrollable on small screens | Intentional (`overflow-x-auto` + `min-w`) — documented as known limitation |
| Ultra-wide (>1760px) uses content max-width | By design (`--layout-hero-max: 1760px`) — prevents stretched lines |
| Cross-browser Safari/Firefox visual parity | Layout CSS is standards-based; full device matrix still recommended in QA |

---

## Pages reviewed

| Page / surface | Status |
| --- | --- |
| Home | ✓ |
| Products (index + detail) | ✓ |
| Solutions (index + detail) | ✓ |
| Healthcare (industry editorial + demos) | ✓ |
| Technology (hub + subpages) | ✓ |
| Resources / Library / Docs | ✓ |
| Company (+ contact / careers) | ✓ |
| Pricing | ✓ |
| Documentation (`/resources/docs`) | ✓ |
| Search (overlay + knowledge search) | ✓ |
| 404 / system states | ✓ |
| Auth forms | ✓ |
| Legal | ✓ |
| Checkout flow | ✓ |
| Dialogs / drawers / dropdowns | ✓ (mobile drawer redesign; mega-menu desktop) |
| Footer / Navigation | ✓ |

---

## Components reviewed

- `MobileDrawer` / `SiteHeader` / `HeaderChrome` / `MainNav` / `MegaMenu`
- `SiteFooter` / `UtilityNav` / `SearchEntry` / `EnterpriseSearchOverlay`
- `Hero` / `ProductHero` / marketing sections
- Buttons, IconButton, Input, ThemeToggle
- Brand SVGs / diagrams (aspect boxes + `overflow-hidden`)
- YouTube embeds (`HealthcareVideoCard` / `DemoVideoGrid`)
- Pricing comparison tables, billing toggle
- Layout `Container` / `PageContainer`

---

## Breakpoints tested

| Band | Widths | Notes |
| --- | --- | --- |
| Mobile | 320, 360, 375, 390, 412, 430, 480 | Nav drawer, hero stack, footer collapse, gutters |
| Tablet | 768, 820, 834, 912, 1024 | Grid columns, mega-menu at `lg` |
| Laptop | 1280, 1366, 1440 | Desktop nav density |
| Desktop | 1600, 1920, 2560 | Hero max-width, illustration balance |
| Ultra-wide | 3440 | Centered content, no stretch |

---

## Implementation highlights

1. **Mobile drawer** — portal, slide-from-right, backdrop, close button, ESC, click-outside, focus trap, scroll lock, accordion sections, safe-area insets.
2. **Touch system** — default Button `md`/`lg` and IconButton defaults meet 44px; inputs `h-11`.
3. **Overflow** — `html`/`body` `overflow-x: clip`; heroes/footer clip decorative fields.
4. **Motion** — drawer animations honor `prefers-reduced-motion`.
5. **Theme** — ThemeToggle hit area aligned with other chrome controls.

---

## Definition of Done checklist

- [x] No overlapping navigation
- [x] Mobile menu redesigned
- [x] Zero intentional horizontal page scroll (tables may scroll locally)
- [x] Responsive 320px → 3440px
- [x] Consistent spacing / token gutters
- [x] Typography scales via fluid tokens
- [x] Illustrations scale in aspect containers
- [x] Video embeds 16:9 responsive
- [x] Footer collapses gracefully
- [x] Accessible navigation (focus trap, ESC, labels)
- [x] Cross-browser compatible CSS
- [x] CLS minimized (fixed header spacer, aspect boxes, lazy iframes)
- [x] Production-ready polish

---

## Screenshots

| Asset | Description |
| --- | --- |
| [`responsive-audit-assets/mobile-drawer-after.png`](./responsive-audit-assets/mobile-drawer-after.png) | Redesigned mobile drawer (375px) — opaque panel, Menu header, Search, accordion sections, close control |

---

## Known limitations

1. **Wide data tables** (pricing / technical specs) use contained horizontal scroll below ~640–768px rather than crushing columns.
2. **Mega-menus** are desktop-only (`lg+`); mobile uses accordion drawer instead.
3. **Ultra-wide** keeps a max content width — side margins grow; this is intentional for readability.
4. **Native browser UI** (autofill, select pickers) varies by OS; forms use 44px fields to reduce keyboard awkwardness.

---

## Future improvements

1. Visual regression suite (Playwright / Percy) at the widths listed above.
2. Optional condensed mega-menu on `md` tablets if product wants hover menus before `lg`.
3. Container queries for card grids inside constrained columns.
4. Automated axe + Lighthouse CI on critical routes after each UI sprint.
5. Screenshot archive under `docs/ui/responsive-audit-assets/` for release notes.

---

## Key files touched

- `apps/website/src/components/navigation/mobile-drawer.tsx`
- `apps/website/src/lib/navigation/use-body-scroll-lock.ts`
- `apps/website/src/lib/navigation/use-focus-trap.ts`
- `apps/website/src/components/navigation/site-header.tsx`
- `apps/website/src/components/navigation/site-footer.tsx`
- `apps/website/src/components/navigation/site-logo.tsx`
- `apps/website/src/components/marketing/hero.tsx`
- `apps/website/src/components/primitives/controls/{button,icon-button,input}.tsx`
- `apps/website/src/components/primitives/layout/container.tsx`
- `apps/website/src/styles/globals.css`
- `packages/design-system/src/react/theme-toggle.tsx`
- `packages/design-system/src/css/tokens.css`
