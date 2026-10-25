# Journey module — controls, behaviours, and API wiring

**Purpose:** Baseline for refactoring (including elevating or restructuring **Standards** in the module).  
**Scope:** `src/app/features/journey/`** as of Phase 35 completion + HTTP binding (waves 06–07).

---

## 1. Current status (integration + build)


| Area               | Status                                                                                                                                                                          |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Build**          | `ng build --configuration=development` succeeds (re-verified 2026-04-07).                                                                                                       |
| **Route**          | `/app/journey` (lazy), behind `authGuard` + `onboardingGuard` on parent `/app`.                                                                                                 |
| **Workspace load** | `GET /api/v1/Journeys/workspace?journeyId={uuid}` via `JourneyDataService.getWorkspaceCached` (cached per journey id).                                                          |
| **Context**        | `journeyId` from `ComplianceContextService.getFrameworkId()` resolved against `OrganizationService.getHospitalJourneys(hospitalId)`; UUID fallback with warning if not in list. |
| **OE updates**     | `PATCH /Journeys/objective-elements/{oeId}` with optimistic UI + rollback on error.                                                                                             |
| **Evidence**       | `POST multipart` field `file` to `/Journeys/objective-elements/{oeId}/evidence`; `GET` evidence list available.                                                                 |
| **History**        | `GET /Journeys/objective-elements/{oeId}/history` when drawer opens; falls back to embedded `oe.history` if request fails or empty.                                             |


**Known gaps** (from `35-UI-BINDING-SUMMARY.md`): no lazy chapter-only API; multipart field name may need backend alignment; PATCH response assumed mappable to domain OE; text fields not debounced.

---

## 2. Domain hierarchy (today)

The UI implements **Chapter → Standard → Objective Element (OE)**.

- **Chapter** (`JourneyChapter`): list in left nav; contains `standards[]`.
- **Standard** (`ComplianceStandard`): **already a first-class model** — each is a Material `mat-expansion-panel` with code, risk badge, completion %, description, optional `coreSummary`, and child **OE** list.
- **Objective Element** (`ObjectiveElement`): leaf entity; cards in workspace; detail **drawer** for edits.

Refactor work around “introducing Standards” may mean: separate navigation/URL for standard, server-driven standard list without loading full workspace, standard-level metrics, or UX that treats Standard as a peer of Chapter — the **data model already has Standard**; clarify product intent against this baseline.

---

## 3. Page shell: `JourneyComponent`


| Control / area        | Behaviour                                                                                                                                    |
| --------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| **Loading**           | Full-page skeleton while `store.loading()`.                                                                                                  |
| **Error**             | Message from `store.loadError()` + **Retry** → clears cache, `initFromRoute()`.                                                              |
| **Header**            | `frameworkName`, `frameworkCode` from snapshot.                                                                                              |
| **Hidden file input** | Used for Evidence tab upload pipeline.                                                                                                       |
| **Hospital switch**   | Subscribes to `AppContextService.hospitalSwitched$` → `clearWorkspaceCache()` + `initFromRoute()`.                                           |
| **Init**              | `ngOnInit`: clears cache + `initFromRoute()`.                                                                                                |
| **Tabs**              | **Standards & objective elements** vs **Evidence** (`activeTab` signal).                                                                     |
| **Evidence tab**      | Passes flattened evidence for **selected chapter** (`chapterEvidence()`), not a single OE; upload requires `selectedOeId` (warning if none). |


---

## 4. Global panels (read-only analytics)

### `AccreditationDecisionPanelComponent`

- **Input:** `AccreditationEligibility | null`.
- **Rules:** Compares overall %, core OE average, average standard score, average chapter score against `NABH_THRESHOLDS` (`nabh-thresholds.ts`).
- **Violations:** Renders API-provided `violations[]` when non-empty.
- **No user actions** (display only).

### `JourneyNabhMetricsBarComponent`

- **Input:** `JourneyOverviewMetrics | null`.
- Shows: core compliance %, failed core OEs, OEs ≤ 3, open action plans. Failed core count styled when > 0.

### `JourneyTrendsMiniComponent`

- **Inputs:** three `JourneyTrendPoint[]` series (OE score, chapter improvement, risk).
- Renders SVG polylines (sparklines). No interaction.

### `LifecycleModeSwitchComponent`

- **Radiogroup** of four modes: Self Assessment, Internal Audit, Final Audit, Surveillance.
- **Click** sets `store.setLifecycleMode(id)` (local signal; not persisted via separate API in this pass).
- **Hint** when `store.readOnly()`: “Read-only mode for this lifecycle stage.”

---

## 5. Store: `JourneyWorkspaceStore` (single provider on page)


| State                                                     | Role                                                |
| --------------------------------------------------------- | --------------------------------------------------- |
| `snapshot`                                                | Full `JourneyWorkspaceSnapshot` or null.            |
| `loading` / `loadError`                                   | Workspace fetch lifecycle.                          |
| `selectedChapterId`, `selectedStandardId`, `selectedOeId` | Selection; chapter/OE cleared when chapter changes. |
| `lifecycleMode`                                           | Drives OE filtering + read-only (see below).        |
| `chapterSearch`, `sortChaptersByRiskDesc`                 | Chapter list filtering/sorting.                     |
| `drawerOpen`                                              | OE drawer visibility.                               |
| `oeMutationInFlight`                                      | Set of OE ids during PATCH/upload.                  |



| Computed / method             | Behaviour                                                                                                                                                     |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `readOnly`                    | `true` if `snapshot.readOnly === true` **or** lifecycle is `final_audit` or `surveillance`.                                                                   |
| `filteredChapters`            | Search by name/code; sort by code or by risk (high first).                                                                                                    |
| `selectedChapter`             | Resolved from id.                                                                                                                                             |
| `standardsForSelectedChapter` | Standards array for selected chapter.                                                                                                                         |
| `filteredOesForStandard(std)` | Applies `filterOesByLifecycle` then `sortOesByCode`.                                                                                                          |
| `initFromRoute`               | Resolves journey id; on failure sets friendly error; on success loads workspace, applies `lifecycleMode` from snapshot if present, selects **first chapter**. |
| `selectChapter`               | Clears standard + OE selection.                                                                                                                               |
| `selectOe`                    | Sets standard + OE ids, opens drawer.                                                                                                                         |
| `submitObjectiveElementPatch` | Optimistic patch, rollback + snackbar on error; clamps score 1–5.                                                                                             |


### Lifecycle → visible OEs (`journey-filters.ts`)

- **default:** all OEs.
- **final_audit:** only `classification` **core** or **commitment**.
- **surveillance:** only **core**.

---

## 6. Chapter navigation: `ChapterNavigationPanelComponent`


| Control           | Behaviour                                                                                                                                    |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| Search input      | `ngModel` → `store.setChapterSearch` — filters by chapter name or code (case-insensitive).                                                   |
| Sort button       | Toggles `store.toggleChapterRiskSort()` — label switches between “Sort: Risk” and “Sort: Code”.                                              |
| Chapter row click | `store.selectChapter(ch.id)`; `aria-current` on active row.                                                                                  |
| Row content       | Code, name, compliance %, risk pill, core compliance line (Met / At risk / Failed), optional **Core** / **Audit** badges, open issues count. |


---

## 7. Main workspace: `JourneyMainWorkspaceComponent`


| Control               | Behaviour                                                                                                       |
| --------------------- | --------------------------------------------------------------------------------------------------------------- |
| Chapter header        | Code, name, compliance %, risk label, open issues.                                                              |
| Empty states          | No chapter selected; or chapter has no standards.                                                               |
| **Standard sections** | `@for` over `standardsForSelectedChapter`; `app-standard-section` with `[expanded]="$first"`.                   |
| Lazy OE bodies        | OE list renders only if first standard **or** standard id ∈ `openedStandards` (expanded when user opens panel). |
| `onStandardOpened`    | Adds standard id to `openedStandards` set.                                                                      |
| **Effect**            | On chapter change, resets `openedStandards` to **first standard only**.                                         |


### `StandardSectionComponent`

- Material **expansion panel**: header shows standard **code**, **riskBadge**, **completionPercent**; body shows description, optional `coreSummary`, projected content (OE cards).
- `(opened)` → parent marks standard as opened for lazy list.

### `OeCardComponent`


| Control                      | Behaviour                                                                                                                                                                                                                                                   |
| ---------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Display                      | Code, classification badge, N/A pill, description (3-line clamp), score + bar, evidence/audit pills, action-plan banner if `oeRequiresActionPlan` (score ≤ 3 and not N/A), core highlight if `coreOeBelowThreshold` (core + score < 4), owner/review dates. |
| View tasks / Upload evidence | Emits events; **parent maps both to `selectOe`** (opens drawer) — tasks/evidence shortcuts do not run distinct flows.                                                                                                                                       |
| Details                      | `openDetails` → `store.selectOe`.                                                                                                                                                                                                                           |
| Disabled                     | View tasks / Upload when `readOnly()`.                                                                                                                                                                                                                      |


---

## 8. OE drawer: `OeDetailDrawerComponent`


| Control               | Behaviour                                                                                                                              |
| --------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| Backdrop / × / Escape | Closes drawer (`store.closeDrawer()`).                                                                                                 |
| Content sections      | OE code, full description, score rationale, audit remarks (read-only text).                                                            |
| N/A form              | Checkbox + textarea; **Save** validates justification when N/A (`naRequiresJustification`).                                            |
| Save                  | `submitObjectiveElementPatch` with `isNa` + `naJustification` only.                                                                    |
| Action plans          | `ActionPlanListComponent` — read-only table.                                                                                           |
| Evidence              | `JourneyEvidenceTabComponent` with drawer’s OE evidence + upload (hidden file input).                                                  |
| History               | Loads `getObjectiveElementHistory` when drawer opens; loading text; timeline sorted by timestamp **desc**; empty message if no events. |


**Note:** Drawer does **not** expose score/audit remark editing in the template (display-only for rationale/remarks); PATCH path supports more fields in API/store for future use.

---

## 9. Evidence tab (page-level)

- Same `JourneyEvidenceTabComponent` as in drawer but fed **all evidence items** across OEs in the **selected chapter**.
- Upload triggers hidden file input on page; uses `store.selectedOeId` — if no OE selected, **warning** toast to open an OE in the drawer first.

---

## 10. Services (non-UI)


| Service                         | Responsibility                                                                                                                    |
| ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `JourneyDataService`            | HTTP: workspace, PATCH OE, GET evidence, POST evidence (`FormData` + `file`), GET history; `shareReplay(1)` cache per journey id. |
| `JourneyContextResolverService` | Resolve `journeyId` from compliance + org hospital journeys.                                                                      |


---

## 11. Refactor notes (Standards-focused)

- **Today:** Standards are **nested under chapters** in one workspace payload; UI is expansion panels + lazy OE lists.
- **Possible directions:** standard-based routing (`/journey/.../standard/:id`), lazy-fetch standards for a chapter, standard-level dashboard strip, or API that returns standards before OEs — all require explicit backend contract changes beyond current `GET workspace`.
- **Reuse:** `ComplianceStandard`, `StandardSectionComponent`, and `filteredOesForStandard` are the main touchpoints for a Standard-first UX.

---

## 12. Related docs

- `.planning/phases/35-journey-module-nabh-ui-refactor/35-UI-BINDING-SUMMARY.md` — API table and known gaps.
- `.planning/phases/35-journey-module-nabh-ui-refactor/BACKEND_CONTRACT_SPEC.md` — contract notes.
- `.planning/phases/35-journey-module-nabh-ui-refactor/35-UAT.md` — manual verification checklist.