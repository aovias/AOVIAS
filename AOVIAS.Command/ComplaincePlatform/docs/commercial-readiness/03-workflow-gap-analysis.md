# 03 — Workflow Gap Analysis

**Program:** CRP v1.0  
**Assessment Date:** 26 July 2026  
**Rule:** If a workflow stops midway, it is a gap — no future-implementation credit.

Severity: **Critical** | **High** | **Medium** | **Low**

---

## Workflow Index

| ID | Workflow | Overall state | Highest severity |
|----|----------|---------------|------------------|
| WF-01 | Organization creation | Partial | Medium |
| WF-02 | Hospital onboarding wizard | Mostly complete | Medium |
| WF-03 | Department setup | Complete | Low |
| WF-04 | Framework assignment | Mostly complete | Medium |
| WF-05 | Compliance journey / OE assignment | Partial | Medium |
| WF-06 | Evidence upload → review | Mostly complete | Medium |
| WF-07 | CAPA / deficiency lifecycle | Broken duality | **High** |
| WF-08 | Internal audit | Partial | High |
| WF-09 | External auditor session | Partial | High |
| WF-10 | Document control | Complete | Low |
| WF-11 | Incident → RCA → CAPA | Partial | Medium |
| WF-12 | User management | Partial | Medium |
| WF-13 | Roles & permissions | Partial | Medium |
| WF-14 | Notifications | Broken delivery | **High** |
| WF-15 | Search (directory/graph) | Partial | Medium |
| WF-16 | Reporting | Partial | Medium |
| WF-17 | Committee meetings | Partial | Medium |
| WF-18 | Clinical indicators | Mostly complete | Medium |
| WF-19 | Surveillance | Missing | **High** |
| WF-20 | Subscriptions / licensing | Non-enforcing | **Critical** |
| WF-21 | Backup / restore / upgrade | Missing | **Critical** |
| WF-22 | Demo hospital journey (seeded) | Demo-ready | Medium |

---

## WF-01 Organization Creation

| | |
|--|--|
| **Current state** | Org/hospital create APIs + onboarding signup path exist |
| **Expected state** | Self-serve or sales-assisted org provisioning with license, branding, admin user, audit trail |
| **Missing steps** | Commercial license binding; physical campus hierarchy; systemic OrgAccess on all org APIs |
| **Broken step** | Tenant endpoints lack systemic authorization; subscription trial assign is optional/unenforced |
| **Expected behaviour** | New org cannot exceed plan; all org APIs require authenticated org-scoped principal |
| **Current behaviour** | Org created; trial may assign; quotas ignored; tenant isolation patchy |
| **Impact** | Multi-tenant leakage risk; commercial entitlement bypass |
| **Severity** | Medium (isolation High when combined with WF-20) |
| **Recommendation** | Enforce OrgAccess globally; bind signup to plan; remove anonymous tenant access |

---

## WF-02 Hospital Onboarding Wizard

| | |
|--|--|
| **Current state** | 6-step wizard (welcome → hospital → frameworks → assessment → team → complete) |
| **Expected state** | Deterministic rollback, verified email invites, framework-specific policy gates |
| **Missing steps** | Production email invites; non-NABH policy enforcement; robust rollback UX messaging |
| **Broken step** | Framework journey policies allow all transitions (placeholders) |
| **Impact** | Hospitals can progress journeys without framework-specific rules |
| **Severity** | Medium |
| **Recommendation** | Implement NABH policy gates; wire real invite email; E2E onboarding smoke in CI |

---

## WF-03 Department Setup

| | |
|--|--|
| **Current state** | Department tree + hospital master department links + workforce admin UI |
| **Expected state** | Single coherent department model for ownership, staffing, and OE mapping |
| **Missing steps** | Unified mental model (org dept vs master dept) for customers |
| **Severity** | Low |
| **Recommendation** | Document and UX-unify dual catalogs; avoid exposing two “department” concepts |

---

## WF-04 Framework Assignment

| | |
|--|--|
| **Current state** | Enroll hospital in framework; create journeys during onboarding |
| **Expected state** | Entitlement-checked framework subscription; policy-aware journey creation |
| **Missing steps** | Framework subscription quota enforcement; non-NABH operational catalogs |
| **Severity** | Medium |
| **Recommendation** | Gate enroll on `FrameworkSubscription`; seed only sold frameworks |

---

## WF-05 Compliance Journey / OE Assignment

| | |
|--|--|
| **Current state** | Journey create/transition; OE execution screens; department ownership routing; tasks |
| **Expected state** | Explicit OE ownership (person/role), assignment notifications, locked stages |
| **Missing steps** | First-class “assign OE to user” as hospital-facing workflow; surveillance stage |
| **Broken step** | OE assignment is indirect (ownership/tasks), not a clear QM workflow |
| **Impact** | Quality Manager cannot easily answer “who owns OE X?” |
| **Severity** | Medium |
| **Recommendation** | Ship OE ownership matrix UI + notification on assign |

---

## WF-06 Evidence Upload → Review → Accept/Reject

| | |
|--|--|
| **Current state** | Upload, library, review queue, verification state machine, binder |
| **Expected state** | Always-on upload in prod; AI suggestions optional; immutable audit of verify decisions |
| **Missing / broken** | `enableOeWizardEvidenceUpload: false` in production environment; binder AI empty; evidence verification historically noted incomplete in contracts |
| **Impact** | Prod demo/live may block OE wizard upload path |
| **Severity** | Medium |
| **Recommendation** | Enable prod upload with virus scan/size limits; complete verification audit trail |

---

## WF-07 CAPA / Deficiency Lifecycle

| | |
|--|--|
| **Current state** | Active write path patches CAPA fields on `ComplianceDeficiency`; Control Center may still read orphan `Capas` |
| **Expected state** | One CAPA aggregate: create → assign → RCA → action → verify → close; linked to finding/incident/OE |
| **Missing steps** | Unified CAPA create API; retirement of orphan `Capa` / ActionPlan semantics |
| **Broken step** | Dual/triple models; `Capas` table has no create writes found; copilot “create CAPA” does not insert `Capa` |
| **Expected behaviour** | Assessor finds CAPA register consistent across DCC, Control Center, Graph |
| **Current behaviour** | Deficiency CAPA works; aggregate CAPA register inconsistent |
| **Impact** | NABH assessors lose confidence; wrong CAPA counts on dashboards |
| **Severity** | **High** |
| **Recommendation** | Choose deficiency-embedded CAPA as SSOT; migrate/remove `Capa` + clarify ActionPlan; add CAPA register report |

---

## WF-08 Internal Audit

| | |
|--|--|
| **Current state** | Operational mode Internal Audit; findings; auditor-adjacent screens |
| **Expected state** | Planned internal audits, checklists, observation→finding→CAPA, audit report package |
| **Missing steps** | Unified audit backend; scheduled audit UX maturity; surveillance handoff |
| **Broken step** | Parallel `AuditService` vs Compliance `Auditor` domain |
| **Impact** | Engineering ambiguity; inconsistent APIs for same business concept |
| **Severity** | High |
| **Recommendation** | Retire or merge AuditService; single AuditSession model |

---

## WF-09 External Auditor Session

| | |
|--|--|
| **Current state** | `/app/auditor/*` sessions, evidence review, findings, reports |
| **Expected state** | Role-isolated auditor workspace; read-scoped evidence; observation capture; formal report |
| **Missing steps** | Route-level role isolation (`roleGuard` unused); observations/history reuse sessions page |
| **Impact** | External auditor may see operator navigation; weak assurance story |
| **Severity** | High |
| **Recommendation** | Wire auditor-only shell + roleGuard; separate observation UX |

---

## WF-10 Document Control (Create → Approve → Publish → Acknowledge)

| | |
|--|--|
| **Current state** | Full API + Documentation UI (policies/SOPs/manuals/forms, authoring, ack, calendar) |
| **Expected state** | Same + training attestation linkage + controlled print |
| **Missing steps** | LMS acknowledgement coupling; some analytics maturity |
| **Severity** | Low |
| **Recommendation** | Keep as launch pillar; add training link in next release |

---

## WF-11 Incident → RCA → CAPA Link

| | |
|--|--|
| **Current state** | Create incident → investigation/RCA → promote finding/deficiency |
| **Expected state** | Closed loop to CAPA with effectiveness check and committee review |
| **Missing steps** | Native CAPA entity link; incident SLA; ML clustering (future) |
| **Broken step** | CAPA is deficiency fields after promotion — register inconsistency (WF-07) |
| **Severity** | Medium |
| **Recommendation** | Explicit “Promote to CAPA” with SSOT CAPA; effectiveness review task |

---

## WF-12 User Management

| | |
|--|--|
| **Current state** | Identity users admin; password management; MFA admin policy |
| **Expected state** | Bulk import/export, invite lifecycle, suspend, multi-org membership UX |
| **Missing steps** | Bulk import/export (Coming soon); some invitation edge cases historically gapped |
| **Severity** | Medium |
| **Recommendation** | Ship CSV invite/import before multi-hospital rollouts |

---

## WF-13 Roles & Permissions

| | |
|--|--|
| **Current state** | AccessGovernance roles/permissions/policies/simulator; permissionGuard on admin routes |
| **Expected state** | Every operational route permissioned; hospital role templates (QM, HOD, Nurse, Auditor) |
| **Missing steps** | Many operational routes not permission-gated; coarse `Compliance.Standard.View` reuse; `roleGuard` unused |
| **Severity** | Medium |
| **Recommendation** | Publish hospital role pack; apply permissionGuard to Tier-1 modules |

---

## WF-14 Notifications

| | |
|--|--|
| **Current state** | CommunicationService templates/queues/history; in-app APIs; admin UI |
| **Expected state** | Reliable email + in-app + optional SMS; real-time bell via SignalR |
| **Broken step** | `ConsoleEmailService` only; `NotificationHub` not wired to outbox; ASB required for consumers |
| **Expected behaviour** | Task assignment emails doctor/HOD |
| **Current behaviour** | Emails log to console; real-time delivery incomplete |
| **Impact** | Hospital workflows stall without chase emails |
| **Severity** | **High** |
| **Recommendation** | SMTP/SendGrid provider; wire SignalR; smoke-test assignment → email → in-app |

---

## WF-15 Search

| | |
|--|--|
| **Current state** | Enterprise directory search; compliance graph search |
| **Expected state** | Unified governed search across people, docs, OE, incidents |
| **Missing steps** | Unified UX; RLS masking maturity; NL search maturity |
| **Severity** | Medium |
| **Recommendation** | Single search entry in shell; mask PII by permission |

---

## WF-16 Reporting

| | |
|--|--|
| **Current state** | Reports landing: compliance/audit/tasks/evidence/readiness/custom |
| **Expected state** | Assessor-ready packages; scheduled delivery; custom builder on live metadata |
| **Missing steps** | Custom filters partially hardcoded; export admin audit Coming soon |
| **Severity** | Medium |
| **Recommendation** | Harden readiness + compliance reports first; defer custom builder |

---

## WF-17 Committee Meetings

| | |
|--|--|
| **Current state** | Governance schedule/complete/decisions APIs; thin Angular forms |
| **Expected state** | Agenda, attendance, MoM, decisions linked to CAPA/risk, NABH committee calendar |
| **Missing steps** | Polished meeting UX; document attachments; decision→CAPA link |
| **Severity** | Medium |
| **Recommendation** | Upgrade meeting workspace before selling “governance” as a pillar |

---

## WF-18 Clinical Indicators / Scorecards

| | |
|--|--|
| **Current state** | Indicators, measurements, thresholds, department scorecards |
| **Expected state** | Monthly data entry, variance investigation, board pack |
| **Missing steps** | Training readiness on scorecards; benchmarking depth |
| **Severity** | Medium |
| **Recommendation** | Seed hospital indicators in onboarding; add variance→investigation link |

---

## WF-19 Surveillance / Continuous Monitoring

| | |
|--|--|
| **Current state** | OperationalMode enum + disabled nav “Coming soon”; `SurveillanceDue` returns false |
| **Expected state** | Post-accreditation continuous monitoring with due windows and alerts |
| **Broken step** | Entire workflow missing |
| **Impact** | Cannot sell post-accreditation monitoring |
| **Severity** | **High** |
| **Recommendation** | Hide completely until implemented OR implement due engine + alerts |

---

## WF-20 Subscriptions / Licensing

| | |
|--|--|
| **Current state** | Plans/subscriptions entities; assign/upgrade APIs; trial on signup |
| **Expected state** | Enforce MaxUsers/MaxFrameworks; block over-quota; customer portal |
| **Broken step** | No enforcement middleware; `CurrentUserCount` not maintained |
| **Impact** | Revenue leakage; uncontrolled growth |
| **Severity** | **Critical** |
| **Recommendation** | Gateway entitlement filter on member create + framework enroll; admin license page |

---

## WF-21 Backup / Restore / Upgrade

| | |
|--|--|
| **Current state** | Subscription “upgrade” API only; no tenant backup product |
| **Expected state** | Documented backup/restore per tenant; versioned upgrade; rollback |
| **Broken step** | No APIs, no runbooks, no tooling in compose |
| **Impact** | Hospital IT cannot accept deployment risk |
| **Severity** | **Critical** |
| **Recommendation** | Publish AOVIAS ops package: SQL backup scripts, restore drill, migration playbook |

---

## WF-22 Demo Hospital Journey (Seeded)

| | |
|--|--|
| **Current state** | DemoDataSeeder enriches fixed hospital with dense compliance data |
| **Expected state** | One-click demo environment for sales |
| **Missing steps** | Greenfield user creation; equipment/LMS aggregates; full-stack docker |
| **Severity** | Medium |
| **Recommendation** | Demo bundle script: migrate all DBs → seed KB → seed demo → start all services |

---

## Cross-Workflow Failure Modes

| Failure mode | Workflows affected | Severity |
|--------------|-------------------|----------|
| Azure Service Bus unavailable | Notifications, directory, integration, outbox | High |
| Docker compose incomplete | All except 5 domain services | High |
| Dual CAPA / dual Audit | CAPA, audit, graph counts | High |
| Console email | Onboarding invites, assignments, SLA | High |
| Unenforced license | Org growth, framework enroll | Critical |
| Secrets in git | All deployments | Critical |

---

## Priority Fix Order (Workflow)

1. WF-20 Licensing enforcement  
2. WF-21 Backup/restore/ops runbooks  
3. WF-14 Notification delivery (email + SignalR)  
4. WF-07 CAPA unification  
5. WF-08/09 Audit unification + auditor isolation  
6. WF-19 Surveillance hide-or-build  
7. WF-06 Prod evidence upload flag  
8. WF-05 OE ownership clarity  
9. WF-17 Committee UX  
10. WF-22 One-click demo stack  
