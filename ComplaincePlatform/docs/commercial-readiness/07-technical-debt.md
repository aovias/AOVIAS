# 07 — Technical Debt Register

**Program:** CRP v1.0  
**Assessment Date:** 26 July 2026  
**Scope:** Architecture, CQRS/DDD, APIs, performance, security, data, deployment, code smells  
**Rule:** Evaluate only what exists; unfinished code is debt.

---

## 1. Engineering Verdict

The platform shows **mature microservice ambition** (many bounded contexts, Gateway BFF, outbox, shared auth libraries) and **uneven production hardening**. Debt concentrates in: dual domain models, incomplete deploy topology, secret management, messaging dependency, authorization consistency, and AI placeholders.

**Technical readiness (suggested): 5.0 / 10**

---

## 2. Architecture Debt

| ID | Debt | Evidence | Impact | Severity | Recommendation |
|----|------|----------|--------|----------|----------------|
| TD-A01 | Dual CAPA models | `Capa` DbSet without writes; deficiency CAPA active; ActionPlan residual | Wrong aggregates / graph counts | High | Pick SSOT; migrate; delete orphan |
| TD-A02 | Dual Audit systems | `AuditService` + Compliance `Auditor` + WeatherForecast scaffold | API confusion | High | Merge/retire AuditService |
| TD-A03 | Dual department catalogs | Org Department vs MasterDepartment | Mapping bugs | Medium | Publish canonical model |
| TD-A04 | Gateway hybrid proxy | MediatR+HttpClient primary; YARP only 3 routes | Cognitive load; inconsistent paths | Medium | Standardize edge pattern |
| TD-A05 | Compliance not MediatR-first | App services + Hangfire vs Gateway CQRS | Inconsistent style | Low | Accept or gradually align |
| TD-A06 | Empty Knowledgebase.Application project | Folder with no source | Solution noise | Low | Remove or implement |
| TD-A07 | Parallel product names | PRAMAN / AOVIAS / CompliancePlatform | Docs/ops confusion | Medium | Naming standard in commercial pack |
| TD-A08 | Stale status reports | Root reports from early 2025 claim missing OE/Risk | Misleading planners | Medium | Mark obsolete; CRP supersedes |

---

## 3. CQRS / DDD Boundary Debt

| ID | Debt | Severity | Recommendation |
|----|------|----------|----------------|
| TD-D01 | DDD boundaries generally good (Person/Workforce/Professional/AG/Governance split) but ComplianceService is a **mega-service** (journey, QDMS, incidents, quality, auditor, copilot) | High | Extract modules carefully or accept modular monolith inside Compliance with clear folders |
| TD-D02 | Cross-service writes rely on outbox/ASB; local docker often incomplete → silent event lag | High | Full topology in compose; contract tests |
| TD-D03 | Gateway handlers sometimes pass through without strong validation | Medium | FluentValidation on all mutating gateway commands |
| TD-D04 | Internal APIs + shared keys couple services tightly | Medium | mTLS or SPIFFE later; Key Vault now |

---

## 4. API Design Debt

| ID | Debt | Severity | Recommendation |
|----|------|----------|----------------|
| TD-API01 | Swagger disabled on multiple services (.NET 10) | Medium | Restore OpenAPI; publish Gateway catalog |
| TD-API02 | Versioning mixed (`V1`, `V1_1`, proxies) | Medium | API version policy doc |
| TD-API03 | FE double-prefix risk (`/api/v1` + `apiBaseUrl`) on Integration/WI services | High | Fix clients immediately |
| TD-API04 | TenantController authorization gap | Critical | `[Authorize]` + OrgAccess |
| TD-API05 | Subscriptions not entitlement-gated on business APIs | Critical | Middleware |
| TD-API06 | Scaffold WeatherForecast in AuditService | Low | Delete |
| TD-API07 | Inconsistent error contracts historically improving via Shared.Infrastructure | Medium | Enforce ApiResponse everywhere |

---

## 5. Performance Debt

| ID | Debt | Evidence | Severity | Recommendation |
|----|------|----------|----------|----------------|
| TD-P01 | Broad workspace reads / in-memory paging | `docs/performance/*`, performance audit report | High | Push filters to SQL; paging |
| TD-P02 | N+1 / duplicate aggregation (copilot/analytics) | PlatformTechnicalDebtRegister / Sprint 13F notes | Medium | Projection tables |
| TD-P03 | Catalog caching limited | Perf docs | Medium | Memory/redis cache for NABH catalog |
| TD-P04 | Dashboard timing flag only | Gateway PerformanceObservability | Low | Promote to metrics |
| TD-P05 | No OpenTelemetry | Code search empty | High | Add OTel traces/metrics/logs |

---

## 6. Caching / Logging / Monitoring

| Area | Status | Debt |
|------|--------|------|
| Serilog | Wired widely | Good |
| Correlation | Platform middleware | Good |
| Health endpoints | `/health`, ready/live | Good |
| OpenTelemetry | Absent | High debt |
| Central log sink | Not productized | Medium |
| Hangfire dashboard | Compliance `/hangfire` | Secure in prod (auth) |
| Alerting/SRE runbooks | Absent | High |

---

## 7. Exception Handling & Resilience

| ID | Debt | Severity |
|----|------|----------|
| TD-R01 | `NotImplementedException` paths (LLM draft generator) can surface at runtime | High |
| TD-R02 | GlobalExceptionMiddleware handles NotImplemented — masks incomplete features | Medium |
| TD-R03 | ASB required — no local emulator story in compose | High |
| TD-R04 | Historical silent catches in integration (Phase 12 fixed many; remain vigilant) | Medium |
| TD-R05 | Console email swallows real delivery failures | High |

---

## 8. Security Debt

| ID | Debt | Severity | Recommendation |
|----|------|----------|----------------|
| TD-S01 | JWT secrets / internal API keys in appsettings committed | Critical | Key Vault + rotate |
| TD-S02 | Live-looking AI provider API key in AIOrchestrator appsettings | Critical | Revoke + vault |
| TD-S03 | SA password in docker-compose.dev | High | Env secrets only |
| TD-S04 | OrgAccess not systemic | High | Fallback deny + OrgAccess |
| TD-S05 | `roleGuard` unused; auditor routes open to any authenticated user | Critical | Wire guards |
| TD-S06 | MFA/lockout exist — good; verify email confirmation hard requirements | Medium | Enforce for prod |
| TD-S07 | Swagger historically open in dev comments | Low | Keep off in prod |
| TD-S08 | Field-level RLS/masking incomplete (directory gap docs) | Medium | Mask PII |

---

## 9. Database Debt

| ID | Debt | Severity | Recommendation |
|----|------|----------|----------------|
| TD-DB01 | 15 databases — backup/restore complexity | Critical (ops) | Document per-DB DR |
| TD-DB02 | Mixed naming LocalDB `Praman*` vs `Compliance`/`Identity` | Medium | Standardize |
| TD-DB03 | Orphan/unused tables (`Capas` writes missing) | High | Migrate/drop |
| TD-DB04 | Indexes — not fully audited in this pass | Medium | Perf index review on journey/evidence |
| TD-DB05 | Soft-delete inconsistency across entities (historical) | Medium | Policy |
| TD-DB06 | Demo seeder depends on integrated auth SQL Express | Medium | Document; support SA for docker |

---

## 10. Deployment & Configuration Debt

| ID | Debt | Severity | Recommendation |
|----|------|----------|----------------|
| TD-DEP01 | docker-compose covers ~6 of 16+ runtimes | Critical | Full stack compose profile |
| TD-DEP02 | Gateway missing from compose | Critical | Add Gateway |
| TD-DEP03 | OutboxRelayWorker missing from compose | High | Add + ASB emulator/dev option |
| TD-DEP04 | No production install/upgrade runbook in repo | Critical | Write ops pack |
| TD-DEP05 | Feature flags only for AI — no general flag service | Low | Accept for v1 |
| TD-DEP06 | Connection string sprawl | Medium | Central config provider |
| TD-DEP07 | Phase 12.1 Key Vault deferred | High | Blocker for prod |

---

## 11. Incomplete Implementation Hotspots (Code)

| Location | Issue | Severity |
|----------|-------|----------|
| `DefaultLLMAdapter.cs` | Synthetic drafts | High |
| `LLMExplanationAdapter.cs` | Placeholder | High |
| `LlmDraftContentGenerator.cs` | `NotImplementedException` | High |
| `*JourneyPolicy.cs` (NABL/Pollution/Fire/Default) | Placeholder allow-all | High |
| `ComplianceJourneyService` SurveillanceDue | Hardcoded false | High |
| `ConsoleEmailService` | No real email | High |
| `NotificationHub` | Delivery unwired | High |
| `ControlsController` | System-generated placeholder controls | Medium |
| Gateway Identity password expire / bulk reset handlers | Placeholder | Medium |
| Evidence binder AI suggestions | Empty until AI | Medium |
| Workforce `ShiftPlaceholder` / `SalaryPlaceholder` | Domain placeholders | Low |
| WebApp Coming soon clusters | Settings/AI/Surveillance/CCC | High (product trust) |

---

## 12. Frontend Technical Debt

| ID | Debt | Severity |
|----|------|----------|
| TD-FE01 | `roleGuard` / `frameworkGuard` / `journeyRoleRedirectGuard` unwired | High |
| TD-FE02 | API double-prefix on Integration & WorkforceIntelligence clients | High |
| TD-FE03 | Journey mock workspace remnants | Medium |
| TD-FE04 | Logger TODO: Sentry/LogRocket | Medium |
| TD-FE05 | Template-driven forms in critical workflows | Medium |
| TD-FE06 | Hidden vs disabled unfinished nav inconsistency | Medium |
| TD-FE07 | Frontend lives in sibling repo — version coupling undocumented | Medium |

---

## 13. Test & Quality Debt

| ID | Debt | Severity |
|----|------|----------|
| TD-Q01 | Many unit test projects exist; full E2E hospital journey CI not evidenced as gate | High |
| TD-Q02 | Launch readiness phase plans exist; not a commercial release checklist | Medium |
| TD-Q03 | Swagger gap hurts contract testing | Medium |
| TD-Q04 | Demo verify mode helps; not CI-integrated here | Medium |

---

## 14. Technical Debt Priority Backlog (Sprints)

### Sprint CR-T1 — Security & Entitlement (launch blockers)

1. Remove secrets from git; Key Vault  
2. Authorize Tenant APIs; systemic OrgAccess  
3. Wire auditor `roleGuard`  
4. Subscription quota enforcement  

### Sprint CR-T2 — Runtime topology

1. Full docker-compose (Gateway + all domain + worker)  
2. ASB emulator or documented cloud dependency  
3. Real email provider  

### Sprint CR-T3 — Domain integrity

1. CAPA unification  
2. AuditService retirement/merge  
3. Journey policy real gates for NABH  
4. Hide/remove stub routes  

### Sprint CR-T4 — Observability & performance

1. OpenTelemetry  
2. SQL paging for workspace APIs  
3. Secure Hangfire  
4. FE error tracking (Sentry)  

### Sprint CR-T5 — API hygiene

1. Restore OpenAPI  
2. Fix FE base URLs  
3. Delete scaffolds / empty projects  
4. Mark obsolete root status reports  

---

## 15. Debt Burn-Down Metrics (Suggested)

| Metric | Target before commercial launch |
|--------|----------------------------------|
| Critical TD open | 0 |
| High TD open | ≤ 5 |
| Secrets in repo | 0 |
| Services in prod compose | 100% of sold modules |
| Dual domain models | 0 |
| NotImplemented in request path | 0 |
| OpenTelemetry coverage | Gateway + Compliance + Identity |

---

## 16. Closing Statement (Engineering)

This is not a greenfield prototype — it is a **large advanced codebase with production-shaped pieces**.  

Commercial risk is less “can we build modules?” and more “can we operate one coherent, secure, multi-tenant hospital deployment without heroics?” The debt list above is the engineering path from the former to the latter.
