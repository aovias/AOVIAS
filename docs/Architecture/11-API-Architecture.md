# API Architecture

[← README](./README.md)

**Base URL (dev):** `http://localhost:5000/api/v1`  
**Envelope:** `ApiResponse<T>` with `ApiResponseCode` registry.

Full inventory: `ComplaincePlatform/docs/backend-api-inventory.md`, `praman-v1-backend-api-map.md`.

---

## Versioning

| Version | Path | Notes |
|---------|------|-------|
| v1 | `api/v1/*` | Primary |
| v1.1 | `api/v1.1/*` | Evidence objective elements, framework structure read, scoring read |

---

## Gateway API Categories

### Identity (proxied)

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/identity/login` | Login (proxied to IdentityService) |
| POST | `/identity/refresh` | Token refresh |

### Compliance — Journey

| Method | Route | Auth |
|--------|-------|------|
| GET/POST | `/journeys/*` | ComplianceScope |
| GET | `/journey/*` | Obligation reads |

### Compliance — Tasks

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/tasks/summary` | Task command center |
| GET/POST/PATCH | `/tasks/*` | CRUD, comments, evidence |

### Compliance — Evidence

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/evidence/summary` | Library summary |
| GET | `/evidence/review-queue` | Verification queue |
| POST | `/evidence/*` | Upload, approval |

### Compliance — Findings

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/findings/workspace/*` | Findings dashboard |
| POST | `/findings/{id}/promote` | Promotion to deficiency |

### Compliance — Deficiencies

| Method | Route | Purpose |
|--------|-------|---------|
| GET/PATCH | `/deficiencies/*` | DCC lifecycle |

### Compliance — Analytics

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/analytics/*` | Analytics Hub data |

### Compliance — Copilot (Sprint 13)

| Method | Route | Purpose | Side effects |
|--------|-------|---------|--------------|
| GET | `/copilot/recommendations` | List recommendations | — |
| POST | `/copilot/recommendations/refresh` | Rule engine refresh | Writes recommendations |
| GET | `/copilot/drafts` | List drafts | — |
| POST | `/copilot/drafts/generate` | Generate draft | Writes draft |
| GET | `/copilot/proposals` | List proposals | — |
| POST | `/copilot/proposals/{id}/approve` | Approve | Status change |
| POST | `/copilot/proposals/{id}/execute` | Execute | Operational mutation |
| GET | `/copilot/experience/workspace` | Persona workspace | — |
| GET/POST | `/copilot/activity` | Feed / record | POST writes event |
| GET | `/copilot/analytics/*` | Observability | Read-only |

**Authorization:** `[Authorize(Policy = ComplianceScope)]`, `[ValidateHospitalId]` on Copilot controllers.

---

## Internal APIs (not for SPA)

| Prefix | Purpose |
|--------|---------|
| `/internal/v1/obligations` | Onboarding obligation apply |
| `/internal/v1/assignment` | Routing |
| `/internal/v1/deficiencies` | Service integrations |
| `/internal/v1/sla` | SLA engine |

---

## Angular API Client Pattern

```typescript
// WebApp/src/app/shared/services/base-api.service.ts
// apiBaseUrl = {host}/api/v1
// Feature services extend BaseApiService
```

Copilot: `private readonly base = '/copilot'` (not `/api/v1/copilot` — base URL already includes prefix).

---

## Deprecation

- Legacy `/api/compliance/*` YARP route — prefer Gateway MediatR v1 routes.
- AuditService findings vs Compliance findings — consolidation **in progress**.

---

## Related

- [events/event-catalog.md](./events/event-catalog.md)
- [15-AI-Copilot-Architecture.md](./15-AI-Copilot-Architecture.md)
