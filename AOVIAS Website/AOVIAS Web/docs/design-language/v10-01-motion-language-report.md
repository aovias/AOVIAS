# Phase V10 — Motion Language Report

## Objective

Establish one enterprise motion language so interactions feel intentional, responsive, and calm — never spectacular.

## Timing system

| Purpose | Token | Duration |
|---|---|---|
| Immediate feedback | `--duration-instant` / `instant` | 100ms |
| Micro / fast | `--duration-fast` / `fast` | 150ms |
| Hover | `--duration-hover` / `hover` | 200ms |
| Dropdown / UI | `--duration-standard` / `standard` | 250ms |
| Mega-menu | `--duration-dropdown` / `dropdown` | 260ms |
| Overlay | `--duration-overlay` / `overlay` | 350ms |
| Deliberate | `--duration-deliberate` | 450ms |
| Hero entrance | `--duration-hero` / `hero` | 700ms |
| Diagram progression | `--duration-diagram` / `diagram` | 800ms |
| Signature (Horizon) | `--duration-signature` / `signature` | 800ms |

## Easing

- Standard: `cubic-bezier(0.2, 0, 0, 1)`
- Enter: `cubic-bezier(0.16, 1, 0.3, 1)`
- Exit: `cubic-bezier(0.4, 0, 1, 1)`

No bounce, elastic, or playful curves.

## Purposes (required)

Every animation must guide attention, explain relationships, confirm interaction, reduce cognitive load, reinforce hierarchy, or smooth context changes.

## Sources of truth

- CSS: `packages/design-system/src/css/tokens.css`
- TS tokens: `packages/design-system/src/tokens/motion.ts`
- Framer: `apps/website/src/lib/motion/*`
- Utilities: `apps/website/src/styles/globals.css` (`.em-*`)
