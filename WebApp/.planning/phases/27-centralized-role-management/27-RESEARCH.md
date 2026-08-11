# Phase 27 — Research: Centralized role IDs + UI labels

## RESEARCH COMPLETE

### Current state (codebase)

| Area | Finding |
|------|---------|
| **Team invitation (onboarding)** | `OnboardingDataService.getRoles()` returns **display-only** strings: *Quality Manager*, *Compliance Officer*, … — **not** canonical API IDs. Template uses `<option [value]="role">{{ role }}</option>` — **value sent to API equals label**, conflicting with requirement **POST body = `ORG_ADMIN`**. |
| **User context** | `UserContextService` / `UserContext.roles: string[]` — stores whatever login returns; **`roleGuard(allowedRoles)`** compares **exact strings** — must use **same canonical IDs** as backend after migration. |
| **Login** | `LoginResponse` has **`role: string`**; `login.component` sets `roles: [response.role]` — alignment with **`USER` / `ORG_ADMIN`** depends on **backend**; frontend must not assume old display strings. |
| **Organization members** | `OrganizationMember.role: string` — display in UI should use **label pipe** when showing member lists (if any). |
| **Step numbering** | Product text says “Step 4 (Team Invitation)”; current AI wizard has **Team at step 5** (after Welcome). Plan uses **Team Invitation step** without renumbering the whole wizard. |

### Canonical set (from requirements)

`USER`, `ORG_ADMIN`, `GROUP_ADMIN`, `SUPER_ADMIN`, `REGULATORY_ADMIN`, `COMPLIANCE_VIEWER`, `AUTHORITY`, `REGULATOR`, `MANAGER`

### Design direction

1. **`RoleId` type** — `type RoleId = 'USER' | ...` or string union from **single const array** `ROLES` in `core/auth/roles` or `shared/models/roles.ts`.
2. **`ROLE_DEFINITIONS: { id: RoleId; label: string }[]`** or **`Record<RoleId, string>`** — labels derived or explicit (e.g. `ORG_ADMIN` → `Organization Admin`).
3. **`RoleService` or `RoleRegistry`** — `getAllRoles()`, `getLabel(id)`, `parseOrUnknown(raw)`, `isValidRole(id)`, optional **legacy alias map** for old API values → `RoleId`.
4. **Pipe** `roleLabel` or **pure function** for templates — `unknown` → `Unknown Role` + **logger.warn once**.
5. **Team invitation** — options: `[value]="r.id"` / display `r.label`; **`TeamMember.role`** stores **`RoleId`** (rename to **`roleId`** optional — breaking; plan can keep **`role`** field name but document it holds **ID**).

### Non-goals

- Backend schema change beyond accepting standardized IDs (contract owned by API team).
- RBAC policy engine (only identity + display consistency).

### must_haves for PLAN

- Single **source of truth** for IDs + labels.
- **API** payloads use **ID** only; UI uses **labels** from registry.
- **Team invitation** + **onboard submit** + **completion review** updated.
- **Unknown role** safe display + log.
- **`roleGuard`** documented / updated to canonical IDs.
