# PRAMAN Frontend Architecture Analysis

**Generated:** 2026-05-07  
**Agent:** Frontend Engineer Persona (PRA-7)  
**Scope:** WebApp — Angular 21 SPA at `V:\DevelopmentRepo\V1.0\WebApp`  
**Constitution:** PRAMAN Architecture Constitution v1.0

---

## 1. Architecture Map

### 1.1 Technology Stack

| Concern | Technology |
|---|---|
| Framework | Angular 21.1 (standalone component model) |
| UI Library | Angular Material 21.2 |
| CSS Utility | Tailwind CSS 4 (PostCSS plugin) |
| Icon System | ng-icons 33 (Lucide + Material Icons) |
| Reactive State | RxJS 7.8 + Angular Signals |
| HTTP | Angular HttpClient + interceptor chain |
| Build | Angular CLI 21 / `@angular/build` |
| Linting | ESLint + `@angular-eslint` + Prettier |
| Testing | Vitest 4 |
| Design Tokens | CSS custom properties in `src/styles/tokens/` |

### 1.2 Project Structure

```
WebApp/src/app/
├── app.config.ts           — Application providers: router, HTTP interceptors, DI
├── app.routes.ts           — Root route registry (lazy loadChildren per feature)
├── app.ts / app.html       — Root component bootstrap
├── login/                  ⚠ Outside features/ — architectural boundary violation
├── signup/                 ⚠ Outside features/ — architectural boundary violation
├── core/                   — Infrastructure: auth, guards, interceptors, layout, services
│   ├── auth/               — TokenService, SessionService, TokenRefreshService, RoleDefinitions
│   ├── config/             — GeoConfig DI token
│   ├── guards/             — authGuard, onboardingGuard, roleGuard, frameworkGuard
│   ├── http/               — HTTP context tokens (SKIP_AUTH_REFRESH, journey long-read)
│   ├── interceptors/       — loading, auth, journeyLongRead, apiResponse
│   ├── layout/             — ShellComponent, SidebarComponent, HeaderComponent, BottomNavComponent
│   ├── pipes/              — RoleLabelPipe
│   ├── services/           — AppContext, UserContext, ComplianceContext, HospitalContext, Loading, Logger, Theme, Organization...
│   ├── session/            — (path exists under auth/)
│   └── ui/                 — NotificationService (referenced in feature stores)
├── shared/                 — Cross-feature reusable UI and services
│   ├── models/             — GeoModels, LookupModels, OrganizationMemberApiModel
│   ├── services/           — BaseApiService, CommonLookupService, GeoSelectorService
│   └── ui/                 — Shared components (table, dialog, form-field, status-badge, etc.)
└── features/               — Domain feature modules
    ├── auth/               — Error pages (403, 404, 500), forgot/reset password, register
    ├── audit/              — Findings, corrective actions, simulation, reports, dashboard
    ├── compliance-control-center/ — 7 CCC sub-domains (placeholder stubs)
    ├── control-center/     — Compliance/task command centers, knowledge graph
    ├── dashboard/          — Executive dashboard with charts and KPI cards
    ├── evidence/           — Evidence list + binder (with AI mapping, checklist, upload)
    ├── journey/            — Compliance execution workspace (most complex feature)
    ├── onboarding/         — AI-guided setup wizard (multi-step stepper)
    ├── reporting/          — Reports landing + 6 category sub-pages
    └── tasks/              — Task list (table/kanban/risk views) + detail drawer
```

---

## 2. Routing Architecture

### 2.1 Route Tree

```
/ → /app (redirect)
/login                          — LoginComponent (lazy, no guard)
/signup                         — SignupComponent (lazy, no guard)
/register                       — RegisterComponent (lazy, no guard)
/forgot-password                — ForgotPasswordComponent (lazy)
/reset-password                 — ResetPasswordComponent (lazy)
/onboarding                     — [authGuard, onboardingEntryGuard] AiSetupAssistantComponent
/error/403|404|500|maintenance  — Error pages (lazy)
/app                            — [authGuard, onboardingGuard] ShellComponent
  /dashboard                    — DashboardComponent
  /control-center               — ComplianceCommandCenterComponent
    /tasks                      — TaskCommandCenterComponent
    /knowledge-graph            — KnowledgeGraphComponent
  /journey                      — JourneyFeatureShellComponent (provides JourneyWorkspaceStore)
    /                           — JourneyComponent
    /execution                  — JourneyExecutionShellComponent
      /                         — ExecutionScreenComponent
      /Progress/:oeGuid         — ObjectiveElementExecutionScreenComponent
  /tasks                        — TaskPageComponent
  /evidence                     — EvidenceComponent
    /binder                     — EvidenceBinderPageComponent
  /audit                        — AuditDashboardComponent
    /findings                   — AuditFindingsPageComponent
    /findings/:id               — AuditFindingDetailComponent
    /corrective-actions         — CorrectiveActionsComponent
    /simulation                 — AuditSimulationComponent
    /reports                    — AuditReportsComponent
  /reports                      — ReportsLandingComponent
    /compliance|audit|tasks|evidence|readiness|custom — ReportSubPages
  /ccc                          — ComplianceControlCenter (redirects to /regulatory-governance)
    /regulatory-governance|organizational-control|access-accountability|...
```

### 2.2 Routing Quality

**Strengths:**
- All feature routes use `loadComponent` or `loadChildren` — full lazy loading achieved ✅
- `ShellComponent` is lazy loaded as parent for all authenticated routes ✅
- Journey uses two nested shell components (`JourneyFeatureShellComponent` → `JourneyExecutionShellComponent`) to scope the `JourneyWorkspaceStore` lifetime correctly ✅
- `authGuard` and `onboardingGuard` correctly gate the `/app` subtree ✅

**Gaps:**
- `roleGuard` exists but is **not applied to any route** in `app.routes.ts` or feature routes — RBAC route protection is inoperative at the routing layer
- `frameworkGuard` exists but usage scope is unclear
- `/login` and `/signup` have no redirect-if-already-authenticated guard
- `control-center` has a route `/` and `/compliance` that both load the same component (redundant path)

---

## 3. Component Hierarchy

### 3.1 Shell / Layout Layer (`core/layout/`)

```
ShellComponent
├── HeaderComponent (header-top-nav navigation)
│   └── HeaderTopNavComponent
├── SidebarComponent
│   ├── SidebarComplianceProgressComponent
│   └── SidebarAiAssistantComponent
├── BottomNavComponent (mobile tab bar)
└── <router-outlet> → feature pages
```

**Shell highlights:**
- `ShellSidebarService` manages sidebar state (open/closed, overlay vs rail mode, rail collapse)
- Sidebar uses `CdkTrapFocus` for overlay mode accessibility ✅
- ResizeObserver measures header height dynamically and exposes as CSS custom property ✅
- `BottomNavComponent` provides mobile bottom navigation ✅
- `header-old.component.ts` is a **dead file** — orphaned component still in `core/layout/`

### 3.2 Feature Component Counts

| Feature | Pages | Components | Services | State Stores |
|---|---|---|---|---|
| journey | 6 | 30+ | 7 | 1 (JourneyWorkspaceStore, 1934 lines) |
| tasks | 1 | 6 | 1 | 1 (TaskStore) |
| evidence/binder | 1 | 11 | 2 | 1 (evidence-binder.store) |
| onboarding | 1 | 7 | 4 | 2 (OnboardingStore, AiOnboardingWizardStore) |
| dashboard | 1 | 8 | 1 | — |
| audit | 6 | 3 | 1 | — |
| control-center | 3 | 8 | 2 | — |
| reporting | 7 | — | — | — |
| compliance-control-center | 7 | — | — | — |

---

## 4. Shared Component Architecture

### 4.1 Existing Shared UI Components (`shared/ui/`)

| Component | Purpose | Status |
|---|---|---|
| `TableComponent` | Generic mat-table wrapper with paginator | Underutilized — tasks and audit use their own table implementations |
| `StatusBadgeComponent` | Color-coded status pill | Present |
| `ProgressIndicatorComponent` | Progress bar/spinner | Present |
| `DialogComponent` | Generic dialog shell | Present |
| `ConfirmationDialogComponent` | Yes/no confirmation | Present |
| `FormFieldComponent` | Labeled form input wrapper | Present |
| `FileUploadComponent` | Drag-and-drop file upload | Present |
| `GeoSelectorComponent` | Country/state/city cascading selector | Present |
| `HospitalDisplayComponent` | Hospital name + org display | Present |
| `HospitalSelectorDialogComponent` | Framework-aware hospital switcher | Present |
| `HospitalSelectorModal` | Duplicate hospital selector? | ⚠ Possible duplication with above |
| `AccreditationProgressCardComponent` | Progress card for accreditation | Present |
| `FrameworkCardIconComponent` | Framework logo/icon | Present |
| `ThemeToggleComponent` | Light/dark mode toggle | Present |

### 4.2 Identified Duplicate UI Implementations

1. **Tables**: `shared/ui/table/table.component.ts` exists but `tasks/components/task-table.component.ts` and `audit/components/audit-findings-table.component.ts` implement their own table patterns. No evidence the shared table is used in these features.

2. **CCC placeholder pages**: 7 sub-modules under `compliance-control-center/` each have an identical `ccc.component.ts` stub (same boilerplate, only the `<h1>` title differs). Should be a single parameterized shell component receiving the section title via route data or input.

3. **Hospital selector**: `hospital-selector-modal/` and `hospital-selector/hospital-selector-dialog.component.ts` — two components serving the same function.

4. **Reporting sub-pages**: 6 report sub-pages (`compliance-reports`, `audit-reports`, `task-reports`, `evidence-reports`, `readiness-reports`, `custom-reports`) likely share identical stub structure — candidate for a single parameterized shell.

---

## 5. State Management Architecture

### 5.1 Pattern

All feature stores follow a consistent signal-based pattern:

```typescript
@Injectable()   // Provided at component level (scoped lifetime)
export class XyzStore {
  // Signals for each slice of state
  readonly data = signal<Item[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  // Computed derived state
  readonly filtered = computed(() => ...);

  // Subjects for action dispatch
  private readonly load$ = new Subject<void>();
}
```

**Strengths:**
- Signals used for fine-grained reactivity ✅
- Stores are scoped via component-level `providers: [XyzStore]` — prevents state leaks across feature boundaries ✅
- `JourneyFeatureShellComponent` correctly scopes the store to the journey subtree ✅

**Gaps:**
- `UserContextService` uses `BehaviorSubject`/`Observable` (old pattern) — inconsistent with signal stores; candidate for signal migration
- No global signal/state coordination layer (acceptable for current scale, but worth documenting for growth)
- `JourneyWorkspaceStore` is 1934 lines — violates single responsibility; should be decomposed into execution, wizard, and dashboard sub-stores

### 5.2 Store Inventory

| Store | Provider Level | Lines | Complexity |
|---|---|---|---|
| `JourneyWorkspaceStore` | `JourneyFeatureShellComponent` | 1934 | Very High — candidate for decomposition |
| `TaskStore` | `TaskPageComponent` | ~400 | Medium |
| `OnboardingStore` | `providedIn: 'root'` | ~150 | Low |
| `AiOnboardingWizardStore` | Feature level | ~150 | Low |
| `EvidenceBinderStore` (binder) | Binder page | ~300 | Medium |

---

## 6. API Integration Patterns

### 6.1 HTTP Interceptor Chain

```
Request pipeline:
  loadingInterceptor       — shows global loading spinner
  → authInterceptor        — attaches Bearer token + X-Hospital-Id header; handles 401/token-refresh
  → journeyLongReadInterceptor — applies RxJS timeout to tagged long-read journey GETs
  → apiResponseInterceptor — unwraps ApiResponse<T> envelope; throws on success=false
```

**Strengths:**
- Tenant isolation header `X-Hospital-Id` injected on every authenticated request ✅
- Token refresh with `switchMap` prevents parallel 401 storms ✅
- `SKIP_AUTH_REFRESH` HttpContext token prevents infinite refresh loops ✅
- API envelope unwrapping is centralized ✅
- Evidence multipart upload uses raw XHR (bypasses interceptor chain — intentional, documented)

**Gaps:**
- `loadingInterceptor` increments/decrements on every request without request-type exclusions — can cause flickering on concurrent requests (no counter-based approach; uses `show()`/`hide()` directly)
- Error handling in `apiResponseInterceptor` throws a synthetic 400 for all API-level failures regardless of actual HTTP status — can mask real status codes from feature error handlers

### 6.2 BaseApiService

All feature API services extend `BaseApiService` which provides typed `get`, `post`, `put`, `patch`, `delete` methods wrapping Angular `HttpClient`. This is correctly used across:
- `FindingApiService` (audit)
- `DashboardService`
- `JourneyDataService`
- `TaskApiService`
- `EvidenceApiService`
- `EvidenceBinderApiService`

**Gap:** Some services reference `environment.apiBaseUrl` but `BaseApiService` already encapsulates it — verify no services bypass the base class and call `HttpClient` directly.

---

## 7. Form Architecture

### 7.1 Pattern Usage

Forms use Angular Reactive Forms throughout:
- `LoginComponent` — `FormGroup` with `email` + `password` + `rememberMe`
- `SignupComponent` — multi-step `FormGroup` with step validation
- `CreateAssignTaskDrawerComponent` — reactive task creation form
- Onboarding steps — individual step `FormGroup` instances

**Strengths:**
- Validators applied at `FormControl` level ✅
- Signal-wrapped `isSubmitting` to prevent double-submit ✅

**Gaps:**
- `shared/ui/form-field/form-field.component.ts` exists as a reusable wrapper but usage is inconsistent — onboarding and login forms don't appear to use it
- No centralized form error message strategy — each component handles error display independently

---

## 8. Table / Grid Patterns

### 8.1 Current State

| Feature | Table Implementation | Shared Table Used? |
|---|---|---|
| Tasks | `TaskTableComponent` (custom, mat-table) | No |
| Audit | `AuditFindingsTableComponent` (custom, mat-table) | No |
| Shared | `shared/ui/table/table.component.ts` | — (unused in features) |

The shared `TableComponent` is minimal (accepts `dataSource` input, renders `ng-content` columns). It has a paginator built in but lacks:
- column definition API
- sorting integration
- filter/search slot
- row selection
- empty-state slot

This explains why features implement their own tables rather than extending the shared one. The shared table component is effectively non-functional as a reuse vehicle in its current state.

---

## 9. Dashboard Architecture

`DashboardComponent` is a composition page that:
- Loads `DashboardSnapshot` from `DashboardService`
- Renders 8 child components: `ExecutiveSummaryComponent`, `GaugeChartComponent`, `RiskDistributionComponent`, `HighRiskChaptersComponent`, `ChapterBreakdownComponent`, `ScoreTrendChartComponent`, `HospitalOverviewComponent`, `TaskTypeBreakdownComponent`
- Uses skeleton loading pattern while data loads ✅
- Uses `ChangeDetectionStrategy.OnPush` ✅
- Reacts to `ComplianceContextService` framework/hospital changes via `combineLatest` + `debounceTime`

Charts use inline SVG/canvas rendering (no external chart library detected). This is a maintainability concern for complex chart components.

---

## 10. Lazy Loading and Performance

### 10.1 Lazy Loading

- All routes use `loadComponent` or `loadChildren` ✅
- Angular Material modules imported per-component (not globally) ✅
- `JourneyWorkspaceStore` uses `switchMap` on a load Subject to cancel superseded loads ✅
- `throttleTime` applied to workspace loads to prevent rapid-fire calls ✅

### 10.2 Performance Gaps

1. **JourneyWorkspaceStore (1934 lines)**: The store holds all journey state in one service. Under memory pressure on slow tablets, this can remain loaded for the full session. Decomposition into sub-stores would improve GC and testability.

2. **No virtual scrolling**: Tasks table, audit findings table, and OE tables do not use `cdkVirtualScrollViewport`. For large datasets (500+ tasks or OEs), rendering will degrade.

3. **Loading interceptor design**: Global `show()`/`hide()` without a request counter means any concurrent request completes and hides the spinner while others are still in-flight.

4. **SVG gauge charts**: Inline SVG renders synchronously during Angular's CD cycle. For the dashboard with 8 chart components, this can cause long tasks on initial paint.

5. **No HTTP response caching**: `JourneyCacheService` exists (referenced in store) — scope of caching should be documented.

---

## 11. Design System and Consistency

### 11.1 Design Token System

**Foundation tokens** (`foundation.tokens.css`): Typography scale, 4px spacing system, border radii, breakpoints, z-index.  
**Theme tokens** (`theme.tokens.css`): Light/dark mode semantic tokens (`--bg-primary`, `--text-primary`, `--border-color`, `--color-status-*`, etc.)

This is a well-structured token system. Tailwind CSS 4 is configured alongside (scoped to `src/**`) providing utility classes where appropriate.

### 11.2 Consistency Issues

1. **Tailwind vs CSS variables**: Some components use Tailwind utility classes; others use CSS-in-`styles` with custom property references. No enforced rule on which to use where. Risk of visual inconsistency as the team grows.

2. **Material theme**: `indigo-pink` prebuilt theme imported as baseline, with some overrides in `material-m3-button-overrides.css`. Angular Material v21 defaults to M3 — verify the prebuilt theme is intentional vs migrated.

3. **Color usage in reports landing**: `reports-landing.component.ts` passes inline `[style.color]="category.color"` and `[style.border-left-color]="category.color"` with hardcoded hex strings in component TS — bypasses design tokens (constitution violation: Section 4.2).

4. **Icon rendering**: SVG icons in some components use `[innerHTML]="category.icon"` with raw SVG strings (reports landing) — bypasses `ng-icons` system used elsewhere; XSS risk if icon strings were dynamic.

---

## 12. Responsive Behavior

### 12.1 Strengths

- `ShellComponent` has explicit mobile CSS: `bottom-nav` padding, full-width main content on `max-width: 767px` ✅
- `BottomNavComponent` provides mobile-specific navigation ✅
- Sidebar supports overlay mode (mobile) vs rail mode (desktop/tablet) with `ShellSidebarService` ✅
- `CdkTrapFocus` on sidebar overlay prevents focus escape on mobile ✅

### 12.2 Gaps

1. **Journey execution workspace**: The OE execution wizard and standards panel are complex multi-panel layouts. Responsive behavior on tablet (768px–1024px) is not evident from code inspection and likely requires testing.

2. **Task table**: Dense data tables typically require horizontal scroll or column hiding on mobile. No evidence of responsive table handling.

3. **Audit findings table**: Same concern as tasks — no responsive table strategy.

4. **Dashboard charts**: Chart components use fixed pixel dimensions in some cases — may overflow on smaller viewports.

---

## 13. Accessibility

### 13.1 Strengths

- Sidebar has `aria-label="Main navigation"` ✅
- Footer rail toggle has `aria-label` bound to expand/collapse state ✅
- `CdkTrapFocus` on sidebar overlay ✅
- `role="status"` on overdue task banner ✅
- Footer toggle uses `type="button"` to avoid accidental form submission ✅
- `aria-hidden="true"` on decorative icon elements ✅

### 13.2 Gaps

1. **No focus management on route navigation**: Angular Router doesn't auto-focus `<main>` on navigation. No `DOCUMENT` scroll/focus restoration strategy detected.

2. **Dynamic SVG icons via `innerHTML`**: Screen readers may not announce inline SVG correctly without `aria-label` or `<title>` elements.

3. **Chart accessibility**: Dashboard charts (gauge, trend, risk distribution) have no ARIA roles, `aria-label`, or data table fallbacks for screen reader users.

4. **Form errors**: Error messages in login/signup forms use conditional `@if` but may not be announced via `aria-live` regions.

5. **Status badges**: `StatusBadgeComponent` — verify it uses text content or `aria-label` rather than color alone to convey status.

---

## 14. Authentication Flows

### 14.1 Login Flow

```
LoginComponent
  → AuthService.login(email, password)
  → POST /identity/login
  → TokenService.storeTokens(access, refresh)
  → SessionService.setSessionValid()
  → UserContextService.setUser(user)
  → ComplianceContextService.setHospitalId(...)
  → OrganizationService.loadOrganizationDetails()
  → Router.navigate(['/app/dashboard']) or returnUrl
```

### 14.2 Token Refresh Flow

```
authInterceptor catches 401
  → TokenRefreshService.getFreshAccessToken$()
  → POST /identity/refresh
  → Retry original request with new token
  → SKIP_AUTH_REFRESH context flag prevents retry loops
```

### 14.3 Session / Guard Flow

```
authGuard:       SessionService.isSessionValid() → true | redirect /login
onboardingGuard: OrganizationService.loadOrganizationDetails()
                   → hasAnyOnboardedHospital() → true | redirect /onboarding
onboardingEntryGuard: inverse — redirect already-onboarded users to /app/dashboard
roleGuard:       UserContextService.getUserRoles() → checks allowed roles (⚠ not wired to routes)
```

---

## 15. Enterprise UX Assessment

### 15.1 Navigation Architecture

The navigation uses a three-tier system:
- **Header**: `HeaderTopNavComponent` with mega-menu (section icons + grouped nav items)
- **Sidebar**: Collapsible rail with section groupings, compliance progress bar, AI assistant
- **Bottom nav**: Mobile-only 5-tab shortcut bar

This is a well-thought-out enterprise navigation pattern that handles both dense desktop and mobile use cases.

### 15.2 Workflow UX

- **Journey execution**: Most mature workflow — multi-level navigation (chapter → standard → OE), wizard for OE execution, task creation drawer, evidence upload
- **Tasks**: Three view modes (table, kanban, risk) — good enterprise flexibility
- **Evidence binder**: Chapter sidebar + checklist + evidence drawer — solid workflow
- **Audit**: Finding detail, corrective actions, simulation — coverage exists

### 15.3 UX Inconsistencies

1. **CCC module**: 7 sub-domains are all placeholder stubs — users navigating `/ccc` see blank placeholder pages
2. **Reporting sub-pages**: May be stubs depending on implementation depth
3. **Control center vs CCC**: Two modules with overlapping semantic purpose (`control-center` has command centers; `ccc` has compliance control center sub-domains) — product boundary may confuse users

---

## 16. Frontend Technical Debt Report

### Priority 1 — Architecture Violations (constitution breaches)

| ID | Issue | Location | Constitution Rule |
|---|---|---|---|
| FE-TD-001 | `login/` and `signup/` outside `features/` | `src/app/login/`, `src/app/signup/` | Sec 4.1 — feature-based module structure |
| FE-TD-002 | 7 identical CCC placeholder components | `features/compliance-control-center/*/pages/ccc.component.ts` | Sec 1.3 — no duplication |
| FE-TD-003 | Hardcoded hex colors in reports landing | `features/reporting/pages/reports-landing.component.ts` | Sec 4.2 — no hardcoded styles |
| FE-TD-004 | `roleGuard` not applied to any route | `app.routes.ts`, feature routes | Sec 5.1 — enforce RBAC |
| FE-TD-005 | Raw SVG via `innerHTML` bypasses icon system | `features/reporting/pages/reports-landing.component.ts` | Sec 4.2 — no bypassing shared patterns |

### Priority 2 — Maintainability Debt

| ID | Issue | Location | Impact |
|---|---|---|---|
| FE-TD-006 | `JourneyWorkspaceStore` is 1934 lines | `features/journey/state/journey-workspace.store.ts` | Very hard to maintain, test, or extend |
| FE-TD-007 | Dead file `header-old.component.ts` | `core/layout/header-old.component.ts` | Dead code confusion |
| FE-TD-008 | Duplicate hospital selector components | `shared/ui/hospital-selector-modal/` vs `hospital-selector/` | Inconsistent reuse |
| FE-TD-009 | Shared `TableComponent` not reusable in practice | `shared/ui/table/table.component.ts` | Features re-implement tables |
| FE-TD-010 | `UserContextService` uses BehaviorSubject (old pattern) | `core/services/user-context.service.ts` | Inconsistent with signal stores |

### Priority 3 — Performance Gaps

| ID | Issue | Impact |
|---|---|---|
| FE-TD-011 | No virtual scrolling on task/audit/OE tables | Degraded performance at scale |
| FE-TD-012 | Loading interceptor no request counter | Spinner hides too early on concurrent requests |
| FE-TD-013 | Dashboard inline SVG charts | Long tasks on initial paint |

### Priority 4 — Accessibility Gaps

| ID | Issue | Impact |
|---|---|---|
| FE-TD-014 | No focus management on route change | Screen reader users lose context on navigation |
| FE-TD-015 | Chart components have no ARIA/fallback | Inaccessible to screen reader users |
| FE-TD-016 | Form errors may not use aria-live | Screen readers may miss validation feedback |

---

## 17. Reusable Component Opportunities

| Opportunity | Benefit | Effort |
|---|---|---|
| Parameterized CCC section shell (replaces 7 stubs) | Eliminate 7 duplicate components | Low |
| Enhanced shared `TableComponent` (columns API, sort, filter, empty state, virtual scroll) | Unify 3+ table implementations | Medium |
| Shared reporting sub-page shell (title + chart placeholder) | Reduce reporting stub duplication | Low |
| Journey sub-store decomposition (workspace / wizard / dashboard) | Improve testability of 1934-line store | High |
| Unified form-error display directive/component | Consistent form UX, accessibility | Low |
| Shared page-header component (title + breadcrumb + actions bar) | Most feature pages repeat this pattern | Low |

---

## 18. Summary Scorecard

| Dimension | Score | Notes |
|---|---|---|
| Module structure | 7/10 | Feature-based but login/signup misplaced |
| Routing architecture | 8/10 | Full lazy loading; roleGuard wiring gap |
| Shared component reuse | 5/10 | Library exists but underutilized; duplications present |
| State management | 7/10 | Signal stores consistent; journey store too large |
| API integration | 8/10 | Interceptor chain well-designed; minor gaps |
| Form architecture | 6/10 | Reactive forms correct; shared form-field underused |
| Performance | 6/10 | Lazy loading good; no virtual scroll; chart rendering |
| Design system | 7/10 | Token system solid; inline styles violate it in places |
| Responsive behavior | 7/10 | Shell handles breakpoints; dense feature pages less clear |
| Accessibility | 5/10 | Good shell basics; charts/focus/forms need work |
| Enterprise UX | 7/10 | Navigation mature; CCC and reporting are stubs |
| **Overall** | **6.8/10** | Solid foundations, targeted technical debt to address |

---

## 19. Recommended Next Actions (Prioritized)

1. **FE-TD-001** — Move `login/` and `signup/` into `features/auth/pages/` (constitution compliance)
2. **FE-TD-004** — Wire `roleGuard` to appropriate `/app/**` routes per RBAC matrix
3. **FE-TD-002** — Replace 7 CCC placeholder stubs with a single parameterized `CccSectionShellComponent` using route data
4. **FE-TD-006** — Decompose `JourneyWorkspaceStore` into `JourneyDashboardStore`, `JourneyExecutionStore`, `OeWizardStore`
5. **FE-TD-009** — Extend shared `TableComponent` with column definition API and virtual scroll to enable real reuse
6. **FE-TD-007** — Delete `header-old.component.ts`
7. **FE-TD-014/015** — Add Angular Router scroll/focus strategy; add ARIA to chart components
8. **FE-TD-003/005** — Replace hardcoded colors and raw SVG in reporting landing with design token vars and `ng-icons`

---

*Analysis performed without modifying implementation. All findings are read-only observations.*
