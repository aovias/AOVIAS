# OE Execution Wizard — API and integration

## Objective

Persist multi-step OE execution wizard state per **(ComplianceJourneyId, ObjectiveElementId)** with optimistic concurrency (**RowVersion** only), throttled auto-save on the client, and a completion flow that applies scoring via existing journey services.

## Database: `ObjectiveJourneys`

| Column | Purpose |
|--------|---------|
| `Id` | Surrogate PK (informational on responses as `id`) |
| `TenantId`, `HospitalId` | Scope / denormalized from journey |
| `ComplianceJourneyId`, `ObjectiveElementId` | **Unique** business key |
| `WizardPayloadJson` | Canonical JSON (see below) |
| `CurrentStepIndex` | Denormalized from payload |
| `Status` | `Draft` / `InProgress` / `Completed` (stored as int) |
| `LastUpdatedAt` | Audit / UX only — **not** used for conflicts |
| `CompletedAt` | Set when status is `Completed` |
| `RowVersion` | SQL `rowversion` — **only** OCC token |

## Canonical JSON

Server and WebApp use the same **fixed property order** when comparing payloads (string equality, no deep compare):

`auditChecklistChecked`, `completeAck`, `evalJustification`, `evalScore`, `evalSubmitted`, `evalTone`, `evidenceAck`, `executeReady`, `improveReady`, `needsImprove`, `savedAt`, `stepIndex`, `understandAck`

- C#: `OeWizardPayloadCanonicalizer.Canonicalize`
- Angular: `canonicalizeOeWizardPayload` in `oe-wizard-payload-canonicalize.ts`

Max UTF-8 size: **64 KB** (enforced on save).

## DTOs (aligned names)

Shared shape: `OeWizardPersistedState`, `ObjectiveJourneyStateDto`, `ObjectiveJourneyStatusDto`, `SaveObjectiveJourneyDraftRequest`, `CompleteObjectiveJourneyRequest` in:

- `ComplianceService.Application.DTOs`
- `Gateway.DTOs`
- `WebApp` `journey-api.model.ts` (+ existing `oe-execution-wizard.model.ts` for the payload interface)

Status JSON strings: `draft`, `inProgress`, `completed`.

## Endpoints (Gateway = Compliance paths)

Base:

` /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/oe-execution-wizard`

| Method | Path | Purpose |
|--------|------|---------|
| GET | `.../oe-execution-wizard` | Load state; **404** if no row |
| PUT | `.../oe-execution-wizard` | Upsert draft (idempotent; skip DB write if canonical JSON unchanged) |
| POST | `.../oe-execution-wizard/complete` | Validate, patch OE score (mapper), complete task if resolved, mark row `Completed` |

Optional query on **POST complete**: `operationalMode`, `auditLifecycleMode` (same as other journey OE APIs).

### Request bodies

- **PUT:** `{ "payload": { ...OeWizardPersistedState }, "rowVersion": "<base64 optional>" }`
- **POST complete:** `{ "payload": { ... }, "rowVersion": "<optional>", "complianceTaskId": "<optional guid>" }`

### Concurrency

- If `rowVersion` is sent and does not match the current DB token → **409** (`RESOURCE_CONFLICT` / `VERSION_CONFLICT` semantics).
- Completed rows reject further **PUT** with **409**.

## Completion mapping (single place — backend)

`OeWizardCompletionMapper.ToPatchRequest` in Compliance service only:

| Wizard | `PatchObjectiveElementRequest` / remarks |
|--------|-------------------------------------------|
| `evalScore` | `Score` |
| `evalJustification` | `ScoreRationale` (trimmed) |
| `auditChecklistChecked` | `AuditRemarks`: `[oe-wizard:checklist] {done}/{total} checked; flags={0|1…}` |

## Frontend behavior

1. **Open wizard:** `GET` state; on **404**, fall back to `sessionStorage`; on success, apply server payload and `rowVersion`.
2. **Auto-save:** `markWizardDirty()` → throttled **PUT** (500 ms, leading + trailing); skips when canonical JSON matches last successful save.
3. **Save draft button:** `flushWizardDraftToServer()` — immediate **PUT**.
4. **Mark as complete:** `POST .../complete` with latest payload + `rowVersion`; on success, `status === 'completed'` locks UI (`wizardInputsLocked`).
5. **Read-only:** `wizardInputsLocked` = operational wizard read-only **or** server `completed`.

## Textual state flow

1. User edits → local signals → `sessionStorage` + throttled PUT.
2. Server upserts row, returns new `rowVersion`.
3. Another tab sends stale `rowVersion` → 409 → client reloads `GET`.
4. User completes → POST → `PatchObjectiveElementAsync` + optional `CompleteTaskAsync` → row `Completed` → UI read-only.

## Validation scenarios

- Partial save + refresh: state restored from `GET`.
- Concurrent edit: second client gets **409** on PUT with stale `rowVersion`.
- Completion: OE score updated; compliance task completed when found for `(hospitalId, objectiveElementId)`.
