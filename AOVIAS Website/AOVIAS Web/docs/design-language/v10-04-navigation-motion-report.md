# Phase V10 — Navigation Motion Report

## Surfaces

| Surface | Behavior |
|---|---|
| Mega-menu | Framer enter/exit (opacity + 6px), dropdown timing (~260ms) |
| Mobile drawer | Backdrop fade + panel slide (16px), overlay/dropdown tokens |
| Header chrome | Scroll elevation via CSS vars (`--duration-dropdown`) |
| Nav links / items | Color transitions on hover duration |
| Theme toggle | Existing control; color transitions honor tokens |

## Goals

Effortless open/close, no flicker, no abrupt mounts. Hover delays unchanged (90ms open / 180ms close) for intentional menus.
