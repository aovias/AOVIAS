# AccredaOne Application Shell v1.0

Navigation foundation for the single-page engineering workspace (`/`). Implements Phase 2A header and sidebar on top of **Design System v1.0 (closed baseline)**.

## Component responsibilities

```text
AppShell (components/AppShell.tsx)
├── AppHeader (components/AppHeader.tsx)
│   ├── Brand mark + name + descriptor
│   └── Global discovery search entry
├── AppSidebar (components/AppSidebar.tsx)
│   ├── Collapse control (desktop/tablet ≥961px)
│   └── Navigation groups + items
└── MainContent (.ao-main)
    ├── app/page.tsx workspace sections
    └── AppFooter (components/AppFooter.tsx) — compact product identity + engineering footnote
```

Supporting modules:

- `lib/shell/navigation.ts` — IA and hash routes
- `lib/shell/useLocationHash.ts` — active section detection
- `lib/shell/discoverySearch.ts` — header search → `#library` + query prefill

Styles: `application/frontend/app/globals.css` (`.ao-header*`, `.ao-sidebar*`, `.ao-nav*`).

Icons: **Lucide React** (consistent 20px navigation, 18–22px header).

## Header structure

| Zone | Content |
| ---- | ------- |
| Left | Mobile menu (≤960px), brand link (`#overview`), Shield mark, “AccredaOne”, descriptor “Knowledge & accreditation intelligence” |
| Centre | Global search — submits to discovery (`#library`, focuses `#discovery-query-input`, optional prefill via `sessionStorage`) |
| Right | “Foundation preview” badge (non-interactive engineering label) |

**Omitted controls (no product capability yet):** notifications, help/docs link, user profile / auth menu. Do not add decorative controls until backends exist.

Height: `--ao-header-height` (64px). Sticky, `z-index` above sidebar drawer backdrop.

## Sidebar information architecture

Implemented hash navigation only:

| Group | Item | Hash |
| ----- | ---- | ---- |
| Workspace | Overview | `#overview` |
| Workspace | Presentation states | `#states` |
| Discovery | Knowledge discovery | `#library` |

**Future candidates (not in nav — no routes):**

- Framework Explorer, Knowledge Library (duplicate surface), Saved Knowledge
- Learning Centre, Announcements
- Settings / Account

## Navigation behaviour

- **Active state:** `aria-current="page"` when `window.location.hash` matches item (default `#overview` when empty).
- **Hover / focus:** token-backed soft teal background; focus ring `--ao-border-focus`.
- **Desktop collapse:** toggle narrows sidebar to `--ao-sidebar-collapsed-width` (72px); icons remain with `title` tooltips.
- **Mobile (≤960px):** sidebar is off-canvas drawer; header menu opens; backdrop click, Escape, or nav selection closes; `html.ao-scroll-lock` prevents background scroll.

Breakpoint: existing **960px** shell breakpoint (unchanged).

## Responsive summary

| Viewport | Header | Sidebar |
| -------- | ------ | ------- |
| Desktop (≥961px) | Full brand + search + badge | Persistent column; collapsible |
| Tablet (768px) | Menu + shortened brand + search | Drawer pattern at ≤960px |
| Mobile (390px) | Same as tablet | Drawer; full-width search |

## Accessibility

- Landmark: `<header>`, `<aside aria-label="Application navigation">`, `<main id="main-content">`, `<footer>` (end of main scroll area; does not span sidebar)
- Menu button: `aria-expanded`, `aria-controls="app-sidebar"`
- Collapse button: descriptive `aria-label`
- Keyboard: focus visible on all shell controls; Escape closes mobile drawer
- Search: visible label (visually hidden) for screen readers

## Future extension points

1. Add nav items in `APP_NAVIGATION` when real routes/sections exist.
2. Wire header utilities (notifications, help, profile) to authenticated services.
3. Replace hash-only routing when App Router pages are introduced — keep token classes.
4. Optional: persist sidebar collapsed preference in `localStorage`.

## Design system relationship

All colours, spacing, typography, and elevation come from `:root` tokens in `globals.css`. Tailwind v4 `@theme inline` utilities may be used in new TSX but must reference the same CSS variables.
