# 07 — Customer Onboarding

**Phase:** 9.7  
**Sprint:** 4  
**Date:** 2026-07-26  
**Product:** AOVIAS Command v1.0.0

## Delivered (content)

| Artifact | Path |
|----------|------|
| Implementation checklist | [`commercial-launch/onboarding/implementation-checklist.md`](../../../commercial-launch/onboarding/implementation-checklist.md) |
| Deployment validation | [`commercial-launch/onboarding/deployment-validation.md`](../../../commercial-launch/onboarding/deployment-validation.md) |
| Initial configuration | [`commercial-launch/onboarding/initial-configuration.md`](../../../commercial-launch/onboarding/initial-configuration.md) |
| Framework import | [`commercial-launch/onboarding/framework-import.md`](../../../commercial-launch/onboarding/framework-import.md) |
| Administrator setup | [`commercial-launch/onboarding/administrator-setup.md`](../../../commercial-launch/onboarding/administrator-setup.md) |
| Training schedule | [`commercial-launch/onboarding/training-schedule.md`](../../../commercial-launch/onboarding/training-schedule.md) |
| Success milestones | [`commercial-launch/onboarding/success-milestones.md`](../../../commercial-launch/onboarding/success-milestones.md) |

## Ops script linkage

Onboarding validation explicitly uses:

- `scripts/ops/Validate-Environment.ps1`
- `scripts/ops/validate-production.ps1`
- `scripts/ops/backup-databases.ps1` / `restore-database.ps1`
- `scripts/ops/upgrade.ps1` / `rollback.ps1`
- `scripts/ops/scan-security-headers.ps1`

## Product dependencies (existing — not rebuilt)

- Setup wizard: `/api/v1/setup/*`, WebApp `/setup`
- Licensing + commercial ops: Phase 6
- Framework / journey / OE ownership: Phases 1–2
- Role homes: Phase 3

## Exit

A new hospital can be taken from bare metal / compose to M1 success milestone using only the onboarding pack + existing APIs.
