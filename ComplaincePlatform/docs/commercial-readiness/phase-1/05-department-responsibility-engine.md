# Phase 1 — Department Responsibility Engine (OE Ownership Matrix)

**Sprint:** 3  
**Status:** Delivered  
**Principle:** EXTEND — new `ObjectiveElementOwnership` alongside existing `DepartmentOwnership`.

## Model

`ObjectiveElementOwnership`:

- OrganizationId, HospitalId, FrameworkId, ObjectiveElementId  
- DepartmentId → `ComplianceDepartment`  
- PrimaryOwnerUserId, DueDate?, Status (Active/Inactive)  
- Unique index: `(HospitalId, ObjectiveElementId)`

## Rules

1. Upsert blocked when `ComplianceDepartment.IsActive == false`.  
2. OE must belong to the requested FrameworkId (via Chapter → Framework).  
3. List returns all OEs (optionally filtered by framework) with ownership overlay + journey status when available.  
4. Department-level ownership (`DepartmentOwnership`) remains for assignment routing; OE matrix is the per-obligation responsibility surface.

## APIs

### ComplianceService

| Method | Path |
|--------|------|
| GET | `/api/v1/hospitals/{hospitalId}/oe-ownership?frameworkId=` |
| PUT | `/api/v1/hospitals/{hospitalId}/oe-ownership` |

PUT body: OrganizationId, FrameworkId, ObjectiveElementId, DepartmentId, PrimaryOwnerUserId, DueDate?, Status?

### Gateway

`OeOwnershipController` thin proxies for the same paths.

## Service

`ObjectiveElementOwnershipAppService` — ListMatrixAsync / UpsertAsync.  
Optional task ensure on upsert was skipped to avoid breaking task contracts; can be wired later via `IComplianceTaskEnsureService`.

## Migration

`20260726120100_Phase1_OeOwnershipMatrix`
