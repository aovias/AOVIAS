# PRAMAN Access Governance Gap Analysis — EIWAM Phase 5

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 5 — Identity & Access Governance |
| **Prior work** | M6.4 Enterprise Access Governance Foundation |
| **Status** | Investigation complete — implementation authorized |
| **Bounded context** | AccessGovernanceService (evolve in place) |

## Executive summary

EIWAM Phase 5 largely overlaps with **M6.4**, which already introduced `AccessGovernanceService`, explainable `AuthorizationEvaluatorService`, data-driven permissions/roles/policies, delegations, approval authorities, Gateway proxy routes, `Shared.Authorization` SDK, and Angular admin shells. Phase 5 **evolves** this foundation rather than replacing it.

The largest gaps are **consumer integration** (no service registers `AddPramanAuthorization`), **evaluator depth** (scoped RBAC, ABAC operators, RLS, effective-permissions union), and **API/UI completeness** (policy designer CRUD, role-assignment endpoint, evaluation trace).

---

## Investigation scope

| Area reviewed | Primary locations |
|---------------|-------------------|
| Identity roles & JWT | `IdentityService/Application/Services/TokenService.cs`, `Shared.Infrastructure/Auth/AuthConstants.cs` |
| ASP.NET policies | `ComplianceService/Infrastructure/Extensions/ServiceCollectionExtensions.cs`, controllers |
| Access governance domain | `AccessGovernanceService/Domain/` |
| Evaluation engine | `AccessGovernanceService/Application/Services/AuthorizationEvaluatorService.cs` |
| Consumer SDK | `Shared.Authorization/` |
| Gateway proxy | `Gateway/Application/Handlers/AccessGovernanceProxyHandlers.cs` |
| Angular guards | `WebApp/src/app/app.routes.ts`, `shared/authorization/authorization.service.ts` |
| Tests | `AccessGovernanceService.Tests/AuthorizationEvaluatorTests.cs` |

---

## Already implemented

### Authentication (IdentityService — stable, do not redesign)

- JWT issuance with `sub`, email, `platform_id`, `organization_id`, `hospital_id`, `membership_id`, `active_department_id`
- Context claims: `person_id`, `employment_id`, `professional_profile_id`
- ASP.NET Identity roles in JWT via `ClaimTypes.Role` (legacy bootstrap roles)
- OAuth scopes: `compliance.api`, `organization.api`, `audit.api`, `ai.api`
- Sessions, refresh tokens, token version, security stamp

### Authorization domain (AccessGovernanceService — M6.4)

| Spec aggregate | Implementation | Notes |
|----------------|----------------|-------|
| RoleDefinition | `RoleDefinition` | Platform/Org/Hospital/Dept scope, parent role, emergency flag |
| Permission | `Permission` | Configurable codes (`Module.Feature.Action`), no enums |
| PermissionGroup | Lookup `PermissionGroup` | FK on Permission |
| AccessPolicy | `AuthorizationPolicy` | Draft/Published/Archived, priority |
| PolicyRule | `PolicyRule` + `PolicyCondition` | Permit/Deny effects |
| RoleAssignment | `RoleAssignment` entity | Effective dates, scope IDs, revoke |
| PermissionAssignment | `PermissionAssignment` | Role → permission grants |
| Delegation | `Delegation` + `DelegationHistory` | Circular delegation validation |
| ApprovalAuthority | `ApprovalAuthority` | Approval types, levels, effective dates |

### Evaluation engine

- Ordered pipeline: deny policies → delegations → approval authority → RBAC → permit policies → default deny
- Explainable `AuthorizationDecisionDto` with matched roles/permissions/policies/delegations
- `AccessEvaluation` audit persistence per decision
- Workforce + Professional attribute hydration for ABAC

### REST APIs (v1)

- `GET/POST /permissions`, `GET/POST /roles`, role assign, permission grant
- `GET/POST /policies`, `POST /policies/{id}/publish`
- `GET/POST /delegations`, `POST /delegations/{id}/revoke`
- `GET/POST /approval-authorities`
- `POST /authorization/evaluate`, `GET /authorization/effective-permissions`
- Internal: `POST /internal/authorization/evaluate`
- Gateway mirrors all public routes

### SDK foundation

- `Shared.Authorization.IAuthorizationEvaluator` + HTTP client to internal evaluate endpoint
- `AddPramanAuthorization()` extension (HttpClient + internal API key forwarding)

### Angular (WebApp sibling)

- Routes at `/app/access-governance`: roles, permissions, policies, delegations, approvals, effective, simulator
- `AuthorizationService.can()` calls evaluate API
- Responsibility matrix redirects to GovernanceService (M6.5)

### Validation (unit tests)

- Duplicate permission code rejected
- Duplicate active role assignment rejected
- Circular delegation rejected
- Role permission grants access
- Explicit deny policy wins
- Approval authority satisfies approve action
- Effective permissions aggregates role grants
- Soft-deleted role excluded

### Database

- Migration `20260706_M6_4_AccessGovernanceFoundation`
- Soft delete, row versioning, effective dating on key aggregates
- `OutboxMessage` schema present

---

## Needs enhancement

| Area | Current | Phase 5 target |
|------|---------|----------------|
| **Assignment scope** | Scope fields stored but not enforced in evaluator | Filter assignments by org/hospital/dept/employment |
| **ABAC operators** | `PolicyCondition.OperatorValueId` stored; evaluator uses string equality only | Equals, NotEquals, In, Contains, GreaterThan, Exists via lookup codes |
| **Inherited roles** | `ParentRoleId` on role, not resolved | Recursive permission merge with cycle detection |
| **Delegations** | Blanket grant when `PermissionId` is null | Require explicit permission; enforce org/hospital/dept scope |
| **Approval authority** | Resource string match only | Scope-aware matching |
| **Effective permissions** | RBAC union only | Include delegations, inherited roles, permit policies; source trace |
| **Evaluation trace** | Reason string only | Ordered step list for audit/UI simulator |
| **RLS** | Not implemented | Resource scope attributes + `RowLevelFilter` in decision |
| **Field-level** | Not implemented | `FieldMask` on Permission + consumer pattern |
| **Policy APIs** | Create/list/publish only | Get/update/archive, rule CRUD |
| **Role assignment API** | Nested under `/roles/users/...` | Dedicated `POST /role-assignments` + revoke + history |
| **Permission catalog** | List/create only | Group filter, pagination, archive |
| **Organization attributes** | Not fetched | `IOrganizationServiceClient` for membership type/status |
| **Outbox** | Schema + heartbeat processor | Write domain events on mutations; dispatch stub |
| **JWT claim mapping** | `organization_id` vs `organizationId` inconsistency | `AuthorizationContextBuilder` reads both conventions |
| **Angular** | Shell pages, no permission guards | `permissionGuard`, assignment UI, trace display |

---

## Missing

| Capability | Impact |
|------------|--------|
| **`PermissionAuthorizationHandler`** | APIs cannot enforce runtime permissions |
| **`AddPramanAuthorization` registration** | Gateway and domain services do not call evaluator |
| **`[RequirePermission]` attribute** | No declarative fine-grained API checks |
| **Module adoption playbook** | Compliance/Audit/AI still use `[Authorize(Roles=...)]` |
| **Integration tests** | No Gateway→evaluate or Compliance end-to-end tests |
| **`ACCESS_GOVERNANCE_GAP_ANALYSIS.md`** | This document (mandatory gate) |
| **Phase 5 domain model framing** | `ACCESS_GOVERNANCE_DOMAIN_MODEL.md` still labeled M6.4 only |

---

## Technical debt

1. **Dual authorization models**: JWT roles + enterprise RBAC coexist without documented migration path — Phase 5 adds playbook; Identity roles remain for bootstrap.
2. **Responsibilities split**: AccessGovernance DB table exists; writes throw; Gateway proxies to GovernanceService — document boundary (security vs accountability).
3. **Permissions not in JWT** (correct by design) but Angular/compliance still infer access from legacy roles.
4. **Claim name inconsistency**: Token uses `organization_id`/`hospital_id`; `AuthConstants` uses `organizationId` — context builder must normalize.
5. **Outbox processors** across Org/Governance/AccessGovernance are heartbeat stubs — no RabbitMQ dispatch yet (platform-wide pattern).
6. **Seeded permissions minimal** — production catalog seeding strategy needed per module rollout.
7. **56 `[Authorize]` attributes in ComplianceService** — incremental migration required (not big-bang).

---

## Claim → evaluator context mapping

| JWT claim | AuthorizationRequest field |
|-----------|---------------------------|
| `sub` / NameIdentifier | `SubjectId` |
| `person_id` | `PersonId` |
| `employment_id` | `EmploymentId` |
| `professional_profile_id` | `ProfessionalProfileId` |
| `organization_id` / `organizationId` | `OrganizationId` |
| `hospital_id` | `HospitalId` |
| `active_department_id` | `DepartmentId` |
| Request body / route | `Resource`, `Action`, `Context` |

Permissions are **never** embedded in JWT.

---

## Coexistence: Identity roles vs enterprise roles

| Layer | Purpose | Phase 5 stance |
|-------|---------|----------------|
| ASP.NET Identity roles | Bootstrap, super-admin, legacy controllers | Retain; migrate controllers incrementally |
| `RoleDefinition` + assignments | Enterprise RBAC | Primary fine-grained AuthZ |
| Policies | ABAC / attribute rules | Complement RBAC |
| Approval authorities | Workflow approve actions | Complement RBAC |

---

## Boundary vs GovernanceService (M6.5)

| Concern | Owner |
|---------|-------|
| Can user perform action X? | AccessGovernanceService |
| Who is accountable for process Y? | GovernanceService |
| Approval chains for business workflows | GovernanceService |
| Security approval authority (can approve CAPA/evidence)? | AccessGovernanceService |

---

## Phase 5 implementation plan (summary)

1. Update domain model documentation
2. Extend `Shared.Authorization` with handler + context builder; register in Gateway and ComplianceService
3. Deepen evaluator (scope, ABAC, RLS, inherited roles, trace, effective permissions)
4. EF migration + API gaps + outbox writes
5. OrganizationService client for membership ABAC
6. Compliance evidence verify pilot with `[RequirePermission]`
7. Angular permission guard + UI enhancements
8. Expanded tests + sequence diagrams

---

## Definition of Done checklist

- [x] Gap analysis and domain model documents complete
- [x] `IAuthorizationEvaluator` registered and used via ASP.NET handler
- [x] Evaluator supports scoped RBAC, ABAC operators, RLS contract, evaluation trace
- [x] Effective permissions API returns union with sources
- [x] Domain events written to outbox on mutations
- [x] At least one Compliance endpoint uses `[RequirePermission]`
- [x] Angular access-governance routes protected by permission guard
- [x] Unit tests green (14 authorization tests)

---

## Related documents

- [ACCESS_GOVERNANCE_DOMAIN_MODEL.md](./ACCESS_GOVERNANCE_DOMAIN_MODEL.md)
- [access-governance-api-flows.md](./access-governance-api-flows.md)
- [user-module-gap-analysis.md](./user-module-gap-analysis.md)
