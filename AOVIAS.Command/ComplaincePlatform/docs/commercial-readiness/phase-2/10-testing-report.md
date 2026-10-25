# Phase 2 — Testing Report (Sprint 6)

**Sprint:** 6  
**Date:** 2026-07-26

## Added / extended unit tests

| Test | File | Covers |
|------|------|--------|
| Closed CAPA rejects corrective patch | `ComplianceService.Tests/ComplianceDeficiencyCapaLifecycleTests.cs` | Rule #1 `EnsureMutable` |
| Closed CAPA rejects RCA patch | same | Rule #1 |
| Audit session rejects findings when Closed/Submitted | `ComplianceService.Tests/AuditSessionFindingGateTests.cs` | Rule #2 |
| Open sessions accept findings | same | Rule #2 negative |
| Verify blocked for non-reviewer | `ComplianceService.Tests/CapaVerifyRoleGateTests.cs` | Rule #3 |
| Self-verify blocked | same | Rule #3 |
| Manager non-assignee allowed | same | Rule #3 |

Prior Sprint 1–3 tests remain: CAPA close gates, audit close gate, task dependency gate.

## API / build verification

- ComplianceService → `_build_verify_phase0/P2S6` (0 errors)  
- Gateway → `_build_verify_phase0/P2S6_Gateway` (0 errors)  
- Focused unit tests: **23 passed / 0 failed** (`ComplianceDeficiencyCapaLifecycleTests`, `AuditSessionFindingGateTests`, `CapaVerifyRoleGateTests`)  
- Summary: `_build_verify_phase0/P2S6/BUILD_RESULTS.txt`

## Playwright / E2E

| Item | Status |
|------|--------|
| WebApp Playwright infra (`playwright.config`, `e2e/`) | **Absent** |
| Happy-path smoke (dashboard → DCC → tasks) | **Deferred** — no Playwright package in WebApp `package.json` (Vitest unit tests only) |

Honest gap: browser smoke is not automated in this sprint. Prefer adding `@playwright/test` in a follow-up rather than inventing a one-off harness here.

## Gaps / follow-ups

1. Integration test for `FindingQueryService.CreateAsync` with EF-backed closed session (domain gate covered).  
2. Task assignee complete currently unit-tested only via service method presence; add a focused test double when TaskExecutionService can be constructed lightly.  
3. Playwright smoke once WebApp e2e infra exists.  
