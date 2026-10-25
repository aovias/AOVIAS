# 01 — Architecture Validation

**Program:** Commercial Readiness — Phase 0 Foundation  
**Date:** 26 July 2026  
**Scope:** CompliancePlatform solution (`net10.0`)

---

## Verdict

The platform has a **credible microservice + Gateway BFF shape**. Domain folders are largely clean of infrastructure. Highest-severity issues are **cross-service project references**, **fat Compliance controllers**, and a **shared kernel** that hosts org + lifecycle types under `Shared.Infrastructure`.

**Architecture stability score (pre Phase-0 fixes): 6.0 / 10**  
**After Phase-0 incremental fixes: 6.5 / 10** (debt documented; CAPA KPI SSOT aligned; secrets hygiene started)

---

## Project Structure

- **45** `.csproj` projects, all **net10.0**
- **No circular project references**
- Shared libs: Shared.Infrastructure, Shared.Logging, Shared.Authorization, Shared.Communication, Shared.Governance, Platform.Dashboard, Compliance.MasterData

### Cross-service edges (smells)

| From | To | Severity |
|------|-----|----------|
| AuditService | ComplianceService (full assembly) | Critical |
| OutboxRelayWorker | ComplianceService (DbContext) | Critical |

Gateway correctly references only Shared.* + Platform.Dashboard and uses `IProxyService` HTTP clients.

---

## Bounded Contexts

| Context | Owner |
|---------|-------|
| BFF / orchestration | Gateway |
| Compliance (journey, evidence, CAPA, QDMS, auditor, incidents, quality) | ComplianceService |
| Organization / subscriptions | OrganizationService (+ shared kernel entities) |
| Identity | IdentityService |
| Access governance | AccessGovernanceService |
| Governance / committees | GovernanceService |
| Knowledge / frameworks | KnowledgeBase |
| AI | AIOrchestratorService |
| Communication | CommunicationService |
| Person / Workforce / Professional | respective services |
| Directory / Integration / WI | respective services |
| Outbox relay | OutboxRelayWorker → Azure Service Bus |

---

## Layering Findings

### Clean

- Domain folders: **no** EF Core / HttpClient / RabbitMQ
- `AggregateRoot` is pure (domain events only)
- Gateway: no DbContext, no backend domain project refs

### Violations

| Severity | Finding |
|----------|---------|
| High | 11 controllers inject DbContext (Compliance V1/V1_1 + Org internal) |
| High | Public Compliance controllers inject repositories / mutate aggregates |
| High | Shared.Infrastructure hosts Organization aggregates + ComplianceLifecycleStateMachine |
| Medium | Parallel `JourneyState` enum alongside `ComplianceLifecycleState` |
| Medium | MediatR hosted inside many domain services (rule preferred Gateway-only) |
| Medium | ApiResponse envelope inconsistent (AuditService raw Ok/BadRequest) |
| Medium | Messaging docs mention RabbitMQ; runtime is Azure Service Bus |

---

## CQRS

| Location | Pattern |
|----------|---------|
| Gateway | MediatR primary |
| ComplianceService | Application services (no MediatR) |
| Newer domain services | Local MediatR + thin controllers |
| AuditService / KnowledgeBase | App services only |

---

## Dependency Direction Summary

```
WebApp → Gateway → (HTTP) → Domain Services → SQL / Outbox
                              ↓
                     OutboxRelayWorker → Azure Service Bus
                              ↓
                     Consumers (Communication, Directory, Integration, …)
```

---

## Remediation Priority (Architecture)

1. Decouple AuditService from ComplianceService assembly (contracts package or retire AuditService)  
2. Stop Controllers → DbContext (move to app services) — incremental  
3. CAPA/Audit domain SSOT (see 04 / 05)  
4. Clarify messaging ownership docs (Service Bus, not RabbitMQ at Gateway)  
5. Plan extraction of shared org/lifecycle kernel ownership  

---

## Phase 0 Actions Taken

- Documented violations (this file)  
- CAPA Control Center KPIs retargeted to deficiency SSOT (see 04)  
- WeatherForecast scaffolds removed (AuditService, IdentityService)  
- No large redesign performed (per Phase 0 rules)
