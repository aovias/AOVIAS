# 05 — Healthcare Domain Gap Analysis

**Program:** CRP v1.0  
**Assessment Date:** 26 July 2026  
**Lenses:** Hospital Administrator · Quality Manager · NABH Consultant · Department Head · Internal Auditor · External NABH Assessor

---

## 1. Domain Verdict

AOVIAS Command has a **credible NABH Hospital 6th Edition core** (catalog, journey, OE, evidence, findings/deficiency CAPA, QDMS, incidents, clinical indicators, committees).  

It is **not yet a complete hospital accreditation operating system**. Critical healthcare concepts are missing, dual-modeled, or placeholder.

**Healthcare domain readiness (suggested): 6.0 / 10**

---

## 2. NABH Concept Coverage Matrix

| Healthcare concept | Exists? | Where | Hospital-usable today? | Gap severity |
|--------------------|---------|-------|------------------------|--------------|
| Organization structure | Yes | OrganizationService | Partial (flat org→hospital) | Medium |
| Multi-hospital group | Partial | Org/membership | Weak multi-org JWT/UX historically | Medium |
| Departments | Yes | Org + MasterDepartment | Yes | Low |
| Campus / building / wing | No | — | No | Medium |
| NABH chapters/standards/OEs | Yes | KnowledgeBase + Compliance catalog | Yes (seeded 6E) | Low |
| Entry Level NABH | Yes | Compliance.MasterData | Yes | Low |
| JCI / ISO / NABL / Fire NOC | Partial | Policy stubs / filters | No (not operational) | High |
| Framework assignment | Yes | Enroll + journey | Yes (NABH) | Medium |
| Compliance program / journey | Yes | ComplianceJourney | Yes | Medium |
| Objective element execution | Yes | OE workspace + tasks | Yes | Medium |
| Evidence | Yes | Evidence + versions + OE links | Yes | Medium |
| Evidence verification | Yes | State machine | Partial | Medium |
| Policies / SOPs / manuals | Yes | ControlledDocument QDMS | Yes | Low |
| Document control lifecycle | Yes | Approve/publish/ack | Yes | Low |
| Document–OE mapping | Yes | Graph / QDMS | Yes | Medium |
| CAPA | Partial | Deficiency-embedded + orphan `Capa` | Fragile | **High** |
| Internal audit | Partial | Operational mode + dual backends | Partial | High |
| External assessment support | Partial | Auditor workspace | Partial | High |
| Observations | Partial | Auditor domain / reused UI | Partial | Medium |
| Incidents / near-miss / sentinel | Yes | Patient Safety | Yes | Medium |
| RCA | Yes | Incident RCA workspace | Yes | Medium |
| Patient feedback | Yes | Patient Safety | Yes | Low |
| Lessons learned | Yes | Patient Safety | Yes | Low |
| Training / LMS | No domain | WI training matrix only | No | **High** |
| Competency / credential tracking | Partial | ProfessionalService | Partial | Medium |
| Risk register | Partial | Risk / RiskSnapshot / deficiency risk | Partial | Medium |
| Committee meetings | Yes | GovernanceService | Partial UX | Medium |
| Clinical indicators | Yes | Quality Intelligence | Yes | Medium |
| Department scorecards | Yes | Quality | Yes | Medium |
| Compliance scoring / maturity | Yes | Scoring + maturity APIs | Yes | Medium |
| Management review | Partial | Committees + executive reports | No dedicated MR module | High |
| Hospital readiness report | Yes | Readiness reports | Yes | Medium |
| Surveillance / continuous monitoring | Stub | Mode + disabled nav | No | **High** |
| Infection control specialty | No dedicated | — | No | Medium |
| Medication management specialty | No dedicated | — | No | Medium |
| Equipment / biomedical | No | Demo seeder notes unsupported | No | Medium |
| Facility / engineering compliance | Partial | Fire NOC policy stub | No | Medium |
| Consent / clinical documentation | No | — | No | Low (out of core scope?) |
| Statutory licenses register | Partial | Professional renewals | Partial | Medium |

---

## 3. Missing Healthcare Concepts (Must Document)

### Critical / High for NABH commercial story

1. **Unified CAPA system** — Assessors expect one CAPA register with status, owner, due date, effectiveness. Dual models break trust.  
2. **Surveillance / continuous compliance** — Post-accreditation monitoring is a sold hospital expectation; currently stubbed.  
3. **Training & competency LMS** — NABH HRM chapters need training plans, attendance, assessment, acknowledgment — only analytics matrix exists.  
4. **Management Review** — Formal MR agenda, inputs (quality indicators, incidents, CAPA, audit), outputs/actions — no dedicated module.  
5. **Internal audit program** — Annual audit calendar, auditor independence, checklist packs, observation grading — fragmented across dual services.  
6. **Multi-framework accreditation** — Hospitals often run NABH + statutory (Fire, Pollution, AERB, etc.); non-NABH policies are placeholders.

### Medium

7. Physical facility hierarchy (campus/building/wing/unit)  
8. Biomedical equipment compliance register  
9. Infection control / antibiotic stewardship specialty packs  
10. Medication management / crash cart checks (if in product scope)  
11. Risk register as first-class hospital risk (beyond deficiency risk)  
12. Committee effectiveness (quorum, attendance %, decision→CAPA closure)  
13. Statutory license & inspection calendar (Fire NOC, PCB, PCPNDT, etc.)

### Lower / later

14. JCI chapter packs  
15. Clinical pathway / order-set compliance  
16. Nursing quality dashboards beyond indicators

---

## 4. NABH Role Expectations vs Platform

### External NABH Assessor

| Expectation | Platform today |
|-------------|----------------|
| Read-only evidence packs by chapter/OE | Partial (auditor evidence review) |
| Observation & finding capture | Partial |
| Isolated auditor login experience | **Weak** (`roleGuard` unused) |
| Traceability OE↔policy↔evidence↔CAPA | Partial (graph hidden from nav) |
| Closing meeting report | Partial reports |

**Gap:** Assessor experience is not a hardened product mode.

### Quality Manager

| Expectation | Platform today |
|-------------|----------------|
| Journey progress & maturity | Strong |
| Task & evidence chase | Strong (if email works — it doesn’t in prod) |
| CAPA register | Fragile |
| Document control | Strong |
| Indicator monitoring | Strong |
| Committee MoM | Weak UX |
| Training compliance | Missing |

### Department Head

| Expectation | Platform today |
|-------------|----------------|
| Dept OE ownership | Partial (routing/ownership) |
| Dept tasks | Partial |
| Scorecard | Yes |
| Staff competency | Partial (professional) |
| Dept document acknowledgements | Yes (ack module) |

### Hospital Administrator

| Expectation | Platform today |
|-------------|----------------|
| Executive readiness | Partial (dashboards/reports) |
| License & user governance | Weak commercial license |
| Risk overview | Partial |
| Board pack | Partial (executive quality + reports) |

### Internal Auditor

| Expectation | Platform today |
|-------------|----------------|
| Audit plan & schedule | Partial |
| Checklist execution | Partial |
| Independence of roles | Weak |
| CAPA follow-up | Tied to deficiency path |

---

## 5. Domain Model Integrity Issues

| Issue | Clinical/compliance impact | Severity |
|-------|----------------------------|----------|
| Dual CAPA (`Capa` vs Deficiency CAPA vs ActionPlan) | Wrong CAPA counts; assessor confusion | High |
| Dual Audit (`AuditService` vs Compliance Auditor) | Duplicate semantics; engineering risk | High |
| Dual department catalogs | Ownership mapping errors | Medium |
| Evidence historically task-centric vs OE-centric (migrated) | Residual dual paths possible | Medium |
| Journey policies no-op for non-NABH | Invalid stage transitions allowed | Medium |
| `SurveillanceDue` hardcoded false | False compliance posture | High |
| Training only as WI projection | Cannot prove staff competency for OE | High |

---

## 6. Framework & Catalog Reality

| Framework | Seeded | Journey policy | Commercial claim allowed? |
|-----------|--------|----------------|---------------------------|
| NABH Hospital Full 6E | Yes (~503 OEs) | Partial (NABH policy exists but weak gates) | **Pilot yes / full claim careful** |
| NABH Entry Level | MasterData seeder | Partial | Pilot |
| NABL | Policy placeholder | No | No |
| Pollution | Policy placeholder | No | No |
| Fire NOC | Policy placeholder | No | No |
| JCI | Custom report filters only | No | No |

---

## 7. Healthcare Workflow Fitness (Assessor Lens)

| Workflow | Fit for NABH visit tomorrow? |
|----------|------------------------------|
| Show chapter→standard→OE structure | Yes |
| Show evidence per OE | Yes (if populated) |
| Show controlled policies mapped to OE | Yes |
| Show open CAPA aging | Risky (duality) |
| Show internal audit program FY | Weak |
| Show training compliance % | No |
| Show surveillance after accreditation | No |
| Show committee minutes with actions | Weak |
| Show clinical indicator trends | Yes (if measured) |
| Show incident RCA closed loop | Partial |

---

## 8. Recommended Healthcare Scope for v1 Commercial Release

### Include (sell)

- NABH Hospital 6E journey + evidence + tasks  
- Findings → Deficiency CAPA (**after unification**)  
- QDMS document control  
- Patient safety incidents + RCA  
- Clinical indicators + scorecards  
- Readiness / compliance reports  
- Committees (MVP)  
- Onboarding + org/dept setup  

### Exclude or label “Beta / Not included”

- Surveillance  
- CCC pillars  
- Audit simulation  
- Production AI autonomy  
- JCI/NABL/Fire packs  
- LMS/training attestation  
- Management Review dedicated module  
- Biomedical equipment register  

### Must-fix before any paid hospital go-live

1. CAPA SSOT  
2. Real notification email  
3. Auditor role isolation  
4. License enforcement  
5. Ops backup/restore runbook  
6. Hide unfinished healthcare nav items  

---

## 9. Domain Gap Register (Healthcare-specific)

| ID | Gap | Expected | Current | Severity |
|----|-----|----------|---------|----------|
| HD-001 | Unified CAPA | Single register | Dual/triple models | Critical/High |
| HD-002 | Surveillance | Continuous monitoring | Stub/disabled | High |
| HD-003 | LMS / training | Attestation workflows | Matrix analytics only | High |
| HD-004 | Management Review | Formal MR module | Fragmented | High |
| HD-005 | Internal audit program | Calendar + checklists | Dual backends / partial | High |
| HD-006 | Assessor mode | Isolated read/write rules | Weak role shell | High |
| HD-007 | Multi-statutory frameworks | Real policies + catalogs | Placeholders | High |
| HD-008 | Facility hierarchy | Campus/wing structure | Flat hospital | Medium |
| HD-009 | Equipment compliance | Biomed register | Missing | Medium |
| HD-010 | Risk register | Enterprise risk | Partial entity | Medium |
| HD-011 | Committee effectiveness | Quorum/MoM/actions | Thin UI | Medium |
| HD-012 | Graph discoverability | Assessor traceability | Hidden nav | Medium |
| HD-013 | Email chase for overdue OE | Operational quality | Console email | High |
| HD-014 | Indicator → investigation loop | Closed quality loop | Partial | Medium |

---

## 10. Closing Statement (Healthcare)

A hospital can **demonstrate** NABH preparation on AOVIAS Command today with seeded data and a Quality Manager guiding the tool.  

A hospital **cannot** independently run a full accreditation lifecycle (training, surveillance, management review, multi-statutory compliance, trustworthy CAPA register, assessor-isolated review) without significant process workarounds and engineering support.

That gap is the difference between an engineering platform and a commercial healthcare product.
