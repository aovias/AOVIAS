# Phase 43 — Research: Themed Scrollbar System

## What planners need to know

### Platform and standards

- **Firefox** supports the **standard** properties `scrollbar-width` and `scrollbar-color` (track and thumb). These are the preferred mechanism for Firefox and align with a “minimal JS” approach.
- **Chromium / Safari (WebKit)** use the **`::-webkit-scrollbar` family** of pseudo-elements for full visual control (width, radius, thumb/track). There is no single standard that covers both WebKit and Firefox with identical selectors; a **dual-track strategy** (CSS standard + WebKit) is the norm for cross-browser themed scrollbars.
- **Overlay scrollbars** (common on macOS) may reduce how often the track is visible; design should assume **idle subtlety** and **interaction-based visibility** rather than relying on a permanently visible track.

### Mobile and touch

- Forcing custom scrollbar visuals on **small viewports** or **coarse pointers** can harm touch UX (smaller content area, gesture conflicts). Industry practice is to **defer to native overflow behavior** on mobile-sized breakpoints or when `pointer: coarse` applies, unless product explicitly requires otherwise.

### Layout and stability

- **`scrollbar-gutter: stable`** (where supported) can reserve space for the scrollbar and reduce layout shift when content becomes scrollable. Whether to enable it globally is a product/layout decision (trade-off: permanent gutter vs. jump).

### Accessibility

- **Keyboard scrolling** does not require a visible scrollbar thumb if the scroll container is focusable and scrollable; however, **focus-visible** styling on the scroll region (or an inner focusable child) should remain clear when navigating with the keyboard.
- **Reduced motion:** transitions on thumb/track opacity or color should respect **`prefers-reduced-motion`** by collapsing to instant state changes or near-zero duration.

### Performance

- **CSS-only** scrollbar styling is lightweight. **JS scrollbars** (custom div-based) add observers, listeners, and bundle size — excluded by PRAMAN constraints unless a future exception is documented.

### Design tokens

- All colors must map to existing **semantic** tokens (`--color-*`, legacy aliases like `--bg-tertiary`, `--border-color`, `--primary-color`) via **`color-mix()`** where transparency is needed — consistent with `DESIGN_RULES.md` and Phase 38 Journey patterns.

---

## RESEARCH COMPLETE

Findings are sufficient to author **token-backed** scrollbar behavior specs and phased rollout plans without prescribing implementation code in the design documents.
