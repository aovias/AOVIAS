# Infrastructure Gap Analysis (Sprint 8D.2)

Platform-wide audit of deployable services vs deployment contracts and Runtime Agent provisioning (pre-8D.2 baseline).

Legend: **Contract** = declared in generated deployment contracts | **Provisioned** = Runtime Agent auto-provisions today | **Implicit** = required at runtime but not contract-driven | **Missing** = not declared and not provisioned

## Summary Matrix

| Service | Filesystem | Certificates | Secrets | Database | Docker Network | Storage/Volumes | External Providers | Dependencies | Health | Gap Severity |
|---------|------------|--------------|---------|----------|----------------|-----------------|-------------------|--------------|--------|--------------|
| runtime-agent | Contract | N/A | Missing | N/A | Implicit | Contract | Nginx, SignalR | Contract (none) | Contract+probes | Medium |
| gateway | Contract | N/A | Partial | N/A | Implicit | Contract | SignalR | Contract | Contract | Medium |
| identity-service | Contract | Contract | Contract | Contract | Implicit | Contract | N/A | Contract | Contract | Low (8D.1) |
| organization-service | Contract | N/A | Partial | Contract | Implicit | Contract | N/A | Contract | Contract | Medium |
| compliance-service | Partial | N/A | Partial | Contract | Implicit | Partial | RabbitMQ | Contract | Contract | **High** |
| audit-service | Contract | N/A | Partial | Contract | Implicit | Contract | N/A | Contract | Contract | Medium |
| ai | Contract | N/A | Partial | Contract | Implicit | Contract | Azure OpenAI, Sarvam, Service Bus | Contract | Contract | **High** |
| knowledge-base | Contract | N/A | Missing | Contract | Implicit | Contract | N/A | Contract | Contract | Medium |
| portal | Partial | N/A | Missing | N/A | Implicit | Contract | Nginx | Contract | Contract | Medium |
| outbox-worker | Contract | N/A | Partial | Contract | Implicit | Contract | Service Bus | Partial | Contract | **High** |

---

## Per-Service Detail

### runtime-agent

| Requirement | Contract | Provisioned | Implicit / Hardcoded |
|-------------|----------|-------------|----------------------|
| `/opt/aovias/runtime/logs` | Yes | Yes (filesystem) | — |
| `/opt/aovias/runtime/packages` | Yes | Yes | — |
| `/opt/aovias/runtime/config` (ro) | Yes | Yes | — |
| Config profile + secrets | Partial | No | `AOVIAS_CONFIG_ROOT`, `AOVIAS_ACTIVE_PROFILE` in agent config |
| Docker network | No | No | `${AOVIAS_DOCKER_NETWORK}` in compose |
| Nginx site config | Feature flag only | Manual/nginx provider | Not in infrastructure contract |
| SignalR hub | Feature flag only | No | Runtime bootstrap only |
| SQL / Redis / RabbitMQ | N/A | N/A | — |
| Dependencies | Empty (correct) | — | Startup order=10 hardcoded in publishing |
| Health | `/health` + 7 API probes | Post-compose only | Bootstrap diagnostics stage (runtime-agent only) |

### gateway

| Requirement | Contract | Provisioned | Implicit |
|-------------|----------|-------------|----------|
| Logs, packages, config mounts | Yes | Yes | — |
| JWT secret/issuer/audience | Partial (issuer/aud optional) | Secret validate only | Bound from profile `settings.security.json` |
| Identity authority URL | Missing | No | Hardcoded in appsettings / profile |
| SignalR backplane | Feature flag | No | Not provisioned |
| Depends on runtime-agent | Yes | No gate | Order only in publishing |
| Docker network | No | No | Compose external network |

### identity-service

| Requirement | Contract | Provisioned | Implicit |
|-------------|----------|-------------|----------|
| Signing PFX + password | Yes (8D.1) | Yes + validate | — |
| SQL Server + migrations | Yes (Automatic) | **No** | EF migrations at startup (dev) / manual |
| Connection string secret | **Missing** | No | `databaseConnectionString` in secrets file, not in secret contract |
| JWT settings | Partial | Validate in DRV | Profile-driven |
| Depends on gateway | Yes | No readiness gate | — |

### organization-service

| Requirement | Contract | Provisioned | Implicit |
|-------------|----------|-------------|----------|
| SQL (DevelopmentOnly migration) | Yes | **No** | Connection string implicit |
| Identity client | Missing | No | OAuth config in profile |
| Depends on gateway + identity | Yes | No gate | — |

### compliance-service

| Requirement | Contract | Provisioned | Implicit |
|-------------|----------|-------------|----------|
| Evidence uploads `/app/App_Data/evidence_uploads` | Yes (container path) | **No host mapping** | Writable volume not on host |
| RabbitMQ host/password | Optional in contract | **No** | Required at runtime for outbox relay |
| SQL + seed Always | Yes | **No** | Large seed scripts |
| Packages mount | Yes | Yes | — |
| Depends on gateway + organization | Yes | No gate | — |

### audit-service

| Requirement | Contract | Provisioned | Implicit |
|-------------|----------|-------------|----------|
| SQL Automatic migration | Yes | **No** | — |
| Connection string | Missing from secrets contract | No | — |
| Depends on gateway + compliance | Yes | No gate | — |

### ai (AIOrchestrator)

| Requirement | Contract | Provisioned | Implicit |
|-------------|----------|-------------|----------|
| Azure OpenAI / Sarvam keys | Optional | **No** | Required for inference |
| Service Bus connection | Optional | **No** | Event processing |
| SQL | Yes | **No** | — |
| Depends on gateway + compliance | Yes | No gate | — |

### knowledge-base

| Requirement | Contract | Provisioned | Implicit |
|-------------|----------|-------------|----------|
| SQL | Yes | **No** | — |
| Secrets | **Empty contract** | No | May need API keys later |
| Depends on gateway | Yes | No gate | — |

### portal (Angular static)

| Requirement | Contract | Provisioned | Implicit |
|-------------|----------|-------------|----------|
| `PRAMAN_API_BASE_URL` | Config contract | No | Profile settings |
| Nginx static hosting | Feature flag | nginx provider | Not infrastructure contract |
| No packages mount | Correct | — | — |
| Depends on gateway | Yes | No gate | — |

### outbox-worker

| Requirement | Contract | Provisioned | Implicit |
|-------------|----------|-------------|----------|
| Service Bus connection | Yes (required) | **No** | — |
| Compliance DB connection | Yes | **No** | Uses `ComplianceConnection` not DefaultConnection |
| Depends on compliance-service | Yes | **Missing from GetDependencies** | Only in template |
| Worker health (no HTTP) | Yes | Post-compose | — |

---

## Cross-Cutting Gaps (All Services)

| Area | Current State | Sprint 8D.2 Target |
|------|---------------|-------------------|
| Unified infrastructure contract | Split across 10 JSON files | `infrastructure.contract.json` aggregate |
| Provisioning pipeline | Ad-hoc calls in `EnsureRuntimeFoldersAsync` | Ordered pipeline with state machine |
| Docker network | External network assumed to exist | Auto-create if missing |
| Docker engine / compose | Checked only if disabled flag | Verify before compose |
| Database | Declared, never validated/provisioned | Reachability + optional migration gate |
| Secrets | File-only validation | Provider framework (file first; vault pluggable) |
| Certificates | PFX file provision (8D.1) | Generalized certificate provider framework |
| Storage / volumes | Host paths from volume contract | Explicit storage provisioner |
| External providers | Feature flags only | `providers[]` in contract |
| Dependencies | Flat list, no readiness gates | Topological resolver + startup order |
| Recovery | Partial (cert regen, folder create) | Idempotent recovery for all stages |
| Readiness report | DeploymentPlanner blockers only | Full Infrastructure Readiness Report |
| Service-specific logic | Minimal (good) | Zero service-specific provisioners |

---

## Implementation Priority (Driven by Matrix)

1. **P0 — Generic pipeline + infrastructure.contract.json** (all services)
2. **P0 — Docker network + engine verification** (all compose services)
3. **P0 — Secret contract completion** (SQL connection strings, RabbitMQ required for compliance)
4. **P1 — Database reachability validation** (all SQL services)
5. **P1 — Provider validation** (AI, Service Bus, RabbitMQ)
6. **P1 — Storage/evidence host provisioning** (compliance)
7. **P2 — Vault/KeyVault secret backends** (framework only)
8. **P2 — Migration execution** (contract flag, opt-in)
9. **P3 — E2E full-platform simulation** (Deliverable 14)

---

## Contract Additions Required

| Service | Add to Contract |
|---------|-----------------|
| All SQL services | `databaseConnectionString` or mapped connection secret (required) |
| compliance-service | RabbitMQ secrets → **required**; evidence storage host path |
| ai | Azure/Sarvam/ServiceBus → required when feature enabled |
| outbox-worker | Add to `GetDependencies`; Service Bus required |
| gateway, portal | Identity authority / API base URL in configuration contract |
| All | `infrastructure.contract.json` docker.network.createIfMissing=true |

This matrix drives Sprint 8D.2 implementation scope.
