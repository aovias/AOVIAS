# 02 — Technical Debt Resolution (Phase 0)

**Date:** 26 July 2026  
**Source:** CRP Master Gap Register Critical/P0 items

---

## Resolved in Phase 0

| CRP ID | Issue | Resolution | Status |
|--------|-------|------------|--------|
| CRP-001 (partial) | Secrets in source | Removed live Sarvam API key from AIOrchestrator `appsettings.json`; emptied JWT/InternalApi/CredentialEncryptionKey/RabbitMQ password from base configs for Gateway, Compliance, Organization, Integration, AIOrchestrator; Dev overlays retain local keys; `.env.example` + `.gitignore` for `.env` | **Partial** — remaining services still have base JWT; **revoke Sarvam key out-of-band** |
| CRP-042 | WeatherForecast scaffold | Deleted AuditService + IdentityService WeatherForecast files | **Closed** |
| CRP-008 (Phase A) | Dual CAPA KPIs | Control Center loads `ActiveDeficiencies` and computes CAPA KPIs from deficiency fields; orphan `Capas` no longer loaded | **Partial** — Capas table retained for schema; graph/incident Capas Phase B |
| CRP-050 | AI autonomy unsafe | `EnableAutonomousWorkflows` forced **false** in Gateway + AIOrchestrator AIFeatureFlags + AutonomyControl | **Closed** |
| CRP-014 (partial) | NotImplemented LLM draft | `LlmDraftContentGenerator.CanGenerate` → false; no longer throws `NotImplementedException` on selection path | **Partial** — LLM still not production |
| CRP-005 (partial) | Docker secrets | `SA_PASSWORD` moved to `${SA_PASSWORD}` from `.env` | **Partial** — compose still covers subset of services |

---

## Not Resolved (remain Open — tracked)

| CRP ID | Issue | Why deferred |
|--------|-------|--------------|
| CRP-002 | Tenant isolation / OrgAccess systemic | Requires Gateway policy sweep — Phase 1 security |
| CRP-003 | Subscription quota enforcement | Commercial feature work |
| CRP-004 | Backup/restore product | Documented in 11; tooling not built |
| CRP-005 full | Full docker topology | Large DevOps effort |
| CRP-006/007 | Real email + SignalR | Provider integration |
| CRP-009 | AuditService retirement | Needs AI AuditClient facade first |
| CRP-010 | Auditor roleGuard | WebApp sibling change |
| CRP-015 | ASB operability | Infra |

---

## Unsafe / Broken Code Addressed

1. **Live Sarvam key removed from git working tree** — treat as compromised; rotate in Sarvam console.  
2. **Autonomy flags off** — prevents accidental auto-execution.  
3. **Control Center CAPA dual-truth** — KPIs now match DCC SSOT.

---

## Dead Code Removed

- `AuditService/WeatherForecast.cs`  
- `AuditService/Controllers/WeatherForecastController.cs`  
- `IdentityService/WeatherForecast.cs`  
- `IdentityService/Controllers/WeatherForecastController.cs`  

---

## Build Status

ComplianceService changes compile; full `bin/` copy may fail while service process holds locks — verified via alternate output path when needed.

---

## Remaining Critical Debt Score

| Before Phase 0 | After Phase 0 |
|----------------|---------------|
| Many P0 open | Secrets + CAPA KPI + scaffolds improved; **launch blockers still open** (tenant, license, email, DR, audit retirement) |
