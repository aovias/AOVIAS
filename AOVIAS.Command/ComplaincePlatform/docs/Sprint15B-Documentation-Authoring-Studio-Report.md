# Sprint 15B — Quality Documentation Authoring Studio Report

**Date:** June 25, 2026  
**Status:** Implemented  
**Builds on:** Sprint 15A QDMS (reuses aggregates — no duplicate document storage)

---

## 1. Objective

Transform the Documentation workspace from a controlled register into a **healthcare document authoring platform**. Hospitals can create, edit, review, and maintain SOPs, policies, manuals, and forms inside PRAMAN without Microsoft Word for normal controlled documentation work.

Design philosophy: **structured enterprise authoring** (Notion × Confluence × Git versioning × healthcare compliance) — not a Word clone or TinyMCE blob editor.

---

## 2. Architecture

### 2.1 Layering

| Layer | Role |
|-------|------|
| **ControlledDocument / DocumentVersion** (15A) | Aggregate roots — unchanged SSOT |
| **DocumentSection** (15B) | First-class structured sections with JSON blocks |
| **DocumentAuthoringComment** | Review-mode inline comments |
| **DocumentAuthoringCheckpoint** | Autosave / revision recovery snapshots |
| **DocumentCrossReference** | Links to policies, evidence, OEs, findings, CAPAs |
| **AuthoringSchemaRegistry** | Dynamic section library per document type |
| **DocumentationAuthoringService** | Authoring, AI draft, compare, export |
| **Compliance Copilot** | Advisory draft engine — never publish/approve |

### 2.2 Key Paths

```
ComplianceService/Domain/Documentation/
  DocumentSection.cs
  DocumentAuthoringComment.cs
  DocumentAuthoringCheckpoint.cs
  DocumentCrossReference.cs

ComplianceService/Application/Documentation/
  AuthoringSchemaRegistry.cs

ComplianceService/Application/Services/
  DocumentationAuthoringService.cs

ComplianceService/Application/DTOs/
  DocumentationAuthoringDtos.cs

ComplianceService/Controllers/V1/DocumentationController.cs  (+ authoring endpoints)
Gateway/Controllers/DocumentationV1ProxyController.cs

WebApp/src/app/features/documentation/
  models/authoring.models.ts
  services/authoring-api.service.ts
  pages/authoring-studio-home-page.component.ts
  pages/authoring-studio-page.component.ts
```

---

## 3. Editor Model

### 3.1 Layout (full-screen studio)

```
┌─────────────┬──────────────────────────┬─────────────────┐
│   Outline   │   Block Editor           │  Right Panel    │
│  (sections) │   (structured blocks)    │  AI · Mapping   │
│             │                          │  Props · Review │
│  resizable  │                          │  Health · Diff  │
└─────────────┴──────────────────────────┴─────────────────┘
```

- Resizable left/right panels
- Dark mode toggle
- Autosave every 2s (debounced PATCH)
- Manual checkpoint save

### 3.2 Section Model

Sections are **not** stored as one HTML blob. Each `DocumentSection` has:

| Field | Purpose |
|-------|---------|
| SectionKey | Stable id (`purpose`, `procedure`, …) |
| Title | Display name |
| SortOrder | Outline ordering |
| IsRequired | Completeness / health scoring |
| BlocksJson | Array of typed blocks |

### 3.3 Block Types

**Basic:** heading, paragraph, bulletList, numberedList, checklist, table, callout, warning, image, reference, crossRef, collapsible, pageBreak, code

**Healthcare:** riskWarning, nabhNote, legalRequirement, patientSafetyAlert, committeeDecision, bestPractice, clinicalTip

---

## 4. Dynamic Section Library

| Schema Key | Sections |
|------------|----------|
| `sop` | General Info, Purpose, Scope, Definitions, Responsibilities, Procedure, References, Records, Attachments, Revision History, Approvals |
| `policy` | Policy Statement, Applicability, Compliance Requirements, … |
| `form` | Header, Fields, Instructions, Sign-off |
| `committee-minutes` | Meeting Details, Agenda, Discussion, Decisions, Action Items |
| `manual` | Overview, Organization, Procedures, References |

Resolved from `AuthoringSchemaRegistry` or template `AuthoringSchemaKey`.

---

## 5. AI Integration

Endpoint: `POST /documentation/draft-ai`

Actions (advisory only):

- Draft Purpose / Scope
- Rewrite / improve readability
- Expand Procedure
- Suggest Responsibilities
- Generate Checklist
- Suggest References
- Summarize document

**Copilot NEVER publishes or approves.** Response includes `isAdvisoryOnly: true`.

---

## 6. Live Compliance Mapping

While authoring, keyword analysis suggests NABH mappings (e.g. Hand Hygiene → COP.13.d). User can **Accept**, apply **manual override**, or ignore via `PATCH /documentation/authoring/{id}`.

---

## 7. Document Intelligence

`DocumentHealthScoreDto` computed server-side:

- Completeness % (required sections filled)
- Readability score (word-count heuristic)
- Approval readiness %
- Missing sections list
- Mapped OE count
- Unresolved comments
- Linked evidence / CAPA / finding counts (via cross-references)

---

## 8. Version Compare

`POST /documentation/compare` — structured section-level diff:

- Added / Removed / Modified sections
- Block-level before/after text
- AI summary: "What changed?"

No raw HTML comparison.

---

## 9. Review Mode

`POST /documentation/authoring/comments` — inline comments with types: Inline, Suggestion, Approval, Discussion. Mention support via `mentionedUserName`. Architecture ready for future real-time collaboration.

---

## 10. Autosave & Checkpoints

- Debounced autosave via `PATCH /documentation/authoring/{id}`
- `DocumentAuthoringCheckpoint` stores full section snapshot JSON
- Manual "Save" creates labeled checkpoint
- Activity audited via `DocumentLifecycleAuditEntry`

---

## 11. Export

`POST /documentation/export`

| Format | Implementation |
|--------|----------------|
| PDF | QuestPDF (reuses platform library) |
| DOCX | Word-compatible HTML (.doc) |
| HTML | Read-only web |
| Markdown | Plain-text composite |

---

## 12. REST APIs (Sprint 15B)

| Method | Endpoint |
|--------|----------|
| POST | `/documentation/authoring` |
| GET | `/documentation/authoring/{id}` |
| PATCH | `/documentation/authoring/{id}` |
| POST | `/documentation/compare` |
| POST | `/documentation/draft-ai` |
| POST | `/documentation/authoring/comments` |
| POST | `/documentation/authoring/cross-references` |
| POST | `/documentation/export` |

---

## 13. Business Rules

| Rule | Enforcement |
|------|-------------|
| Sections are first-class | `DocumentSection` entity |
| Every revision immutable | Still uses 15A version model |
| AI drafts only | Advisory responses; no publish API in draft-ai |
| Humans approve | Existing 15A approval workflow unchanged |
| No duplicate storage | Extends QDMS aggregates only |
| Every edit audited | Lifecycle audit on save |

---

## 14. Future Collaboration Roadmap

Architecture supports later addition of:

- Real-time co-editing (OT/CRDT on BlocksJson)
- WebSocket presence
- Suggestion accept/reject workflow
- Digital signatures on publish
- LMS / training attestation linkage

---

## 15. Migration

EF migration: `Sprint15B_AuthoringStudio`

New tables: `DocumentSections`, `DocumentAuthoringComments`, `DocumentAuthoringCheckpoints`, `DocumentCrossReferences`

Extended: `DocumentVersions.AuthoringSchemaKey`, `DocumentTemplates.SectionsSchemaJson`, `AuthoringSchemaKey`

---

## 16. Success Criteria

All Sprint 15B success criteria met: Authoring Studio nav, structured sections, block editor, AI assistant, live mapping, version compare, review comments, templates, cross-reference API, healthcare blocks, autosave/checkpoints, PDF/DOCX export, QDMS reuse, no Word dependency for normal SOP creation.
