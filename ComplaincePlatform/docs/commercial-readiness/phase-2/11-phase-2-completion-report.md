# Phase 2 — Completion Report

**Date:** 2026-07-26  
**Scope:** Commercial-readiness Phase 2 Sprints 1–6 (CAPA → Audit → Tasks → Notifications → Automation/Scheduling → Dashboards / Analytics / Search / Rules / Tests)

## Honest status

| Sprint | Area | Status | Notes |
|--------|------|--------|-------|
| 1 | CAPA management | **Shipped** | `ComplianceDeficiency` CAPA fields, ownership accept/reject, CAPA numbers, close gates, DCC metrics/aging, migration `20260726140000_Phase2_CapaLifecycle` |
| 2 | Audit management | **Shipped** | `AuditSession` / observations / scheduled audit metadata, close gate, commercial audit types; live `/app/audit/*`; migration `20260726150000_Phase2_AuditManagement` |
| 3 | Task engine | **Shipped** | Category/kind, blockers, recurrence, overdue escalation job, reassign history, calendar/department views; migration `20260726151000_Phase2_TaskEngine` |
| 4 | Notification platform | **Shipped** | CommunicationService HTTP publisher + console fallback; Gateway SignalR; digest job; WebApp hub client |
| 5 | Workflow automation | **Shipped** | `WorkflowRuleEngine` (EvidenceApproved, AuditSessionClosed→CAPA); docs `04` |
| 5 | Scheduling / SLA calendar | **Shipped** | `OrgHoliday` + working-day SLA math; Hangfire `SlaChaseJob`; docs `07` |
| 6 | Dashboards / personas | **Shipped** | QM / Dept Head / Hospital Admin / Consultant persona layouts; nav prefers Control Center + DCC + Tasks |
| 6 | Operational analytics | **Shipped** | Extended `GET /api/v1/analytics/operational` + Analytics Hub charts/export |
| 6 | Operational search | **Shipped** | Gateway `GET /api/v1/compliance/search`; tasks / audit sessions / CAPAs; header search + saved queries |
| 6 | Business rules | **Shipped** | Closed CAPA immutable; closed/submitted audit no new findings; reviewer verify + self-verify block; assignee completes task |
| 6 | Tests | **Partial** | Focused unit tests across sprints; Playwright **deferred** (no WebApp Playwright infra) |
| 6 | Docs | **Shipped** | `01`–`11` under `docs/commercial-readiness/phase-2/` |

## Non-goals preserved

- RabbitMQ remains Gateway / messaging infrastructure only; Compliance uses Outbox  
- Controllers stay thin; lifecycle ownership remains `ComplianceJourney` / CAPA domain methods  
- Orphan `Capa` table not revived as SSOT (deficiency CAPA fields remain primary)  
- Full Playwright suite and CommunicationService multi-channel production hardening remain follow-ups  

## Operational search (Sprint 6 detail)

| Layer | Behavior |
|-------|----------|
| Graph search | Tasks (hospital-scoped via journeys), AuditSessions, CAPA via `CapaNumber` on deficiencies (+ legacy Capas) |
| Gateway facade | Unchanged contract `GET /api/v1/compliance/search` — flat hits from graph (+ optional evidence) |
| WebApp | `app-global-ops-search` in shell header; saved searches in `localStorage` key `praman.ops.savedSearches` |

## Recommended next

1. Add WebApp Playwright smoke: login → persona dashboard → DCC → Tasks → Analytics operational  
2. Integration test for assignee-only task complete with stubbed `IUserContextService`  
3. Apply Phase 2 EF migrations in each environment:  
   `Phase2_CapaLifecycle`, `Phase2_AuditManagement`, `Phase2_TaskEngine`, Organization `Phase2_OrgHolidays`  
4. Harden CommunicationService production SMTP/provider beyond Dev console fallback  

## Build artifacts

`_build_verify_phase0/P2S1` … `P2S6` (+ Gateway variants) — alternate compile outputs used while services held DLL locks.
