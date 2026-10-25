# Phase V10 — Search Overlay Report

## Positioning

Enterprise command center — not a consumer modal.

## Motion

| Layer | Token |
|---|---|
| Backdrop | Overlay exit (~350ms) |
| Panel elevation | Overlay enter (opacity, y, scale 0.98→1) |
| Result appearance | Staggered step animation (capped) |
| Dismissal | Graceful overlay exit |

## Interaction cues

- Focus moves to search field on open
- ↑↓ / Enter keyboard navigation documented in UI
- `esc` affordance visible
- Reduced motion: zero-duration transitions
