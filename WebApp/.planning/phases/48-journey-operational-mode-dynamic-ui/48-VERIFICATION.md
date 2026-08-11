# Phase 48 — Verification checklist (goal-backward)

## Goal

Journey UI **adapts behavior** to **`OperationalMode`** via **one centralized policy**, with **no layout redesign** and **no new UI components**.

## must_haves (release)

- [x] **Policy module** exists; store exposes consumable computeds; **unit tests** cover progression and key policy flags.
- [x] **Dashboard**: phase progression + lock reasons; progress title/subtitle; metrics filtered; high-risk action visibility/emphasis/disabled.
- [x] **Execution**: chapter / standard / OE / context panel wired to `operationalModeUi` (mock indicators until API).
- [x] **Wizard**: stepper + execute/evidence/tasks + footer actions aligned with policy (Final read-only / Self gates).
- [ ] **Accessibility**: manual — tooltips on disabled upload when provided; run keyboard pass on execution + wizard.
- [x] `ng build` passes (development configuration).

## Human runtime (recommended)

- [ ] Keyboard navigation through execution + wizard per mode.
- [ ] Light/dark parity for new badge/indicator states (tokens only).

## Known limitations

- Mock indicators (PASS/FAIL, “changed since audit”) until API fields exist—must be clearly labeled in devtools or comments.
