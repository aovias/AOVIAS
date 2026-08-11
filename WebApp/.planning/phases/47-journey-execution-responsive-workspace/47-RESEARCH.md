# Phase 47 — Research: Journey Execution responsive workspace

## RESEARCH COMPLETE

### Scope lock

- UI/layout/interaction/styling only; no new product features; no API or store data contracts except optional **UI-local** signals for panel open/close.
- Reference: `DESIGN_RULES.md`, `src/styles/tokens/*`.

### Current implementation map

| Area | Primary files |
|------|----------------|
| Execution shell | `execution-screen.component.html`, `execution-screen.component.scss`, `execution-screen.component.ts` |
| Chapter nav | `execution-chapter-panel.component.*` |
| Standards + OE | `execution-standards-panel.component.*`, `standard-card/*` |
| Context | `journey-context-panel.component.*` |
| App shell | `shell.component.ts`, `sidebar.component.ts`, `shell-sidebar.service.ts` |

### Layout facts (code)

- Desktop: `.execution-screen__workspace` uses **3-column** grid with fractional columns; panels have `overflow: hidden` and `block-size: 100%`.
- **≤64rem**: grid collapses to **single column**; **≤40rem**: each panel gets **`min(40vh, 28rem)`** fixed block height — risks **crowding**, **triple scroll**, and **standards feeling cramped/hidden**.
- **Shell**: `ShellSidebarService` is overlay mode **≤767px** with **backdrop** in sidebar template; **main-content** scrolls independently — **z-index/stacking** between sidebar, backdrop, and page content must be verified on Journey route.

### UX gaps vs goal

1. **No adaptive strategy** beyond single-column stack; **priority** (standards first) not encoded.
2. **No** tablet-specific **context** treatment (slide-over / collapsible).
3. **Chapter** list density and **hover/active** affordances need strengthening per tokens.
4. **Interaction** on OE rows / buttons — align with existing patterns; ensure **focus-visible** and **touch** spacing.

### Breakpoint strategy (proposal for plans)

- **Desktop** (e.g. `> 64rem` or `> 90rem`): 3 columns; independent scroll per panel.
- **Tablet** (e.g. `48rem`–`64rem` or `64rem`–`90rem`): 2 columns (left + center) + **context** as overlay/drawer triggered by **existing or new** local UI toggle (no new data).
- **Mobile** (`≤ 48rem` or `≤ 40rem`): **stack**; **single primary vertical scroll** on outer workspace where possible; context **bottom sheet** or **expandable section**.

### Risk

- **Over-fixing z-index** globally may break modals; scope **shell + execution** first.

---

## RESEARCH COMPLETE
