# Keyboard Shortcuts (NAV-4)

Registered by `KeyboardShortcutsService` (started from the shell).

| Shortcut | Action |
|----------|--------|
| Ctrl+K / Cmd+K | Toggle command palette |
| Esc | Close palette (when open) |
| Alt+1 | Dashboard (`/app/dashboard`) |
| Alt+2 | Audit module default route |
| Alt+3 | Reports module default route |
| ↑ ↓ Enter | Navigate palette results |
| Tab | Focus trap inside palette |

Alt shortcuts are ignored while typing in inputs/textareas (except Ctrl/Cmd+K, which always opens the palette).

---

## Extension point

```ts
shortcuts.registerExtension({
  id: 'ai.overdue-capas',
  chord: 'future',
  description: 'Show overdue CAPAs',
  execute: () => { /* future AI */ },
});
```

No AI chords ship in NAV-4.
