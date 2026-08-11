# PRAMAN OrganizationService — Gap Analysis (EIWAM Phase 4)

| Field | Value |
|-------|-------|
| **Status** | Investigation complete — implementation authorized |
| **Date** | 2026-07-07 |
| **Scope** | OrganizationService, Gateway org proxies, WebApp org UI |
| **Milestone** | EIWAM Phase 4 — Organization & Membership Evolution |

---

## 1. Existing aggregates / entities

Entities live in [`Shared.Infrastructure/Domain/`](Shared.Infrastructure/Domain/) (anemic POCOs). No `OrganizationService/Domain/` folder exists today.

| Entity | Table | Role |
|--------|-------|------|
| `Organization` | `Organizations` | Tenant root; `PlatformId` (ORG prefix) |
| `Hospital` | `Hospitals` | Facility under org; unique `(OrganizationId, Code)` |
| `OrganizationMember` | `Members` | User ↔ org membership; `DepartmentId` → **MasterDepartment** |
| `MasterDepartment` | `MasterDepartments` | Global wizard / workforce dept template catalog |
| `Department` | `Departments` | Global compliance/finding dept catalog |
| `Service` | `Services` | Child of global `Department` |
| `HospitalMasterDepartment` | `HospitalMasterDepartments` | Hospital ↔ master dept M:N (wizard selection) |
| `OrganizationMemberInvitation` | `OrganizationMemberInvitations` | Schema only; no API |
| `SubscriptionPlan` / `OrganizationSubscription` / `FrameworkSubscription` | respective tables | Billing entitlements |

**Hierarchy today:** flat `Organization → Hospital[]`. No campus/building/wing/team. No org-scoped department tree.

**Cross-service:** `WorkforceService.Employment` links `OrganizationMemberId`; `DepartmentAssignment` uses `MasterDepartmentId` (not `Department`).

---

## 2. Existing APIs

### OrganizationService V1 (JWT `OrganizationScope`)

| Method | Route | Status |
|--------|-------|--------|
| GET | `/api/v1/organizations/ping` | Active |
| POST | `/api/v1/organizations/Create` | Active |
| GET | `/api/v1/organizations/{id}/detail` | Active |
| GET | `/api/v1/common/departments` | Active — global catalog |
| GET | `/api/v1/common/services` | Active |
| GET | `/api/v1/common/master-departments` | Active |

### Internal (`InternalOnlyPolicy`)

| Method | Route | Status |
|--------|-------|--------|
| POST/DELETE | `/internal/organizations` | Active |
| POST/DELETE/GET/PUT | `/internal/hospitals/*` | Active |
| PUT | `/internal/hospitals/{id}/wizard-profile` | Active |
| GET | `/internal/organizations/{orgId}/hospitals` | Active |
| GET | `/internal/master-departments/{id}` | Active |
| POST/PUT/GET | `/internal/organization-members/*` | Active |
| GET | `/internal/organizations/{orgId}/organization-members` | Active |
| GET | `/internal/users/{userId}/auth-context` | Active — **first membership by CreatedAt** |
| GET | `/internal/users/{userId}/organization-hospital` | Legacy |
| GET/POST/PATCH | `/internal/subscriptions/*` | Active — **SubscriptionAppService not in DI** |

### Gateway proxies

Tenant detail, common lookups, hospitals CRUD, organization-members CRUD, subscriptions, onboarding signup/wizard — all proxy to OrganizationService.

### Classification

| Category | APIs |
|----------|------|
| **Reusable** | Org/hospital/member CRUD, wizard profile, lookups, auth-context (needs evolution) |
| **Deprecated patterns** | Denormalized PII on `OrganizationMember`; auth-context first-hospital heuristic |
| **Missing** | Tree/graph APIs, membership search/history/transfer, invitations, org unit hierarchy, member DELETE, public hospital list |
| **Overlapping** | Single `DepartmentId` on member vs Workforce `DepartmentAssignment`; two dept catalogs (`Department` vs `MasterDepartment`) |

---

## 3. Existing database

**Migrations:** `20260406081133_Initial`, `20260624054650_Sprint9B_MemberDesignation`, `20260706071723_M6_2_OrganizationMemberPersonLink`

| Gap | Severity |
|-----|----------|
| No FK `Members.OrganizationId → Organizations` | High |
| No unique index `(UserId, OrganizationId)` for active primary | Critical |
| No soft delete on org/hospital/member | High |
| No membership effective dates / status / type | Critical |
| No audit/timeline/outbox tables | High |
| Invitation filtered unique index exists but unused | Medium |
| No temporal/history tables | Critical |

---

## 4. Existing UI (WebApp sibling repo)

| Present | Missing |
|---------|---------|
| Onboarding wizard (team invites) | Organization Explorer |
| Assignee picker (member list) | Membership Explorer / history |
| | Department hierarchy admin |
| | Hospital hierarchy admin |
| | Organization graph |
| | Assignment timeline (Workforce aggregate) |

---

## 5. Existing business rules

**OrganizationMemberAppService:** one membership per `(UserId, OrganizationId)`; email unique per org; role canonicalization via `ApplicationRoles` / `RoleLegacyMap`.

**HospitalAppService:** code unique per org; wizard profile replaces `HospitalMasterDepartment` atomically.

**Auth-context:** first member + first hospital by `CreatedAt` — incorrect for multi-org.

**Workforce boundary:** hospital placement and dept assignments owned by WorkforceService — must not duplicate in OrganizationService.

---

## 6. Gap analysis by severity

### Critical

- Multi-org / temporal membership (type, status, effective dates, history)
- Primary membership uniqueness per org at a point in time
- Invitation create/accept/revoke workflow
- Auth-context must resolve **primary active** membership
- Circular hierarchy prevention (org units, org departments)

### High

- Organization/hospital/dept tree APIs
- Soft delete + audit + outbox + domain events
- Fix `SubscriptionAppService` DI registration
- Add missing FKs and indexes

### Medium

- `OrganizationalUnit` physical hierarchy (campus/building/wing/team)
- Organization graph traversal API
- Bridge `MasterDepartment` → org `Department`

### Low

- Subscription counter enforcement on member create
- Materialized path for deep trees

---

## 7. Reuse vs create justification

| Need | Decision |
|------|----------|
| Organization / Hospital / Member | **Extend** existing entities |
| Department hierarchy | **Extend** `Department` with nullable `OrganizationId` (null = global catalog) |
| Physical hierarchy | **New** `OrganizationalUnit` — no existing entity supports unlimited depth |
| Dept/hospital workforce assignments | **No new tables** — read Workforce via HTTP |
| Invitations | **Wire up** existing `OrganizationMemberInvitation` table |

---

## 8. Readiness for Phase 5–6

After Phase 4: OrganizationService provides canonical membership + structure; AccessGovernance and Governance consume org context via existing internal APIs without duplicate org entities.
