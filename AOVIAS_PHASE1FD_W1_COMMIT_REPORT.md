# AOVIAS Phase 1F-D — W1 Task-Branch Commit Report

**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Branch:** `agent/cursor/phase1fb-w1-layout-migration`  
**Parent (base):** `cc3df70f182a7da00d62372b178b2e038753bc1e`  
**Merge to Main:** Not performed (out of scope).

---

## 1. Pre-commit revalidation

| Check | Result |
|-------|--------|
| Branch | `agent/cursor/phase1fb-w1-layout-migration` |
| HEAD before commit | `cc3df70…` |
| Main / Claude | `cc3df70…` unchanged |
| Diff inventory | 5 renames + 2 adds + 19 mods (26 staged paths) |
| Operational dirs | Absent |
| Probe | Untracked, excluded from stage |

---

## 2. Staging

- **Method:** Individual `git add -- <path>` (26 paths).
- **Excluded:** probe, `AOVIAS_PHASE1FB_*`, `AOVIAS_PHASE1FC_*`.
- **`git diff --cached --check`:** Exit **0**.

---

## 3. Commit

| Field | Value |
|-------|--------|
| **SHA** | `a3188c91975b7d0ca444c9478c0b1590eb747837` |
| **Message** | `feat: migrate aovias config to infrastructure layout` |
| **Files** | 26 changed (263 insertions, 35 deletions) |

### 3.1 Committed paths

**Renames (5):**

- `aovias-config/templates/nginx/runtime-agent.site.conf.template` → `AOVIAS.Infrastructure/aovias-config/templates/nginx/runtime-agent.site.conf.template`
- `aovias-config/templates/publishing/Dockerfile.angular` → `AOVIAS.Infrastructure/aovias-config/templates/publishing/Dockerfile.angular`
- `aovias-config/templates/publishing/Dockerfile.aspnet` → `AOVIAS.Infrastructure/aovias-config/templates/publishing/Dockerfile.aspnet`
- `aovias-config/templates/publishing/Dockerfile.static` → `AOVIAS.Infrastructure/aovias-config/templates/publishing/Dockerfile.static`
- `aovias-config/templates/publishing/Dockerfile.worker` → `AOVIAS.Infrastructure/aovias-config/templates/publishing/Dockerfile.worker`

**Added (2):**

- `Aovias.Configuration/Layout/RepositoryLayoutAoviasConfigPaths.cs`
- `Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/RepositoryLayoutW1TransitionTests.cs`

**Modified (19):** `.gitignore`, `repository-layout.json`, legacy discovery (2), bootstrap, appsettings (2), tools (2), preflight, praman script, DeploymentValidationStages, layout tests (3), other tests (4).

---

## 4. Post-commit working tree

**Untracked (not in commit):**

- `AOVIAS_PHASE1FB_W1_IMPLEMENTATION_REPORT.md`
- `AOVIAS_PHASE1FC_W1_DIFF_REVIEW.md`
- `scripts/dev/_phase3h_runtime_probe.py`

**Main / Claude:** Still `cc3df70…` with root-level `aovias-config/` on Main.

---

## 5. Validation (committed tree)

| Command | Exit | Result |
|---------|------|--------|
| `dotnet test ...Configuration.Tests.csproj --filter "FullyQualifiedName~Layout" --no-restore` | 0 | **34/34 passed** |
| `dotnet test ...PublishingProjectPathsTests` | 0 | **2/2 passed** |
| `dotnet test ...ConfigTemplateGenerator` | 0 | **6/6 passed** |
| `dotnet build ...Publishing.Core.csproj -c Release --no-restore` | 0 | **Succeeded** |
| `git diff --check cc3df70..HEAD` | 0 | Clean |

---

## 6. Remaining risks

- Merge to Main required before W1 is live on integration worktree.
- Gitignored operational data must be migrated manually on environments that have storage under old path.
- Milestone docs may still reference old paths (not in commit).

---

## 7. Status

**W1 TASK-BRANCH COMMIT COMPLETE**

Await separate human authorization for merge to `main`.
