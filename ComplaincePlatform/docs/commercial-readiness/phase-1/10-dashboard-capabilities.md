# Phase 1 — Dashboard Capabilities

**Sprint:** 5  
**Status:** Delivered (role-aware sections on existing dashboard)

## Approach

Extend the existing WebApp dashboard (`features/dashboard`) rather than a greenfield role dashboard product.

| Persona (UI) | Canonical roles | Emphasis |
|--------------|-----------------|----------|
| Quality Manager | `MANAGER` (+ legacy “Quality Manager”) | KPIs, alerts, department heatmap, tasks/evidence widgets |
| Department Head | `MANAGER` (+ legacy “Department Head”) | Department heatmap + local ops widgets |
| Hospital Admin | `ORG_ADMIN` / `GROUP_ADMIN` / `SUPER_ADMIN` | Hospital overview, executive KPIs, framework row |

Role resolution uses `UserContextService` + `RoleRegistry` / legacy aliases (not `SessionService`).

## Backend

Unchanged read path: Gateway `GET /dashboard/experience` → Compliance platform dashboard experience.

## Exit criteria

Users with admin/manager roles see a persona selector (or auto-default) that filters which dashboard sections are emphasized, without breaking the phased widget host model.
