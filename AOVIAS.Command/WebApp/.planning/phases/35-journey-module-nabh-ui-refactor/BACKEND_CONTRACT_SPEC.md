# Backend Contract Specification — Journey NABH Workspace UI

This document describes the **APIs, DTOs, and business rules** the backend should implement to support the **Framework → Chapter → Standard → Objective Element (OE)** journey UI delivered in Phase 35. The WebApp currently uses **mock data** (`JourneyDataService` + fixture); replace with HTTP when contracts are available.

---

## 1. Resource model (domain)


| Entity                     | Description                                                               |
| -------------------------- | ------------------------------------------------------------------------- |
| **Journey**                | A compliance journey instance for a hospital + framework (`journeyId`).   |
| **Chapter**                | Top-level grouping (e.g. ACC, COP).                                       |
| **Standard**               | Mid-level (e.g. AAC.1, COP.5); contains many OEs.                         |
| **Objective Element (OE)** | Leaf unit; scored 1–5, classified, evidence-linked, audit state, NA flag. |


---

## 2. Proposed REST endpoints


| Method  | Path                                                              | Purpose                                                                                                 |
| ------- | ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `GET`   | `/api/v1/journeys/{journeyId}/workspace`                          | Full workspace snapshot (chapters → standards → OEs + accreditation metrics + overview + trend series). |
| `GET`   | `/api/v1/journeys/{journeyId}/chapters/{chapterId}/standards`     | Optional pagination/lazy load for large tenants.                                                        |
| `PATCH` | `/api/v1/journeys/{journeyId}/objective-elements/{oeId}`          | Update OE fields (e.g. `isNa`, `naJustification`, scores — subject to lifecycle permissions).           |
| `POST`  | `/api/v1/journeys/{journeyId}/objective-elements/{oeId}/evidence` | Upload / register evidence (multipart or metadata + storage URL).                                       |
| `GET`   | `/api/v1/journeys/{journeyId}/objective-elements/{oeId}/evidence` | List evidence rows for an OE.                                                                           |
| `GET`   | `/api/v1/journeys/{journeyId}/objective-elements/{oeId}/history`  | Timeline events (audit, evidence, tasks, action plans).                                                 |


> Exact prefix (`/api/v1/compliance/...`) should align with the API gateway; paths are indicative.

---

## 3. DTOs (aligned with TypeScript models)

### 3.1 `JourneyWorkspaceSnapshot` (response body)

- `journeyId`: `string` (UUID)
- `frameworkCode`, `frameworkName`: `string`
- `chapters`: `JourneyChapterDto[]`
- `accreditation`: `AccreditationEligibilityDto`
- `overview`: `JourneyOverviewMetricsDto`
- `oeScoreTrend`, `chapterImprovementTrend`, `riskEvolutionTrend`: `TrendPointDto[]` (`label`, `value`)

### 3.2 `JourneyChapterDto`

- `id`, `code`, `name`: `string`
- `complianceScorePercent`: `number` (0–100)
- `risk`: `low` | `medium` | `high`
- `coreComplianceStatus`: `met` | `at_risk` | `failed`
- `openIssuesCount`: `number`
- `pendingAudit`: `boolean`
- `coreFailureFlag`: `boolean`
- `standards`: `ComplianceStandardDto[]`

### 3.3 `ComplianceStandardDto`

- `id`, `code`, `description`: `string`
- `completionPercent`: `number` (0–100)
- `riskBadge`: `low` | `medium` | `high`
- `coreSummary?`: `string`
- `objectiveElements`: `ObjectiveElementDto[]`

### 3.4 `ObjectiveElementDto`

- `id`, `code`, `description`: `string`
- `classification`: `core` | `commitment` | `achievement` | `excellence`
- `score`: `1`–`5`
- `scoreLabel`: enum matching UI (`Poor` … `Full Compliance`)
- `scoreRationale?`, `auditRemarks?`: `string`
- `evidenceStatus`: `complete` | `missing` | `pending_verification`
- `isNa`: `boolean`
- `naJustification?`: `string` (**required when `isNa` is true** for persistence)
- `auditStatus`: `compliant` | `non_compliant` | `not_applicable`
- `departmentLabel?`, `assigneeDisplayName?`: `string`
- `lastUpdatedAt?`, `dueAt?`, `nextReviewAt?`: ISO-8601 `string` or `null`
- `actionPlans`: `OeActionPlanDto[]`
- `evidence`: `OeEvidenceItemDto[]`
- `history`: `OeHistoryEventDto[]`

### 3.5 Supporting DTOs

- `AccreditationEligibilityDto`: numeric fields + `violations: string[]`
- `JourneyOverviewMetricsDto`: `coreCompliancePercent`, `failedCoreCount`, `oesAtOrBelowThreeCount`, `openActionPlansCount`
- `OeActionPlanDto`: `id`, `title`, `ownerName`, `deadline`, `status` (`open` | `in_progress` | `closed`)
- `OeEvidenceItemDto`: `id`, `name`, `verificationStatus`, `uploadedAt`, `expiryAt`, `uploadedBy`
- `OeHistoryEventDto`: `id`, `type`, `title`, `description`, `timestamp`, `actor`

---

## 4. Business logic (server-side)

1. **NABH scoring**
  - Persist authoritative **1–5** score and derived **label**; UI may display server-computed label only.
2. **NA (not applicable)**
  - If `isNa === true`, `**naJustification` is mandatory** before save; reject `400` with field errors if missing.
3. **Lifecycle modes** (Self / Internal / Final / Surveillance)
  - Server should expose **current mode** and **effective permissions** (read-only vs editable) per role + mode.  
  - UI filters OEs in Final/Surveillance (Core+Commitment / Core-only); server should **enforce** the same on mutations.
4. **Action plans**
  - OEs with **score ≤ 3** (and not NA) should surface **open action plans**; counts roll into `overview.openActionPlansCount`.
5. **Accreditation panel**
  - Thresholds (e.g. overall ≥ 80%, core OE avg ≥ 4) should be **computed server-side** for auditability; UI mirrors `accreditation` DTO.
6. **Evidence**
  - Verification status, expiry, and linkage to OE IDs; soft-delete / version as per policy.
7. **Audit trail / history**
  - Append-only events for audits, evidence changes, tasks, action-plan state transitions.

---

## 5. Non-goals (this phase)

- No database schema migration in WebApp repo.  
- No breaking changes to existing **organization-detail** or **dashboard snapshot** APIs unless explicitly coordinated.

---

## 6. WebApp integration points


| File                          | Responsibility                                           |
| ----------------------------- | -------------------------------------------------------- |
| `journey-data.service.ts`     | Swap `of(mock)` → `this.get('/compliance/journeys/...')` |
| `journey-workspace.mapper.ts` | Map API envelope → `JourneyWorkspaceSnapshot`            |
| `journey-workspace.store.ts`  | Optimistic PATCH for NA / future fields                  |


---

*Generated as part of Phase 35 execution.*