# Phase 3 — Consultant Experience (Sprint 5)

**Sprint:** 5  
**Status:** Delivered  
**Principle:** Multi-hospital portfolio for consultant-like roles without a new microservice.

## Pack

`RoleExperiencePackRegistry` pack `consultant`:

| Field | Value |
|-------|--------|
| Persona | `ExecutiveAdvisor` |
| Default home | `/app/consultant` |
| Role aliases | `COMPLIANCE_VIEWER`, `REGULATORY_ADMIN`, `AUTHORITY`, `Consultant`, `NABH Consultant` |
| Quick actions | Portfolio, Readiness, Audit calendar |

WebApp local pack mirrors the same `defaultHomeRoute`.

## Portfolio page

Routes (same component):

- `/app/consultant`
- `/app/portfolio` (alias)

Guarded by `roleGuard` for:

`COMPLIANCE_VIEWER`, `REGULATORY_ADMIN`, `AUTHORITY`, `ORG_ADMIN`, `GROUP_ADMIN`, `SUPER_ADMIN`

### Composition

1. NBA strip from `/role-experience/home`
2. KPI strip — hospital count, avg readiness, active hospital CAPA / audit sessions
3. Hospital cards from session org hospitals API (`OrganizationService.loadOrganizationDetails`)
4. Active hospital enriched via analytics overview/CAPA/operational + hospital dashboard + audit calendar
5. Switcher calls `AppContextService.switchContext` / `switchHospital`
6. Links to audit calendar (`/app/audit/planning`), maturity, readiness reports

## Nav

Dashboard → **Consultant portfolio** with `requiredRoles` matching the route guard set.
