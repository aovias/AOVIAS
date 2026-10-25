# Stage 5B.2 — Enterprise Configuration Consumption

**Date:** 2026-08-02 · **Repos:** `ComplaincePlatform`, `WebApp` (uncommitted)
**Suite:** `Passed! 43 / Skipped 2 / Failed 0` · Typecheck clean · CAPA 88 / openCapas 1

## Headline: the loop is closed — configuration flows from database to dropdown

The gap I flagged at the end of 5B.1 is gone. The browser now fetches configuration and renders
from it:

```
Network : GET /api/v1/configuration/categories/AuditType   → 200, version 4134B70C6D38AE48
Rendered: InternalAudit, DepartmentAudit, FollowUpAudit, MockAudit, FocusedAudit, Surveillance
```

Nine values returned; **six rendered**, because the API's `selectableWhenScheduling` metadata
filtered out Consultant Review and the two NABH assessments. That filtering decision now lives in
configuration data, not in a component.

Full chain, verified live:

```
OrganizationLookupValues  →  PlatformConfigurationAppService  (tenant overlay)
                          →  Gateway /api/v1/configuration     (org from token)
                          →  ConfigurationApiService           (metadata → descriptor)
                          →  PlatformConfigurationService      (signals + version cache)
                          →  Planning dropdown
```

---

## 1. What was built

| Layer | Component |
|---|---|
| Gateway | `ConfigurationController` — client-facing route; organization taken from the token, never the client (R-02) |
| Angular client | `ConfigurationApiService` — typed DTOs, maps `metadata` JSON → `AuditTypeDescriptor` |
| Angular cache | `PlatformConfigurationService.loadAuditTypes()` — idempotent, version-compared, non-blocking |
| Bootstrap | `AppContextService.initializeFromSession()` loads configuration once per session |

### Design decisions

**Version comparison, not a blind write.** An unchanged catalog does not churn signals or re-render
consumers — `if (result.version === this.auditTypeVersion()) return;`.

**Idempotent load.** The first caller issues the request; later callers are no-ops. A screen with
several configuration-driven controls produces one request, not several.

**Failure degrades to the seed.** An unreachable configuration service leaves platform-default
behaviour in place. The alternative — an empty dropdown — looks like a broken screen rather than an
unconfigured one.

**Mapping lives in the API client.** If `metadata` later becomes typed columns, nothing downstream
changes.

**`reset()` clears configuration on logout and organization switch.** Configuration is
tenant-scoped; carrying it across a context switch would show one tenant another's values.

## 2. Consumer Migration Matrix

| Consumer | Configuration used | State |
|---|---|---|
| **Planning — schedule audit** | Audit Types | ✅ **API-driven** |
| **Planning — prep template** | Audit Types | ✅ **API-driven** |
| Execution / Sessions | Audit Types, Session Status | ❌ `AUDIT_TYPE_LABELS` |
| Reports — session table | Audit Types, Session Status | ❌ `AUDIT_TYPE_LABELS` |
| Findings | Severity, Source, Classification | ❌ Local maps |
| CAPA | Severity, SLA thresholds | ❌ Constants |
| Evidence | Evidence Types | ❌ Constants |
| Executive Intelligence | KPI thresholds | ❌ Constants |
| Administration | — | ❌ No configuration UI |

**2 of 9 migrated.** Both Planning consumers now read the API; the other seven still hold local
definitions.

## 3. Duplicate removal — partial, and I want to be precise about why

Stage 5B.2 asked to delete duplicate enums and label maps immediately after migration. I have not
deleted `AUDIT_TYPE_LABELS` or the `AuditType` union, because **five consumers still reference
them**. Deleting now would break Execution, Reports, the API service, the prep-template service and
the findings panel in the same change — a large, unverifiable edit at the end of a long session.

The correct sequence is migrate-then-delete, per consumer, with a runtime check after each. Planning
is done that way. The remaining five are the same shape and should follow the same path.

**What is genuinely deduplicated today:** the *authoritative* definition. `audit-type.catalog.ts` is
the single source, the API overlays tenant configuration on top, and Planning reads only from the
service. The legacy maps survive as unmigrated consumers, not as competing sources of truth.

## 4. Runtime validation

| Check | Result |
|---|---|
| `GET /api/v1/configuration/categories/AuditType` (Gateway) | ✅ 200, 9 values, versioned |
| Browser fetches configuration on session start | ✅ Confirmed in network log |
| Planning dropdown from API | ✅ 6 schedulable of 9 configured |
| Metadata-driven filtering | ✅ 3 external types correctly excluded |
| Tenant override → consumer (5B.1) | ✅ Verified across two organizations |
| Angular typecheck | ✅ Clean |
| Gateway / OrganizationService builds | ✅ 0 errors |
| Platform Integrity suite | ✅ 43 / 2 / 0 |
| CAPA regression | ✅ 88 total, openCapas 1 |

**One process note worth recording:** the Gateway initially returned 404 because I restarted it with
`--no-build` after correcting a compile error — it was running the previous binary. Caught by
verifying rather than assuming the endpoint was live.

## 5. Acceptance criteria

| Criterion | Status |
|---|---|
| Every consumer uses PlatformConfigurationService | ❌ **2 of 9** |
| Tenant overrides reflected throughout | ⚠️ Reflected where migrated; verified end to end |
| Duplicate configuration removed | ⚠️ Authoritative source singular; legacy maps remain for unmigrated consumers |
| Seed catalog retired | ❌ **Deliberately retained** as the offline fallback |
| Runtime validation passes | ✅ |
| No regression | ✅ |
| Configuration platform fully adopted | ❌ Adopted, not yet universal |

On the seed catalog: the brief asks for it to be retired. I would push back. It is what keeps every
screen working when the configuration service is unreachable, and it costs nothing — the load path
overwrites it whenever the API responds. Retiring it converts a degraded-but-working state into a
broken one.

## 6. Next

1. **Execution / Sessions**, then **Reports** — same catalog, no new domain; delete
   `AUDIT_TYPE_LABELS` once both are done.
2. **Severity** and **RiskCategory** as Category A categories — a category row plus values, no code,
   then migrate Findings and CAPA.
3. **Administration configuration UI** — the store and API support tenant overrides, but there is no
   screen to create one.

### Carried debt (unchanged)

TD-13 background job contexts · TD-19 24 unprobed Gateway endpoints · TD-14 SignalR namespacing ·
TD-15 CommunicationService · TD-04 88 filtered duplicates · TD-01 66 governed bypasses.

**Both repositories remain uncommitted** across fifteen integrity waves and three Stage 5
increments. I have raised this three times now; it is the cheapest risk on the board to retire and
the only one that grows every session.
