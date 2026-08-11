# Journey UI — Backend API Binding (Phase 35 waves 06–07)

## 1. APIs integrated

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/Journeys/workspace?journeyId={guid}` | Full workspace snapshot (chapters → standards → OEs, metrics, trends, optional lifecycle/readOnly) |
| `PATCH` | `/Journeys/objective-elements/{oeId}` | Update OE (N/A, justification, score, rationale, audit remarks — partial body) |
| `GET` | `/Journeys/objective-elements/{oeId}/evidence` | List evidence rows (available for refresh flows) |
| `POST` | `/Journeys/objective-elements/{oeId}/evidence` | Upload evidence (`multipart/form-data`, field `file`) |
| `GET` | `/Journeys/objective-elements/{oeId}/history` | Timeline events (drawer loads when open) |

All paths are relative to `environment.apiBaseUrl` (e.g. `https://localhost:7216/api/v1`). Successful responses use the global `ApiResponse<T>` envelope; the `apiResponseInterceptor` unwraps to `T` before mapping.

## 2. Components / files updated

| Area | Files | Role |
|------|--------|------|
| DTOs | `src/app/features/journey/models/journey-api.model.ts` | HTTP boundary types for workspace, PATCH body, nested DTOs |
| Mapper | `src/app/features/journey/mappers/journey-workspace.mapper.ts` | API → domain `JourneyWorkspaceSnapshot` + `mapObjectiveElementApiToDomain`, evidence/history helpers |
| Context | `src/app/features/journey/services/journey-context-resolver.service.ts` | Resolve `journeyId` from `ComplianceContextService` + `OrganizationService.getHospitalJourneys` |
| Data service | `src/app/features/journey/services/journey-data.service.ts` | HTTP, `shareReplay(1)` workspace cache per journey id |
| Store | `src/app/features/journey/state/journey-workspace.store.ts` | Load/retry, lifecycle from snapshot, optimistic PATCH + rollback, evidence merge, OE mutation tracking |
| Domain | `src/app/features/journey/models/journey-tree.model.ts` | Optional `lifecycleMode`, `readOnly` on snapshot |
| Errors | `src/app/features/journey/utils/journey-api-error.util.ts` | User-facing messages from `HttpErrorResponse` |
| Drawer | `.../oe-detail-drawer/oe-detail-drawer.component.ts` | PATCH via store, history GET, evidence upload |
| Page | `.../pages/journey.component.ts` | Skeleton loading, error + Retry, hospital switch cache invalidation, evidence tab upload |
| Timeline | `.../oe-history-timeline/oe-history-timeline.component.ts` | Empty state when no events |
| Removed | `src/app/features/journey/data/journey-workspace.fixture.ts` | Mock fixture deleted |

## 3. Known gaps

- **Lazy chapter API**: `getStandardsForChapter` slices the cached workspace only; no separate lazy chapter endpoint.
- **Multipart contract**: POST evidence assumes field name `file`; backend may require different field names or extra metadata.
- **PATCH response**: Mapper assumes PATCH returns a full/partial OE DTO compatible with `mapObjectiveElementApiToDomain`; highly partial responses still merge optimistically.
- **History + snapshot**: Drawer prefers GET `/history` when loaded; falls back to embedded `oe.history` from workspace if the request fails or returns empty.
- **Debounce**: Text fields (audit remarks / score) are not debounced in this pass; N/A save is explicit button action.

## 4. Assumptions

- **`journeyId`**: `ComplianceContextService.getFrameworkId()` matches `HospitalJourney.journeyId` from `organization-detail` for the active hospital; if not listed but value is a UUID, it is still used (with a console warning).
- **Envelope**: List endpoints return arrays as `data` after unwrap; POST evidence returns an array of evidence DTOs in `data`.
- **Read-only**: UI respects `snapshot.readOnly` from API and existing lifecycle rules (`final_audit`, `surveillance`).

## 5. Testing notes (manual smoke)

1. Select hospital + framework in sidebar; open `/app/journey` — workspace loads (network: `GET .../Journeys/workspace?journeyId=`).
2. Open an OE in the drawer — `GET .../history` runs; N/A save triggers `PATCH .../objective-elements/{oeId}`.
3. Upload evidence from drawer or Evidence tab (OE selected) — `POST .../evidence` with `FormData`.
4. Turn off API or force 500 — error message + **Retry** on page load; snackbar on PATCH/upload failure.
5. Switch hospital from sidebar — journey reloads (cache cleared, new workspace fetch).
