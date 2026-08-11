# Global Search & Command Palette

## Shortcut

- **Ctrl+K** / **Cmd+K** — open/close command palette
- **Esc** — close
- **↑ / ↓** — move highlight
- **Enter** — open selection

Owned by [`KeyboardShortcutsService`](../../src/app/core/navigation/shortcuts/keyboard-shortcuts.service.ts). Compliance `GlobalOpsSearch` no longer owns Ctrl+K.

---

## Sources

| Source | Status |
|--------|--------|
| Navigation pages (registry) | Live via `SearchIndexService` |
| Modules | Live from `headerModules()` |
| Actions / Settings commands | Static `COMMAND_ACTIONS` |
| Ops / Entities | Live compliance API when query ≥ 2 chars |
| Departments, Audits, Reports, Documents, Users | Placeholders (future APIs) |

Empty query shows **Recent Searches** and **Suggested Pages**.

---

## Index cache

`SearchIndexService.index` is a computed signal rebuilt when Query surface trees change (roles/features). Keystrokes only filter the cached index.

---

## Components

- [`command-palette.component.ts`](../../src/app/core/navigation/search/command-palette.component.ts) — shell-hosted overlay
- [`command-palette.service.ts`](../../src/app/core/navigation/search/command-palette.service.ts) — open state
- [`search-index.service.ts`](../../src/app/core/navigation/search/search-index.service.ts)
- Header Search trigger calls `CommandPaletteService.open()`
