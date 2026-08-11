# Phase 30 Research: Hospital + Framework Card Enhancement

## Objective
Enhance the Phase 29 hospital switcher to match design reference images — adding framework display + selection, seamless sidebar styling, and combined hospital + framework modal.

## Current State (Phase 29 Output)

### Sidebar Hospital Card (`sidebar.component.ts`)
- **Location:** Top of sidebar, above `<nav class="sidebar-nav">`
- **Structure:** `.hospital-card` → icon + content (name + org) + change icon
- **Styling:** Has `border: 1px solid`, `border-radius: 12px`, `margin: 16px 12px 0` — card-like appearance
- **Missing from design ref:**
  - No "Selected Framework" section below hospital info
  - Has visible border (design wants borderless / seamless)
  - Icon is `border-radius: 10px` (square-ish), design shows circular
  - No separator line below the card
  - No framework display at all

### Hospital Selector Dialog (`hospital-selector-dialog.component.ts`)
- **Location:** `shared/ui/hospital-selector/`
- **Current:** Hospital-only selection with radio buttons, confirm/cancel
- **Missing from design ref:**
  - Title is "Select Hospital" — design wants "Select Hospital & Framework"
  - No close (X) icon top-right
  - No "Hospital" section label above hospital list
  - No Framework section at all
  - Returns `string` (hospitalId) — needs to return `{ hospitalId, frameworkId }`

### State Services
- **ComplianceContextService:** Has `hospitalId$`, `frameworkId$`, `organizationId$` BehaviorSubjects — already supports framework state
- **AppContextService:** `switchHospital(hospitalId)` — only updates hospital, calls `loadFrameworkForHospital()` which just clears framework. Needs to also set framework.
- **CommonLookupService:** `loadFrameworks()` returns `Observable<FrameworkLookup[]>` from `/frameworks/all-frameworks` — **this is the framework data source**

### CSS Variables (`styles.css`)
- `--hospital-card-*` variables exist for both light and dark themes
- `--hospital-card-border` and `--hospital-card-bg` need adjustment for borderless design
- Need new variables for framework section styling

## Design Reference Analysis

### Image 1 — Sidebar Card
1. **Hospital row:** Circular icon (dark blue bg) + "ST. JUDE MEDICAL CENTER" (bold, large, white) + "Global Health Partners" (muted) + pencil icon (right, contained in rounded square)
2. **Framework row:** Label "Selected Framework" (muted, small) + check badge (yellow/gold) + "NABH Entry Level" (bold, white) + "Global Health Partners" (muted)
3. **No visible card border** — blends with sidebar
4. **Thin separator line** below framework section before nav starts

### Image 2 — Selection Modal
1. **Header:** "Select Hospital & Framework" (bold) + close (X) icon top-right
2. **Hospital section:** "Hospital" label + radio list with:
   - Selected: "Apollo Hospitals" + "Apollo Hospitals Group" (sub text) + blue radio + checkmark
   - Unselected: "SRL Labs", "Fortis Healthcare" — no sub text shown (simpler items)
3. **Framework section:** "Framework" label + radio list:
   - Selected: "NABH Accreditation" + blue radio filled
   - Unselected: "ISO 9001", "JCI Accreditation"
4. **Footer:** "Cancel" (left) + "Confirm Selection" (right, blue button)

## Key Decisions

### Data Sources
- **Hospitals:** `OrganizationService.getHospitals()` — already in place
- **Frameworks:** `CommonLookupService.loadFrameworks()` — returns `FrameworkLookup[]` with `platformId`, `name`, `authority`, `code`
- **Framework ID storage:** `ComplianceContextService.frameworkId$` using `platformId` as the key

### Dialog Return Type
Current: `dialogRef.close(hospitalId: string | null)`
New: `dialogRef.close({ hospitalId: string, frameworkId: string } | null)`

### Card Styling
- Remove `border`, `border-radius`, `margin` from hospital card
- Use `padding` only for spacing
- Background: transparent or ultra-subtle contrast (no visible boundary)
- Icon: `border-radius: 50%` (circular)
- Add full-width separator (`border-bottom` or `::after` pseudo-element)

### Framework Loading
- Frameworks may not be loaded when dialog opens
- Need to load from `CommonLookupService.loadFrameworks()` if not cached
- Pass loaded frameworks as dialog data (not load inside dialog — keeps dialog dumb)

## Files to Modify
1. `src/app/shared/ui/hospital-selector/hospital-selector-dialog.component.ts` — Enhanced dialog
2. `src/app/core/layout/sidebar.component.ts` — Card redesign + framework display + wiring
3. `src/app/core/services/app-context.service.ts` — `switchContext(hospitalId, frameworkId)`
4. `src/styles.css` — Updated CSS variables for borderless card
