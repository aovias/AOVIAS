# 02 — Business Capabilities Catalogue

**Program:** CRP v1.0  
**Assessment Date:** 26 July 2026  
**Question for each capability:** Can a hospital complete this business process today?

**Scoring definitions**

| Metric | Meaning |
|--------|---------|
| Completion % | Code + UI + API surface present |
| Demo Ready % | Can be shown in a controlled demo (often with seed data) |
| Production Ready % | Hospital can run without engineer intervention / stubs |
| Risk | Launch risk if sold as-is |

---

## Capability Summary Heatmap

| ID | Capability | Completion | Demo | Prod | Risk |
|----|------------|------------|------|------|------|
| BC-01 | Authentication & session | 90 | 95 | 75 | Medium |
| BC-02 | Hospital onboarding wizard | 85 | 90 | 70 | Medium |
| BC-03 | Organization / hospital admin | 80 | 85 | 65 | Medium |
| BC-04 | Department setup | 85 | 90 | 80 | Low |
| BC-05 | Framework catalog (NABH) | 90 | 95 | 80 | Medium |
| BC-06 | Framework enrollment / journey create | 85 | 90 | 70 | Medium |
| BC-07 | OE execution workspace | 85 | 90 | 70 | Medium |
| BC-08 | Compliance task management | 85 | 90 | 75 | Medium |
| BC-09 | Evidence upload / library | 85 | 90 | 70 | Medium |
| BC-10 | Evidence review / verify | 80 | 85 | 65 | Medium |
| BC-11 | Evidence binder | 75 | 80 | 55 | Medium |
| BC-12 | Findings management | 85 | 90 | 75 | Medium |
| BC-13 | Deficiency / CAPA lifecycle | 70 | 85 | 55 | **High** |
| BC-14 | SLA / escalation | 75 | 80 | 60 | Medium |
| BC-15 | Internal audit operational mode | 75 | 80 | 60 | Medium |
| BC-16 | External auditor workspace | 75 | 80 | 55 | **High** |
| BC-17 | Audit simulation | 20 | 40 | 5 | Low (not sellable) |
| BC-18 | Surveillance / continuous monitoring | 10 | 15 | 0 | **High** |
| BC-19 | Document control (QDMS) | 85 | 90 | 75 | Low–Medium |
| BC-20 | Document authoring studio | 80 | 85 | 65 | Medium |
| BC-21 | Patient safety / incidents / RCA | 80 | 85 | 65 | Medium |
| BC-22 | Clinical indicators / quality | 80 | 85 | 65 | Medium |
| BC-23 | Compliance graph / traceability | 75 | 80 | 55 | Medium |
| BC-24 | Dashboards & analytics | 80 | 90 | 65 | Medium |
| BC-25 | Reporting hub | 75 | 85 | 60 | Medium |
| BC-26 | Gap analysis (onboarding) | 80 | 85 | 70 | Medium |
| BC-27 | Copilot / AI assist | 55 | 70 | 25 | **High** |
| BC-28 | User administration | 70 | 75 | 55 | Medium |
| BC-29 | Access governance (RBAC) | 75 | 80 | 55 | Medium |
| BC-30 | Governance / committees | 70 | 75 | 50 | Medium |
| BC-31 | Workforce / professional | 75 | 80 | 60 | Medium |
| BC-32 | Enterprise directory | 70 | 75 | 50 | Medium |
| BC-33 | Workforce intelligence | 65 | 75 | 45 | Medium |
| BC-34 | Notifications (in-app) | 70 | 80 | 40 | **High** |
| BC-35 | Email / channel delivery | 35 | 40 | 15 | **High** |
| BC-36 | Integrations / import-export | 80 | 85 | 70 | Medium |
| BC-37 | Subscriptions / licensing | 40 | 50 | 15 | **Critical** |
| BC-38 | CCC pillars | 15 | 20 | 0 | Low (nav noise) |
| BC-39 | Training / LMS | 20 | 30 | 5 | **High** |
| BC-40 | Commercial ops (backup/DR/upgrade) | 10 | 10 | 0 | **Critical** |

**Weighted platform averages (capability surface):**  
Completion ≈ **70%** · Demo ≈ **75%** · Production ≈ **52%**

---

## Detailed Catalogue

### BC-01 Authentication & Session

| Field | Value |
|-------|-------|
| Purpose | Secure access for hospital users |
| Business value | Prerequisite for all operations |
| Major screens | `/login`, MFA, forgot/reset, verify-email |
| Major APIs | `/identity/login`, refresh, logout, MFA |
| Workflow | Register/login → optional MFA → JWT session → refresh |
| Dependencies | IdentityService, Gateway |
| Completion / Demo / Prod | 90 / 95 / 75 |
| Risk | Medium — secrets in config; email confirmation hardening needed for prod |
| Hospital answer | **Yes** for login; **partial** for enterprise IdP federation |

### BC-02 Hospital Onboarding Wizard

| Field | Value |
|-------|-------|
| Purpose | First-time hospital setup |
| Business value | Time-to-value for new customers |
| Screens | `/onboarding` AI Setup Assistant (6 steps) |
| APIs | `/onboarding/*`, frameworks, assessments |
| Workflow | Signup → hospital profile → frameworks → assessment → team invite → complete |
| Dependencies | Organization, KnowledgeBase, Compliance |
| Completion / Demo / Prod | 85 / 90 / 70 |
| Risk | Medium — journey policies are placeholders; email invites optional/console |
| Hospital answer | **Yes** for guided setup with NABH |

### BC-03 / BC-04 Organization & Departments

| Field | Value |
|-------|-------|
| Purpose | Org structure, hospitals, department trees |
| Screens | Settings org/branding; workforce-admin departments |
| APIs | Organizations, hospitals, department tree, memberships |
| Completion / Demo / Prod | Org 80/85/65 · Depts 85/90/80 |
| Risk | Medium — dual department catalogs (org vs master/workforce); no campus/building hierarchy |
| Hospital answer | **Yes** for flat hospital + departments |

### BC-05 / BC-06 Framework & Journey

| Field | Value |
|-------|-------|
| Purpose | NABH catalog + hospital enrollment into compliance journey |
| Screens | Onboarding framework selector; Journey execution |
| APIs | KnowledgeBase frameworks/nodes; enroll; journeys transition |
| Completion / Demo / Prod | Catalog 90/95/80 · Enroll 85/90/70 |
| Risk | Medium — non-NABH policies stubbed; JCI/ISO not operationally seeded |
| Hospital answer | **Yes for NABH Hospital 6E**; **No** for multi-framework production rigor |

### BC-07 – BC-11 Compliance Execution (OE / Tasks / Evidence)

| Field | Value |
|-------|-------|
| Purpose | Execute objective elements, assign work, collect/verify evidence |
| Screens | Journey execution, OE Progress, Tasks, Evidence, Review Queue, Binder |
| APIs | Journeys, compliance-tasks, evidence library/verify, binder |
| Completion / Demo / Prod | 75–85 / 80–90 / 55–75 |
| Risk | Medium — prod flag can disable OE wizard upload; binder AI suggestions empty; verification notes incomplete historically |
| Hospital answer | **Mostly yes** for NABH evidence loop with trained operators |

### BC-12 – BC-14 Findings / CAPA / SLA

| Field | Value |
|-------|-------|
| Purpose | Raise findings, drive deficiency CAPA, enforce SLA |
| Screens | Findings workspace, Deficiency Command Center, corrective-actions |
| APIs | Findings, deficiencies patch CAPA fields, SLA/escalation |
| Completion / Demo / Prod | Findings 85/90/75 · CAPA 70/85/55 · SLA 75/80/60 |
| Risk | **High** — dual CAPA models (`Capa` orphan table vs deficiency-embedded CAPA vs ActionPlan) |
| Hospital answer | **Partial** — deficiency CAPA works; standalone CAPA register inconsistent |

### BC-15 – BC-18 Audit & Surveillance

| Capability | Completion / Demo / Prod | Hospital answer |
|------------|--------------------------|-----------------|
| Internal audit mode | 75 / 80 / 60 | Partial — mode exists; dual audit backends confuse |
| External auditor | 75 / 80 / 55 | Partial — workspace exists; role isolation weak (`roleGuard` unused) |
| Audit simulation | 20 / 40 / 5 | **No** — client mock |
| Surveillance | 10 / 15 / 0 | **No** — nav disabled; `SurveillanceDue` hardcoded false |

### BC-19 – BC-20 Document Control

| Field | Value |
|-------|-------|
| Purpose | Policies/SOPs lifecycle, authoring, acknowledgements |
| Screens | Documentation dashboard, register, authoring, review calendar, ack |
| APIs | Documentation create/approve/publish/acknowledge |
| Completion / Demo / Prod | 85 / 90 / 75 (authoring 80/85/65) |
| Risk | Low–Medium — LMS linkage for training attestation missing |
| Hospital answer | **Yes** for controlled document workflow |

### BC-21 Patient Safety

| Field | Value |
|-------|-------|
| Purpose | Incidents, near-miss, RCA, lessons, feedback |
| Screens | Patient Safety module routes |
| APIs | Incidents, RCA, promote finding/deficiency |
| Completion / Demo / Prod | 80 / 85 / 65 |
| Risk | Medium — CAPA link is via deficiency promotion, not first-class CAPA entity |
| Hospital answer | **Mostly yes** for incident→RCA; weaker closed-loop CAPA semantics |

### BC-22 Quality Intelligence

| Field | Value |
|-------|-------|
| Purpose | Clinical indicators, scorecards, thresholds, trends |
| Screens | Quality Intelligence routes |
| APIs | `/quality/*` |
| Completion / Demo / Prod | 80 / 85 / 65 |
| Risk | Medium — depends on seed/jobs; benchmarking maturity limited |
| Hospital answer | **Yes** if indicators seeded |

### BC-23 Compliance Graph

| Field | Value |
|-------|-------|
| Purpose | Traceability across OE/docs/findings/incidents |
| Screens | Graph viewer/search/entity/traceability (header nav **hidden**) |
| Completion / Demo / Prod | 75 / 80 / 55 |
| Risk | Medium — discoverability poor; enterprise search maturity |
| Hospital answer | **Partial** — exists but not primary navigation |

### BC-24 – BC-26 Dashboards / Reports / Gap Analysis

| Capability | Completion / Demo / Prod | Notes |
|------------|--------------------------|-------|
| Dashboards/analytics | 80 / 90 / 65 | Projection/Hangfire dependent |
| Reporting hub | 75 / 85 / 60 | Custom reports weaker / hardcoded filters |
| Gap analysis | 80 / 85 / 70 | Strong post-onboarding |

### BC-27 Copilot / AI

| Field | Value |
|-------|-------|
| Purpose | Assist compliance drafting/recommendations |
| Screens | `/app/copilot`, AI sidebar, AI admin settings |
| Completion / Demo / Prod | 55 / 70 / 25 |
| Risk | **High** — placeholder LLM adapters; `NotImplementedException` on LLM draft path; feature flags gate autonomy |
| Hospital answer | **No** for production AI claims without real LLM + governance |

### BC-28 – BC-33 People / Access / Governance / Workforce

| Capability | Completion / Demo / Prod | Hospital answer |
|------------|--------------------------|-----------------|
| User admin | 70 / 75 / 55 | Partial — no bulk import/export |
| Access governance | 75 / 80 / 55 | Partial — deep-link; coarse permissions on some routes |
| Committees/governance | 70 / 75 / 50 | Partial — BE stronger than FE UX |
| Workforce/professional | 75 / 80 / 60 | Mostly yes |
| Directory / WI | 65–70 / 75 / 45–50 | Partial analytics; not LMS |

### BC-34 – BC-36 Communication & Integration

| Capability | Completion / Demo / Prod | Hospital answer |
|------------|--------------------------|-----------------|
| In-app notifications | 70 / 80 / 40 | Partial — SignalR delivery unwired |
| Email delivery | 35 / 40 / 15 | **No** — console email only |
| Integrations | 80 / 85 / 70 | Yes for connector/sync foundation (Phase 12.1) |

### BC-37 Subscriptions / Licensing

| Field | Value |
|-------|-------|
| Purpose | Commercial entitlement (plans, trials, quotas) |
| Screens | **No customer billing UI** |
| APIs | Assign/get/upgrade subscription (internal + gateway) |
| Completion / Demo / Prod | 40 / 50 / 15 |
| Risk | **Critical** — `MaxUsers` / framework quotas not enforced; counters not maintained |
| Hospital answer | **No** for commercial license control |

### BC-38 CCC Pillars

| Field | Value |
|-------|-------|
| Screens | `/app/ccc/**` × 7 |
| Status | Placeholder shells |
| Completion / Demo / Prod | 15 / 20 / 0 |
| Hospital answer | **No** |

### BC-39 Training / LMS

| Field | Value |
|-------|-------|
| Purpose | Training plans, attendance, competency attestation for NABH HR chapters |
| Reality | WorkforceIntelligence training **matrix analytics only**; no LMS domain |
| Completion / Demo / Prod | 20 / 30 / 5 |
| Risk | **High** for NABH HR/training chapters |
| Hospital answer | **No** |

### BC-40 Commercial Operations

| Field | Value |
|-------|-------|
| Purpose | Install, backup, restore, upgrade, support |
| Reality | Dev compose subset; no customer DR runbooks |
| Completion / Demo / Prod | 10 / 10 / 0 |
| Risk | **Critical** |
| Hospital answer | **No** |

---

## Capability Groups — Can Hospital Succeed Today?

| Group | Verdict |
|-------|---------|
| Get on platform & onboard NABH | **Yes with caveats** |
| Run day-to-day OE/evidence/tasks | **Yes with trained QM team** |
| Close findings with CAPA | **Fragile** (model duality) |
| Run document control | **Yes** |
| Run incident management | **Mostly yes** |
| Run continuous surveillance | **No** |
| Rely on email notifications | **No** |
| Enforce paid plan limits | **No** |
| Train staff via LMS | **No** |
| Operate DR/backup as customer | **No** |
| Trust AI recommendations in production | **No** |

---

## Dependencies Map (Business)

```
Onboarding → Framework enroll → Journey → OE Tasks → Evidence → Verification
                                         ↓
                              Findings → Deficiency CAPA → SLA
                                         ↓
                         Incidents/RCA ──┘     Documents ↔ OE mapping
                                         ↓
                              Dashboards / Reports / Graph
```

External hard dependencies for “production hospital tomorrow”:

1. Azure Service Bus (events)
2. SQL Server (15 databases)
3. Real email provider (missing)
4. Key Vault / secret management (missing)
5. Full service topology beyond docker-compose.dev (missing)
