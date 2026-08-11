# AOVIAS Command — Intelligent Navigation (NAV-4)

Adaptive workspace built on the [NAV-3 Navigation Framework](./navigation-framework.md).

Navigation answers three questions: **Where can I go?** · **What do I use most?** · **What should I do next?**

Related: [Global Search](./global-search.md) · [Personalization](./personalization.md) · [Keyboard Shortcuts](./keyboard-shortcuts.md)

---

## Architecture

```
MENU_REGISTRY / MODULE_REGISTRY
        ↓
NavigationEngine → NavigationQueryService
        ↓
┌───────────────────────────────────────────┐
│ Command Palette · Sidebar · Module Switcher│
│ Favorites · Pinned · Recent · Preferences │
└───────────────────────────────────────────┘
```

All surfaces consume Query/Providers. UI does not filter by role.

---

## Package layout

```
core/navigation/
  search/           # Command palette, search index, sources
  favorites/        # FavoritesService, PinnedService
  personalization/  # Workspace prefs, heuristics, preferences UI
  shortcuts/        # KeyboardShortcutsService
  engine/           # History extended for searches (NAV-3+)
```

---

## Features

| Feature | Implementation |
|---------|----------------|
| Global Search / Command Palette | Ctrl/Cmd+K → `CommandPaletteComponent` |
| Favorites | Ordered list, sidebar + switcher + palette |
| Pinned | Distinct from favorites; always top of sidebar |
| Recent | `NavigationHistoryService` SSOT (max 10 default) |
| Role-aware | Engine `NAV_ROLE_VISIBILITY` (unchanged) |
| Personalization | Usage counts + time-of-day suggested pages |
| Preferences | `WorkspacePreferencesService` + Settings panel |
| Badges | `NAV_BADGE_RESOLVER` gated by `showBadges` pref |

---

## AI extension points (architecture only)

- `KeyboardShortcutsService.registerExtension()` for future NL / AI chords
- Command palette Actions category can grow AI-generated commands
- `PersonalizationService` heuristics can be replaced by a recommendation provider token later

No AI/voice implementation in NAV-4.
