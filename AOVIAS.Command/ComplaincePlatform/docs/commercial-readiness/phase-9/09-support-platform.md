# 09 — Support Platform

**Phase:** 9.9  
**Sprint:** 5  
**Date:** 2026-07-26  
**Product:** AOVIAS Command v1.0.0

## Delivered (content)

| Artifact | Path |
|----------|------|
| KB index | [`commercial-launch/support/kb/README.md`](../../../commercial-launch/support/kb/README.md) |
| KB-001 Login | [`commercial-launch/support/kb/kb-001-login.md`](../../../commercial-launch/support/kb/kb-001-login.md) |
| KB-002 License activate | [`commercial-launch/support/kb/kb-002-license-activate.md`](../../../commercial-launch/support/kb/kb-002-license-activate.md) |
| KB-003 Backup / restore | [`commercial-launch/support/kb/kb-003-backup-restore.md`](../../../commercial-launch/support/kb/kb-003-backup-restore.md) |
| KB-004 AI citations | [`commercial-launch/support/kb/kb-004-ai-citations.md`](../../../commercial-launch/support/kb/kb-004-ai-citations.md) |
| KB-005 Maintenance mode | [`commercial-launch/support/kb/kb-005-maintenance-mode.md`](../../../commercial-launch/support/kb/kb-005-maintenance-mode.md) |
| Issue triage | [`commercial-launch/support/issue-triage.md`](../../../commercial-launch/support/issue-triage.md) |
| Escalation | [`commercial-launch/support/escalation.md`](../../../commercial-launch/support/escalation.md) |
| Diagnostic collection | [`commercial-launch/support/diagnostic-collection.md`](../../../commercial-launch/support/diagnostic-collection.md) |
| Customer comms templates | [`commercial-launch/support/customer-comms-templates.md`](../../../commercial-launch/support/customer-comms-templates.md) |
| Status page template | [`commercial-launch/support/service-status-page-template.md`](../../../commercial-launch/support/service-status-page-template.md) |

## Product hooks (existing)

| Capability | Evidence |
|------------|----------|
| Diagnostics pack | `GET /api/v1/commercial/diagnostics?organizationId=` |
| Customer health | `GET /api/v1/commercial/customer-health?organizationId=` |
| Maintenance mode | `MaintenanceModeMiddleware` + `MAINTENANCE_MODE` / `maintenance.lock` |
| Ops scripts | `scripts/ops/*` |

## Exit

L1 can triage common issues, collect diagnostics, escalate cleanly, and communicate with customers using the support pack.
