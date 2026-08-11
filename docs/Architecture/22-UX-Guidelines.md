# UX Guidelines

[← README](./README.md) · [DESIGN_RULES.md](../../WebApp/DESIGN_RULES.md)

---

## Design Philosophy

**Enterprise healthcare command center** — dense but scannable, dark-first, token-driven, audit-friendly (status badges, trails, clear hierarchy).

**Source of truth for visuals:** Control Center patterns + `theme.tokens.css`.

---

## Layout Patterns

| Pattern | Usage |
|---------|-------|
| Shell + sidebar | All `/app/*` routes |
| Command center | Findings DCC, Control Center, Copilot workspace |
| Wizard / drawer | OE execution, task drawer, evidence drawer |
| Hub with sections | Analytics Hub, Copilot Analytics sub-tabs |

---

## Tokens

- Semantic: `--color-bg-page`, `--color-bg-surface`, `--text-primary`, `--border-color`
- Legacy aliases: `--card-bg`, `--bg-secondary` (mapped to semantic)
- Theme switch: `data-theme='light'|'dark'` on `documentElement`

**Copilot accent:** purple (`#a78bfa`) for persona chrome — intentional product accent on shared surfaces.

---

## Components

| Element | Guideline |
|---------|-----------|
| Cards | `var(--card-bg)`, border `var(--border-color)`, radius from foundation tokens |
| Tables | `TableComponent` or feature tables with sticky headers |
| Badges | `StatusBadgeComponent`, severity-colored chips |
| Dialogs | `DialogComponent`, Material dialogs with token overrides |
| Empty states | Dashed border, secondary text, primary CTA |
| Loading | Skeleton shimmer or `LoadingService` overlay |

---

## Typography

- Base: `var(--font-family-base)` from foundation tokens.
- Page titles: 26px bold (Copilot/Dashboard pattern).
- Section titles: 18px / 15px hierarchy.

---

## Accessibility

- Material components for form controls.
- `aria-label` on icon buttons.
- Tab nav with `aria` on Copilot workspace.
- **Gap:** full WCAG audit not documented — treat as ongoing.

---

## AI UX

| Surface | Pattern |
|---------|---------|
| Copilot workspace | Structured tabs, no fake autonomy |
| Sidebar AI | Conversational chips, page context injection |
| Suggested questions | Chip buttons → send prompt to sidebar |

Never imply AI executed an action without showing proposal/approval state.

---

## Navigation Consistency

- Sidebar is canonical module list — do not duplicate full nav inside features.
- Deep links from Copilot recommendations must use valid `/app/*` routes.

---

## Responsive

- Mobile: bottom nav, sidebar overlay.
- Copilot: glance grid collapses to 1 column < 520px.
- Tables: horizontal scroll or card fallback per feature.
