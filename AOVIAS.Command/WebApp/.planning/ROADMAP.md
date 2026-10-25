# PRAMAN Operator Portal - Infrastructure Bootstrap Roadmap

## Phase 01: Project Structure ✅

Create the folder architecture for core, shared, and features modules.
**Status:** Complete - All directories created, application builds successfully.

## Phase 02: Environment Configuration

Create environment.ts and environment.prod.ts with apiBaseUrl, oauthAuthority, appName, enableDebugLogging.

## Phase 03: Core Layout Infrastructure

Create shell layout with header, sidebar, and router outlet in core/layout.

## Phase 04: Angular Routing Architecture

Create application routing with top-level routes (/login, /register, /forgot-password, /reset-password) and /app routes with lazy loading.

## Phase 05: Route Guards

Create auth.guard.ts, framework.guard.ts, and role.guard.ts in core/guards.

## Phase 06: Authentication Infrastructure

Create AuthService, TokenService, and SessionService in core/auth.

## Phase 07: HTTP Infrastructure

Create auth.interceptor.ts, api-response.interceptor.ts, and loading.interceptor.ts in core/interceptors.

## Phase 08: Context State Management

Create UserContextService, ComplianceContextService, and AppContextService in core/services.

## Phase 09: Shared UI Library

Create reusable UI components (table, form-field, dialog, file-upload, status-badge, progress-indicator, confirmation-dialog) in shared/ui using Angular Material.

## Phase 10: API Layer

Create BaseApiService in shared/services and feature services extending it.

## Phase 11: Error Handling

Create global error handling strategy and error pages (/error/403, /error/404, /error/500, /error/maintenance).

## Phase 12: Design System

Create global SCSS tokens in styles/design-tokens.scss.

## Phase 13: Logging Infrastructure

Create logger.service.ts in core/services with debug, info, warn, error methods.

## Phase 14: Performance Foundations

Enable lazy loading, OnPush change detection, and server pagination readiness.

## Phase 15: Developer Tooling

Configure ESLint and Prettier with npm scripts.

## Phase 16: Verification

Ensure application compiles, routes resolve, interceptors registered, guards functional, shell layout loads, no console errors.

## Phase 17: Login API Integration ✅

Integrate login UI with backend authentication API. Update AuthService to extend BaseApiService, create proper models, update interceptors, integrate LoginComponent with error handling.
**Status:** Complete - Login flow fully integrated with backend API, context services updated, Material SnackBar error handling implemented.

## Phase 18: Signup Wizard API Integration ✅

Integrate signup wizard UI with backend onboarding API. Create onboarding models and service, implement state management with Signals, convert to reactive forms with validation, and handle success/error flows.
**Status:** Complete - Signup wizard fully integrated with backend API, reactive forms implemented, error handling with NotificationService, sessionStorage persistence added.

## Phase 19: GeoSelector Module (India-first)

Implement a reusable, config-driven GeoSelector module using the `country-state-city` npm package with India-first optimization (pre-filtered to India). Provide a core service for geo data (cached, client-side), a shared UI component supporting reactive forms + edit mode, and integrate into onboarding with minimal standardized payload (countryCode/stateCode/cityName).
**Status:** Complete - `GEO_CONFIG` added with India-first defaults, cached GeoSelectorService implemented, reusable GeoSelectorComponent (CVA + autocomplete + manual city fallback) created, signup hospital step integrated to emit/store minimal geo contract fields.

## Phase 20: Departments Lookup (Onboarding) ✅

Refactor onboarding wizard Hospital Profile → Departments to fetch from `GET /api/v1/common/departments` (no hardcoded values), bind Name/Id, store only `DepartmentIds`, support edit-mode preselection, caching, and graceful loading/error/empty states.
**Status:** Complete - Shared cached lookup service added, onboarding hospital profile stores `departmentIds`, Hospital Profile departments UI is API-driven with retry + invalid-id protection.

## Phase 21: Post-Login Hospital Routing & Selector Sync

Use `organization-detail` response (`data.hospitals[]`) to determine post-login route and active hospital context. If at least one hospital has `isOnboarded: true`, route to `/app/dashboard`; otherwise route to onboarding screen. Populate header hospital selector from API hospitals and auto-select an onboarded hospital by default.
**Status:** Complete - Login now routes by `hospitals[].isOnboarded`, default hospital context is selected from organization-detail, and header hospital selector is API-driven with onboarded-first selection.

## Phase 22: Global Auth + Onboarding Guard Enforcement

Extend route protection so authentication and onboarding status are enforced on all protected routes (including deep links/refresh). Add an onboarding guard layer that uses centralized organization state (cached organization-detail) to redirect non-onboarded users to onboarding and block access to main app routes until onboarding is complete.
**Status:** Complete - Global guard chain applied (`authGuard` + onboarding guards), onboarding access constrained by hospital onboarding status, and organization-detail calls de-duplicated via centralized in-flight cache.

## Phase 23: Dynamic Compliance Frameworks (Onboarding) ✅

Replace the static compliance framework list in the AI onboarding wizard (`FrameworkSelectorStep`) with data from `GET /frameworks/all-frameworks`. Bind `name` / `authority` in the UI, use `platformId` as the unique identifier for multi-select, persist selections in onboarding state, and include selected platform IDs in any onboarding submission payload. Match loading/error UX and caching patterns used for departments lookup (Phase 20).
**Status:** Complete — `CommonLookupService.loadFrameworks`, dynamic framework step with loading/error/retry, `Hospital.frameworkPlatformIds` + `OnboardingStore` merge in `buildRequestPayload()`, completion summary uses API-backed display names.

## Phase 24: Onboarding Framework Cards — Binding, Selection & Icons ✅

Harden the framework selection step: bind `<p>` strictly to API `authority` (with documented fallback only if absent), treat `**platformId`** as the canonical non-display selection key (normalized from API `platformId` / `frameworkId`), keep checkbox/UI IDs in sync, replace first-letter placeholder with a **data-driven icon** system using a standard icon package (e.g. Lucide via `ng-icons`), and remove remaining static UI placeholders while preserving wizard navigation state.
**Status:** Complete — `FrameworkLookup.platformId`, authority prefers API field then `code`, `app-framework-card-icon` + `@ng-icons/lucide`, local selection copy + `trackBy`, emit `{ platformIds, displayNames }`, OnPush + `markForCheck`.

## Phase 25: Global Session Expiry, 401 & Refresh-Token Resilience ✅

Centralize handling of expired sessions and **401** on API calls; refactor **auth + HTTP interceptors** so `**POST /identity/refresh`** uses **RxJS `catchError`** (not `try/catch` alone) for **all** refresh failures (**401, 403, 5xx, 503, network/timeout**). Implement **single-flight refresh** (one in-flight refresh call), **queue** concurrent requests while refresh runs, **replay** on success or **reject + logout** on failure; prevent **infinite retry loops** (especially on `/identity/refresh`). On any non-recoverable outcome: clear session, optional toast, **one** redirect to login, structured **logging** (status, endpoint). Align guards and feature error copy (no misleading “Failed to load…” during session termination). Preserve API contracts and existing token storage.
**Status:** Complete — `SessionExpiryCoordinatorService`, `TokenRefreshService`, `SKIP_AUTH_REFRESH`, updated `auth`/`api` interceptors, `CommonLookupService.clearCaches()` on logout, onboarding guards + hospital profile UX; see `25-VERIFICATION.md`.

## Phase 26: AI Onboarding Wizard — State, Cross-Step Flow & Final Submit ✅

Refactor `**AiSetupAssistantComponent`** so wizard data has a **single source of truth** (store + **sessionStorage** draft), **persist** across navigation/refresh (distinct key from signup), **validate** per step (including **duplicate team emails**), bind **Team Invitation** department dropdown **only** to departments selected in **Hospital Profile**, expand **Review/Completion** summary, and implement `**POST /onboarding/onboard`** (typed payload, retry on failure, no data loss). Align framework/hospital fields with backend contract (`**frameworkCodes**` vs IDs as required).
**Status:** Complete — `AiOnboardingWizardStore`, `AiOnboardingValidationService`, `submitOnboard`, team `**departmentId`** + `**POST_ONBOARD_PAYLOAD.md**`; see `26-VERIFICATION.md`.

## Phase 27: Centralized Role Management (canonical IDs + UI labels) ✅

Define `**USER`, `ORG_ADMIN`, `GROUP_ADMIN`, `SUPER_ADMIN`, `REGULATORY_ADMIN`, `COMPLIANCE_VIEWER`, `AUTHORITY`, `REGULATOR`, `MANAGER**` as the single source of truth for APIs and storage; central `**RoleRegistryService**` + `**RoleLabelPipe**`; UI shows formatted labels only; **Team Invitation** and all role dropdowns bind **value = ID**; unknown/null-safe display; optional legacy alias mapping; `**roleGuard`** and routes use canonical IDs.
**Status:** Complete — `role-definitions.ts`, `RoleRegistryService`, team step + wizard load + completion labels + login normalize; see `27-VERIFICATION.md`, `27-01-SUMMARY.md`.

## Phase 28: Onboarding Wizard — User-Scoped State (no cross-user leakage) ✅

Bind **AI wizard** and **signup onboarding** `sessionStorage` drafts to `**userId`** (namespaced keys or owner validation); **clear** stores on **logout** and on **login** when identity changes; **migrate** legacy unscoped keys; optional multi-tab follow-up; backend ownership checks recommended.
**Status:** Complete — `onboarding-storage.keys.ts`, `OnboardingStateIsolationService`, namespaced stores + login/logout/refresh hooks; see `28-VERIFICATION.md`, `28-01-SUMMARY.md`.

## Phase 29: Hospital Switcher Redesign ✅

Redesign the hospital selection experience: remove header hospital & framework dropdowns, add a hospital display card above the sidebar, and introduce a modern modal-based hospital selector. Enhance `AppContextService` to orchestrate hospital switching with state persistence and data refresh signals. Maintain light/dark theme support throughout.
**Status:** Complete — Header selectors removed, hospital card above sidebar, `HospitalSelectorDialogComponent` modal with radio selection, `AppContextService.switchHospital()` orchestrator, light/dark CSS custom properties; see `29-VERIFICATION.md`.

## Phase 30: Hospital + Framework Card Enhancement ✅

Enhance the sidebar hospital card to include a "Selected Framework" display section with seamless borderless design, circular icon, and separator. Upgrade the selector popup to support both hospital AND framework selection in a single modal with section labels, close button, and dual-radio selection. Wire framework state through `ComplianceContextService` and `AppContextService` with journey-based framework data from `organization-detail`.
**Status:** Complete — Borderless sidebar card, dual-selection dialog, `switchContext()`, click-target restricted to edit icon, journey-based dynamic frameworks per hospital, sessionStorage persistence, auto-popup on first login, logout cleanup; see `30-VERIFICATION.md`.

## Phase 31: Dynamic User Name in Header

Bind the logged-in user's name in the top-right header (replacing the static "Dr. Sarah Johnson") to the `members[]` array from the `organization-detail` API response. Update the `OrganizationMember` model to include all member fields (`firstName`, `lastName`, `email`, `phoneNumber`, `alternatePhoneNumber`, `profilePicUrl`, `preferences`). Match the logged-in user's `userId` (from `SessionService`) against `members[].userId` and display `firstName + lastName`. Add a helper method on `OrganizationService` for member lookup.
**Status:** Complete — `OrganizationMember` model expanded with full API fields, `getMemberByUserId()` + `getLoggedInMemberDisplayName()` helpers added, header `userName` signal bound reactively from `organization$` + `SessionService.getUserId()`, avatar initials UX; see `31-VERIFICATION.md`.

## Phase 32: Signup First Name & Last Name Fields ✅

Enhance the signup flow by adding First Name and Last Name input fields to Step 3 (Administrator Account). Update the `AdminUser` model to include `firstName` and `lastName`. Add these fields above the email input in side-by-side layout (responsive stacking on mobile). Include validation (required, min 2 chars, no numeric-only). Pass the new fields in the signup API request payload. Maintain sessionStorage persistence and existing UX patterns.
**Status:** Complete — `AdminUser` model extended, `firstName`/`lastName` form controls with validation added to Step 3, responsive side-by-side layout, all save/restore points updated, build passes; see `32-VERIFICATION.md`.

## Phase 33: Onboard Success → Navigate to Dashboard ✅

Bug fix: after `POST /onboarding/onboard` returns 200 OK, force-reload `organization-detail` (so `isOnboarded` updates in cached state) and navigate to `/app/dashboard` instead of the current non-existent `/control-center/compliance-command-center` route.
**Status:** Complete — `submitOnboard()` success handler refreshes org-detail via `loadOrganizationDetails(true)` then navigates to `/app/dashboard`, `handleSkip()` also fixed; see `33-VERIFICATION.md`.

## Phase 34: Dashboard API Binding (Program Overview Snapshot)

Bind the dashboard to `GET /dashboard/program-overview-snapshot`. Create typed models for the full response (summary, frameworks, hospitals, scoreTrends, tasksSummary, milestones, alerts, filters). Update `DashboardService` to call the endpoint. Update `DashboardComponent` to fetch data on init, map API fields to existing sub-components (executive summary cards, gauge charts, risk distribution, framework scores, chapter breakdown), and add loading/error states. Maintain light/dark theme support and existing project patterns.
**Status:** Planned

## Phase 35: Journey Module — NABH-Compliant UI Refactor (Enterprise)

Replace the legacy **clause-based** Journey mock UI with a **hierarchical** audit-ready experience: **Framework → Chapter → Standard → Objective Element (OE)**. Implement NABH-aligned scoring (1–5), classification (Core / Commitment / Achievement / Excellence), evidence-driven compliance, audit lifecycle modes (Self → Internal → Final → Surveillance), accreditation decision panel, action plans for low scores, NA handling with justification, performance (lazy load, virtual scroll), and theme-consistent styling. Preserve existing color tokens and dark/light support; **no reuse of clause card/list UI patterns**. See `.planning/phases/35-journey-module-nabh-ui-refactor/`.
**Status:** Complete — typed models + mock `JourneyDataService`, `JourneyWorkspaceStore`, NABH UI components, OE drawer, `BACKEND_CONTRACT_SPEC.md`; see `35-VERIFICATION.md` / `35-SUMMARY.md`.

## Phase 36: Journey Module — Standards-First Navigation, Mock Data & Tab Workspace

Refactor Journey so **Standards are first-class** (**Framework → Chapters → Standards → ObjectiveElements**), aligned with **NABH Hospital Standards 6th Edition (effective 1 Jan 2025)**: **10 chapters (AAC–IMS)**, **639** OEs / **100** standards at full catalog. Replace **HTTP** with `**mockJourneyData.ts**` + `loadMockData()` and **edition metadata**. Refactor `**JourneyWorkspaceStore**` (**`selectedStandard`**, **`objectiveElementsForSelectedStandard`**); left **nested standards**; main **OEs + `JourneyTabsComponent`**. Implement **OE derivation** and **`nabh-accreditation-decision.util.ts`** (final / surveillance / re-accreditation; **80%** rules per OE band scope per guidebook). Validate: official chapter codes; classifications; no Journey API calls. PDF-sourced research: **`36-RESEARCH.md`**. See `.planning/phases/36-journey-standards-first-mock-tabs/`.
**Status:** Planned

## Phase 37: PRAMAN Journey Dashboard — Enterprise Redesign (Token-Strict)

Rebuild the Journey dashboard UI from scratch (do not reuse old Journey screen structure/styles/components) to deliver a premium enterprise SaaS experience aligned with PRAMAN workflow: **Header → Journey Mode Selector → Recommendation Panel → Primary Action Bar → Metrics Overview → Insights & Charts**. Enforce `DESIGN_RULES.md` and token-only styling from `src/styles/tokens/*` with no inline CSS and no hardcoded hex/px values (unless introduced as approved tokens). Add reusable, OnPush, input/output-driven components (`journey-card`, `recommendation-panel`, `action-bar`, `metric-card`, `chart-card`), responsive 12-column grid, accessibility, light/dark theme parity, and centralized chart color semantics.
**Status:** In verification — implementation and build checks complete; awaiting manual runtime UX signoff (responsive behavior, keyboard/focus flow, and theme parity).

## Phase 37.1: Journey Execution Screen (Mission Control Workspace)

Design and implement a brand-new Journey Execution screen launched from **Continue Journey**. Build an enterprise 3-column task workspace (left: chapter navigation, center: standards + OE table workspace, right: selected OE/action/progress context) with independent panel scrolling, sticky local headers, and action-first UX. Follow `DESIGN_RULES.md` strictly, use only tokens from `src/styles/tokens/*`, avoid inline styles/hardcoded visual values, and keep UI components presentational with OnPush + Input/Output. Include a clear navigation action to return to the main Journey screen.
**Status:** Planned

## Phase 37.2: Journey Execution Screen — Visual Polish & Hierarchy (Refactor-Only)

Refine the **existing** Journey Execution UI (`ExecutionScreenComponent` and execution panel subcomponents) to match reference **layout structure, spacing, hierarchy, card segmentation, and interaction feedback**—without changing data models, store contracts, or feature behavior. Enforce token-only styling per `DESIGN_RULES.md`; improve center-column emphasis, left navigation card treatment, right-panel section cards, OE row scanability, and button/action tiers. See `.planning/phases/37.2-journey-execution-visual-polish/`.
**Status:** Complete — implementation and build verified; `37.2-VERIFICATION.md` marks **human_needed** for subjective reference parity (light/dark, runtime).

## Phase 38: Journey Module — DESIGN_RULES Token Compliance (Full Sweep)

Refactor **all** Journey-related UI (`src/app/features/journey/`) to align with **`DESIGN_RULES.md`** and `src/styles/tokens/*`: replace hardcoded hex/rgba/px with CSS custom properties (`--color-*`, `--space-*`, `--radius-*`, `--font-*`, `--shadow-*`, and legacy aliases where documented). Cover Journey list, execution screen, stepper, OE detail, modals/drawers/side panels; optional foundation aliases for names like `--border-radius-standard` / `--button-height` if mapped to existing tokens. See `.planning/phases/38-journey-design-rules-token-compliance/`.
**Status:** Complete (implementation) — `38-01-SUMMARY.md`, `38-VERIFICATION.md`; **human_needed** for runtime light/dark/tablet signoff.

## Phase 39: Journey Module — Enterprise UI Polish (Visual-Only)

Refine **all** Journey screens (`src/app/features/journey/`) from functional to **enterprise-grade** visual quality: lighter borders and elevation, consistent compact card density, improved typography hierarchy, neutral-first color usage (primary only for key actions), standardized spacing scale, button tier clarity, and consistent icon/badge treatment. **Mandatory** update to **`DESIGN_RULES.md`** (and foundation/theme tokens as needed) so borders, shadows, card padding/density, badges, icon sizes, section gaps, and button hierarchy are documented for future screens. **No** new fields, controls, API/store changes, or feature behavior — styling, layout rhythm, and documented tokens only. See `.planning/phases/39-journey-enterprise-ui-polish/`.
**Status:** Complete (implementation + build) — `39-VERIFICATION.md`; **human_needed** for light/dark/tablet subjective signoff.

## Phase 40: Journey Dashboard — Reference Design Parity (Mode + KPI + Charts)

Close the **Design Gap Analysis: Reference vs. Implemented** for the **Journey** route (`/app/journey`): restore a **content header** (not the removed global shell) with **“Select Your Journey Mode”** and **“Choose the audit phase to focus on”**; align **journey mode cards** with the reference (horizontal four-up on desktop, **Lucide/ng-icons** instead of letter placeholders, **active** blue glow / stronger hierarchy, muted inactive/locked states); **recommendation panel** copy and footer pattern; **action bar** icon prominence; **metric** and **chart** card styling per reference; responsive breakpoints so desktop matches the reference layout. Use **`DESIGN_RULES.md`** and `src/styles/tokens/*` only; no new product features beyond presentation and copy driven by **existing** store/VM data. See `.planning/phases/40-journey-dashboard-reference-parity/` and the stakeholder gap document.
**Status:** Complete — see `40-VERIFICATION.md` (manual theme/tablet signoff recommended).

## Phase 41: Journey Execution — Action-Driven Workspace (Standards → OE → Journey)

Transform the Journey Execution **Standards + OE** workspace into a **journey-aligned, action-first** flow: improve standard card headers (truncation, hover, full-header click), make OE rows interactive with **Journey Status** column, **Start / Resume Journey** primary CTA, optional quick filter chips, responsive table→card layouts, title wrapping fixes, and a functional **Journey Context** right panel (empty state, selected OE details, evidence summary, quick actions). Align with `DESIGN_RULES.md` and existing `JourneyWorkspaceStore` contracts; extend VM/models for journey lifecycle state (mock-backed until API exists). See `.planning/phases/41-journey-execution-action-workspace/`.
**Status:** Complete (implementation + build) — `41-VERIFICATION.md`; **human_needed** for runtime theme/tablet signoff.

## Phase 42: Journey Context Panel — Reference UI Redesign (Execution Right Column)

Redesign `execution-screen__panel--right` / `app-journey-context-panel` to match the **reference** enterprise layout: **three** stacked cards (OE Context, Journey Progress, Quick Actions), token-only styling (`DESIGN_RULES.md` + `src/styles/tokens/*`), pill chips with semantic variants, determinate progress bar + score/evidence/last-updated rows, **primary** Start Journey + **secondary** View History, **3×** quick-action icon tiles (responsive **2-col** on small screens), empty/loading/disabled states, and VM extensions (`progressPercent`, formatted dates, `readOnly`/loading) without hardcoded colors or fixed widths. See `.planning/phases/42-journey-context-panel-reference-redesign/`.
**Status:** Complete (implementation + build) — `42-01-SUMMARY.md`, `42-02-SUMMARY.md`, `42-VERIFICATION.md`; **human_needed** for runtime theme/tablet signoff.

## Phase 43: PRAMAN Themed Scrollbar System (Global + Contextual)

Replace default browser scrollbars with a **custom themed scrollbar system** aligned to PRAMAN design tokens: thin, rounded, minimal idle state, stronger hover/active/focus-visible affordances, light/dark parity, context variants (primary content vs secondary panels vs dense lists), responsive behavior (desktop thin, tablet slightly wider touch affordance, mobile native scrolling without forced custom chrome), cross-browser strategy (WebKit + Firefox standards), accessibility (keyboard scroll, focus visibility, contrast when active), and **no hardcoded visual values** — all derived from `src/styles/tokens/*` and documented in `DESIGN_RULES.md`. Prefer native scrolling with CSS-only enhancement; avoid heavy JS scrollbar libraries. See `.planning/phases/43-praman-themed-scrollbar-system/`.
**Status:** Complete (implementation + build) — `43-VERIFICATION.md`; **human_needed** optional for cross-browser visual signoff.

## Phase 44: OE Execution Full-Screen Wizard Modal

Design and implement a **full-viewport overlay workspace** (not a small dialog) for **Objective Element (OE) journey execution** as a **guided five-step wizard** (Understand → Execute → Evaluate → Improve → Complete), opened from **Start / Execute Journey** in the Journey Context Panel. Includes fixed **header** (breadcrumb, OE identity, status, close with unsaved guard), **wizard stepper** (linear unlock rules, completed checkmarks), **dynamic main content** per step, **persistent right context panel** (progress, next-action guidance, quick actions), **smart locking** (Evaluate after minimum execution; Improve only when non-compliant / gaps exist; Complete when prerequisites met), **responsive** reflow (desktop 2-column + context; tablet slide-over context; mobile stacked / horizontal stepper), **light/dark** token-only styling per `DESIGN_RULES.md`, and **accessibility** (keyboard step nav, focus trap in overlay, ARIA). See `.planning/phases/44-oe-execution-fullscreen-wizard/`.
**Status:** Complete (implementation + build) — `44-VERIFICATION.md`; **human_needed** for full keyboard/SR and visual polish.

## Phase 45: Login Screen — Two-Column Layout (2:1)

Refactor the **existing** login screen (`src/app/login/login.component.html` + `.css`) to a **two-column horizontal layout** with **left : right = 2 : 1** (primary form column vs secondary supportive column), matching the stakeholder reference. **Layout and alignment only:** do **not** add or remove controls, change reactive form fields, SSO handlers, or auth flow. Left column: welcome title → email → password → remember/forgot row → Sign In → divider → Microsoft/Google (horizontal) → Create Hospital Account link. Right column: **PRAMAN AI Security Assistant** card (top), **Quick Secure Login** biometric block (below). Preserve remaining footer/meta lines (secure access disclaimer, contact support) without removing—place under the primary column or full-width below the grid per plan. Responsive: desktop strict 2:1; tablet proportional/narrower right; mobile stack (primary first, secondary below). See `.planning/phases/45-login-screen-two-column-layout/`.
**Status:** Complete — `45-01-SUMMARY.md`, `45-VERIFICATION.md`; **human_needed** optional for pixel parity vs reference screenshot.

## Phase 46: Shell Left Sidebar — Operational Command Center Navigation

Transform the **app shell left sidebar** from a **static grouped list** into a **context-aware, action-driven, enterprise-grade navigation system**: **brand + system header**, **elevated context panel** (organization / hospital, active framework, **operational mode**, optional **compliance progress**, switcher affordance), **restructured primary navigation** (Dashboard; **Compliance Execution** with **Journey** as primary emphasis; **Action Center** for findings / issues / alerts with **badges**; **Audit Management** lifecycle; **Knowledge & Documentation**; **Administration**), **nav row** anatomy (icon, label, optional badge) with **strong active** and **disabled** states, **expand/collapse** rail with tooltips and **persisted** width state, **quick actions** strip, **scroll/sticky** behavior, **responsive** (tablet width, **mobile** overlay + hamburger), **accessibility** (keyboard, focus, landmarks), and **scalability** patterns for **50+** modules — **without removing** existing modules (reorganize and enhance only). UX/structure/behavior first; route-level mapping and implementation follow `46-01`–`46-03` plans. See `.planning/phases/46-shell-sidebar-operational-command-center/`.
**Status:** Complete (implementation + build) — `46-01-SUMMARY.md`, `46-02-SUMMARY.md`, `46-03-SUMMARY.md`, `46-VERIFICATION.md`; **human_needed** for AT + real badge/progress API wiring.

## Phase 47: Journey Execution Screen — Responsive Enterprise Workspace (Layout + Interaction Only)

Refactor **`ExecutionScreenComponent`** and its **execution panel** children so the Journey Execution route is **fully responsive** with **stable layout**, **no overlapping panels**, **clear visual hierarchy**, and **consistent interaction feedback** on chapter rows, standard blocks, and OE rows. **Tablet:** collapse the **right Journey Context** column into a **slide-over** or **collapsible rail**; keep **chapters + standards** usable. **Mobile:** **stack** regions in order: header → **collapsible chapter nav** → **standards/OE primary workspace** → **context** as **bottom sheet** or **expandable section**. **Shell:** ensure **app sidebar** overlay uses **backdrop + z-index** so **main content never paints above** the nav drawer; **disable interaction** on obscured content when overlay open. Align with `DESIGN_RULES.md` and `src/styles/tokens/*` only. **No** new backend fields, **no** new data-dependent controls, **no** store contract changes beyond **optional UI-only signals** (e.g. context rail open state). See `.planning/phases/47-journey-execution-responsive-workspace/`.
**Status:** Complete (implementation + build) — `47-01-SUMMARY.md`–`47-03-SUMMARY.md`, `47-VERIFICATION.md`; **human_needed** for device/tablet runtime signoff.

## Phase 48: Journey — OperationalMode-Dynamic UI (No Layout Change)

Make the **existing** Journey surfaces (dashboard phase cards, NABH progress + metrics + action bar, execution chapter/standard/OE panels, context panel, full-screen OE wizard) **behaviorally mode-aware** using the domain **`OperationalMode`** enum (`nabh.enums.ts`). Introduce a **single centralized policy** (`OperationalMode` → visibility, labels, disabled states, validation gates) and apply it across templates and VM mappers. **Do not** redesign layout, **do not** add new components or new persisted fields; mock- or VM-only indicators are allowed where API data is not yet available. **ReAccreditation** may be hidden from phase cards until product enables it. See `.planning/phases/48-journey-operational-mode-dynamic-ui/`.
**Status:** Complete (implementation + build) — `48-01-SUMMARY.md`–`48-04-SUMMARY.md`, `48-VERIFICATION.md`; **human_needed** for accessibility + runtime mode matrix signoff.

## Phase 49: Journey Module — Full API Binding (Session → Dashboard → Chapters → OE)

Replace **`JourneyMockWorkspaceService`**-backed data with **split REST calls** per `journey-module-api-specification.md` (Compliance Platform): `GET` session, dashboard, chapters index, chapter standards (paginated), OE detail; `PATCH` OE + operational mode; evidence **POST/DELETE**; handle **`ApiResponse<T>`** (already unwrapped by `api-response.interceptor`). Align **`JourneyDataService`** paths and DTOs with `/journeys/{journeyId}/…` routes; extend **`JourneyWorkspaceStore`** for async load, caches, partial refresh, **`version`** on PATCH, **409** recovery; bind **`readOnly` / `snapshotReadOnly` / `changedSinceLastAudit` / `modeProgression`** from API; remove mock fallbacks when `journeyId` resolves. **No** new backend endpoints; wizard **POST …/wizard/progress`** is **not implemented** on backend — persist wizard step locally until product enables. See `.planning/phases/49-journey-module-api-binding/`.
**Status:** Complete (implementation + build) — `49-01-SUMMARY.md`–`49-03-SUMMARY.md`, `49-VERIFICATION.md`; **human_needed** for Gateway E2E + `v1` vs `v1.0` path verification.

## Phase 50: OE Execution — Enterprise Compliance Wizard (Inline Workspace)

Redesign **`app-oe-execution-wizard`** (Objective Element execution) to a **premium SaaS** guided workflow: **horizontal stepper (desktop) / vertical stepper (mobile)**, five steps — **Understanding** → **Compliance Guidance** → **Evidence** → **Audit Checklist** → **Review & Submit** — with **card-based** step content, **sticky** prev/next + **Save draft**, progress **%** and **estimated time**, semantic status colors, and **inline validation**. **Layout:** content width and visual rhythm **aligned with Journey Execution** (`ExecutionScreenComponent` workspace), **not** a separate full-viewport modal shell (supersedes Phase 44 presentation for this surface). **Entry:** **`journey-context__btn--primary`** (**Start Journey** / **Resume Journey** / **View Journey**). **Constraints:** `DESIGN_RULES.md` + `src/styles/tokens/*` only; **no new backend or persisted business fields** — reuse existing OE / evidence / checklist VM fields and local wizard state; Phase 49 note on wizard API still applies. See `.planning/phases/50-oe-execution-compliance-wizard-inline/`.
**Status:** Complete (implementation + build) — `50-01-SUMMARY.md`, `50-02-SUMMARY.md`, `50-VERIFICATION.md`; **human_needed** for runtime theme/tablet signoff and style budget follow-up.

## Phase 51: Objective Element Execution — Knowledge Base API Binding

Bind **`ObjectiveElementExecutionScreenComponent`** / **`OeExecutionWizardComponent`** to the Gateway **Knowledge Base** HTTP API (`KnowledgeBaseController`) per **ComplaincePlatform** `docs/backend/gateway-knowledge-base-api-specification.md`: typed DTOs (`FrameworkDto`, `ComplianceNodeDto`, `KnowledgeItemDto`, `NodeDetailsDto`, etc.), `GET` node details, knowledge by node, optional AI ask / decisions / assessments; **`ApiResponse<T>`** + auth (`ComplianceScope` / `compliance.api`). **Complements** Phase 49 Journey APIs (`/journeys/...`) — **does not** replace OE workspace load; resolve **journey objective-element id ↔ knowledge-base node id** (UUID) with backend product owner before shipping. See `.planning/phases/51-objective-element-knowledge-base-api-binding/`.
**Status:** Planned — `51-RESEARCH.md`, `51-01-PLAN.md`, `51-02-PLAN.md`.

## Phase 52: OE Execution Wizard — Cross-Session Persistence (Session + Server)

Persist **per objective element** wizard progress in **`sessionStorage`** (existing `journey.wizard.{userId}.{journeyId}.{oeId}` key) **and** **server-side draft** so the user can **resume on another device** after login. Extend persisted fields to include **audit checklist line checks** (currently component-local); add **versioned** JSON payload + **`revision` / `updatedAt`** for optimistic concurrency; **load order** server → session merge → refetch KB. Requires **Gateway agreement** on wizard-draft **GET/PATCH** (Phase 49 noted wizard API not yet implemented — coordinate before Wave 2). See `.planning/phases/52-oe-wizard-cross-session-persistence/`.
**Status:** Planned — `52-RESEARCH.md`, `52-01-PLAN.md`, `52-02-PLAN.md`.

## Phase 53: PRAMAN — Evidence Upload UI Enablement (Gateway API v1.3)

Prepare **OE Execution** evidence upload UI (**inline wizard** Evidence step) for **single** and **bulk** multipart uploads aligned strictly with Gateway **v1.3** routes: `POST …/objective-elements/{oeId}/evidence` and `POST …/objective-elements/{oeId}/evidence/bulk`. Implement **frontend-only** state, validation (10 MB/file, 15 files bulk), **FormData** payload builders, and **non-invoked** service methods — **no** file picker, **no** live HTTP calls from UI in this phase. See `.planning/phases/53-praman-evidence-upload-ui-gateway-v13/`.
**Status:** Complete (implementation + tests) — `53-01-SUMMARY.md`, `53-02-SUMMARY.md`, `53-VERIFICATION.md`; **human_needed** optional for manual Evidence step UX (snackbar, tooltip, readonly).

## Phase 54: Evidence Upload UI — Verification & Checklist Refinement (Post–53)

**Audit-only pass** over Phase 53 deliverables: confirm Gateway v1.3 route alignment in `JourneyDataService`, FormData field names, client validation (10 MB / 15 files), OE wizard UX (no file picker, snackbar + tooltip copy), component state/hooks, and **zero** evidence `POST` from wizard flows. **No** new product features, **no** enabling picker or API calls — **minimal fixes** only where the checklist deviates. Produce an updated compliance report (`54-VERIFICATION.md` or refresh `53-VERIFICATION.md`). See `.planning/phases/54-evidence-upload-ui-verification-refinement/`.
**Status:** Complete — `54-01-SUMMARY.md`, `54-VERIFICATION.md`; **human_needed** optional for DevTools manual smoke on Evidence step click.

## Phase 54.2: Evidence Upload — Controlled Activation (Picker + API)

**Enable** OE wizard Evidence step: hidden `<input type="file">` (single + multiple), basic **drag-and-drop**, **`triggerUpload()`** wired to **`JourneyDataService.uploadSingleEvidence` / `uploadBulkEvidence`** using existing FormData builders + validation; minimal success/error **`MatSnackBar`** (or `NotificationService` if already used for journey toasts); optional **refresh** of selected OE after upload. **Feature flag** in `environment` for instant rollback. **No** progress bars, retry UX, or contract changes to Gateway paths; bulk response typing extended only to match real Gateway JSON (confirm `successCount` / `failureCount` / `items` with backend). See `.planning/phases/54.2-evidence-upload-controlled-activation/`.
**Status:** Complete (implementation + tests) — `54.2-01-SUMMARY.md`, `54.2-02-SUMMARY.md`, `54.2-VERIFICATION.md`; **human_needed** optional for prod flag off + Gateway bulk JSON parity.

## Phase 55: Gateway Evidence API — Spec binding & client parity

Map the **canonical** Gateway document `ComplaincePlatform/docs/backend/gateway-evidence-api-specification.md` (v1.3) to the WebApp: cross-links in `docs/backend`, **strong typing** for bulk upload responses (§5.5), **clear 400/validation** messaging on evidence upload, and **`JourneyDataService` methods** for evidence detail, streamed download, verify/reject, and new-version multipart (§5.6–5.9) where not yet exposed. **Wave 1** — docs + DTOs + error UX; **Wave 2** — HTTP parity + tests. See `.planning/phases/55-gateway-evidence-api-spec-binding/`.
**Status:** Complete — `55-01-SUMMARY.md`, `55-02-SUMMARY.md`, `55-VERIFICATION.md`; **human_needed** optional for Gateway E2E on evidence detail/download/verify/version.

## Phase 56: Journey workspace — HTTP timeouts, cancellation policy & stable loading

Fix **premature client-side abort** and flaky failures for **long-running Journey GETs** (chapter standards and similar aggregation-heavy reads): align **client wait budget** with Gateway/API limits (no undocumented stricter cap), **cancel only when product-intent** (navigation away, explicit supersession), avoid accidental cancellation from **duplicate store instances**, **concurrent in-flight loads**, or **stale responses** winning races. Deliver **consistent loading/error UX** (recoverable retry), **safe retry policy** (no amplification), and **dev observability** (abort vs timeout vs network). Backend query tuning is out of scope. See `.planning/phases/56-journey-workspace-http-cancellation-timeouts/`.
**Status:** Complete — `56-01-SUMMARY.md`–`56-03-SUMMARY.md`, `56-VERIFICATION.md`; **human_needed** for Gateway/proxy timeout parity and manual Journey navigation QA.

## Phase 57: Shell Sidebar — Icon-First Navigation & Responsive Menu Refactor

Evolve the **app shell left sidebar** (post–Phase 46) toward an **icon-first** navigation model aligned with the **Task / Evidence / Finding** quick-action treatment: **config-driven** nav (`sidebar-nav.model.ts` / registry), **primary visuals via icons** (20–24px, token-scaled), **labels** as optional (expanded sidebar, **hover-flyout** in hybrid “compact” mode, or **tooltip** when labels are hidden), **active** route styling preserved/enhanced, **badges** (Findings / Issues / Alerts semantics), **MatRipple** or equivalent **pressed** feedback, and **responsive** behavior (desktop full/hybrid rail; **narrow viewports** icon strip and/or **optional bottom navigation** + existing overlay drawer) — **without** changing **Angular route paths**, **module** structure, or **backend** APIs. See `.planning/phases/57-shell-sidebar-icon-first-navigation/`.
**Status:** Complete (implementation + dev build) — `57-01-SUMMARY.md`–`57-03-SUMMARY.md`, `57-VERIFICATION.md`; **human_needed** for collapsed peek + mobile bottom nav runtime signoff. Production `ng build` may fail on pre-existing SCSS budget (see `57-VERIFICATION.md`).

## Phase 58: OE Wizard Evidence Step — Uploaded Evidence Section (Reference UI)

Add a dedicated **Uploaded Evidence** section to **`app-oe-execution-wizard`** step **Evidence** (step index 2), matching the stakeholder reference layout: **Evidence required** block (icon + title + KB paragraphs), a bordered **Uploaded Evidence** list for **server-backed** files (filename, relative upload time, type label + optional size, **Download** affordance), then the existing **drop zone** and **pending selection / Upload** flow. Bind the list to **`ObjectiveElement.evidence`** from the workspace snapshot (refreshed after successful upload per Phase 54.2). Use **`JourneyDataService.downloadObjectiveElementEvidenceFile`** for downloads; **`DESIGN_RULES.md`** and **`src/styles/tokens/*`** only — no new backend routes. If exact byte sizes are required to match the reference, coordinate a Gateway **`OeEvidenceItem`** field follow-up; until then, derive display type from filename / `name` and omit or placeholder size. See `.planning/phases/58-oe-wizard-uploaded-evidence-section/`.
**Status:** Complete (implementation + build) — `58-01-SUMMARY.md`, `58-02-SUMMARY.md`, `58-VERIFICATION.md`; **human_needed** optional for screenshot parity + Gateway download E2E.

## Phase 59: OE Wizard — Create & Assign Task (right drawer)

Design and implement a **right-side drawer** (enterprise SaaS UX: Notion / Linear / Azure Portal–inspired) for **Create & Assign Task**, opened from **`oe-wizard__quick-btn` “Add task”** on the objective-element execution wizard. Includes **five sections** (task details, assignment, classification, linkage, attachments), **sticky footer** (Cancel, Save Draft, Create Task), **responsive** full-screen on mobile, **inline validation**, **multi-assignee** with avatars, **auto-fill** standard/objective from compliance session context, and **data binding** to **`JourneyWorkspaceStore`** + session-scoped draft persistence. Styling: **16px spacing rhythm**, rounded panels, subtle shadows, priority color tokens, section icons — **`DESIGN_RULES.md`** + tokens primary; Tailwind only when aligned to tokens. See `.planning/phases/59-oe-create-assign-task-drawer/` (`59-RESEARCH.md`, `59-01-PLAN.md`, `59-02-PLAN.md`).
**Status:** Complete (implementation + build + unit smoke) — `59-01-SUMMARY.md`, `59-02-SUMMARY.md`, `59-VERIFICATION.md`; **human_needed** optional for runtime theme/tablet QA.

## Phase 60: Create & Assign Task — API binding (org members, departments, compliance create)

Bind **`CreateAssignTaskDrawerComponent`** to **Gateway** HTTP APIs: **assignee** list from **`GET /api/v{version}/organization-members/organization/{organizationId}`** (typed DTO aligned with backend); **department** as **`mat-select`** backed by the **correct hospital/organization department catalog** (endpoint to confirm with platform team — see `60-RESEARCH.md`); **Create task** submit via **`POST /compliance/compliance-tasks`** using **`COMPLIANCE_TASK_API_SPEC.md`** (`CreateComplianceTaskRequest`: `objectiveElementId`, `complianceStandardId`, `assignedToUserId`, `departmentId`, optional `departmentName`, `dueDate`, `title`, `description`, `criticality`, `programId`). Map UI enums to API strings (**TaskCriticalityLevel**, **ComplianceTaskStatus** where applicable); resolve **single assignee** vs Phase 59 multi-select **per spec** (create payload allows one `assignedToUserId`). Shared **`ComplianceTaskApiService`** (or feature client), loading/error states, draft persistence compatibility. See `.planning/phases/60-create-assign-task-api-binding/` (`60-RESEARCH.md`, `60-01-PLAN.md`, `60-02-PLAN.md`).
**Status:** Complete (implementation + build + unit tests) — `60-01-SUMMARY.md`, `60-02-SUMMARY.md`, `60-VERIFICATION.md`; **human_needed** Gateway E2E + department catalog confirmation.
## Phase 61: Enterprise Administration Platform (Stage 5)

Transform AOVIAS COMMAND from an Enterprise Audit application into a configuration-driven **Enterprise Quality Management Platform**. Three waves: **5A** — single **Enterprise Administration Workspace** with twelve tabs (Organization, Programs, Templates, Frameworks, Risk, Workflow, Notifications, Approvals, Teams, Security, AI, System), shared toolbar / search / filters / **Context Drawer**, and consolidation of scattered admin features with **no duplicate pages** and **no child sidebar menus**; **5B** — schema-driven governance domains (programs, templates, risk + severity matrices, workflow, notifications, approvals, digital signatures, AI, system configuration) under **INF-01 migration governance** (generated migrations only, `scripts/ci/validate-migrations.ps1` must pass); **5C** — replace hardcoded severity, risk, workflow, notification, approval, template, and AI behaviour with administration configuration, each domain cut over via seed → switch → verify-identical → release-to-diverge so "no regression" is demonstrated rather than assumed. Prerequisite **INF-01 verified complete** (`ComplaincePlatform` commit `a73c9be`). See `.planning/phases/61-enterprise-administration-platform/` (`61-RESEARCH.md`, `61-01-PLAN.md`, `61-02-PLAN.md`, `61-03-PLAN.md`).
**Status:** Wave 1 (Stage 5A) Complete (implementation + build + route verification) — `61-01-SUMMARY.md`; **human_needed** for rendered-UI signoff (workspace is behind `authGuard`). Waves 2–3 (Stages 5B, 5C) Planned.
