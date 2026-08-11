# PRAMAN – Evidence System Discovery & Extension Analysis

**Generated:** 2026-04-16  

This document summarizes a codebase-wide review of **Evidence / Document / File handling**, cross-service data flow, storage mechanisms, Audit Service readiness, gaps, and a **non-breaking** extension strategy toward scalable, audit-friendly evidence storage.

---

## 1. Evidence Object Catalog

### 1.1 Domain & persistence (ComplianceService)

| Object | Role | Notable properties / enums |
|--------|------|------------------------------|
| `Evidence` | Core evidence row (task-scoped, file metadata) | `FileUrl`, `Hash`, `Metadata` (JSON string), `EvidenceType`, `Title`, `UploadedBy`, `UploadedOn`, `ExpiresAt`, `CurrentVersion`, nested `EvidenceArtifactType` (Document / Process / Outcome), `EvidenceLifecycleStatus` (Draft / Submitted / Verified / Rejected), `Source`, `Department`, `Tags` |
| `EvidenceVersion` | Version history rows | `VersionNumber`, `FileUrl`, `Hash`, `CreatedBy`, `CreatedAt`, `Notes` |
| `ObjectiveElementEvidence` | OE ↔ Evidence link + per-link review | `VerificationStatus` (Pending / Verified / Rejected), `VerifiedBy`, `VerifiedAt`, `Remarks` |

### 1.2 Application / infrastructure

| Object | Role |
|--------|------|
| `IEvidenceService` / `EvidenceService` | Create evidence (+ seed v1 in `EvidenceVersions`), map to OE, list by OE, set verification on link, remove link / delete orphan evidence |
| `IEvidenceRepository` / `EvidenceRepository` | EF repository for `Evidence` |
| `JourneyWorkspaceAppService` | Journey/OE flows: register evidence (JSON `FileUrl` or multipart upload), list OE evidence DTOs, delete OE evidence, locking when journey ≥ Submitted |

### 1.3 DTOs / requests

| Object | Role |
|--------|------|
| `RegisterOeEvidenceRequest` | `EvidenceType`, `FileUrl`, optional `Title`, `Metadata` |
| `OeEvidenceItemDto` | List item: id, name, verification status, upload/expiry, uploader (**no `FileUrl` in list**) |
| `CreateEvidenceRequest` (V1 task API) | `EvidenceType`, `FileUrl`, `Metadata` |

### 1.4 Controllers / APIs

| Location | Endpoints (summary) |
|----------|---------------------|
| `ComplianceService/Controllers/V1/EvidenceController.cs` | `POST .../compliance/tasks/{taskId}/evidence` (JSON; uses repository + UoW, **not** `EvidenceService`) |
| `ComplianceService/Controllers/V1_1/EvidenceController.cs` | `GET .../evidence/objective-elements/{id}` (query filters); `POST .../verify/{evidenceId}` |
| `ComplianceService/Controllers/V1/JourneysController.cs` | `GET` / `POST` / `DELETE` `.../journeys/{journeyId}/objective-elements/{oeId}/evidence` (JSON + **multipart** upload) |

### 1.5 Gateway (orchestration only)

| Object | Role |
|--------|------|
| `GetObjectiveElementEvidenceQuery` + handler | Proxies GET to ComplianceService |
| `PostObjectiveElementEvidenceCommand` + handler | Proxies JSON POST |
| `PostObjectiveElementEvidenceMultipartCommand` + handler | Streams **raw multipart** body to Compliance (`ProxyToComplianceServiceRawAsync`) |
| `DeleteObjectiveElementEvidenceCommand` + handler | Proxies DELETE |

### 1.6 Other services

| Object | Role |
|--------|------|
| `AIOrchestratorService` — `EvidenceContext` | Snapshot DTO for drafting/LLM: `FileUrl`, `EvidenceType`, `UploadedOn`, `Metadata` (not storage) |
| `KnowledgeBase` — `KnowledgeItem` | Text field `EvidenceRequired` (guidance text, **not** binary evidence) |
| **AuditService** | `AuditSession`, `AuditFinding`, `AuditReport`, `CorrectiveAction` — **no file / attachment / evidence entity** |

### 1.7 Keyword sweep (high signal)

- **Evidence / File / Upload:** Concentrated in **ComplianceService** + **Gateway** proxies; AIOrchestrator context types; KnowledgeBase **wording** only.
- **Blob / Azure / S3 / MinIO:** No cloud object SDK usage found in `.cs` sources at time of analysis.
- **Attachment / Media / Asset:** No dedicated attachment entities found beyond the evidence linkage described above.

---

## 2. Current Data Flow (Textual)

### 2.1 Objective Element evidence (primary UI path)

```
Gateway/JourneysController
  → MediatR (PostObjectiveElementEvidence* / GetObjectiveElementEvidence*)
  → IProxyService
  → ComplianceService/JourneysController
  → IJourneyWorkspaceService (JourneyWorkspaceAppService)
  → IEvidenceService + ComplianceDbContext
  → SQL Server (Evidences, EvidenceVersions, ObjectiveElementEvidences)
```

- **Multipart:** Request body is forwarded as a stream; Compliance writes bytes to disk, then persists DB rows.

### 2.2 Task-scoped evidence (alternate path)

```
ComplianceService/V1/EvidenceController
  → IEvidenceRepository + IUnitOfWork
  → SQL (Evidences)
```

- Uses **`EvidenceRepository.AddAsync`** directly — **does not** call `EvidenceService.CreateEvidenceAsync`, so **no automatic `EvidenceVersions` row** on this path (behavioral inconsistency vs journey upload).

### 2.3 Auditor verify / reject (OE link)

```
Client → ComplianceService/V1_1/EvidenceController
  → IEvidenceService.SetVerificationAsync
  → Updates ObjectiveElementEvidence (not AuditService)
```

### 2.4 Knowledge / execution

- **KnowledgeBase:** Content and scoring metadata; no file pipeline tied to `Evidence` rows.
- **No `Execution*` service project** was found in the solution layout at time of analysis.

---

## 3. Storage Analysis

| Aspect | Finding |
|--------|---------|
| **Where bytes live** | Local filesystem under the API content root: `App_Data/evidence_uploads/{journeyId}/{objectiveElementId}/{evidenceId}/...` |
| **DB reference** | `Evidence.FileUrl` set to a **`local://...`** URI built from a relative path (`JourneyWorkspaceAppService.RegisterObjectiveElementEvidenceFromUploadAsync`) |
| **Abstraction** | **No** `IFileStorageService` (or equivalent) in repo |
| **Cloud** | **None** detected |
| **Hardcoded paths** | Relative segment `App_Data/evidence_uploads` is fixed in code (portable but not configurable via options) |
| **Reusability** | **Low** for multi-instance / cloud: path is local and `local://` is not a standard fetch URL for external clients |

**Relevant implementation (excerpt):**

```csharp
// JourneyWorkspaceAppService.RegisterObjectiveElementEvidenceFromUploadAsync
var safeRelative = Path.Combine(
    "App_Data",
    "evidence_uploads",
    journeyId.ToString("N"),
    objectiveElementId.ToString("N"),
    evidenceId.ToString("N"));
// ...
var fileUrl = $"local://{relativeUrl}";
```

---

## 4. Audit Service Readiness

### 4.1 Present today

- Sessions, findings, reports, corrective actions; `Explainability` on session/finding as serialized DTO.
- **`ControlCode`** on findings (string) — could align with requirement codes in principle, but **not** wired to Compliance `ObjectiveElement` / evidence.

### 4.2 Missing for audit-driven evidence

- No **attachment**, **blob id**, **file URL**, or **evidence** entity.
- No **review workflow** for files at the Audit aggregate (Compliance keeps OE-level verification on `ObjectiveElementEvidence` instead).
- No cross-link table from Audit findings to Compliance evidence IDs.

### 4.3 What can be reused

- **Patterns:** EF + migrations, `BaseEntity` / `PlatformEntity`, repository + app service style — good templates for new `AuditEvidence` tables.
- **Not reusable as-is** for binary evidence: current Audit schema has no place for it.

---

## 5. Gap Analysis

### 5.1 Missing

| Gap | Detail |
|-----|--------|
| Centralized blob storage | Only local disk + `local://` URI |
| Storage abstraction | Direct `FileStream` / `Path` in application service |
| Integrity | `Hash` often **empty**; no enforced hashing on upload |
| Versioning usage | `EvidenceVersion` exists and v1 is seeded from `EvidenceService.CreateEvidenceAsync`, but **additional versions** are not evident in upload APIs |
| Secure download | No dedicated “get file” API surfaced in the traced controllers (list returns metadata without exposing `FileUrl` in `OeEvidenceItemDto`) |
| Deletion vs files | `RemoveEvidenceFromObjectiveElementAsync` removes DB rows; **no traced cleanup** of files under `App_Data` |
| Single write path | Task `EvidenceController` bypasses `EvidenceService` → **skips `EvidenceVersions` seed** |

### 5.2 Risks

- **Multi-node / scale-out:** Local paths are not shared; risk of broken or inconsistent access without shared storage.
- **Compliance vs Audit duplication** if both grow evidence features without a single **correlation id** or ownership rule.
- **Audit trail:** DB records who uploaded / verified on Compliance side; **AuditService** does not yet store evidence events or file references.

---

## 6. Extension Plan (Non-Breaking)

Constraints honored: **no full module rewrites**, **backward compatibility**, **reuse existing models**, **Clean Architecture**, **microservice-friendly**.

1. **Introduce `IFileStorageService` (ComplianceService.Application)**  
   - Methods: `SaveAsync(stream, metadata)`, `DeleteAsync(uriOrKey)`, optional `OpenReadAsync`.  
   - Implementations: `LocalFileEvidenceStorage` (current behavior, behind interface), later `AzureBlobEvidenceStorage`.  
   - **Non-breaking:** Keep storing the result in existing `Evidence.FileUrl` (evolve from `local://` to `https://...` or `azure://container/blob` as an opaque string).

2. **Refactor only the upload block** in `RegisterObjectiveElementEvidenceFromUploadAsync` to call the interface; **same API contract**, same DB shape.

3. **Add configuration** (`EvidenceStorage:Provider`, connection strings) — default `Local` to preserve behavior.

4. **Align task-based `EvidenceController`** with `EvidenceService.CreateEvidenceAsync` (or equivalent) so **one** path maintains `EvidenceVersions`. Small internal fix; public contract unchanged.

5. **AuditService: add `AuditEvidence` (new table)**  
   - Fields (illustrative): `Id`, `AuditSessionId` (or `FindingId`), optional `ComplianceEvidenceId`, `StorageUri`, `ContentType`, `FileName`, `UploadedAt`, `UploadedBy`, `Status` (review).  
   - **Non-breaking:** new tables only; new endpoints when ready.

6. **Integration**  
   - **Option A:** Gateway calls Compliance upload, then Audit API **registers** a reference (HTTP + correlation id).  
   - **Option B:** Outbox event from Compliance (`EvidenceRegistered`) consumed by a worker that creates `AuditEvidence` — aligns with platform Outbox rules.

7. **Migration**  
   - Background job: copy `local://` files to blob, update `FileUrl` in place; retain old paths until verified.

---

## 7. Target Architecture Proposal

### 7.1 Entities

- **ComplianceService (existing, extended)**  
  - `Evidence`, `EvidenceVersion`, `ObjectiveElementEvidence` — keep; optionally add **storage key** column later if URI vs key are split (nullable = backward compatible).

- **AuditService (new)**  
  - `AuditEvidence`: session/finding scope, storage reference, review state, optional `ComplianceEvidenceId`.

- **EvidenceMapping**  
  - OE mapping already exists as **`ObjectiveElementEvidence`**. For Audit, add **`AuditEvidence`** or a link table `AuditEvidenceLink(AuditFindingId, ComplianceEvidenceId)` if many-to-many is required.

### 7.2 Interfaces

- `IFileStorageService` — introduced first in Compliance; Audit can use the same **concept** in its application layer (duplicate interface per service if strict assembly boundaries are required).

### 7.3 Services

- **Compliance:** Evidence registration + OE mapping + verification remains in `IEvidenceService` / journey workspace.  
- **Audit:** `AuditEvidenceAppService` for register/list/review, either calling `IFileStorageService` for Audit-owned uploads or storing references to Compliance-created evidence only.

### 7.4 Storage

- **Dev:** Local provider (current layout or configurable temp path).  
- **Prod:** Azure Blob implementation of `IFileStorageService`; SAS or gateway-mediated download for **secure access**.

---

## 8. Validation Checklist

| Requirement | Current + proposed extension |
|-------------|------------------------------|
| Upload evidence | Yes (multipart + JSON `FileUrl`); strengthen with storage abstraction + hashing |
| Map to OE (e.g. AAC.1.c) | Yes via `ObjectiveElementEvidence` + `ObjectiveElementId` |
| Versioning | Schema supports (`EvidenceVersion` + `CurrentVersion`); explicit “new version” API may be added later |
| Audit review | Partially: `ObjectiveElementEvidence` verification; AuditService can add audit-side review via `AuditEvidence` |
| Secure access | **Gap:** add download/token API + blob SAS or BFF streaming |

---

## 9. Summary

| Area | Conclusion |
|------|------------|
| **Ownership** | Evidence is **owned by ComplianceService** (relational model + local disk uploads), **proxied by Gateway**. |
| **Object storage** | **No** Azure/S3 SDK usage found; **no** shared storage abstraction. |
| **AuditService** | **No** evidence/attachment entities today; session/findings/report/CAR only. |
| **Non-breaking path** | Keep `Evidence` / `ObjectiveElementEvidence`, extract **`IFileStorageService`**, align task evidence creation with **`EvidenceService`**, add **`AuditEvidence`** in AuditService with HTTP/Outbox integration. |

---

*This document reflects repository state at analysis time; re-run discovery after major refactors.*
