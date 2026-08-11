# 29-02 Summary: Hospital Display Card Above Sidebar

## Status: Complete

## What Changed
- Added hospital card section above the sidebar nav (`<div class="hospital-card">`) with:
  - Hospital icon (SVG house icon in a styled container)
  - Hospital name (bold, primary text)
  - Organization name (subtle, uppercase, secondary text)
  - Change icon / loading spinner on the right
- Card uses CSS custom properties for full light/dark theme support
- Added `--hospital-card-*` CSS custom properties to both `:root[data-theme='light']` and `:root[data-theme='dark']` in `styles.css`
- Card has hover effects: background change, border glow, subtle shadow, icon scale
- Text uses `text-overflow: ellipsis` for long names
- Loading spinner shown during hospital switch via `isSwitching()` signal
- Responsive CSS: compact padding at ≤1024px breakpoint
- Sidebar layout changed to `display: flex; flex-direction: column` to accommodate card + nav

## Files Modified
- `src/app/core/layout/sidebar.component.ts`
- `src/styles.css`

## Commit
- `b0bcc2c` — feat(29-02): add hospital display card above sidebar
