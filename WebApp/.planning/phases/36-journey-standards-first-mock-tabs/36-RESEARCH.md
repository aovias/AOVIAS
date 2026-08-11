# Phase 36 — Research: NABH 6th Edition (Jan 2025) + Compliance Platform

**Primary source:** *NABH Accreditation Standards for Hospitals, 6th Edition, Effective 1 Jan 2025* (ISBN 978-81-965264-9-8), user-provided PDF — extracted sections: Foreword, Contents, Scoring, NA, Accreditation Decision, Summary table, About NABH.

**Product context:** Operator-facing **Compliance Platform** for hospital NABH accreditation — Journey module is the **OE workspace**; accreditation **decision math** and **documentation** expectations must align with this guidebook where the product claims NABH alignment.

---

## Standard Stack (authoritative numbers from PDF)


| Concept               | NABH 6th Edition (Hospital)                                                                                      | Platform implication                                                                                                   |
| --------------------- | ---------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Structure             | **10 Chapters** (hallmark methodology retained)                                                                  | `JourneyChapter` must use official codes: **AAC, COP, MOM, PRE, IPC, PSQ, ROM, FMS, HRM, IMS**.                        |
| Standards             | **100** standards total across chapters                                                                          | `ComplianceStandard` count should match catalog (not 100 in mock if demo-only — but **metadata** must record edition). |
| Objective Elements    | **639** total                                                                                                    | Mock/API catalog should target full count for production; demo may use representative subset with **disclaimer**.      |
| Classification counts | **105** Core, **457** Commitment, **60** Achievement, **17** Excellence                                          | Each OE carries exactly one band; assessment **which OEs are scored** depends on assessment type (see below).          |
| Scoring scale         | **1–5** with published rationale (implementation-based; documentation inadequacy may **downgrade by one**)       | `NabhScore` 1–5 + `ScoreLabel` already fit; add optional **documentation risk flag** in UX copy.                       |
| Not applicable        | Org identifies NA **before** assessment; informs **NABH secretariat**; team **finalizes list** during assessment | Product: NA + justification, workflow for **pre-declared NA list** + audit trail (future backend).                     |


### Chapter → Standard → OE counts (Summary table, PDF p. 19)


| Chapter                                | Code | Standards | OEs     | Core    | Commitment | Achievement | Excellence |
| -------------------------------------- | ---- | --------- | ------- | ------- | ---------- | ----------- | ---------- |
| Access, Assessment and Continuity      | AAC  | 13        | 87      | 6       | 68         | 9           | 4          |
| Care of Patients                       | COP  | 20        | 135     | 13      | 107        | 12          | 4          |
| Management of Medication               | MOM  | 11        | 68      | 13      | 48         | 6           | 1          |
| Patient Rights and Education           | PRE  | 8         | 52      | 12      | 32         | 7           | 1          |
| Infection Prevention and Control       | IPC  | 8         | 49      | 13      | 33         | 3           | 0          |
| Patient Safety and Quality Improvement | PSQ  | 7         | 46      | 8       | 28         | 7           | 3          |
| Responsibility of Management           | ROM  | 6         | 37      | 4       | 23         | 8           | 2          |
| Facility Management and Safety         | FMS  | 7         | 43      | 11      | 29         | 2           | 1          |
| Human Resource Management              | HRM  | 13        | 76      | 16      | 56         | 4           | 0          |
| Information Management System          | IMS  | 7         | 45      | 9       | 33         | 2           | 1          |
| **Total**                              |      | **100**   | **639** | **105** | **457**    | **60**      | **17**     |


### When each band is assessed (Foreword, PDF pp. 42–47)

- **Core (105):** mandatorily assessed **each** assessment.
- **Commitment (457):** assessed at **final** assessment (and contributes to ongoing rules as specified).
- **Achievement (60):** assessed at **surveillance**.
- **Excellence (17):** assessed at **re-accreditation**.

Map product `AuditLifecycleMode` to NABH assessment types for **scoring scope**, not just UI filtering:


| Product mode                      | NABH assessment context | Which OE bands typically scored                                         |
| --------------------------------- | ----------------------- | ----------------------------------------------------------------------- |
| `self_assessment`                 | Internal prep           | All (practice) — align with hospital policy                             |
| `internal_audit`                  | Internal prep           | All (practice)                                                          |
| `final_audit`                     | Final assessment        | Core + Commitment (per PDF notes; **562** OEs in cumulative examples)   |
| `surveillance`                    | Surveillance            | Core + Commitment + Achievement (**622** OEs in PDF cumulative example) |
| Re-accreditation (add if missing) | Re-accreditation        | **All 639** including Excellence                                        |


---

## Architecture Patterns (PDF: Scoring & Accreditation Decision)

### Five-point scale (PDF pp. 555–588)

Scores 1–5 with sample compliance % bands per OE; **non-conformity** language through level 4; level 5 **no non-conformity**. Platform analytics and dashboards should **not** invent a different scale** for “official readiness” without labeling it as internal.

### Accreditation decision — Final assessment (PDF pp. 603–633)

For accreditation after **final assessment**, hospital must achieve **overall compliance ≥ 80%** AND:

1. Every **Core** OE score **≥ 4**.
2. Overall compliance **≥ 80%** for **Commitment**-level OEs.
3. **No standard** has **more than one** OE scored **≤ 2**.
4. **Average** score for each **standard** **≥ 4**.
5. **Average** score for each **chapter** **≥ 4**.
6. Every OE scored **≤ 3** has an **accepted action plan** with timelines.

**Note in PDF:** For final assessment, **only Core + Commitment** OEs in numerator/denominator for the **80%** overall compliance calculation (with NA excluded). Example uses **562** OEs (105+457).

### Surveillance & re-accreditation (PDF pp. 634–678)

Additional rules include **80%** thresholds per **Achievement** and (for re-accreditation) **Excellence**, **improvement** on OEs previously **≤ 2**, and stricter **standard-level** cap on low scores at re-accreditation (**0** OEs ≤ 2 per standard vs **1** at final/surveillance). **See summary table PDF p. 686–700.**

### Alignment with existing app code

`NABH_THRESHOLDS` (`minOverallCompliancePercent: 80`, `minCoreOeAverage: 4`, `minAverageStandard: 4`, `minAverageChapter: 4`) matches the **final assessment** numeric themes in the PDF.

**Gaps vs PDF:**

- **Rule 3:** “No standard with **>1** OE **≤ 2**” — **not** obviously implemented in `AccreditationEligibility`; add validation.
- **Rule 6:** Action plan required for **all** OEs **≤ 3** — ensure platform tracks **acceptance** state, not only “open task”.
- **Band-specific 80%** (Commitment / Achievement / Excellence) — separate cumulative compliance metrics for surveillance/re-accreditation.
- `**readOnly` / lifecycle** should drive **which OEs appear in scoring denominator** per PDF (562 vs 622 vs 639).

---

## Don’t Hand-Roll

- **OE catalog:** Must be traceable to guidebook **AAC.1.a**-style codes and official **Core/Commitment/Achievement/Excellence** flags — no ad hoc counts.
- **Accreditation math:** Implement PDF rules in **tested pure functions** with chapter/standard IDs from real tree; expose **assessment phase** enum (`final` | `surveillance` | `re_accreditation`).
- **NA workflow:** Pre-assessment NA list + secretariat notification is **outside** pure UI — design **API hooks** for hospital compliance officer workflow.

---

## Common Pitfalls

- Treating **internal lifecycle** as identical to NABH **survey types** without mapping.
- Single **80%** headline without **band-specific** denominators (562 vs 622 vs 639).
- Ignoring **documentation downgrade** rule in assessor training copy (product tips / methodology tab).

---

## Current codebase (post–Phase 35) — implementation gaps

- **Hierarchy**: `JourneyChapter.standards[]` → `ComplianceStandard.objectiveElements[]` exists.
- **Data**: `JourneyDataService` uses HTTP `/Journeys/*`; Phase 36 replaces with mock per plans.
- **Store**: Missing `**selectStandard`**, `**selectedStandard**`, `**objectiveElementsForSelectedStandard**`.
- **UI**: Chapter-only left nav; standards in accordion; not five-tab spec.
- **Accreditation panel:** Partial rules only — extend per **Architecture Patterns** above.

---

## Verdict


| Area                                                                | Verdict                                                                                                                       |
| ------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| Domain model (Chapter / Standard / OE, 1–5 scores, classifications) | **Aligned** with NABH 6th ed. structure.                                                                                      |
| Threshold constants (80%, min 4)                                    | **Aligned** with final-assessment **themes** in PDF.                                                                          |
| Full decision engine                                                | **Incomplete** — add rules 3 & 6, band-specific compliance, assessment-phase matrix.                                          |
| Journey as compliance platform module                               | **Correct scope** for OE-level compliance; **not** full NABH application/CAR/secretariat workflow without additional modules. |


---

## RESEARCH COMPLETE

**Next:** Execute `.planning/phases/36-journey-standards-first-mock-tabs/*-PLAN.md` (updated for PDF-aligned accreditation engine tasks).