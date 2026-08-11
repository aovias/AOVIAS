# Journey Screen — Backend API Specification

This document specifies REST APIs to replace the current **mock** workspace (`JourneyMockWorkspaceService`) and bind **Journey** (`/app/journey`) and **Journey Execution** (`/app/journey/execution`) UIs to real backends.

**References (frontend):**

- **Gateway evidence (canonical multipart + bulk DTOs):** `[gateway-evidence-api-reference.md](gateway-evidence-api-reference.md)` → `ComplaincePlatform/docs/backend/gateway-evidence-api-specification.md` (v1.3). Keep §3.2 evidence tables consistent with that doc.
- `JourneyWorkspaceStore` — load, **OperationalMode** (Phase 48), local OE patches, filters, OE wizard.
- `buildJourneyOperationalModeUiPolicy` — **client-side** UI policy from mode + context (`journey-operational-mode.policy.ts`); backend supplies **mode**, **read-only flags**, and **data fields** listed below.
- `src/app/features/journey/models/journey-api.model.ts` — HTTP DTO sketches.
- `src/app/features/journey/models/journey-tree.model.ts`, `chapter.model.ts`, `standard.model.ts`, `objective-element.model.ts`, `execution-screen-vm.model.ts`, `journey-dashboard-vm.model.ts`, `journey-operational-mode-ui.model.ts`.
- Phase 48 — `.planning/phases/48-journey-operational-mode-dynamic-ui/` (operational-mode–aware metrics, execution panels, context quick actions, OE wizard gating).

### 1.1 Client HTTP budget (Angular)

Long-running split GETs (`/session`, `/dashboard`, `/chapters`, `/chapters/{id}/standards`, `/objective-elements/{id}`) may exceed quick-call defaults. The WebApp applies **`environment.journeyLongReadTimeoutMs`** (RxJS `timeout` on tagged requests via `JOURNEY_LONG_READ_CONTEXT`) and must **not** be lower than Gateway or reverse-proxy limits. **0** disables that client-side deadline (browser and infrastructure limits still apply). Fill deployment-specific numbers from the Gateway/ops owner.

**Cancellation:** unsubscribing an `HttpClient` request aborts it in the browser. That is expected when the user leaves the Journey area or when a **newer** load supersedes an older one (`switchMap`). Phase 56 aligns store lifetime and request supersession so waits on the **same** screen are not cut off by incidental navigation between Journey dashboard and execution.

---

## 1. Scope & principles


| Area                           | Behavior                                                                                                                                                                                                                                            |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Tenant / hospital**          | Every call is scoped to an authorized **hospital** (and organization). Resolve from auth claims or `X-Hospital-Id` (if your platform uses explicit headers).                                                                                        |
| **Journey identity**           | Primary key `**journeyId`** (UUID) aligns with organization “hospital journey” records.                                                                                                                                                             |
| **Operational mode (journey)** | Integer `**operationalMode`** **0–4** (see §2). This drives NABH lifecycle behavior in-app. **Not** the shell sidebar string union (`ShellOperationalModeService`)—that remains a separate UX concern unless you explicitly unify them server-side. |
| **Split payloads**             | The former monolithic “full workspace” response is **split** into smaller GETs (§3) so dashboards can load metrics without the full chapter tree, and execution can load **one chapter** at a time.                                                 |
| **Idempotency**                | PATCH OE supports `**If-Match`** or body `**version`** on the OE.                                                                                                                                                                                   |
| **Envelope**                   | JSON responses SHOULD use a common envelope or Problem Details (`application/problem+json`) on failure.                                                                                                                                             |


---

## 2. Operational mode enum (Phase 48)

Align with `OperationalMode` in `nabh.enums.ts`:


| Value | Name            | Typical `auditLifecycleMode` string (if used) |
| ----- | --------------- | --------------------------------------------- |
| `0`   | SelfAssessment  | `self_assessment`                             |
| `1`   | InternalAudit   | `internal_audit`                              |
| `2`   | FinalAudit      | `final_audit`                                 |
| `3`   | Surveillance    | `surveillance`                                |
| `4`   | ReAccreditation | `re_accreditation`                            |


**UI policy** (which columns/badges/wizard locks show) is computed on the client from `operationalMode` + flags. The API must still return **all data fields** in §8 so every control can bind when that mode expects them (e.g. `passFailLabel` in Final Audit, `changedSinceLastAudit` in Surveillance).

---

## 3. Endpoints (split responses)

### 3.1 Read (GET)


| Method | Path                                                          | Purpose                                                                                                                                             |
| ------ | ------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `GET`  | `/api/v1/journeys/{journeyId}/session`                        | Small: identity, framework, **operationalMode**, read-only flags, sync time, catalog meta, mode progression hints.                                  |
| `GET`  | `/api/v1/journeys/{journeyId}/dashboard`                      | **Journey page**: overview KPIs, accreditation block, three trend series (no chapter tree).                                                         |
| `GET`  | `/api/v1/journeys/{journeyId}/chapters`                       | **Chapter index** for execution left rail (summaries + Phase 48 chapter indicators).                                                                |
| `GET`  | `/api/v1/journeys/{journeyId}/chapters/{chapterId}/standards` | **Execution center panel**: all standards for one chapter, each with nested OEs (full rows + Phase 48 standard/OE fields).                          |
| `GET`  | `/api/v1/journeys/{journeyId}/objective-elements/{oeId}`      | **OE detail**: evidence, history, action plans, remarks, **changedSinceLastAudit**, optional **journeyWorkflowStatus**, for context panel + wizard. |


**Query parameters**

- All GETs that depend on lifecycle: `**operationalMode`** (integer 0–4) **or** `auditLifecycleMode` (string enum matching §2)—server MUST reject inconsistent pairs with `400`.
- `GET .../chapters` optional: `search`, `sort` (`risk`  `code`).

### 3.2 Write


| Method   | Path                                                                           | Purpose                                                                                |
| -------- | ------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------- |
| `PATCH`  | `/api/v1/journeys/{journeyId}/session/operational-mode`                        | Persist **operationalMode** when allowed (replaces older “PATCH lifecycle-mode only”). |
| `PATCH`  | `/api/v1/journeys/{journeyId}/objective-elements/{oeId}`                       | Update OE score, N/A, rationale, audit remarks.                                        |
| `POST`   | `/api/v1/journeys/{journeyId}/objective-elements/{oeId}/evidence`              | Multipart evidence upload.                                                             |
| `POST`   | `/api/v1/journeys/{journeyId}/objective-elements/{oeId}/evidence/bulk`         | Multipart **bulk** evidence upload (Gateway **v1.3+**).                                |
| `DELETE` | `/api/v1/journeys/{journeyId}/objective-elements/{oeId}/evidence/{evidenceId}` | Remove evidence (policy permitting).                                                   |


#### Gateway v1.3 — evidence multipart contracts

Paths in product docs use `/api/v{version}/…` — align `version` with `environment.apiBaseUrl` (e.g. `v1`). The Gateway may also expose `**/api/v1.0/…`** or require an `**api-version: 1.0**` header per deployment; confirm the running Gateway matches the segment in `environment.apiBaseUrl` so multipart POSTs hit the intended route. See `[gateway-evidence-api-reference.md](gateway-evidence-api-reference.md)` §3.


| Endpoint suffix   | `Content-Type`        | Parts / fields                                                                                                                                                                                |
| ----------------- | --------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `…/evidence`      | `multipart/form-data` | `**file`** (required), `**evidenceType`** (required), `**title`**, `**metadata**` (optional; JSON string)                                                                                     |
| `…/evidence/bulk` | `multipart/form-data` | `**files**` — one part per file (repeat the same field name; confirm part name against deployed Gateway OpenAPI), `**evidenceType**` (required), `**sharedMetadata**` (optional; JSON string) |


**Limits (v1.3 UI + gateway):** max **10 MB** per file; max **15** files per bulk request. Each file in a bulk request creates a **separate** evidence record. If the journey is **locked**, the server may reject the **entire** request (handle as a single error in clients).

### 3.3 Optional (wizard persistence)


| Method | Path                                                                     | Purpose                                                                       |
| ------ | ------------------------------------------------------------------------ | ----------------------------------------------------------------------------- |
| `POST` | `/api/v1/journeys/{journeyId}/objective-elements/{oeId}/wizard/progress` | Persist wizard step / ack state (replaces pure `sessionStorage` when needed). |


### 3.4 Recommended load sequence (frontend)

1. `**/session`** — establish mode, read-only, framework labels for header chip.
2. **Journey route**: `**/dashboard`** for cards, charts, mode tiles (use overview + trends).
3. **Execution route**: `**/chapters`** then `**/chapters/{id}/standards`** when user picks a chapter; `**/objective-elements/{oeId}`** when opening wizard or refreshing context after edits.

---

## 4. Response templates

### 4.1 `GET .../session`

```json
{
  "journeyId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "frameworkCode": "CPDDDDDD02",
  "frameworkName": "NABH Entry Level Accreditation (Hospital)",
  "operationalMode": 0,
  "readOnly": false,
  "snapshotReadOnly": false,
  "lastSyncedAt": "2026-04-11T12:00:00.000Z",
  "catalog": {
    "standardEdition": "6th",
    "effectiveFrom": "2023-01-01",
    "totalStandardsExpected": 100,
    "totalOesExpected": 639,
    "isPartialCatalog": false
  },
  "modeProgression": {
    "allowedNextModes": [1],
    "lockReasonByTargetMode": {
      "1": null,
      "2": "Complete internal audit prerequisites."
    }
  }
}
```

**Notes**

- `**snapshotReadOnly`** — server-side catalog/journey frozen (in addition to lifecycle read-only).
- `**modeProgression`** — optional; mirrors `operationalModeProgressionBlocked` / lock copy for mode tiles. If omitted, client may derive locks from business rules (weaker).

---

### 4.2 `GET .../dashboard`

```json
{
  "overview": {
    "coreCompliancePercent": 78,
    "failedCoreCount": 2,
    "oesAtOrBelowThreeCount": 5,
    "openActionPlansCount": 3
  },
  "accreditation": {
    "overallCompliancePercent": 72,
    "coreOeAverageScore": 4.1,
    "averageStandardScore": 3.8,
    "averageChapterScore": 4.0,
    "violations": ["Core OE average below threshold for current phase."]
  },
  "accreditationReadinessPercent": 72,
  "oeScoreTrend": [{ "label": "Week 1", "value": 65 }],
  "chapterImprovementTrend": [{ "label": "Ch-A", "value": 12 }],
  "riskEvolutionTrend": [{ "label": "Q1", "value": 40 }]
}
```

**Notes**

- `**accreditationReadinessPercent`** — optional precomputed **0–100** for `AccreditationProgressCard` (today the UI blends accreditation + mode; providing this avoids duplicate business logic).
- `**trends`**: `value` is **0–100** for chart bars/lines.

---

### 4.3 `GET .../chapters` (chapter index)

Each item MUST support **Journey** mode tiles (not this call) and **Execution** chapter list + Phase 48 badges.

```json
{
  "items": [
    {
      "id": "ch-uuid-1",
      "code": "AAC",
      "name": "Access, Assessment and Continuity of Care",
      "complianceScorePercent": 82,
      "risk": "high",
      "coreComplianceStatus": "met",
      "openIssuesCount": 4,
      "pendingAudit": false,
      "coreFailureFlag": false,
      "issueBadgeLabel": "4",
      "passFailLabel": "PASS",
      "changedSinceLastAudit": true,
      "maturityLabel": "Maturity L3",
      "highlightHighRisk": false
    }
  ]
}
```

**Phase 48 field semantics**


| Field                   | Used when                       | Notes                                                                    |
| ----------------------- | ------------------------------- | ------------------------------------------------------------------------ |
| `passFailLabel`         | Final Audit                     | Short PASS/FAIL or localized equivalent.                                 |
| `changedSinceLastAudit` | Surveillance                    | **Required** for accurate “Changed” chip and wizard **Evaluate** gating. |
| `maturityLabel`         | Re-accreditation                | Shown in chapter row when mode expects maturity.                         |
| `highlightHighRisk`     | Internal Audit                  | Emphasize row for high-risk chapters.                                    |
| `issueBadgeLabel`       | All (when issues badge visible) | Preformatted for display (e.g. count or "—").                            |


---

### 4.4 `GET .../chapters/{chapterId}/standards`

Returns `**standards[]`** each with `**objectiveElements[]`** for the execution standards panel and standard card expansion.

**Standard** (extends `ComplianceStandardApiDto` + Phase 48):

```json
{
  "chapterId": "ch-uuid-1",
  "items": [
    {
      "id": "st-uuid-1",
      "code": "AAC.1",
      "title": "Patient access",
      "description": "Full standard text…",
      "completionPercent": 75,
      "riskBadge": "medium",
      "coreSummary": "Core OE summary…",
      "openIssuesCount": 2,
      "complianceStatusLabel": "Met",
      "evidenceGapLabel": "2 gaps",
      "changeTrend": "up",
      "previouslyFailed": false,
      "excellenceBadge": false,
      "objectiveElements": [
        {
          "id": "oe-uuid-1",
          "standardId": "st-uuid-1",
          "code": "AAC.1.1",
          "description": "OE requirement text",
          "classification": "core",
          "score": 4,
          "scoreLabel": "Substantial",
          "scoreRationale": null,
          "evidenceStatus": "complete",
          "isNa": false,
          "naJustification": null,
          "auditStatus": "compliant",
          "departmentLabel": "Clinical",
          "assigneeDisplayName": "Jane Doe",
          "lastUpdatedAt": "2026-04-10T10:00:00.000Z",
          "dueAt": null,
          "nextReviewAt": null,
          "auditRemarks": null,
          "journeyWorkflowStatus": "in_progress",
          "changedSinceLastAudit": false,
          "excellenceNote": null,
          "version": 7,
          "actionPlans": [],
          "evidence": [],
          "history": []
        }
      ]
    }
  ]
}
```

**Objective element — required bindings**

- Core assessment: `classification`, `score`, `scoreLabel`, `evidenceStatus`, `isNa`, `naJustification`, `auditStatus`, `departmentLabel`, `assigneeDisplayName`, dates, `auditRemarks`.
- **Phase 48**: `changedSinceLastAudit` (Surveillance), `excellenceNote` (Re-accreditation).
- **Workflow**: `journeyWorkflowStatus` — `not_started`  `in_progress`  `completed`. If omitted, client may **derive** (see `oeJourneyStatus`); exposing it is **recommended** for consistent mission-control labels.

**Standard — Phase 48 meta** (for `standard-card` mode row)

- `complianceStatusLabel`, `evidenceGapLabel`, `changeTrend` (`up`  `down`  `flat`), `previouslyFailed`, `excellenceBadge`, `openIssuesCount`.

---

### 4.5 `GET .../objective-elements/{oeId}`

Same **single OE** shape as in §4.4 (full nested `evidence`, `history`, `actionPlans`). Use for:

- OE execution wizard header + steps (Understand uses `description`).
- Context panel after PATCH / upload.
- Surveillance: authoritative `**changedSinceLastAudit`** for Evaluate step.

---

### 4.6 `PATCH .../session/operational-mode`

```json
{ "operationalMode": 1 }
```

Response: **204** or **200** with body of `**GET .../session`**.

---

### 4.7 `PATCH .../objective-elements/{oeId}`

Request (aligns with `PatchObjectiveElementBody`):

```json
{
  "version": 7,
  "isNa": false,
  "naJustification": null,
  "score": 4,
  "scoreRationale": "Met all criteria during observation.",
  "auditRemarks": "Reviewed with department head."
}
```

Response: updated **OE** fragment + optional `**overview`** rollup (same shape as `dashboard.overview`) to refresh KPIs without `GET /dashboard`.

---

### 4.8 `POST .../evidence` — unchanged

Multipart `file`, optional `name`, `expiryAt`. Response: evidence metadata (`id`, `name`, `verificationStatus`, `uploadedAt`, `expiryAt`, `uploadedBy`).

---

### 4.9 `POST .../wizard/progress` (optional)

```json
{
  "stepIndex": 2,
  "understandAck": true,
  "executeReady": true,
  "evalSubmitted": false
}
```

Persist only what product requires; UI today keeps most state locally.

---

## 5. Business logic (by endpoint)

### 5.1 `GET /session`

- Authorize hospital + journey.
- Return current **operationalMode** and read-only flags.
- **Catalog** block: edition metadata for “partial catalog” messaging (`JourneyCatalogMeta`).

### 5.2 `GET /dashboard`

- Authorize; compute **overview** and **accreditation** for `**operationalMode`** (same rules as `evaluateAccreditationEligibility` / NABH phase mapping).
- Compute **trends** for the three chart cards.
- Optionally compute **accreditationReadinessPercent** for the progress card.

### 5.3 `GET /chapters`

- Authorize; return chapter **summaries** with Phase 48 fields **populated or null** (null when not applicable to current mode—client still uses `operationalModeUi` to show/hide).

### 5.4 `GET /chapters/{chapterId}/standards`

- Authorize; return full standards + OEs for **one** chapter (keeps payload bounded vs. whole journey).
- Recompute standard-level `openIssuesCount`, completion, and Phase 48 standard meta from child OEs where appropriate.

### 5.5 `GET /objective-elements/{oeId}`

- Authorize; return full OE graph for wizard and context.
- Include authoritative **changedSinceLastAudit** for surveillance wizard gating.

### 5.6 `PATCH` operational mode

- Authorize; enforce **progression** rules (no skipping Self Assessment → Final Audit, etc.—mirror `operationalModeProgressionBlocked`).
- Reject **403** if read-only; **409** if invalid transition.

### 5.7 `PATCH` OE / evidence

- As in prior spec; respect **readOnly** for Final Audit / frozen journey; Surveillance may allow limited updates per policy.

---

## 6. Validations


| Input                                          | Rule                                                                                         |
| ---------------------------------------------- | -------------------------------------------------------------------------------------------- |
| `journeyId`, `chapterId`, `standardId`, `oeId` | UUID; resources must belong to journey + hospital.                                           |
| `operationalMode`                              | Integer **0–4** (or string enum covering all five).                                          |
| PATCH OE                                       | `score` **1–5** when not N/A; `naJustification` required when `isNa`; max lengths as before. |
| Evidence                                       | Size, MIME, scan policy.                                                                     |


---

## 7. Errors (HTTP)


| Code  | When                                                           |
| ----- | -------------------------------------------------------------- |
| `400` | Bad enum, mismatched `operationalMode` / `auditLifecycleMode`. |
| `401` | Unauthenticated.                                               |
| `403` | Wrong tenant; read-only.                                       |
| `404` | Unknown journey/chapter/standard/OE.                           |
| `409` | OE version conflict; invalid mode transition.                  |
| `413` | Upload too large.                                              |


---

## 8. Field coverage matrix (no control left unbound)

### 8.1 Journey page (`journey.component`)


| UI surface                  | Fields / source                                                                                                                                                                    |
| --------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Framework chip              | `session.frameworkCode`, `session.frameworkName`                                                                                                                                   |
| Mode tiles (4 cards)        | `session.operationalMode`, `session.readOnly`, `session.modeProgression` (or client locks), `dashboard.overview.coreCompliancePercent` on **active** card                          |
| Accreditation progress card | `dashboard.accreditation`, `dashboard.accreditationReadinessPercent`, `dashboard.overview` (fallbacks), titles from **client policy** (`progressCardTitle` / `subtitle` from mode) |
| Action bar — danger         | `dashboard.overview.openActionPlansCount`, `session.readOnly`, policy flags                                                                                                        |
| Metric cards                | `dashboard.overview` (four KPIs)                                                                                                                                                   |
| Chart cards (3×)            | `dashboard.oeScoreTrend`, `chapterImprovementTrend`, `riskEvolutionTrend`                                                                                                          |


**Client-only (no extra API):** Mode card **labels**, **icons**, **lockReason** copy when `modeProgression` absent—policy layer (`buildJourneyOperationalModeUiPolicy`).

---

### 8.2 Execution page (`execution-screen`)


| UI surface                             | Fields / source                                                                                                                                                                                                                      |
| -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Header / framework                     | `session.frameworkCode`, `session.frameworkName`                                                                                                                                                                                     |
| Chapter panel                          | `GET /chapters` → each **§4.3** field; sort/search are query or client-side on list                                                                                                                                                  |
| Standards hero                         | From selected chapter + `GET .../standards` first standard’s chapter context: `complianceScorePercent`, `risk`, `openIssuesCount`, chapter name/code                                                                                 |
| Department / OE quick filters          | **Client** filters over `objectiveElements` (optional future: `GET .../departments` listing distinct `departmentLabel` values)                                                                                                       |
| Standard cards                         | Each **standard** in §4.4 including Phase 48 meta + **objectiveElements** row fields                                                                                                                                                 |
| Context panel                          | `**GET /objective-elements/{oeId}`** or selected OE from standards payload: `code`, `description`, `departmentLabel`, `assigneeDisplayName`, `frequency` → **derive** `frequencyLabel` client-side unless API adds `reviewFrequency` |
| Context — KPI strip (Re-accreditation) | Policy shows strip; optional static copy unless API adds `kpiInsightLabel`                                                                                                                                                           |
| OE wizard                              | **OE detail** + `session` + policy; **changedSinceLastAudit** from OE detail for Surveillance Evaluate                                                                                                                               |


**Wizard policy** (`JourneyOperationalModeUiPolicy.wizard`): driven by **operationalMode** + `readOnly` + `**changedSinceLastAudit`** on the OE—no separate endpoint required beyond §4.4 / §4.5.

---

### 8.3 `JourneyOperationalModePolicyContext` (feeds Phase 48 policy)


| Field                                      | API source                                                            |
| ------------------------------------------ | --------------------------------------------------------------------- |
| `snapshotReadOnly`                         | `session.snapshotReadOnly`                                            |
| `workspaceReadOnly`                        | `session.readOnly` or derived from `operationalMode`                  |
| `coreCompliancePercent`, `failedCoreCount` | `dashboard.overview`                                                  |
| `selectedOeChangedSinceLastAudit`          | `objective-elements/{oeId}.changedSinceLastAudit` (replace mock hash) |


---

### 8.4 Domain models — cross-check


| Model                                                                     | Covered by                                                                                                         |
| ------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `JourneyWorkspaceSnapshot` (aggregate)                                    | `**session`** + `**dashboard`** + `**chapters`** + `**chapters/{id}/standards**` + `**objective-elements/{oeId}**` |
| `JourneyChapter`                                                          | §4.3 + full chapter context in §4.4                                                                                |
| `ComplianceStandard` + Phase 48 extras                                    | §4.4                                                                                                               |
| `ObjectiveElement` + `OeActionPlan` / `OeEvidenceItem` / `OeHistoryEvent` | §4.4–§4.5                                                                                                          |
| `JourneyCatalogMeta`                                                      | `session.catalog`                                                                                                  |
| `AccreditationEligibility`                                                | `dashboard.accreditation`                                                                                          |
| `JourneyOverviewMetrics`                                                  | `dashboard.overview`                                                                                               |
| `JourneyTrendPoint` (×3)                                                  | `dashboard` trend arrays                                                                                           |


---

## 9. Non-goals

- **Shell sidebar** operational mode (`ShellOperationalModeService`) — session storage UX; not the journey `**operationalMode`** API.
- **Real-time** multi-user — optional WebSocket later.

---

## 10. Revision history


| Version | Date       | Notes                                                                                                                                                                                                         |
| ------- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1.0     | 2026-04-11 | Initial monolithic workspace spec.                                                                                                                                                                            |
| 2.0     | 2026-04-11 | **Phase 48**: Operational mode 0–4; split GETs (`session`, `dashboard`, `chapters`, `chapters/{id}/standards`, `objective-elements/{oeId}`); Phase 48 chapter/standard/OE fields; full field coverage matrix. |


