# Phase 4 — Document Control (QDMS) Hardening (Sprint 1)

**Sprint:** 1  
**Status:** Delivered  
**Principle:** EXTEND Sprint 15 QDMS in ComplianceService — do **not** introduce a second DMS.

## Model (SSOT)

Controlled documentation lifecycle lives on `ControlledDocument` + related entities under `ComplianceService/Domain/Documentation`:

| Entity / field | Purpose |
|----------------|---------|
| `ControlledDocument.Status` | Lifecycle SSOT (`ControlledDocumentStatus`) |
| `ControlledDocument.NextReviewDate` | Drives ReviewDue automation |
| `ControlledDocument.ExpiryDate` | Hard expiry — past date cannot stay Effective |
| `DocumentVersion.IsSuperseded` / `IsCurrent` | Version supersede on publish / new major version |
| `DocumentReview` | Open/closed review records created by ReviewDue job |
| `DocumentAcknowledgement.ReminderCount` / `LastReminderAt` | Ack reminder cadence |

### Lifecycle

```text
Draft → UnderReview → Approved → Published → Effective
  → ReviewDue → UnderReview (revision) | Effective (no changes)
  → Superseded → Archived
Any non-archived → Archived (expiry / manual)
```

Transitions are enforced on `ControlledDocument.CanTransitionTo` / `TransitionTo`. Mutating commands call `EnsureMutable()` so expired/archived documents cannot be updated, versioned, approved, or published.

## Background jobs (Hangfire)

Registered in `ComplianceService/Program.cs`:

| Job ID | Schedule (UTC) | Behavior |
|--------|----------------|----------|
| `document-review-due` | Daily 07:00 | `Effective` + `NextReviewDate <= UtcNow` → `ReviewDue`; creates open `DocumentReview` if none |
| `document-expiry` | Daily 07:30 | Past `ExpiryDate` → `Archived`; emits `DocumentExpired` via `IComplianceNotificationPublisher` |
| `document-ack-reminder` | Daily 08:00 | Pending acks on Effective docs: `ReminderCount++`, notify `DocumentAckReminder` (24h cooldown) |

## Publish / version semantics

1. **Publish** (`POST /documentation/publish?documentId=`): requires `Approved`; marks prior versions `IsSuperseded=true`; optionally resets acknowledgements (`RequireReAcknowledge`, default true); sets Effective + next review.
2. **Create version** (`POST /documentation/{id}/versions`): supersedes prior versions; document returns to `Draft`.
3. **Complete review** (`POST /documentation/{id}/reviews/complete`): closes open `DocumentReview`; no-change → `Effective` + new `NextReviewDate`; revision → `UnderReview`.

## APIs

### ComplianceService

| Method | Route |
|--------|--------|
| POST | `/api/v1/documentation/publish?documentId=` |
| POST | `/api/v1/documentation/approve` |
| POST | `/api/v1/documentation/{id}/transition` |
| POST | `/api/v1/documentation/{id}/versions` |
| POST | `/api/v1/documentation/acknowledge` |
| POST | `/api/v1/documentation/{id}/reviews/complete` |

Gateway `DocumentationV1ProxyController` proxies the same paths.

## WebApp (`features/documentation`)

- `documentation-api.service.ts`: publish, approve, transition, createVersion, acknowledge, completeReview.
- Register / policies / SOPs pages: Submit / Approve / Publish / New version by status.
- Acknowledgements: **Ack** action for pending rows.
- Review calendar: **Complete review** for Effective / ReviewDue.

## Migration

`20260726160000_Phase4_DocumentControlExpiry` adds `ControlledDocuments.ExpiryDate` (+ index).

## Tests

`ComplianceService.Tests/DocumentControlLifecycleTests`:

- ReviewDue job transitions Effective → ReviewDue and creates `DocumentReview`.
- Publish supersedes prior `DocumentVersion` rows.
- Expired documents cannot become Effective; archived docs are immutable.

## Exit criteria

Draft → Review → Approve → Publish → Ack → Revision → Archive/Expiry is operable in UI without spreadsheets, with automated ReviewDue / expiry / ack reminders.
