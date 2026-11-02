# AOVIAS Phase 1G-C — Current Operational Documentation Correction Report

**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Task branch:** `agent/cursor/phase1gc-documentation-correction`  
**Base commit:** `202dd525e8661ca0c3db68b40ab3a888140b79cf`

---

## 1. Baseline and safety

| Worktree | Branch | HEAD |
|----------|--------|------|
| Main | `main` | `202dd525…` |
| Cursor (start) | `agent/cursor/active` | `202dd525…` |
| Cursor (now) | `agent/cursor/phase1gc-documentation-correction` | `202dd525…` (no new commits) |
| Claude | `agent/claude/active` | `202dd525…` |

**Preserved:** Untracked Phase 1F reports, runtime probe, Claude `Properties/` (unchanged). Probe **not executed**.

**Branch created:** `agent/cursor/phase1gc-documentation-correction` from Cursor active at W1-integrated HEAD.

---

## 2. Files changed (3)

| File | Rationale |
|------|-----------|
| `Aovias.Runtime.Agent/DEPLOYMENT.md` | Remove obsolete root `aovias-config/templates/` as current repo layout (Phase 1G-B P0). |
| `ComplianceServiceManager/docs/sprint5/VPS-Bootstrap-Guide.md` | Replace V1.0 Windows paths with current AOVIAS repo layout for operational bootstrap example. |
| `Aovias.Runtime.Agent/.dockerignore` | Exclude gitignored package storage under post-W1 config path when platform root is Docker context (Phase 1G-B §2.2). |

No source code, tests, manifest, Dockerfiles, Compose, CI, or Phase reports modified.

---

## 3. Before / after wording

### A. `Aovias.Runtime.Agent/DEPLOYMENT.md` (Docker Compose section)

**Before:**

> The publishing pipeline generates compose files from `aovias-config/templates/publishing/docker-compose.runtime-agent.yml`.

**After:**

> In this repository, tracked publishing and deployment templates live under `AOVIAS.Infrastructure/aovias-config/templates/` (for example `templates/publishing/` for service Dockerfiles and related publish outputs, and `templates/nginx/runtime-agent.site.conf.template` used when rendering nginx site config). The publishing pipeline resolves templates from the configured AOVIAS config root (`AOVIAS_CONFIG_ROOT` on the host or the equivalent `ConfigRoot` in development settings), not from a root-level `aovias-config/` directory.

**Evidence:** Tracked templates at `AOVIAS.Infrastructure/aovias-config/templates/` (5 files: publishing Dockerfiles + nginx template). Bootstrap guide step 4 references nginx template name. Specific file `docker-compose.runtime-agent.yml` is **not** present in the tracked tree; wording avoids claiming that filename exists on disk.

### B. `ComplianceServiceManager/docs/sprint5/VPS-Bootstrap-Guide.md`

**Before (excerpt):**

- Project: `V:\DevelopmentRepo\V1.0\Aovias.Bootstrap\...`
- `--config-root V:\DevelopmentRepo\V1.0\aovias-config`

**After (excerpt):**

- Project: `V:\DevelopmentRepo\AOVIAS\Aovias.Bootstrap\...`
- `--config-root V:\DevelopmentRepo\AOVIAS\AOVIAS.Infrastructure\aovias-config`
- Added one paragraph clarifying `--config-root` vs VPS-mounted config (prerequisite `/opt/aovias-config` unchanged).

**Intent preserved:** One-time bootstrap flow, verification checklist, and “do not reuse for routine deployments” unchanged.

### C. `Aovias.Runtime.Agent/.dockerignore`

**Before:**

```text
aovias-config/storage/
```

**After:**

```text
aovias-config/storage/
AOVIAS.Infrastructure/aovias-config/storage/
```

**Decision:** **Changed** — not cosmetic. File header states platform root may be Docker context; `RuntimeAgentDockerContextStager` copies selected `Aovias.*` folders but full-root builds could still pick up gitignored `storage/` under the post-W1 config tree. Legacy line retained for machines with transitional root-level storage. Root repository `.dockerignore` was not in scope and was not edited.

---

## 4. Validation

| Command | Result |
|---------|--------|
| `git diff --stat` | 3 files, +6 / −3 lines |
| `git diff --check` | **Pass** (exit 0, no conflict markers/whitespace errors) |
| Scope check | Only authorized paths modified |

**Not run:** Publishing, deployment, Docker build, probe, package scripts.

---

## 5. Git state

| Action | Performed? |
|--------|------------|
| `git add` / `git commit` | **No** (per phase authorization) |
| Merge / sync other worktrees | **No** |

Working tree on Cursor task branch contains **uncommitted** documentation edits ready for human diff review.

---

## 6. Final status

**READY FOR HUMAN DOCUMENTATION DIFF REVIEW**

Commit and merge to `main` require **separate explicit authorization**.
