# Navigation Personalization

## Favorites vs Pinned

| | Favorites | Pinned |
|--|-----------|--------|
| Meaning | I use often | I want visible |
| Storage | `praman.nav.favorites.v1` + order key | `praman.nav.pinned.v1` |
| UI | Sidebar (reorder ↑↓), Module Switcher, palette | Sidebar top section |

Registry remains SSOT for labels/routes; personal lists store ids/routes.

---

## Recent history

`NavigationHistoryService` is the SSOT (`praman.nav.recent.v1`, default max **10** via prefs).

Also tracks:

- Module opens (Module Switcher)
- Recent search queries (`praman.nav.recentSearches.v1`)

---

## Workspace preferences

`WorkspacePreferencesService` — localStorage, keyed by user id when available.

Configurable: density/compact, show Recent/Favorites/Pinned/badges, animations, max recent, landing page, default collapsed rail, module order, search history retention.

UI: Settings → Account (`#navigation-preferences`) and header overflow “Navigation preferences”.

---

## Heuristics

`PersonalizationService` stores usage counts (`praman.nav.usage.v1`).

Suggested pages = top by count with light time-of-day boost (morning → dashboard/CAPA/tasks; evening → reports/analytics).

Recommendations: “Continue where you left off” in the palette footer.
