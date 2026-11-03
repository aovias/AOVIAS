# AOVIAS Phase 1F-E — W1 Merge Preflight (Phase A / B)

**Date:** 2026-10-25  
**Scope:** Read-only; **no merge, branch switch, or sync performed.**

---

## 1. Worktree baselines

| Worktree | Branch | HEAD | Index | Working tree notes |
|----------|--------|------|-------|-------------------|
| Main | `main` | `cc3df70f182a7da00d62372b178b2e038753bc1e` | Empty | Untracked Phase 0–1E reports, ZIPs, Lighthouse (unchanged) |
| Cursor | `agent/cursor/phase1fb-w1-layout-migration` | `a3188c91975b7d0ca444c9478c0b1590eb747837` | Empty | Untracked: probe, Phase 1F-B/C/D reports |
| Claude | `agent/claude/active` | `cc3df70f182a7da00d62372b178b2e038753bc1e` | Empty | Untracked `Properties/` only |

All match expected foundation / W1 commit SHAs.

---

## 2. Ancestry and merge method

| Check | Result |
|-------|--------|
| `merge-base --is-ancestor cc3df70 a3188c9` | **Exit 0** (Main is direct parent of W1 commit) |
| Commits on source branch not in Main | **1** — `a3188c9 feat: migrate aovias config to infrastructure layout` |
| Divergence | **None** — linear fast-forward possible |

**Policy (`AGENTS.md`):**

- Human integrates on `V:\DevelopmentRepo\AOVIAS` / `main`.
- Preferred integration: `git merge --no-ff <branch>` for traceability.
- **History also allows** `git merge --ff-only a3188c91975b7d0ca444c9478c0b1590eb747837` (no merge commit).

Phase C should use the method **explicitly authorized** by the human (`--ff-only` vs `--no-ff`).

**Proposed commands (not executed):**

```powershell
# From V:\DevelopmentRepo\AOVIAS, branch main, clean integration intent:
git merge --no-ff agent/cursor/phase1fb-w1-layout-migration
# OR, if ff-only authorized:
git merge --ff-only a3188c91975b7d0ca444c9478c0b1590eb747837
```

**Rollback consideration:** Revert merge commit (or reset main to `cc3df70` only with explicit human authorization for destructive ops); restore gitignored operational data from backup if migrated.

---

## 3. Source commit inventory (26 paths)

Commit: `a3188c91975b7d0ca444c9478c0b1590eb747837`

| Type | Count | Paths |
|------|-------|--------|
| Rename | 5 | Templates under `aovias-config/templates/**` → `AOVIAS.Infrastructure/aovias-config/templates/**` |
| Add | 2 | `RepositoryLayoutAoviasConfigPaths.cs`, `RepositoryLayoutW1TransitionTests.cs` |
| Modify | 19 | `.gitignore`, `repository-layout.json`, legacy discovery (2), bootstrap, appsettings (2), tools (2), preflight, praman script, DeploymentValidationStages, layout/other tests (7) |

Matches Phase 1F-C / 1F-D reviewed scope.

---

## 4. Preservation

| Item | Status |
|------|--------|
| Cursor runtime probe | Untracked, not in `a3188c9` |
| Phase 1F-B/C/D reports on Cursor | Untracked |
| Claude `launchSettings.json` | Present, untracked |
| Operational dirs (`storage`, `secrets`, `logs`, `certificates`) | **Absent** on Main root config and Cursor infra config |

---

## 5. Post-merge expectations (Phase C validation plan)

On Main after merge:

- HEAD includes `a3188c9` (or merge commit containing it).
- Root `aovias-config/templates/` **removed**; `AOVIAS.Infrastructure/aovias-config/templates/` **present**.
- Run: layout tests (34), Publishing paths (2), ConfigTemplateGenerator (6), Publishing.Core Release build, `git diff --check cc3df70..HEAD`.

Claude **not** synchronized in this phase unless separately authorized.

---

## 6. Unresolved risks

| Risk | Note |
|------|------|
| Gitignored config data on other machines | Manual move if `storage/` etc. exist elsewhere |
| Main untracked report sprawl | Unchanged by merge |
| INV-2 / docs | Out of W1 commit scope |

---

## 7. Final status

**READY FOR HUMAN MERGE AUTHORIZATION**

Explicit authorization required for Phase C: merge W1 into `main` on `V:\DevelopmentRepo\AOVIAS`, including choice of `--no-ff` vs `--ff-only`.

This preflight does **not** authorize the merge.

---

## 8. Git writes in Phase A / B

**None.**
