# Phase 40 — Verification (reference parity)

Use this checklist against the stakeholder **Design Gap Analysis** and the reference screenshot.

## 1. Journey mode cards

- [x] Four mode cards in **one row** on desktop (≥64rem viewport width).
- [x] **Lucide** icons (not single-letter placeholders) for self / internal / final / surveillance.
- [x] **Active** card: primary border + visible glow (`box-shadow` + `color-mix` on brand primary).
- [x] **Inactive** cards: muted surface / lower emphasis.
- [x] **Locked** cards: non-interactive, lock styling + tooltip.
- [x] Title size increased (`--font-size-heading-md`); description up to three lines clamped.
- [x] Progress bar + **percentage** when a mode is selected.

## 2. Recommended next action

- [x] **Recommended Next Action** heading with lightbulb icon.
- [x] Summary line driven by **real** OE code + compliance target when data allows (no hardcoded IDs).
- [x] **Go to OE** / **View Details** buttons.
- [x] Footer: **High Risk OE(s) need attention** count from **`oesAtOrBelowThreeCount`** + **Last activity** from **`lastSyncedAt`** (relative).

## 3. Action buttons row

- [x] **Continue Journey** (primary + play icon), **Resume Last OE** (secondary + history icon), **View High Risk OEs** (danger + warning icon).

## 4. Metric cards

- [x] Four KPIs in one row on desktop (12-column grid, span 3).
- [x] **Lucide** icons per metric; **large** value typography (`--font-size-display-lg`).
- [x] Card surface uses **`--card-bg`**.

## 5. Chart cards

- [x] Three chart cards; titles use **`--font-size-heading-md`**; **`--card-bg`** surface.

## 6. Page header

- [x] **Select Your Journey Mode** + **Choose the audit phase to focus on** (left-aligned intro block).

## Build

- [x] `ng build` succeeds.

## Manual (recommended)

- [ ] Light/dark theme parity on Journey dashboard.
- [ ] Tablet breakpoints (64rem / 40rem) — no horizontal scroll on mode row.
