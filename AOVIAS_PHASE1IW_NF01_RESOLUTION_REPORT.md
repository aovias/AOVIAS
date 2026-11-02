# AOVIAS Phase 1I-W — NF-01 Resolution (G1 Runbook Path Consistency)

**Phase:** 1I-W (documentation correction only — **no commit, no automatic staging**)  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Branch:** `agent/cursor/phase1ip-w3-studio-migration`  
**HEAD:** `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` (unchanged)

**Reference:** NF-01 in `AOVIAS_PHASE1IV_W3_STAGED_DIFF_REVIEW.md`

---

## Final status

**`NF-01 RESOLVED — READY FOR STAGING-READINESS RECHECK`**

The runbook WPF build command now matches the post-W3 Studio layout. The fix is **unstaged** on top of the already-staged W3 index entry for the same file (`MM`). A follow-up step should decide whether to re-stage `docs/milestones/G1-EXECUTION-RUNBOOK.md` so the index includes this hunk before commit.

---

## 1. Baseline verification

| Check | Result |
|-------|--------|
| Branch | `agent/cursor/phase1ip-w3-studio-migration` |
| HEAD | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` |
| Staged paths (W3) | **1197** (preserved) |
| Unstaged tracked (before edit) | **0** |
| Unstaged tracked (after edit) | **1** (`G1-EXECUTION-RUNBOOK.md` NF-01 hunk only) |

No reset, clean, stash, rebase, branch switch, or index changes to unrelated paths.

---

## 2. NF-01 diagnosis

**Finding:** Stage 2 WPF optional build used:

```bash
dotnet build ComplianceServiceManager/ComplianceServiceManager.slnx --no-restore -c Release
```

**Runbook execution context (line 18):** Commands run from the **repository root** (parent of `AOVIAS.Studio/Aovias.Platform.slnx`).

**Layout fact:** After W3, `ComplianceServiceManager.slnx` lives at:

`AOVIAS.Studio/ComplianceServiceManager/ComplianceServiceManager.slnx`

(Verified on disk in this worktree.)

**Manifest:** M-S-03 moves the CSM cluster under `AOVIAS.Studio/`; `repository-layout.json` staged updates list `complianceServiceManager.current` as `AOVIAS.Studio/ComplianceServiceManager`.

**Nearby commands:** Stage 1–3 Platform restore/build/test already use `AOVIAS.Studio/Aovias.Platform.slnx` from repo root. The CSM line was the **only** remaining Stage 2 slnx reference using the pre-W3 root-relative path.

**Conclusion:** The path was **stale**, not valid via an alternate working directory. A single-line correction is required.

---

## 3. Correction applied

**File:** `docs/milestones/G1-EXECUTION-RUNBOOK.md`  
**Line 41 (bash block under Stage 2 — WPF leg):**

| Before | After |
|--------|--------|
| `dotnet build ComplianceServiceManager/ComplianceServiceManager.slnx --no-restore -c Release` | `dotnet build AOVIAS.Studio/ComplianceServiceManager/ComplianceServiceManager.slnx --no-restore -c Release` |

No other lines in this file were modified in 1I-W.

**Other occurrences:** Search for `ComplianceServiceManager/ComplianceServiceManager` in this runbook → **one** match (corrected). No duplicate stale slnx paths for CSM.

**Out of scope (not NF-01):** Prose and later-stage `dotnet run --project Aovias.CommandCenter/…` / `Aovias.Bootstrap/…` paths were not changed; NF-01 targeted only the inconsistent CSM **slnx** build command alongside already-updated Platform slnx commands.

---

## 4. Static validation

| Check | Result |
|-------|--------|
| Target file exists | `Test-Path` → **True** for `AOVIAS.Studio/ComplianceServiceManager/ComplianceServiceManager.slnx` |
| Path aligns with M-S-03 / layout JSON | **Yes** |
| `git diff --check` (unstaged) | **PASS** |
| `git diff --cached --check` | **PASS** |

Builds, tests, discover, preflight, Docker, and npm ReleaseValidation were **not** run (phase boundary).

---

## 5. Staged vs unstaged status

| Layer | `G1-EXECUTION-RUNBOOK.md` |
|-------|---------------------------|
| **Index (staged)** | W3 partial update from 1I-U: Platform slnx paths + repo-root wording; CSM line still **`ComplianceServiceManager/ComplianceServiceManager.slnx`** |
| **Working tree** | Same as staged **plus** NF-01 fix → **`AOVIAS.Studio/ComplianceServiceManager/ComplianceServiceManager.slnx`** |
| **Git status** | **`MM`** (staged and unstaged both present) |
| **Unstaged diff** | Single hunk, 1 line changed (see §3) |

**W3 migration staging:** **1197** paths remain staged; no paths unstaged or removed from the index.

**Phase report:** This file is **untracked** and **not staged**.

---

## 6. Confirmations

- **Commit:** not performed  
- **Merge / push / worktree sync:** not performed  
- **Automatic staging of NF-01 fix:** not performed (per authorization)  
- **Unrelated paths:** not modified  

Await human direction on re-staging the runbook (or equivalent) before commit so NF-01 is included in the staged commit snapshot.
