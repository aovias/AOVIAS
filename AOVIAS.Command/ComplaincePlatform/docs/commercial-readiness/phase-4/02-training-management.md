# Phase 4 — Training & Competency Lite (Sprint 2)

**Sprint:** 2  
**Status:** Delivered  
**Principle:** EXTEND ProfessionalService — do **not** introduce a separate LMS microservice.

## Model (SSOT)

Training lifecycle lite lives under `ProfessionalService/Domain/Entities/TrainingEntities.cs`:

| Entity | Purpose |
|--------|---------|
| `TrainingProgram` | Catalog entry; optional `ControlledDocumentId` (WI/SOP), `RoleCode`, `DepartmentId`, `IsMandatory`, `RenewalMonths` |
| `TrainingSession` | Scheduled delivery of a program |
| `TrainingAttendance` | Participant assignment + attendance status + certificate URL / expiry |
| `TrainingAssessment` | Lite score / pass / assessor against an attendance row |

Existing per-profile `TrainingRecord` remains for ad-hoc history on the professional profile.

### Completion %

`TrainingAttendanceCompletion` (pure helper):

- Completed = `AttendanceStatus == Attended` (optionally require `Passed == true`)
- Percent = `completed / assigned * 100` (2 d.p.)

## APIs (ProfessionalService Controllers/V1)

| Method | Route |
|--------|--------|
| GET/POST | `/api/v1/training/programs` |
| GET | `/api/v1/training/programs/matrix` |
| GET/PUT/DELETE | `/api/v1/training/programs/{id}` |
| GET/POST | `/api/v1/training/sessions` |
| GET/PUT/DELETE | `/api/v1/training/sessions/{id}` |
| POST | `/api/v1/training/sessions/{id}/participants` |
| GET | `/api/v1/training/sessions/{id}/attendances` |
| POST | `/api/v1/training/sessions/attendances/{id}/record` |
| POST | `/api/v1/training/sessions/attendances/{id}/assessment` |
| POST | `/api/v1/training/sessions/attendances/{id}/certificate` |

Gateway `TrainingV1ProxyController` proxies `/api/v1/training/*` via `IProxyService` → ProfessionalService.

## Background job

ProfessionalService has **no Hangfire**. `CertificationExpiryReminderHostedService` runs daily:

- Training attendances with `CertificateUrl` + `ExpiryDate` within 30 days → outbox `Training.CertificateExpiryReminder`
- Professional `Certification` rows expiring within 30 days → outbox `Professional.CertificationExpiryReminder`

## Document publish hook (optional stub)

In `DocumentationCommandService.PublishAsync`, when document type is Sop / Procedure / Protocol / Guideline:

- Log `DocumentPublishedForMandatoryTraining`
- Write Compliance outbox `ComplianceEvent.DocumentPublishedForMandatoryTraining`

This is a broker-agnostic stub for later Professional mandatory-training reaction.

## Migration

`20260726180000_Phase4_TrainingCompetencyLite` creates `TrainingPrograms`, `TrainingSessions`, `TrainingAttendances`, `TrainingAssessments`.

## WebApp

- `features/training`: program list, session calendar + attendance, matrix with WI links
- Routed under `/app/settings/workforce/training`
- Professional profile Training tab: create ad-hoc `TrainingRecord`

## Tests

`ProfessionalService.Tests/TrainingAttendanceCompletionTests` covers completion % and pass-required rules.

## Exit criteria

Programs → sessions → assign → attendance/assessment → certificate URL operable via Gateway; matrix links to controlled documents; daily expiry reminders enqueue outbox messages without a separate LMS.
