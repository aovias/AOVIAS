# AOVIAS Phase 0D — Cursor Worktree Final Normalization

**Date:** 2026-10-25  
**Scope:** Approval-gated normalization of `AOVIAS.Cursor` only  
**Policy:** `AGENTS.md`, `docs/development/WORKTREE-OPERATIONS.md`, `AOVIAS_PHASE0C_WORKTREE_PRESERVATION.md`

---

## 1. Executive summary

| Item | Status |
|------|--------|
| **Phase A (read-only verification)** | **COMPLETE** |
| **Phase B (human approval gate)** | **PENDING** — no restore or merge executed in this run |
| **Phase C (restore + ff-only merge)** | **NOT RUN** — awaits explicit human approval in a follow-up execution |
| **Normalization complete?** | **NO** — Cursor HEAD `f5a550d` ≠ Main HEAD `5a1526d` |

---

## 2. Verified repository paths, branches, and HEADs

| Worktree | Path | Branch | HEAD | Tracked dirty | Notable untracked |
|----------|------|--------|------|---------------|-------------------|
| **Main** | `V:\DevelopmentRepo\AOVIAS` | `main` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | None | Website ZIPs, `.lighthouseci-r1-*`, Phase 0/0C/0D reports |
| **Cursor** | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/active` | `f5a550d9784e274acd86e35313c4fa079623f0d0` | `M IdentityService.csproj` | `scripts/dev/_phase3h_runtime_probe.py` |
| **Claude** | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/active` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | None | `Aovias.CommandCenter/.../Properties/` |

**Revalidation vs reported baseline:** Matches Phase 0C post-state (Main and Claude at `5a1526d`; Cursor on `active` @ `f5a550d` with csproj modification).

**Upstream:** Local-only repository model; no remote upstream recorded in this verification.

---

## 3. Preservation hash verification

**Directory:** `V:\DevelopmentRepo\AOVIAS-Phase0C-Preservation-20261025-131104` — **exists**

| Preserved file | SHA-256 (recomputed) | Manifest | Match |
|----------------|----------------------|----------|-------|
| `cursor-probe/_phase3h_runtime_probe.py` | `85A396C87987F1EE71FCAB0B8020390894CAAAADAD7E040696233EBC68DE44AA` | Same | **YES** |
| `claude-launchsettings/launchSettings.json` | `11D394D0B8976BB80854B35C568D08FE5BBDF64AA73A3832C16A64125D70D5D3` | Same | **YES** |

**Diff artifacts present:** `cursor-identity-csproj-diff/cursor-vs-head.diff`, `cursor-vs-main.diff`, `preservation-manifest.txt`.

**Discrepancy check:** None — hashes match manifest.

---

## 4. `.csproj` diff classification

**File:** `ComplaincePlatform/IdentityService/IdentityService.csproj` (Cursor worktree)

### 4.1 Working tree vs Cursor branch HEAD (`agent/cursor/active` @ `f5a550d`)

**Not comment-only.** Uncommitted changes include:

1. Two-line XML **comment** block (signing asset / local build explanation).
2. **`Condition="Exists('deployment-assets\signing\identity-signing.pfx')"`** added to the PFX `Content` item (Cursor **committed** `active` tip still has unconditional `Content Include` for the PFX).

### 4.2 Working tree vs `main` @ `5a1526d`

**Comment-only delta.** On `main`, the PFX item **already has** `Condition="Exists(...)"`. The working tree adds only the **comments** above that block.

### 4.3 `main` @ `5a1526d` (verified via `git show`)

The integration commit includes:

```xml
<Content Include="deployment-assets\signing\identity-signing.pfx"
         Condition="Exists('deployment-assets\signing\identity-signing.pfx')">
```

**Conclusion:** Discarding the worktree diff via `git restore` loses **comments only** relative to `main`’s functional content; relative to Cursor **HEAD**, restore reverts to the older unconditional PFX include until `merge --ff-only main` updates the tree to `main`.

---

## 5. Human approval gate (Phase B)

### Proposed action (single file only)

```powershell
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" restore ComplaincePlatform/IdentityService/IdentityService.csproj
```

### What this does

- Resets **only** that file to the version recorded in the **current branch HEAD** (`f5a550d` on `agent/cursor/active`).
- **Discards** the unstaged working-tree diff (comments + Condition as uncommitted edits vs HEAD).
- Does **not** delete untracked `scripts/dev/_phase3h_runtime_probe.py`.
- Does **not** run `git restore .` or touch any other path.

### Why this is considered safe (pending your approval)

1. Preservation copies and diffs are verified (§3; Phase 0C directory).
2. Versus **`main`**, the only unique content in the worktree diff is **comments**; required PFX `Condition` is already on `main`.
3. Phase 0C blocked `merge --ff-only main` specifically because this unstaged file would be **overwritten** by the merge — restore clears that blocker without a broad restore.

### Planned follow-up after restore (Phase C — not executed yet)

```powershell
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" merge --ff-only main
```

Per `WORKTREE-OPERATIONS.md` § “Returning worktree to active state”. Preconditions: no other unstaged tracked files; `active` is ancestor of `main` (verified in Phase 0C).

### Human approval received?

**NO** — this run stops at the gate per task instructions.

---

## 6. Actions executed in this run

| Action | Result |
|--------|--------|
| Read policy docs | Done |
| Git read-only verification (×3 worktrees) | Done |
| Preservation hash recompute | Done — match |
| `git restore` | **NOT RUN** |
| `git merge --ff-only main` | **NOT RUN** |

**No commits, staging, reset, clean, or source edits.**

---

## 7. Final status (unchanged from Phase 0C)

| Worktree | HEAD vs Main | Normalized? |
|----------|--------------|-------------|
| Main | `5a1526d` | N/A (integration) |
| Claude | `5a1526d` | **Yes** (Phase 0C) |
| Cursor | `f5a550d` | **No** |

---

## 8. Blockers and safest next step

| Blocker | Resolution |
|---------|------------|
| Explicit human approval for `git restore` on one `.csproj` file | Reply with approval (e.g. “approved — run Phase 0D Phase C”) in a **new** agent turn |
| Cursor `active` behind `main` | Resolved by approved restore + `merge --ff-only main` |

**After approval, Phase C must:**

1. Re-run §2 and §3 checks.
2. Run single-file `restore` only.
3. Confirm `git status` — no `M` on csproj; probe still `??`.
4. Run `merge --ff-only main`; on failure, **stop** (no reset/rebase).
5. Verify Cursor HEAD = `5a1526d` and update this report with results.

---

*Phase 0D gate document. Untracked on Main until integrator commits.*
