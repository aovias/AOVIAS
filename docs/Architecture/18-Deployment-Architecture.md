# Deployment Architecture

[← README](./README.md)

---

## Local Development

| Component | Typical URL |
|-----------|-------------|
| Angular WebApp | `http://localhost:4200` |
| Gateway | `http://localhost:5000` |
| ComplianceService | `http://localhost:5016` |
| IdentityService | `https://localhost:7135` |
| OrganizationService | `https://localhost:7181` |
| KnowledgeBase | `http://localhost:5120` |

WebApp `environment.ts` resolves API to Gateway port 5000.

---

## Docker

**File:** `ComplaincePlatform/docker-compose.dev.yml`

| Container | Service |
|-----------|---------|
| SQL Server 2022 | Shared host |
| identity-service | IdentityService |
| compliance-service | ComplianceService |
| audit-service | AuditService |
| organization-service | OrganizationService |
| ai-orchestrator | AIOrchestratorService |

**Not in default compose:** Gateway, KnowledgeBase, OutboxRelayWorker — start manually.

**Dev image:** `docker/Dockerfile.dev.dotnet` (.NET 10 SDK).

---

## Environments

| Env | Config source |
|-----|---------------|
| Development | `appsettings.Development.json`, user secrets |
| Docker | `Docker.postman_environment.json` |

Production K8s/AKS definitions **not** in this repo snapshot — treat as **planned**.

---

## CI/CD

- No unified GitHub Actions pipeline documented in Architecture folder.
- Build: `dotnet build` per service; `ng build` for WebApp.

---

## Versioning

- API: URL version `v1`, `v1.1`.
- EF migrations per service DbContext.
- WebApp: `package.json` version 1.x.

---

## Scaling

- Stateless API replicas behind load balancer (**target**).
- Hangfire single-leader or dedicated worker (**operational decision pending**).
- SQL Server vertical scale first; read replicas **future**.

---

## Configuration Secrets

- JWT signing keys in Identity/Gateway config.
- Service Bus connection for OutboxRelayWorker.
- **Recommendation:** Azure Key Vault for production (not implemented in repo).
