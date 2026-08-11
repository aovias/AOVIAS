# Phase 29: Hospital Switcher Redesign — Research

## Objective

Redesign the hospital selection experience: remove header dropdowns (hospital + framework selectors) and replace with a clean sidebar-top hospital display card + modal-based hospital switcher popup.

---

## Current State Analysis

### 1. Header Component (`core/layout/header.component.ts`)

**Hospital Selector (lines 50–94):**
- `.hierarchy-selectors` div holds both hospital and framework selectors
- Hospital button shows org name (label) + hospital name (value) + chevron
- Dropdown rendered via `@if (hospitalMenuOpen())` with `@for` over `hospitals()` signal
- Selection calls `selectHospital(hospital)` → updates signals + calls `complianceContext.setHospitalId()`
- Hospital list populated from `OrganizationService.getHospitals()` on init

**Framework Selector (lines 98–138):**
- Similar dropdown pattern with static `frameworks` array
- Selection updates `selectedFramework` signal only

**What stays:** Logo, user profile menu, notifications, theme toggle — all in `.header-right`

**Key data flow:**
- `OrganizationService` → `getHospitals()` → local `hospitals` signal
- `ComplianceContextService.setHospitalId()` on selection
- Default hospital auto-selected (first onboarded)

### 2. Shell Component (`core/layout/shell.component.ts`)

Layout structure:
```
shell-container (flex column, 100vh)
  ├── app-header
  ├── shell-content (flex row, flex:1)
  │   ├── app-sidebar (280px)
  │   └── main-content (flex:1)
  └── footer (48px)
```

The new hospital display card needs to go **above** the sidebar nav, still within the sidebar area or between header and sidebar.

### 3. Sidebar Component (`core/layout/sidebar.component.ts`)

- Fixed width: 280px
- Height: `calc(100vh - 64px)` (below 64px header)
- Dark gradient background: `linear-gradient(180deg, #0f172a 0%, #0b1220 100%)`
- Has `sidebar-nav` with sections and menu items
- No current awareness of hospital context

### 4. State Management

**ComplianceContextService:**
- `hospitalId$` — BehaviorSubject, consumed by features
- `frameworkId$` — BehaviorSubject
- `organizationId$` — BehaviorSubject
- `setHospitalId(id)` / `getHospitalId()`

**AppContextService:**
- `switchHospital(hospitalId)` — sets hospitalId + triggers `loadFrameworkForHospital()` (stub)
- Should be the central orchestrator for hospital switching

**OrganizationService:**
- Holds `OrganizationDetails` (hospitals array, org name)
- `getHospitals()`, `getDefaultHospital()`, `hasAnyOnboardedHospital()`

**UserContextService:**
- User profile, roles, hospitals list (separate from OrganizationService)

### 5. Theme System

CSS custom properties in `styles.css`:
- Light theme: `:root[data-theme='light']`
- Dark theme: `:root[data-theme='dark']`
- Key tokens: `--bg-primary`, `--card-bg`, `--header-bg`, `--sidebar-bg`, `--border-color`, `--text-primary`, `--text-secondary`, `--primary-color`, `--primary-bg-light`, `--hover-bg`, `--shadow-*`

### 6. Existing Shared UI Components

- `shared/ui/dialog/` — MatDialog wrapper (`DialogComponent`)
- `shared/ui/confirmation-dialog/` — MatDialog with title/message/confirm/cancel
- Both use Angular Material `MatDialogModule`
- Pattern: open via `MatDialog.open()`, close via `dialogRef.close()`

### 7. Angular Material Available

- Already using `MatDialogModule`, `MatButtonModule`, `@angular/cdk/a11y`
- Can leverage `MatDialog` for the hospital selector modal (overlay + backdrop + center alignment built-in)

---

## Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Modal implementation | Angular Material `MatDialog` | Already in use, provides overlay/backdrop/animation/center-alignment out of box |
| Hospital card placement | Inside sidebar component, above nav | Keeps sidebar self-contained; card shares sidebar width |
| State orchestration | Enhance `AppContextService.switchHospital()` | Central point for hospital change → context updates → data refresh signals |
| Hospital list source | `OrganizationService.getHospitals()` | Already the source of truth, cached, deduplicated |
| Theme compliance | CSS custom properties only | Matches existing light/dark theme system |
| Component architecture | Standalone components, OnPush | Matches project standards |

---

## Files to Modify

| File | Change |
|------|--------|
| `core/layout/header.component.ts` | Remove `.hierarchy-selectors` (hospital + framework dropdowns), rebalance header layout |
| `core/layout/sidebar.component.ts` | Add hospital display card above nav, add change icon click handler |
| `core/layout/shell.component.ts` | Potentially update layout if card is placed between header and sidebar |
| `core/services/app-context.service.ts` | Enhance `switchHospital()` to broadcast change event, trigger data refresh |
| `core/services/compliance-context.service.ts` | No structural changes, continue using `setHospitalId()` |
| **NEW:** `shared/ui/hospital-selector/hospital-selector-dialog.component.ts` | Modal popup component for hospital selection |
| `styles.css` | Add any new CSS custom properties needed for the hospital card |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Breaking existing hospital context flow | Keep `ComplianceContextService.setHospitalId()` as the final setter; wrap in `AppContextService` |
| Framework selector removal impact | Framework context is mostly static/placeholder; ensure no feature depends on header framework selector |
| Responsive sidebar behavior | Hospital card must work at 280px (desktop) and 240px (tablet); hide on mobile with sidebar |
| Theme inconsistency | Use only CSS custom properties, test both themes |
| Data refresh on hospital switch | Emit observable event from `AppContextService`; features that subscribe to `hospitalId$` already react |

---

## Research Complete
