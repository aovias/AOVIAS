# 06 — Role Experience Analysis

**Program:** CRP v1.0  
**Assessment Date:** 26 July 2026  
**Principle:** Each role needs Dashboard · Tasks · Actions · Reports · Notifications · Permissions

**Platform reality:** One shared `/app/dashboard`. `roleGuard` exists but is **unused**. Permission gating is strongest on Settings/admin routes, weakest on core operational routes.

---

## Role Scorecard

| Role | Dashboard | Tasks | Actions | Reports | Notifications | Permissions | Overall |
|------|-----------|-------|---------|---------|---------------|-------------|---------|
| Platform Administrator | Partial | Partial | Strong (settings) | Partial | Partial | Strong | **6/10** |
| Hospital Administrator | Shared | Weak | Partial | Partial | Weak | Partial | **4/10** |
| Quality Manager | Shared (best fit) | Strong | Strong | Strong | Weak (email) | Partial | **7/10** |
| Department Head | Shared | Partial | Partial | Partial | Weak | Weak | **4/10** |
| Doctor | Shared | Weak | Weak | Weak | Weak | Weak | **2/10** |
| Nurse | Shared | Weak | Weak | Weak | Weak | Weak | **2/10** |
| Staff | Shared | Weak | Weak | None | Weak | Weak | **2/10** |
| Consultant (NABH) | Shared | Partial | Partial | Partial | Weak | Weak | **4/10** |
| Auditor (External) | Auditor pages | Partial | Partial | Partial | Weak | **Fail** | **3/10** |
| Internal Auditor | Shared + audit | Partial | Partial | Partial | Weak | Weak | **4/10** |

---

## 1. Platform Administrator

| Need | Current | Gap |
|------|---------|-----|
| Dashboard | Settings home + AI/integration/workforce admin dashboards | No unified platform ops console |
| Tasks | Integration sync failures, notification queues | Incomplete SignalR/email ops |
| Actions | Users, roles, AI config, connectors, org branding | Bulk user import Coming soon; secrets not Key Vault |
| Reports | Admin audit (export Coming soon) | Weak support reporting |
| Notifications | Admin notification module | Delivery not production-grade |
| Permissions | `Users.View`, AI.*, Integration.*, Directory.*, Notification.* | Good coverage on admin routes |

**Can they run the platform tomorrow?** Partially — for engineering-assisted tenants only.  
**Severity of gaps:** High (ops + secrets + incomplete docker topology).

---

## 2. Hospital Administrator

| Need | Current | Gap |
|------|---------|-----|
| Dashboard | Same operator dashboard | No executive hospital pack by default |
| Tasks | Not admin-oriented | No license, onboarding, or risk queue |
| Actions | Org settings, branding, memberships | No billing/license UI; subscription unenforced |
| Reports | Readiness/executive quality exist but not role-landed | Must hunt menus |
| Notifications | Bell | No board-level digests |
| Permissions | Often relies on ORG_ADMIN-like roles | Role templates unclear |

**Hospital answer:** Cannot manage commercial entitlement or board governance cleanly.  
**Severity:** High for commercial sale; Medium for pilot.

---

## 3. Quality Manager (Primary Persona — Best Fit)

| Need | Current | Gap |
|------|---------|-----|
| Dashboard | Overview widgets + maturity + control center | Competing homes (Dashboard/CC/CCC) |
| Tasks | `/app/tasks`, journey OE work | Assignment email chase broken |
| Actions | Evidence, findings, DCC CAPA, docs, incidents, indicators | CAPA duality; surveillance missing |
| Reports | Compliance/readiness/audit/tasks/evidence | Custom builder weaker |
| Notifications | In-app partial | Email/SLA alerts unreliable |
| Permissions | Often broad operator access | Not finely permissioned per action |

**Hospital answer:** **Yes, with caveats** — QM can drive NABH prep if trained and data seeded.  
**Severity:** Medium (notification + CAPA integrity).

---

## 4. Department Head

| Need | Current | Gap |
|------|---------|-----|
| Dashboard | Shared; scorecards under Quality | No dept-default landing |
| Tasks | Filtered tasks possible if assigned | No clear “My department OEs” home |
| Actions | Evidence upload, acknowledgements, some workforce views | Ownership UX indirect |
| Reports | Department scorecards | Limited dept compliance pack |
| Notifications | Weak | Critical gap for overdue dept tasks |
| Permissions | Rarely dept-scoped systematically | Risk of over-broad access |

**Hospital answer:** Not a first-class experience.  
**Severity:** High for multi-department hospitals.

---

## 5. Doctor

| Need | Current | Gap |
|------|---------|-----|
| Dashboard | None clinical | Would see full compliance shell — overload |
| Tasks | Only if assigned compliance tasks | No clinical task inbox concept |
| Actions | Possible incident reporting, doc ack, evidence | Not role-tailored |
| Reports | None doctor-specific | — |
| Notifications | Weak | — |
| Permissions | No doctor template evident | — |

**Hospital answer:** **No** dedicated doctor experience.  
**Severity:** High if product marketed to clinicians; Medium if QM-only tool.

---

## 6. Nurse

| Need | Current | Gap |
|------|---------|-----|
| Dashboard | None nursing | Same overload risk |
| Tasks | Assignment-dependent | No nursing quality checklist UX |
| Actions | Incident/near-miss, acknowledgements, indicators entry (possible) | Not packaged |
| Reports | None | — |
| Notifications | Weak | — |
| Permissions | No nurse template | — |

**Hospital answer:** **No**.  
**Severity:** High for bedside adoption; Medium for QM-centric v1.

---

## 7. Staff (General)

| Need | Current | Gap |
|------|---------|-----|
| Dashboard | Shared complex UI | Inappropriate |
| Tasks | Minimal | Need simple “My tasks / My documents to read” |
| Actions | Ack documents, upload evidence if assigned | Buried in enterprise nav |
| Reports | None | — |
| Notifications | Weak | — |
| Permissions | Likely over/under-privileged | — |

**Hospital answer:** **No** simple staff portal.  
**Severity:** High for acknowledgment/training compliance at scale.

---

## 8. Consultant (NABH Advisor)

| Need | Current | Gap |
|------|---------|-----|
| Dashboard | Shared | Needs multi-hospital portfolio view |
| Tasks | Advisory recommendations | Copilot immature |
| Actions | Gap analysis, maturity, reports | Cross-tenant access model unclear |
| Reports | Strong potential | — |
| Notifications | Weak | — |
| Permissions | REGULATORY_ADMIN-like roles exist in definitions | Not productized as consultant portal |

**Hospital answer:** Partial for single-hospital advisory.  
**Severity:** Medium.

---

## 9. External Auditor / Assessor

| Need | Current | Gap |
|------|---------|-----|
| Dashboard | `/app/auditor/dashboard` | Still within general app shell; not isolated |
| Tasks | Sessions / evidence review | Observations/history reuse wrong pages |
| Actions | Review evidence, findings, reports | Write-scope unclear vs operator |
| Reports | Auditor reports | Incomplete vs formal NABH formats |
| Notifications | Weak | — |
| Permissions | **Fail** — routes not roleGuard-protected | Critical UX/security gap |

**Hospital answer:** **No** for secure external assessment.  
**Severity:** **Critical/High**.

---

## 10. Internal Auditor

| Need | Current | Gap |
|------|---------|-----|
| Dashboard | Audit dashboard + shared | Dual audit backends confuse |
| Tasks | Findings follow-up | Program calendar weak |
| Actions | Findings, corrective actions, modes | Simulation is mock; surveillance missing |
| Reports | Audit reports | — |
| Notifications | Weak | — |
| Permissions | Weak isolation from QM powers | Medium–High |

---

## 11. Role Definition Inventory (Code)

From WebApp `role-definitions.ts` (representative):

- `USER`, `ORG_ADMIN`, `GROUP_ADMIN`, `SUPER_ADMIN`
- `REGULATORY_ADMIN`, `COMPLIANCE_VIEWER`, `AUTHORITY`, `REGULATOR`
- `MANAGER`, `EXTERNAL_AUDITOR`

**Gaps vs hospital reality:** No first-class `QUALITY_MANAGER`, `DEPARTMENT_HEAD`, `DOCTOR`, `NURSE`, `INTERNAL_AUDITOR` product roles mapped to dashboards — even if permissions can be configured in AccessGovernance.

AccessGovernance can define custom roles, but **out-of-box hospital role packs + experiences are missing**.

---

## 12. Per-Role Gap List (Actionable)

| ID | Role | Gap | Severity | Recommendation |
|----|------|-----|----------|----------------|
| RE-001 | All | Single shared dashboard | High | Role-aware dashboard layouts |
| RE-002 | External Auditor | Routes not role-isolated | Critical | Auditor shell + roleGuard |
| RE-003 | Dept Head | No dept home | High | Dept command view |
| RE-004 | Doctor/Nurse/Staff | No simplified portal | High | “My Work” lite shell |
| RE-005 | All operators | Email notifications fail | High | Real email provider |
| RE-006 | Hospital Admin | No license console | Critical | Subscription admin UI + enforcement |
| RE-007 | QM | CAPA register trust | High | Unify CAPA |
| RE-008 | Internal Auditor | Dual audit systems | High | Merge audit domain |
| RE-009 | Staff | Training attestation | High | LMS or ack+quiz MVP |
| RE-010 | Consultant | Multi-hospital portfolio | Medium | Group admin experience |
| RE-011 | Platform Admin | Incomplete deploy topology | High | Full compose + runbooks |
| RE-012 | Assessor | Graph nav hidden | Medium | Enable traceability in assessor mode |

---

## 13. Minimum Role Pack for Commercial v1

Ship these **four** experiences before claiming enterprise readiness:

1. **Quality Manager** — current shell OK after CAPA + email fixes  
2. **Department Head** — filtered tasks/OEs/scorecard home  
3. **Staff Lite** — My Tasks + Documents to Acknowledge  
4. **External Auditor** — isolated read-focused shell  

Defer clinician-rich Doctor/Nurse portals to v1.1 unless sales requires them.

---

## 14. Closing Statement (Roles)

The product today is effectively a **Quality Manager workstation** with many adjacent modules.  

It is **not** a role-complete hospital system. Selling to “the hospital” without role packaging will produce adoption failure outside the Quality department.
