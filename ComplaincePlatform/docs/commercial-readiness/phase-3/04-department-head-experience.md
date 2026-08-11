# Phase 3 — Department Head Experience (Sprint 4)

**Sprint:** 4  
**Status:** Delivered  
**Principle:** Distinguish Department Head from Quality Manager; scope work to membership `departmentId`.

## Role mapping

| Source | Canonical / pack |
|--------|------------------|
| Claim / role `DEPARTMENT_HEAD` | Pack `department-head` |
| Alias `Department Head`, `DepartmentHead`, `DeptHead` | Same pack (checked **before** `MANAGER` → QM) |
| Membership `designation` matching Head / Superintendent / In-charge | Local WebApp pack fallback when JWT still has `MANAGER` only |

### Backend

- `ApplicationRoles.DEPARTMENT_HEAD` seeded / assignable
- `RoleLegacyMap` maps display aliases → `DEPARTMENT_HEAD`
- `RoleExperiencePackRegistry` pack id `department-head`
  - Persona: `DepartmentHeadAssistant`
  - Default home: `/app/department-head`
  - Quick actions: Upload evidence, Complete tasks, Respond to CAPA

### WebApp

- `ROLE_IDS` includes `DEPARTMENT_HEAD` (no longer collapses `Department Head` → `MANAGER`)
- `normalizeRoleId` / `resolveDefaultPersona` / `RoleExperienceService.resolveLocalPack` prefer Dept Head
- `MembershipScopeService` resolves `departmentId` + designation from organization membership

## Dept-scoped filters

When the user is a Department Head experience:

| Surface | Behavior |
|---------|----------|
| Tasks | Auto-applies `departmentId` query filter from membership |
| Evidence | Pins `departmentId` filter (API-supported) |
| CAPA / DCC | Prefills search with department name; client-filters workspace rows by `department` |
| Analytics (home) | Uses `/analytics/departments`, CAPA, operational + hospital heatmap for that department |

## Home workspace

Route: `/app/department-head`  
Composes: NBA strip, assigned tasks, pending/rejected evidence KPIs, open/overdue CAPAs, dept readiness, attention list, quick actions.

Nav: Dashboard → **Department workspace** (`requiredRoles`: DEPARTMENT_HEAD, MANAGER, admins).

## Demo data

`DemoDataSeeder` assigns `DEPARTMENT_HEAD` (not `MANAGER`) to department-head employees.
