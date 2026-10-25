# 08 — Production Configuration

**Date:** 26 July 2026

---

## Environment Profiles

| Profile | Purpose | Secrets source |
|---------|---------|----------------|
| Development | Local engineers | `appsettings.Development.json` (local keys) + optional user-secrets |
| Demo | Identity demo overlay | `appsettings.Demo.json` |
| Production | Customer deploy | Env vars / Key Vault only — base secrets empty |
| Testing | CI | Env injection |

---

## Configuration Domains

| Domain | Status | Notes |
|--------|--------|-------|
| Connection strings | Dev Trusted_Connection / LocalDB | Prod must inject SQL auth or managed identity |
| JWT | Base emptied (Gateway/Compliance/Org/Integration); Dev overlays hold keys | Align single signing secret via secret store |
| Internal API key | Same pattern | Must differ per environment |
| Email / SMTP | Console only | Blocker for prod notifications |
| SignalR | Hub present; delivery unwired | CRP-007 |
| Service Bus | Connection string empty in config | Required for outbox consumers |
| AI providers | Sarvam key cleared from base | Inject via env `Sarvam__ApiKey` |
| Caching | Limited catalog cache | Perf phase |
| Health | `/health`, `/health/ready`, `/liveness`, `/readiness` | Consistent on HTTP services |
| Hangfire | Compliance `/hangfire` | Must auth-lock in prod |

---

## Isolation Rules

1. **Never** commit production secrets.  
2. Base `appsettings.json` = non-secret defaults only.  
3. Development overlays may contain **local-only** shared Dev keys (rotate if repo is public).  
4. Prefer `.env` (gitignored) for docker compose — see `.env.example`.  
5. Production: Key Vault / container secrets; fail-startup if JWT empty when Environment=Production (recommended next).

---

## Docker Compose

- `SA_PASSWORD` required from `.env`  
- Connection strings use `${SA_PASSWORD}`  
- Compose still hosts **subset** of services (CRP-005 remaining)

---

## Remaining Config Work

| Item | Priority |
|------|----------|
| Empty JWT/InternalApi on remaining service base appsettings | P0 continue |
| UserSecretsId on all csproj + setup script | P1 |
| Production fail-fast for missing secrets | P0 |
| Full compose profile | P0 |
| SMTP options section + provider | P0 |
