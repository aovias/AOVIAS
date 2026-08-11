# 11 — Backup & Recovery Strategy

**Date:** 26 July 2026  
**CRP:** CRP-004  
**Status:** Documented strategy — **no automated product tooling yet**

---

## Data Stores

| Store | Type | Backup owner |
|-------|------|--------------|
| Identity, Organization, Compliance, KnowledgeBase, Audit, AI, Person, Workforce, Professional, AccessGovernance, Governance, Communication, Directory, Integration, WI | SQL Server databases | Platform ops |
| Evidence / document blobs | File/object storage (service-configured) | Platform ops |
| Configuration / secrets | Key Vault / env | Platform ops |
| Service Bus | Broker state (mostly ephemeral) | Cloud provider |

---

## Recommended Backup Strategy

### SQL (RPO ≤ 24h pilot / ≤ 1h GA)

1. **Full** nightly backup per database  
2. **Differential** every 6 hours (GA)  
3. **Log** backups every 15 minutes (GA, full recovery model)  
4. Retain 14–30 days; weekly offsite copy  

### File / blob

- Versioned object storage with soft-delete  
- Daily sync of evidence/document containers  

### Configuration

- Export Key Vault secrets inventory (encrypted) quarterly  
- Keep Infrastructure-as-Code for compose/K8s manifests in git (no secrets)

---

## Restore Procedure (Pilot)

1. Provision empty SQL instance  
2. Restore databases in order:  
   `Identity` → `Organization` → `KnowledgeBase` → `Compliance` → remaining satellites  
3. Restore blob containers  
4. Inject connection strings + JWT + InternalApi from secret store  
5. Run `dotnet ef database update` only if restore is behind latest migrations (prefer restore matching build)  
6. Start OutboxRelayWorker last  
7. Smoke: login → dashboard → one OE evidence read  

---

## Disaster Recovery Readiness

| Capability | Today |
|------------|-------|
| Documented strategy | **Yes** (this file) |
| Automated backup jobs in product | **No** |
| Customer self-serve restore API | **No** |
| DR drill evidence | **No** |
| Multi-region | **No** |

---

## Phase 0 Deliverable

Strategy documented. Implementation remains a **launch blocker** (CRP-004) for unsupervised commercial deploy.
