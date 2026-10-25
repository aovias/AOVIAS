# Phase 45 — Plan 02 Summary

- **Structure:** `login-shell` → `login-shell__workspace` (row ≥640px) → `login__left` (2fr: brand + form) + `login__secondary` (1fr).
- **Hero:** Blue `.brand-panel` lives only inside `login__left`, `1024px+`, `max-height` + internal scroll; `isolation: isolate`; illustration capped — no overlap with form.
- **Primary:** Flex centering; `login-card` `max-width: 440px` unchanged.
- **Viewport:** `min-height: 100vh` / `100dvh`, `overflow: hidden` on root; columns use `min-height: 0` + `overflow-y: auto` where needed.
- **Build:** `ng build` OK.
