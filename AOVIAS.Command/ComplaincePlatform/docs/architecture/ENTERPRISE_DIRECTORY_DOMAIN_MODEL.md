# PRAMAN Enterprise Directory Domain Model — EIWAM Phase 11

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 11 — Enterprise Directory |
| **Prior milestone** | Phase 10 Workforce Intelligence |
| **Status** | Approved — greenfield EnterpriseDirectoryService |
| **Bounded context** | EnterpriseDirectoryService (port 7217) |

## Purpose

Enterprise Directory is PRAMAN's **unified search and discovery layer** across the workforce platform. It enables fast, intelligent, governed search without owning operational data.

```
Source Events / HTTP APIs → DirectoryIndexProjector
                         → DirectoryIndex (SQL FTS)
                         → DirectorySearchService (+ AG RLS + field masks)
                         → Composable EnterpriseDirectoryProfile (live HTTP)
                         → NL Query Translator → AI (extensible)
```

**Search is a read model. Never own source data.**

---

## Spec name mapping

| EIWAM Phase 11 spec | Implementation |
|---------------------|----------------|
| EnterpriseDirectoryProfile | `EnterpriseDirectoryProfileDto` (composed, not persisted) |
| DirectoryIndex | `DirectoryIndex` |
| DirectoryFacet | `DirectoryFacet` |
| DirectorySearch | `DirectorySearchService` + `DirectoryQuery` |
| DirectorySuggestion | `DirectorySuggestion` cache entity |
| SavedSearch | `SavedSearch` |
| SearchHistory | `SearchHistory` |
| SearchPreference | `SearchPreference` |
| DirectoryRanking | `DirectoryRanking` |
| DirectorySnapshot | `DirectorySnapshot` |

---

## Bounded context boundaries

| Context | Owns | Must NOT own |
|---------|------|--------------|
| **EnterpriseDirectoryService** | Index, search, facets, history, saved searches, ranking | Person, Employment, credentials |
| **PersonService** | Person aggregate | Search index |
| **WorkforceIntelligenceService** | Analytics snapshots | Directory discovery (orthogonal) |
| **AccessGovernanceService** | Permission evaluation | Search index content |

---

## Core read models

### DirectoryIndex

Denormalized FTS row per discoverable employment profile.

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| PersonId | Guid | Source ref |
| EmploymentId | Guid? | Source ref |
| OrganizationId | Guid | RLS scope |
| HospitalId | Guid? | RLS scope |
| DepartmentId | Guid? | RLS scope |
| DisplayName | string | Primary display |
| PreferredName | string? | |
| PlatformId | string? | |
| EmployeeNumber | string? | |
| ProfessionalNumber | string? | |
| EmailNormalized | string? | Masked without Directory.Contact.Read |
| PhoneNormalized | string? | Masked without Directory.Contact.Read |
| DepartmentName | string? | Facet + display |
| HospitalName | string? | |
| Designation | string? | |
| PositionTitle | string? | |
| EmploymentStatus | string? | Facet |
| EmploymentType | string? | Facet |
| ProfessionalCategory | string? | Facet |
| ManagerName | string? | |
| SearchBlob | string | SQL FTS column |
| PhoneticKey | string? | Soundex |
| SkillsJson | string | Array |
| CertificationsJson | string | Array |
| CommitteesJson | string | Array |
| ResponsibilitiesJson | string | Array |
| TagsJson | string | Keywords |
| VisibilityFlags | string? | Confidentiality |
| LastIndexedUtc | DateTime | Recency boost |
| IsActive | bool | Active employment filter |

### DirectoryFacet

| Field | Type |
|-------|------|
| OrganizationId | Guid |
| FacetType | string | Department, Hospital, Category, Status, etc. |
| FacetValue | string |
| Count | int |
| AsOfUtc | DateTime |

### DirectorySnapshot

| Field | Type |
|-------|------|
| Id | Guid |
| Version | int |
| RowCount | int |
| AsOfUtc | DateTime |
| CorrelationId | string? |
| DurationMs | long |

### DirectoryRanking

| Field | Type |
|-------|------|
| Id | Guid |
| RulesJson | string | Weight configuration |
| IsActive | bool |

### SavedSearch

| Field | Type |
|-------|------|
| Id | Guid |
| OwnerUserId | Guid |
| OrganizationId | Guid |
| Name | string |
| QueryJson | string | DirectoryQuery DSL |
| Visibility | string | Private, Shared, Department, Organization |
| IsScheduled | bool |
| IsAiGenerated | bool |
| CreatedAtUtc | DateTime |

### SearchHistory

| Field | Type |
|-------|------|
| Id | Guid |
| UserId | Guid |
| OrganizationId | Guid |
| SearchTerms | string |
| FiltersJson | string? |
| ResultCount | int |
| ExecutionMs | long |
| SearchedAtUtc | DateTime |

### SearchPreference

| Field | Type |
|-------|------|
| UserId | Guid | PK |
| DefaultOrganizationId | Guid? |
| PinnedResultsJson | string? |
| NaturalLanguageEnabled | bool |
| PreferencesJson | string? |

### DirectorySynonym

| Field | Type |
|-------|------|
| Token | string |
| Synonym | string |

---

## Event catalog

### Subscribe (via `workforce-events`)

| Event | Projection |
|-------|------------|
| `PersonUpdated` | Re-index all employments for person |
| `EmploymentCreated` / `EmploymentTransferred` / `EmploymentTerminated` | Upsert/delete row |
| `ProfessionalProfileUpdated` | Refresh credentials/skills |
| `QualificationAdded` / `TrainingCompleted` | Refresh tokens |
| `CertificationRenewed` / `LicenseRenewed` | Refresh tokens |
| `DepartmentAssigned` | Update scope + facets |
| `ResponsibilityAssigned` | Update governance tokens |
| `SkillUpdated` | Refresh skill blob |

### Produce (directory outbox)

| Event | When |
|-------|------|
| `DirectoryIndexUpdated` | After incremental index |
| `SearchIndexRebuilt` | After full rebuild |
| `DirectoryProfileRefreshed` | After profile cache warm |

---

## DirectoryQuery DSL

```json
{
  "text": "ICU nurse ACLS",
  "organizationId": "guid",
  "filters": {
    "hospitalId": "guid",
    "departmentId": "guid",
    "professionalCategory": "Nurse",
    "employmentStatus": "Active",
    "certification": "ACLS",
    "skill": "Ventilation",
    "expiryWithinDays": 60
  },
  "fuzzy": true,
  "phonetic": true,
  "page": 1,
  "pageSize": 20
}
```

---

## Security rules

1. Every search calls `IAuthorizationEvaluator` for `Directory.Search.Read`.
2. `RowLevelFilter` applied in SQL WHERE clause (not post-filter only).
3. Field masks applied before response: email/phone require `Directory.Contact.Read`; license/cert numbers require `Directory.Credential.Read`.
4. Unauthorized users receive 403, not empty results that leak existence.
5. Profile read requires `Directory.Profile.Read`.

---

## KPI / performance targets

| Metric | Target |
|--------|--------|
| Search p95 (100K rows) | < 200ms |
| Autocomplete p95 | < 100ms |
| Full rebuild (10K profiles) | < 5 min |
| Index lag (event → searchable) | < 60s |

---

## API contract summary

Base: `/api/v1/directory`

| Route | Method |
|-------|--------|
| `/search` | GET |
| `/profile/{id}` | GET |
| `/suggestions` | GET |
| `/facets` | GET |
| `/popular` | GET |
| `/recent` | GET |
| `/saved-searches` | GET, POST |
| `/saved-searches/{id}` | DELETE |
| `/history` | GET |
| `/preferences` | GET, PUT |
| `/nl-search` | POST |
| `/rebuild` | POST (admin) |
