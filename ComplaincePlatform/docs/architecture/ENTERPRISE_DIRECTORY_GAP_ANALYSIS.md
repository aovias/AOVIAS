# PRAMAN Enterprise Directory Gap Analysis — EIWAM Phase 11

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 11 — Enterprise Directory |
| **Prior work** | Phases 0–10 EIWAM (including WorkforceIntelligenceService) |
| **Status** | Investigation complete — implementation authorized |
| **Bounded context** | EnterpriseDirectoryService (new greenfield) |

## Executive summary

PRAMAN has **siloed per-service text search** (Person, Employment, Professional) and a **Phase 10 analytics search index** (`WorkforceSearchIndex`) but no **unified, governed enterprise discovery layer**. Phase 11 introduces `EnterpriseDirectoryService` as the single search and discovery bounded context with SQL Full-Text indexing, Access Governance RLS/field masking, composable profiles, and AI-ready natural language queries.

**Do not extend WorkforceIntelligenceService for directory. Build a separate discovery service.**

---

## Investigation scope

| Area | Locations reviewed |
|------|-------------------|
| Person search | `PersonService/Application/Services/PersonAppService.cs` — `SearchAsync` |
| Employment search | `WorkforceService/Application/Services/EmploymentAppService.cs` |
| Professional search | `ProfessionalService/Application/Services/ProfessionalProfileAppService.cs` |
| Organization | `OrganizationService` — membership list, no text search |
| Governance | `GovernanceService` — timeline filter only |
| Workforce Intelligence | `WorkforceIntelligenceService` — `WorkforceSearchIndex`, `/intelligence/search` |
| Access Governance | `AuthorizationEvaluatorService`, `FieldMask` column, `RowLevelFilterDto` |
| Angular | `persons/`, `workforce/`, `professional/`, `workforce-intelligence/`, assignee picker |
| Gateway | MediatR proxy pattern per domain |

---

## Already exists

| Capability | Implementation |
|------------|----------------|
| Person text search | `GET /api/v1/persons/search?q=` — SearchName, PlatformId, contacts |
| Employment search | `GET /api/v1/employment/search?q=` — PersonSearchName, EmploymentNumber |
| Professional search | `GET /api/v1/professional-profiles/search?q=` — name, number, registration |
| Intelligence workforce search | `GET /api/v1/intelligence/search` — org-scoped `WorkforceSearchIndex` |
| Gateway proxies | Person, Employment, Professional, Intelligence handlers |
| Siloed Angular directories | `person-directory`, `workforce-directory`, `professional-directory` |
| AG evaluator + RLS model | `AuthorizationEvaluatorService.EvaluateRowLevelSecurity` |
| Field mask schema | `Permission.FieldMask` column |
| Outbox relay | `workforce-events` topic via `OutboxRelayWorker` |
| Phase 10 projection pattern | `WorkforceIntelligenceService` snapshot/index architecture |

---

## Reusable

| Asset | Reuse pattern |
|-------|---------------|
| `WorkforceIntelligenceService` scaffold | Greenfield service template (port, DI, migrations) |
| `IAuthorizationEvaluator` | `Shared.Authorization` — evaluate before every search |
| `AuthorizationContextBuilder` | Build AG request from JWT claims |
| HTTP source clients | Pattern from `WorkforceIntelligenceService/Infrastructure/Clients` |
| Event consumer | `IntelligenceEventConsumerHostedService` pattern |
| Gateway proxy | Phase 10 `WorkforceIntelligenceController` pattern |
| `ApiResponseFactory` | Standard envelope |
| Assignee picker UX | Refactor to `person-lookup-dialog` using directory suggestions |

---

## Needs enhancement

| Area | Current | Phase 11 target |
|------|---------|----------------|
| **Search scope** | Global or unscoped per service | Org/hospital/dept RLS on all directory queries |
| **Field masking** | Schema exists, not consumed | Mask email/phone/credentials per permission |
| **Org membership search** | List only, no text search | Index membership names via projector |
| **Intelligence index** | Analytics-scoped, incomplete fields | Separate richer `DirectoryIndex` |
| **Governance discovery** | No people search | Index responsibilities, committees |
| **Permissions** | Only `Intelligence.Search.Read` | `Directory.Search.Read`, `Directory.Profile.Read`, etc. |
| **Angular** | Hidden siloed pages | Unified `/app/directory` in sidebar |

---

## Missing

| Capability | Phase 11 deliverable |
|------------|---------------------|
| `EnterpriseDirectoryService` | Governed discovery bounded context |
| `DirectoryIndex` | SQL FTS projection table |
| `DirectoryFacet` | Faceted search aggregates |
| Unified `/directory/search` | Cross-domain governed search |
| Composable profiles | `GET /directory/profile/{id}` |
| Suggestions/autocomplete | `GET /directory/suggestions` |
| Saved searches | `SavedSearch` entity + API |
| Search history | `SearchHistory` entity + API |
| NL search | `POST /directory/nl-search` |
| `DirectoryRanking` | Deterministic weighted ranking |
| Shared lookup dialog | Angular `person-lookup-dialog` |

---

## Technical debt

| Item | Risk | Phase 11 action |
|------|------|----------------|
| PII in search results | Privacy violation | Field masks on all directory responses |
| No search permissions on Person/Employment | Data leakage | Directory becomes canonical search entry |
| Intelligence hardcoded org ID in Angular | Wrong scope | Directory uses user org context |
| No saved search/history | Poor UX | Implement read models |
| Single-topic relay partial | Stale indexes | Consume `workforce-events` + emit `directory-events` |

---

## Authorization boundary

| Context | Owns | Must NOT own |
|---------|------|--------------|
| **EnterpriseDirectoryService** | Index, search, facets, history, saved searches | Person, Employment, License, Committee |
| **PersonService** | Person master data | Directory index ownership |
| **WorkforceIntelligenceService** | Analytics KPIs | Directory discovery (consume only if needed) |

---

## Implementation authorization

Gap analysis complete. Proceed to `ENTERPRISE_DIRECTORY_DOMAIN_MODEL.md` then `EnterpriseDirectoryService` implementation.
