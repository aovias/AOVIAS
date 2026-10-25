# Phase 35 — User acceptance testing (Journey module)

**Phase:** 35 — Journey module NABH UI + API binding  
**Last updated:** 2026-04-07

## Summary


| Item                  | Result                                                                        |
| --------------------- | ----------------------------------------------------------------------------- |
| Automated build       | **Pass** — `ng build --configuration=development` (2026-04-07)                |
| Implementation status | **Complete** per `.planning/STATE.md` — HTTP APIs wired; mock fixture removed |
| Manual UAT below      | **Pending** — run in app when backend/API available                           |


## Integration checklist (smoke)

Derived from `35-VERIFICATION.md` and `35-UI-BINDING-SUMMARY.md`.


| #   | Test                                                          | Expected                                                          | Result  | Notes |
| --- | ------------------------------------------------------------- | ----------------------------------------------------------------- | ------- | ----- |
| 1   | Navigate to `/app/journey` with hospital + framework selected | Workspace loads; network: `GET .../Journeys/workspace?journeyId=` | Pending |       |
| 2   | No journey in context                                         | Friendly error: choose hospital/framework from sidebar            | Pending |       |
| 3   | Load failure (e.g. 500)                                       | Error message + **Retry** reloads                                 | Pending |       |
| 4   | Select chapter                                                | Main column shows chapter header + standards list                 | Pending |       |
| 5   | Expand standard (not first)                                   | OE cards load for that standard after first expand                | Pending |       |
| 6   | Open OE **Details**                                           | Drawer opens; `GET .../history` runs                              | Pending |       |
| 7   | Mark N/A without justification + **Save**                     | Blocked with validation message                                   | Pending |       |
| 8   | Mark N/A with justification + **Save**                        | `PATCH` succeeds; optimistic UI                                   | Pending |       |
| 9   | Upload evidence (drawer)                                      | `POST` multipart; list updates; success toast                     | Pending |       |
| 10  | Evidence tab — upload without OE selected                     | Warning toast                                                     | Pending |       |
| 11  | Evidence tab — upload with OE selected in drawer              | Upload applies to that OE                                         | Pending |       |
| 12  | Switch lifecycle to Final / Surveillance                      | Hint “Read-only…”; OE list filters (core+commitment / core-only)  | Pending |       |
| 13  | Switch hospital (sidebar)                                     | Journey cache cleared; new workspace fetch                        | Pending |       |


## Issues found (conversational UAT)

*Log gaps here during manual testing (severity inferred).*


| ID  | Test # | Description | Severity | Status |
| --- | ------ | ----------- | -------- | ------ |
| —   | —      | —           | —        | —      |


## Full controls reference

See `**35-JOURNEY-CONTROLS-AND-BEHAVIORS.md`** in this folder.

## Routing note

- **Route A (next phase):** If all manual tests pass and roadmap continues — proceed to Phase 34 or next planned phase per `ROADMAP.md`.
- **Route C:** If issues found — diagnose, then `/gsd-execute-phase` with gaps-only after fix plans exist.

