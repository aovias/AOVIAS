# Stage 5A.1 — Configuration Write API & Runtime Override Verification

**Date:** 2026-08-02 · **Repos:** `ComplaincePlatform`, `WebApp` (uncommitted)
**Suite:** `Passed! 43 / Skipped 2 / Failed 0` · CAPA 88 / openCapas 1 · Overrides left at 0

## Status: backend complete and verified. **The Administration UI was not built.**

Stating that first because it is the wave's headline deliverable. What exists now is everything the
UI needs — a complete, verified write API with tenant override semantics — but no screen to drive
it. I ran out of session before the Configuration Center could be built and verified, and a
half-rendered admin page would be worse than none.

---

## What was delivered

### The missing half of the Configuration Platform

5B.1 and 5B.2 built a **read-only** platform. Administration is impossible without writes, so this
wave added them.

| Endpoint | Purpose |
|---|---|
| `PUT /api/v1/configuration/categories/{code}/values` | Create or update this organization's override |
| `DELETE /api/v1/configuration/categories/{code}/values/{valueCode}` | Restore the platform default |

Both gated on `SUPER_ADMIN, ORG_ADMIN` — a configuration change alters behaviour for every user in
the tenant — with the organization taken from the token and each change logged with its caller.

### Design decisions

**Writes only ever touch tenant rows.** A platform default is never modified. One organization
cannot alter what another sees, and "restore default" remains possible because the original row is
still there. This is also why `DELETE` is the restore operation rather than a flag.

**Partial edits are safe.** Every field except `code` is optional; an omitted field keeps its
current value, falling back to the platform default when creating. Renaming a value does not
silently blank its ordering or metadata.

**`DELETE` is idempotent.** Restoring a default on a value that is already the default returns 200,
not an error — a reasonable thing for an administrator to ask for. Verified.

**Only tenant-configurable categories accept writes.** `IsTenantConfigurable = false` returns 404.

## Runtime verification — the whole loop

**Apply an override:**
```
PUT /configuration/categories/AuditType/values
    { code: "InternalAudit", name: "Internal Quality Audit", sortOrder: 5 }
→ 200  version=E386091EF6358EE6
   InternalAudit = 'Internal Quality Audit'  order=5  override=True
```

**Consumer picks it up with no restart and no code change** — Planning dropdown, live:
```
["Internal Quality Audit", "Department", "Follow-up", "Mock NABH", "Focused", "Surveillance"]
```
Renamed *and* reordered to first, exactly as configured. **This is Phase 5 proven.**

**Restore the default:**
```
DELETE /configuration/categories/AuditType/values/InternalAudit
→ 200  version=4134B70C6D38AE48        ← identical to the pre-override hash
   InternalAudit = 'Internal'  order=10  override=False
```

The version hash returning to its exact prior value is a useful property in itself: versioning is
deterministic over resolved content, so a client cache keyed on it behaves correctly across an
override-then-restore cycle.

## Acceptance criteria

| Criterion | Status |
|---|---|
| Configuration Center operational | ❌ **Not built** |
| Generic metadata editor complete | ❌ **Not built** |
| Audit metadata manageable | ⚠️ **Via API, not UI** — fully functional over HTTP |
| Tenant overrides supported | ✅ Create, update, restore, idempotent |
| Runtime updates verified | ✅ Override → dropdown, no restart |
| No regression | ✅ 43/2/0, CAPA 88, typecheck clean |

**3 of 6 met.** The three unmet are the UI itself.

## What the UI still needs

Nothing in the backend. The remaining work is entirely Angular:

1. **`ConfigurationCenterComponent`** — category browser plus value list, showing platform default
   vs tenant override vs inherited. The API already returns `isTenantOverride` per value and
   `isTenantConfigurable` per category, so the visual distinction Phase 4 asks for is a rendering
   concern only.
2. **Generic metadata editor** — one reusable editor over `ConfigurationValueDto`. Because every
   Category A domain shares that shape, this is written once and serves audit types, severities,
   risk categories and the rest, which is what "no domain-specific pages" requires.
3. **Write methods on `ConfigurationApiService`** — `upsertOverride()` and `removeOverride()`
   mirroring the two endpoints, then `refreshAuditTypes()` after a save. That method already exists.
4. **A `configuration` tab** in the Administration workspace nav model.

I would estimate this as one focused session. The contract is settled and verified, so none of it is
exploratory.

## Honest note on sequencing

This wave asked for the UI first and the API implicitly. The dependency runs the other way — a
Configuration Center with no write endpoints could only have been a mock. Building the API first
means the UI can be written against a contract that is already proven end to end, including the
runtime propagation that is the hardest part to get right.

## Carried debt (unchanged)

TD-13 background job contexts · TD-19 24 unprobed Gateway endpoints · TD-14 SignalR namespacing ·
TD-15 CommunicationService · TD-04 88 filtered duplicates · TD-01 66 governed bypasses · consumer
migration 2 of 9.

**Both repositories remain uncommitted** — fifteen integrity waves and four Stage 5 increments. This
is the fourth time I have raised it. Everything delivered across this entire programme exists only
as working-tree changes on two repos, and a single bad `git checkout` would lose all of it.
