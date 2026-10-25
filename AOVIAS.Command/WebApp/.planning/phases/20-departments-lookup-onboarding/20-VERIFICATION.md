---
phase: 20
status: passed
---

# Verification Phase 20: Departments Lookup (Onboarding)

## Must-haves check

- [x] **Departments load dynamically from API** via `CommonLookupService.loadDepartments()` → `BaseApiService.get('/common/departments')` (base url already includes `/api/v1`)
- [x] **No hardcoded department values remain** in onboarding flow (`OnboardingDataService.getDepartments()` removed; onboarding component no longer uses static list)
- [x] **Binding uses Name/Id**: UI renders `dept.name` and stores `dept.id` in `departmentIds`
- [x] **Multi-select behavior** works with add/remove and no duplicates (toggle guards)
- [x] **Form/state stores only GUIDs** (`HospitalProfileData.departmentIds: string[]`)
- [x] **Edit mode late-load support**: pre-populated ids show selected once list loads; missing ids are detected and user can clear them
- [x] **Error + empty handling**:
  - API error surfaces to UI with retry
  - Empty list shows “No departments available” and blocks Continue (required selection)
- [x] **Performance/caching**: departments cached in service via in-memory `BehaviorSubject` (loads once per session unless refresh)

## Manual spot checks performed

- Searched onboarding feature for `getDepartments()` / `profileData.departments` — none found
- Confirmed `environment.apiBaseUrl` includes `/api/v1`
- Confirmed no new lints in touched files

## Known limitations / follow-ups (non-blocking)

- The AI onboarding wizard currently holds state in-memory (not persisted); if persistence is added later, `departmentIds` is the correct contract to persist/submit as `DepartmentIds`.

