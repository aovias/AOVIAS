# Sprint 16 — Incident Management & Patient Safety Report

## Executive Summary

Sprint 16 delivers the **Patient Safety** operational module for PRAMAN — an enterprise Incident Management platform distinct from complaint management or audit findings alone. The module supports the complete healthcare incident lifecycle while integrating with Findings, Deficiencies, CAPA, Documentation, Analytics, External Auditor, and Compliance Copilot through existing platform services.

---

## Architecture

### Bounded Context Separation

```
Incident (capture)
    ↓
Investigation
    ↓
Finding (optional, human-approved)
    ↓
Deficiency (optional, human-approved)
    ↓
CAPA (optional, via existing engine)
    ↓
Evidence / Verification (existing)
    ↓
Closure
    ↓
Lesson Learned (organizational knowledge)
```

**Incidents never automatically become deficiencies.** Promotion always requires explicit human action via `POST /incidents/promote-finding` and `POST /incidents/promote-deficiency`.

### Backend Structure

| Layer | Location |
|-------|----------|
| Domain aggregates | `ComplianceService/Domain/Incident*.cs`, `PatientFeedback.cs`, `LessonLearned.cs` |
| Enums | `ComplianceService/Domain/Enums/Incident*.cs`, `RcaMethod.cs`, `PatientFeedbackType.cs` |
| DTOs | `ComplianceService/Application/DTOs/IncidentDtos.cs` |
| Services | `IncidentQueryService`, `IncidentCommandService`, `IncidentPromotionService` |
| API | `Controllers/V1/IncidentsController.cs` |
| Gateway | `Gateway/Controllers/IncidentsV1ProxyController.cs` |
| Migration | `Migrations/*Sprint16_IncidentManagement*` |

### Frontend Structure

| Layer | Location |
|-------|----------|
| Feature module | `WebApp/src/app/features/patient-safety/` |
| Route | `/app/patient-safety` |
| Shell nav | Dashboard, Command Center, Investigations, RCA, Near Misses, Sentinel Events, Feedback, Lessons, Analytics, Settings |

---

## Business Rules

1. **Incident ≠ Finding ≠ Deficiency** — separate aggregates with optional linkage IDs
2. **Promotion requires human approval** — no automatic remediation pipeline
3. **RCA is structured** — JSON fields for 5 Whys, Fishbone, contributing factor categories
4. **Patient Feedback is independent** — optional `LinkedIncidentId` only when appropriate
5. **Lessons Learned** — created only from **closed** incidents; searchable knowledge base
6. **Documentation revisions are recommendations** — display related SOPs; no auto-publish
7. **Copilot is advisory only** — never approves or closes incidents
8. **Reuse platform engines** — Assignment, SLA, Escalation, CAPA, Analytics, Copilot

---

## Incident Lifecycle

| Status | Description |
|--------|-------------|
| Draft | Initial capture (optional) |
| Reported | Default on creation |
| UnderInvestigation | Investigation workspace active |
| RcaInProgress | Structured RCA in progress |
| PendingReview | Awaiting review/approval |
| Closed | Incident closed; lessons learned eligible |
| Cancelled | Voided report |

Critical and sentinel severities automatically flag `RcaRequired`.

---

## Incident Categories (Phase 2)

Patient Fall, Medication Error, HAI, Needle Stick, Equipment Failure, Fire Safety, Biomedical Waste, Security, Violence, Patient Identification, Consent, Blood Transfusion, Laboratory, Radiology, Clinical Care, Near Miss, Sentinel Event, Other — extensible via enum.

---

## API Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/incidents/dashboard` | Command center KPIs |
| GET | `/incidents` | Filtered incident list |
| POST | `/incidents` | Report incident |
| PATCH | `/incidents/{id}` | Update incident |
| GET | `/incidents/{id}` | Detail with timeline, docs, investigation, RCA |
| POST | `/incidents/investigation` | Save investigation |
| POST | `/incidents/rca` | Save structured RCA |
| POST | `/incidents/promote-finding` | Human-approved finding creation |
| POST | `/incidents/promote-deficiency` | Human-approved deficiency via FindingPromotionService |
| POST | `/incidents/lesson` | Create lesson learned |
| GET/POST | `/incidents/feedback` | Patient feedback |
| GET | `/incidents/lessons` | Lessons learned repository |
| GET | `/incidents/analytics` | Patient safety analytics |

All proxied via Gateway `IncidentsV1ProxyController`.

---

## Integrations

### Findings & Deficiencies (Phase 9)
- `IncidentPromotionService` creates Finding with `RaisedBy = IncidentReview`
- Deficiency promotion reuses `FindingPromotionService` → SLA/Escalation inheritance

### Documentation (Phase 10)
- `RelatedDocumentationJson` links controlled documents
- Detail panel shows document number, version, acknowledgement %

### Copilot (Phase 11)
- `CriticalIncidentRule` — advisory recommendations for open critical incidents
- Deep link: `/app/patient-safety/command-center?quickFilter=critical`
- `AiPageContext`: `PatientSafety`

### Analytics (Phase 12)
- `/incidents/analytics` — trends, severity, department heatmap, CAPA conversion
- Links to Analytics Hub for executive views

### External Auditor (Phase 13)
- Read-only access via existing `EXTERNAL_AUDITOR` role and Auditor workspace
- Incidents visible through standard compliance scope APIs

### Notification & Escalation (Phase 14)
- Critical incidents inherit SLA after deficiency promotion
- No separate notification framework created

---

## Security (Phase 17)

| Role | Access |
|------|--------|
| Reporter | Create incidents, anonymous flag |
| Investigator | Investigation workspace |
| Department Head / Quality Manager | Review, promote |
| Medical Superintendent / Risk Manager | Oversight, analytics |
| External Auditor | Read-only |

All actions recorded in `IncidentTimelineEntry` and `IncidentAuditTrail`.

---

## Validation

- Backend builds with Sprint 16 migration
- Gateway proxy routes all incident endpoints
- Frontend Patient Safety workspace at `/app/patient-safety`
- KPI cards reuse Findings/DCC patterns
- CSV export on Command Center
- Human promotion workflow enforced in API

---

## Future Roadmap

- Saved views and bulk actions on Command Center grid
- SLA policy binding at incident level (pre-deficiency)
- Copilot draft investigation / RCA question suggestions via AIOrchestrator
- Documentation revision draft creation from incident detail
- Repeat incident ML clustering
- Patient safety indicators linkage (Sprint 17)

---

## Success Criteria

✓ Patient Safety workspace implemented  
✓ Incident Command Center with KPIs  
✓ Investigation workspace  
✓ Structured RCA (5 Whys, Fishbone, factors)  
✓ Patient Feedback module  
✓ Lessons Learned repository  
✓ Finding/Deficiency promotion integration  
✓ Documentation linkage display  
✓ Analytics endpoint  
✓ Copilot critical incident rule  
✓ Gateway proxies  
✓ No duplicated CAPA/SLA/Analytics engines  
