# 09 — Secret Management

**Date:** 26 July 2026  
**CRP:** CRP-001

---

## Immediate Actions Completed

| Secret | Action |
|--------|--------|
| Sarvam `sk_*` in AIOrchestrator appsettings | **Cleared** — **ROTATE KEY IN PROVIDER CONSOLE NOW** (assume leaked) |
| Gateway JWT + InternalApi (base) | Emptied; Dev overlay retains local values |
| Compliance JWT + InternalApi + RabbitMQ password (base) | Emptied; Dev overlay retains |
| Organization JWT + InternalApi (base) | Emptied; Dev overlay retains |
| Integration JWT + InternalApi + CredentialEncryptionKey (base) | Emptied; Dev overlay created |
| AIOrchestrator InternalApi (base) | Emptied |
| Docker SA password | Moved to `.env` via `${SA_PASSWORD}` |
| `.env` | Added to `.gitignore`; `.env.example` provided |

---

## Recommended Secure Patterns

### Local Development

```powershell
# Preferred long-term
dotnet user-secrets init --project Gateway
dotnet user-secrets set "JwtSettings:SecretKey" "<dev-key>" --project Gateway
dotnet user-secrets set "InternalApi:ApiKey" "<dev-key>" --project Gateway
```

Until UserSecretsId is added to all projects, Development overlays hold local keys.

### Containers / Staging / Production

| Variable | Purpose |
|----------|---------|
| `JwtSettings__SecretKey` | Shared signing secret |
| `InternalApi__ApiKey` | Service-to-service |
| `Sarvam__ApiKey` / `AzureOpenAI__ApiKey` | LLM providers |
| `ConnectionStrings__DefaultConnection` | Per service DB |
| `Integration__CredentialEncryptionKey` | Connector credentials |
| `SA_PASSWORD` | SQL (compose only) |
| Service Bus connection string | Messaging |

Prefer **Azure Key Vault** / cloud secret store for Production (Phase 12.1 deferred item).

---

## Remaining Hardcoded Secrets (Still in Repo)

Development/Demo overlays and some service **base** appsettings (AccessGovernance, Communication, EnterpriseDirectory, Governance, WorkforceIntelligence, Person/Professional/Workforce Dev files) still contain Dev JWT/internal keys. Continue emptying bases and consolidating to Dev overlays / user-secrets.

---

## Validation Checklist

- [x] Live Sarvam key removed from base AIOrchestrator config  
- [ ] Sarvam key rotated at provider  
- [x] `.env` gitignored  
- [ ] All base appsettings secrets empty  
- [ ] Production fail-fast guard  
- [ ] Key Vault wiring  
- [ ] Git history scrub if repo shared externally  

**Do not treat Phase 0 secrets work as complete until provider key rotation is confirmed.**
