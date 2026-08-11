# Phase 1 — Organization Foundation and Hierarchy

**Sprint:** 1  
**Status:** Delivered  
**Principle:** Extend `OrganizationalUnit` + UnitType lookups; do not replace Hospital / Department.

## Model

| Layer | Role |
|-------|------|
| Organization | Tenant root |
| OrganizationalUnit | Physical facility tree (Campus → Facility → Building → Floor → Wing → Unit / Location / WorkArea → Team) |
| Hospital | First-class clinical entity under Organization; optional link to a campus/facility OU |
| Department | Compliance ownership structure (not physical space) |

Physical OUs must not become a second ownership system. Departments remain the compliance assignment surface.

### Unit types (seeded)

Campus, Facility, Building, Floor, Wing, Unit, Location, WorkArea, Team (plus legacy Campus/Building/Wing/Team).

### Hierarchy policy

Enforced by `OrganizationalUnitHierarchyPolicy` on create/move:

- Root → Campus | Facility | Building | Team  
- Campus → Facility | Building | Team  
- Facility → Building | Unit | Location | WorkArea | Team  
- Building → Floor | Wing | Unit | Location | WorkArea | Team  
- Floor → Wing | Unit | Location | WorkArea | Team  
- Wing → Unit | Location | WorkArea | Team  
- Unit → Location | WorkArea | Team  
- Location → WorkArea | Team  
- WorkArea → Team  
- Team → Team  

## APIs

### OrganizationService (public v1)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/v1/organizational-units/types` | UnitType lookup |
| GET | `/api/v1/organizational-units/tree` | Tree (`unitTypeCode`, `status`, `hospitalId`; `includeInactive`) |
| POST | `/api/v1/organizational-units` | Create (policy-validated) |
| PUT | `/api/v1/organizational-units/{id}` | Update |
| POST | `/api/v1/organizational-units/{id}/move` | Re-parent |
| POST | `/api/v1/organizational-units/{id}/activate` | Reactivate |
| POST | `/api/v1/organizational-units/{id}/deactivate` | Soft deactivate |
| DELETE | `/api/v1/organizational-units/{id}` | Soft-delete |
| GET/POST | `/api/v1/departments/tree`, `/api/v1/departments` | Department tree + create |

### Gateway

Thin proxies under `/api/v1/organizational-units/*` and departments.  
`GET /api/v1/hospitals?organizationId=` lists hospitals for the explorer (proxies OrganizationService internal list).

Responses use `ApiResponse<T>` / `ApiResponseFactory`.

## UI (WebApp)

**Organization Explorer** (`/app/organization`):

- Hospitals & Facility Tree — live OU create / activate / deactivate; hospitals list for session org  
- Departments — tree + create  
- Membership / Graph — existing read surfaces  

Empty, loading, and error states are handled. Actions require session `organizationId`.

## Rules

1. Hierarchy edges enforced server-side; invalid parent/child rejected.  
2. Inactive units remain in tree when `includeInactive=true` but are not preferred parents for new compliance routing.  
3. Inactive departments must not receive new OE ownership assignments (hook for Sprints 3/6).  
4. Hospital enroll and journey transition APIs are unchanged.

## Exit criteria

Admin can create Campus → Building → Floor → Wing → WorkArea, see hospitals, manage departments, and filter inactive units without breaking enroll or journey contracts.
