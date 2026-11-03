# AOVIAS Phase 0C — Worktree Preservation & Safe Normalization

**Date:** 2026-10-25  
**Scope:** Preservation-first Git operations only — no restructuring, no source fixes, no commits  
**Policy sources:** `AGENTS.md`, `docs/development/WORKTREE-OPERATIONS.md`, `AOVIAS_PHASE0_BASELINE_VALIDATION.md`

---

## 1. Executive summary

| Item | Result |
|------|--------|
| **Overall status** | **PARTIAL** |
| **Preservation checkpoint** | **Verified** (SHA-256 match on all copies) |
| **Claude normalization** | **Completed** — `agent/claude/active` fast-forwarded to `main` @ `5a1526d` |
| **Cursor normalization** | **Blocked** — `merge --ff-only main` aborted to protect unstaged `IdentityService.csproj` |
| **Main integrity** | **Verified** — unchanged `main` @ `5a1526d`; untracked artifacts preserved |

**Preservation directory:** `V:\DevelopmentRepo\AOVIAS-Phase0C-Preservation-20261025-131104`

---

## 2. Initial state (all three worktrees)

Verified at start of Phase 0C (live `git` commands).

### 2.1 Main — `V:\DevelopmentRepo\AOVIAS`

| Field | Value |
|-------|--------|
| Root | `V:/DevelopmentRepo/AOVIAS` |
| Branch | `main` |
| HEAD | `5a1526d99a68241d40ccba759d8fc62586f4af86` |
| Staged / unstaged tracked | **None** |
| `main..HEAD` / `HEAD..main` | Empty |

**Untracked (preserved, not staged):** website release ZIPs, `.lighthouseci-r1-*` dirs, `AOVIAS_GIT_PATH_ANOMALY_REPORT.md`, `AOVIAS_PHASE0_MIGRATION_READINESS.md`, `AOVIAS_PHASE0_BASELINE_VALIDATION.md`.

### 2.2 Cursor — `V:\DevelopmentRepo\AOVIAS.Cursor` (initial)

| Field | Value |
|-------|--------|
| Branch | `agent/cursor/phase3h-propagation-contract-closure` |
| HEAD | `44e83120a74c2de8736206dcec789f930003740a` |
| `main..HEAD` | **Empty** (0 commits ahead of `main`) |
| `HEAD..main` | **12 commits** (behind `main`) |
| `agent/cursor/active` tip | `f5a550d9784e274acd86e35313c4fa079623f0d0` |

**Local-only work:**

| Item | Classification |
|------|----------------|
| `ComplaincePlatform/IdentityService/IdentityService.csproj` | Unstaged tracked modification |
| `scripts/dev/_phase3h_runtime_probe.py` | Untracked |

### 2.3 Claude — `V:\DevelopmentRepo\AOVIAS.Claude` (initial)

| Field | Value |
|-------|--------|
| Branch | `agent/claude/def-env-006-angular-lifecycle` |
| HEAD | `ddcb59ee537748649ff079749fda0163feec9f15` |
| `main..HEAD` | **Empty** |
| `HEAD..main` | **52 commits** (behind `main`) |
| `agent/claude/active` tip | `0c7ebacebc082509ce98296807926471135b52f1` |

**Local-only work:**

| Item | Classification |
|------|----------------|
| `Aovias.CommandCenter/src/Aovias.CommandCenter.Api/Properties/launchSettings.json` | Untracked (entire `Properties/` directory) |

---

## 3. Local-only file classification

### 3.1 Cursor — `IdentityService.csproj`

**Comparison vs `main` @ `5a1526d` (verified diff on disk):**

- **`main` already contains** the functional fix: `Condition="Exists('deployment-assets\signing\identity-signing.pfx')"` on the PFX `Content` item.
- **Cursor working tree adds only XML comments** (two lines) above that same block — **no unique functional change vs `main`.**

**Comparison vs Cursor HEAD (phase3h @ `44e83120`):** Comments + `Condition`/`Content` block relative to older HEAD (see preserved `cursor-vs-head.diff`).

**Recommendation (human, not executed in 0C):** Discard comment-only delta with `git restore` (when authorized) or drop comments before merge — **not required for product behavior** because `main` already has the condition.

### 3.2 Cursor — `_phase3h_runtime_probe.py`

| Aspect | Finding |
|--------|---------|
| Tracking | Untracked |
| Execution | **Not run** in 0C |
| Secrets | Script **reads** `JwtSettings:SecretKey` from `ComplaincePlatform/ComplianceService/appsettings.Development.json` at runtime — **that JSON was not copied** to preservation |
| Dependencies | Hard-coded path `V:/DevelopmentRepo/AOVIAS.Cursor/...`; test GUIDs and localhost URLs — local Phase 3H probe only |
| Disposition | Copied to preservation; original left in place |

### 3.3 Claude — `launchSettings.json`

| Aspect | Finding |
|--------|---------|
| Tracking | Untracked |
| Contents | Standard ASP.NET Core profile: `Development`, localhost HTTPS/HTTP URLs — **no credentials** |
| vs `main` | No tracked `Properties/launchSettings.json` on `main` for CommandCenter API in integration tree |
| Disposition | Copied to preservation; original untracked dir preserved in worktree |

---

## 4. Original branch and SHA inventory

Recorded in `preservation-manifest.txt`. Summary:

| Worktree | Initial branch | Initial HEAD | Active branch tip (pre) |
|----------|----------------|--------------|-------------------------|
| Main | `main` | `5a1526d` | — |
| Cursor | `phase3h-propagation-contract-closure` | `44e83120` | `f5a550d` |
| Claude | `def-env-006-angular-lifecycle` | `ddcb59ee` | `0c7ebace` |

**Task branches not deleted:** `agent/cursor/phase3h-propagation-contract-closure`, `agent/claude/def-env-006-angular-lifecycle` (verified post-Phase 4).

---

## 5. Preservation directory and hash verification

**Path:** `V:\DevelopmentRepo\AOVIAS-Phase0C-Preservation-20261025-131104`

| Subdirectory | Contents |
|--------------|----------|
| `cursor-probe/` | `_phase3h_runtime_probe.py` |
| `claude-launchsettings/` | `launchSettings.json` |
| `cursor-identity-csproj-diff/` | `cursor-vs-head.diff`, `cursor-vs-main.diff` |
| (root) | `preservation-manifest.txt` |

| File | SHA-256 | Source = copy |
|------|---------|---------------|
| `_phase3h_runtime_probe.py` | `85A396C87987F1EE71FCAB0B8020390894CAAAADAD7E040696233EBC68DE44AA` | **Yes** |
| `launchSettings.json` | `11D394D0B8976BB80854B35C568D08FE5BBDF64AA73A3832C16A64125D70D5D3` | **Yes** |

Manifest re-read verified after copy; **no secret values** in manifest.

---

## 6. Normalization authorization

Per `WORKTREE-OPERATIONS.md` § “Returning worktree to active state”:

- Expected branches: **`agent/cursor/active`**, **`agent/claude/active`**
- Procedure: `checkout active` → `merge --ff-only main`
- Preconditions: task work merged to `main`, no uncommitted work that would be lost

| Check | Cursor | Claude |
|-------|--------|--------|
| `main..HEAD` on task branch | Empty | Empty |
| `active` exists | Yes | Yes |
| `active` ancestor of `main` | Yes | Yes |
| Uncommitted work | **Modified csproj** | Untracked only |
| **Authorized to attempt FF** | **Partial** (checkout yes; merge blocked) | **Yes** |

---

## 7. Git operations performed

| Worktree | Operation | Result |
|----------|-----------|--------|
| Main | *(none)* | — |
| Cursor | `git checkout agent/cursor/active` | **Success** — unstaged csproj + untracked probe **retained** |
| Cursor | `git merge --ff-only main` | **Aborted** — Git: local changes to `IdentityService.csproj` would be overwritten |
| Claude | `git checkout agent/claude/active` | **Success** |
| Claude | `git merge --ff-only main` | **Success** — fast-forward `0c7ebac` → `5a1526d` |

**Not performed:** commit, stage, stash, reset, clean, restore, branch delete, force merge.

---

## 8. Final state (verification)

### 8.1 Main — unchanged

| Field | Value |
|-------|--------|
| Branch | `main` |
| HEAD | `5a1526d99a68241d40ccba759d8fc62586f4af86` |
| Untracked | Same as §2.1 (+ this report when written) |
| `git diff --check` | **PASS** |

### 8.2 Cursor — partial normalization

| Field | Value |
|-------|--------|
| Branch | `agent/cursor/active` |
| HEAD | `f5a550d9784e274acd86e35313c4fa079623f0d0` (**not** at `main`) |
| Status | ` M ComplaincePlatform/IdentityService/IdentityService.csproj`; `?? scripts/dev/_phase3h_runtime_probe.py` |
| `git diff --check` | **PASS** |
| Behind `main` | Yes (active branch not ff-merged) |

### 8.3 Claude — normalized

| Field | Value |
|-------|--------|
| Branch | `agent/claude/active` |
| HEAD | `5a1526d99a68241d40ccba759d8fc62586f4af86` (**matches `main`**) |
| Status | `?? Aovias.CommandCenter/src/Aovias.CommandCenter.Api/Properties/` |
| `git diff --check` | **PASS** |

---

## 9. Outstanding blockers

| ID | Blocker | Owner |
|----|---------|--------|
| B1 | Cursor `agent/cursor/active` **not** fast-forwarded to `main` due to unstaged csproj | Human: `git restore` comment-only diff (safe vs `main`) **or** commit/stash then `merge --ff-only main` |
| B2 | Untracked probe + Claude `launchSettings.json` **not integrated** — by design in 0C | Separate review if they should enter Git |
| B3 | Phase 0B build failures (NU1903, website npm) | Out of scope for 0C |

---

## 10. Recommendation — next task

**Smallest safe next step (Cursor worktree only):**

1. Confirm `cursor-vs-main.diff` in preservation dir (comment-only vs `main`).
2. On `V:\DevelopmentRepo\AOVIAS.Cursor` @ `agent/cursor/active`, with human approval for a **single-file restore** of tracked content only:

   ```powershell
   git -C "V:\DevelopmentRepo\AOVIAS.Cursor" restore ComplaincePlatform/IdentityService/IdentityService.csproj
   git -C "V:\DevelopmentRepo\AOVIAS.Cursor" merge --ff-only main
   git -C "V:\DevelopmentRepo\AOVIAS.Cursor" status --short --branch
   ```

   Untracked `_phase3h_runtime_probe.py` should remain unless separately handled.

3. Optionally commit Phase 0/0C markdown reports on **Main** when integrator approves.

**Do not** start Phase 1 directory restructuring until Cursor active aligns with `main` or the team explicitly accepts a lagging Cursor baseline.

---

## 11. Files changed by this task

| Path | Change |
|------|--------|
| `V:\DevelopmentRepo\AOVIAS-Phase0C-Preservation-20261025-131104\**` | **Created** (preservation artifacts outside Git repos) |
| `V:\DevelopmentRepo\AOVIAS\AOVIAS_PHASE0C_WORKTREE_PRESERVATION.md` | **Created** (this report; untracked on Main) |
| Worktree Git refs only | Claude: `agent/claude/active` updated to `5a1526d`; Cursor: checkout to `active` @ `f5a550d` (no merge) |

**No** application source files were edited. **No** commits.

---

*End of Phase 0C report.*
