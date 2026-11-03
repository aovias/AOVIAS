# AOVIAS Git Path-Enumeration Anomaly Report

**Date:** 2026-10-25  
**Repository:** `V:\DevelopmentRepo\AOVIAS`  
**Scope:** Diagnose and resolve `git status` warnings under `apps/website/src/components/...`  
**Out of scope:** Product-directory migration, worktree changes, commits

---

## 1. Executive summary

**Status:** **RESOLVED**

An **empty, untracked directory** whose name is literally **`...`** (three dots) existed at:

`AOVIAS Website/AOVIAS Web/apps/website/src/components/...`

It was **not tracked in Git** (`git ls-files` and `git ls-tree` showed no paths under `components/.../`). During `git status`, Git descended into this untracked directory and attempted to enumerate synthetic child paths such as `components/.../auth/`, `components/.../brand/`, etc. On **Windows**, the segment `...` is treated by path normalization similarly to **`..` + `.`**, so those combined paths **do not resolve to real on-disk locations** (sibling folders like `components/auth/` exist separately). Git then emitted repeated warnings:

`warning: could not open directory '.../components/.../<name>/': No such file or directory`

**Resolution:** After verifying the directory was empty and not in the index, it was removed using a **long-path literal** `Remove-Item` (see §6). Post-repair `git status` on `main` **no longer produces those warnings**.

**No tracked website source files were modified.** No Git index, config, or `.gitignore` changes were made.

---

## 2. Initial Git and filesystem state

### 2.1 Verified Git baseline (post-investigation commands on integrator machine)

| Item | Value |
|------|--------|
| Repository root | `V:/DevelopmentRepo/AOVIAS` |
| Branch | `main` |
| HEAD | `5a1526d99a68241d40ccba759d8fc62586f4af86` |

(Initial investigation matched the known HEAD; branch was confirmed `main` via `.git/HEAD` and live `git` commands.)

### 2.2 Known symptoms (confirmed)

- Repeated `git status` warnings for paths under `components/.../` for names: `auth`, `brand`, `branding`, `content`, `feedback`, `forms`, `hero`, `layout`, `legal`, `marketing`, `media`, `navigation`, `pricing`, `primitives`, `product`, `products`, `resources`, `search`, `seo`, `shared`, `system-states`, `trust`.
- Physical **`...`** entry visible as a sibling under `src\components`.
- Recursive PowerShell enumeration of `...` failed without `\\?\` long-path prefix; `cmd dir /a` showed empty directory.
- `fsutil reparsepoint query`: not a reparse point.
- `git ls-files`: no tracked paths matching `components/.../`.
- `git diff --check`: clean on Main, Cursor, and Claude (per prior manual check; re-verified on Main after repair).

### 2.3 Untracked items on Main (unchanged by this repair except removal of `components/.../`)

- `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v1.0.0.zip`
- `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v1.1.0.zip`
- `AOVIAS Website/AOVIAS Web/apps/website/.lighthouseci-r1-4/`
- `AOVIAS Website/AOVIAS Web/apps/website/.lighthouseci-r1-art/`
- `AOVIAS Website/AOVIAS Web/apps/website/.lighthouseci-r1-ui/`
- `AOVIAS_PHASE0_MIGRATION_READINESS.md`
- (This report: `AOVIAS_GIT_PATH_ANOMALY_REPORT.md` — untracked until committed)

Website `.gitignore` ignores `.lighthouseci/` but not the suffixed `.lighthouseci-r1-*` directories (separate hygiene topic; not modified here).

---

## 3. Commands executed and results

### Phase A — Baseline

| Command | Result |
|---------|--------|
| `git rev-parse --show-toplevel` | `V:/DevelopmentRepo/AOVIAS` |
| `git branch --show-current` | `main` |
| `git rev-parse HEAD` | `5a1526d99a68241d40ccba759d8fc62586f4af86` |
| `git status --short --branch` (before repair) | Untracked website zips/lighthouseci + Phase 0 doc; **warnings** for `components/.../<sibling>/` |
| `git diff --check` | Clean (no output) |
| `git ls-files "*components/.../*"` | Empty |
| `git ls-files --others --directory` (website components) | Included `components/.../`; also empty untracked `components/auth/`, `components/feedback/` |

### Phase B — Filesystem

| Command / check | Result |
|-----------------|--------|
| `Get-ChildItem` on `src\components` | Listed sibling directory named `...` |
| `Get-Item -LiteralPath '\\?\...\components\...'` | Directory `...`; created **2026-07-21**; empty |
| `Get-Item -LiteralPath` without `\\?\` | **Mis-resolves** to parent `components` folder (Windows normalization) |
| `fsutil reparsepoint query` | Not a reparse point |
| `Test-Path -LiteralPath '\\?\...\components\...\<sibling>'` | **False** for paths like `.../auth` |

### Phase C — Git index / history

| Check | Result |
|-------|--------|
| Tracked component trees | Only under `components/<name>/` (e.g. `brand/`, `forms/`, `trust/`) |
| Commit `557d0df` (website import) | No `components/...`, `auth`, or `feedback` in tree |
| Sparse-checkout / index flags | Not investigated; no evidence of index entries for `...` |
| Cause class | **Untracked filesystem artifact** + **Windows path normalization**, not corrupt index |

### Phase D — Repair (approved narrow action)

| Command | Result |
|---------|--------|
| `Get-ChildItem -LiteralPath '\\?\...\components\...'` | **0 items** |
| `Remove-Item -LiteralPath '\\?\...\components\...' -Force` | **Success** |
| `Test-Path -LiteralPath '\\?\...\components\...'` | **False** |

### Phase E — Verification (after repair)

| Command | Result |
|---------|--------|
| `git status --short --branch` | **No** `could not open directory` warnings |
| `git status --short --branch -- "AOVIAS Website/AOVIAS Web"` | Untracked zips + lighthouseci only |
| `git diff --check` | Clean |
| HEAD / branch | Unchanged (`main` @ `5a1526d`) |

*Note: Primary agent session could not spawn a local shell; diagnostics and repair were executed via an isolated subagent with shell access on the same repository paths. Results are recorded as reported by that execution.*

---

## 4. Evidence supporting root cause

1. **Literal `...` directory existed** as an empty untracked sibling under `components/` (timestamps 2026-07-21).
2. **Documentation uses `{app,components,...,config}` ellipsis** in folder-structure diagrams — not a real folder name:

   `AOVIAS Website/AOVIAS Web/docs/development/sprint-1.1-completion-report.md` (line 28).

3. **Canonical architecture** places `auth/` and `feedback/` directly under `components/`, not under `components/.../`:

   `AOVIAS Website/AOVIAS Web/docs/specifications/05-component-architecture-inventory.md` (lines 662–680).

4. **No repository script** creates a directory named `...` (grep for `mkdir` / `New-Item` with `'...'` — no matches).
5. **Git never tracked** paths under `components/.../`; warnings arose during **untracked directory scanning**, not from stale index entries.
6. **Windows path normalization**: without `\\?\`, tools conflate `...` with `..` + `.`, explaining failed enumeration and Git’s inability to open `components/.../<real-sibling>/`.

---

## 5. Root cause

**Primary:** Accidental creation of an **empty directory named `...`**, most plausibly from misreading **documentation ellipsis** as a literal folder name during Sprint 1.1-era scaffolding (2026-07-19 spec vs 2026-07-21 directory creation).

**Mechanism:** Git’s status logic walks untracked directories and constructs paths by appending sibling component names under `components/.../`. Windows fails to open those paths because `...` is not a stable, normal directory segment for nested resolution—while the real component trees live at `components/<name>/`.

**Not the cause:** Git index corruption, tracked symlink farm, sparse-checkout misconfiguration, or `.gitignore` suppressing tracked files.

---

## 6. Changes made

| Path | Action |
|------|--------|
| `AOVIAS Website/AOVIAS Web/apps/website/src/components/...` | **Removed** (empty directory only) |

**Files changed:** None (no tracked file edits). **Directories removed:** one empty untracked directory.

**Not removed (by design):** release zips, `.lighthouseci-r1-*` dirs, empty untracked `components/auth/` or `components/feedback/` (if still present—they did not reproduce the warning pattern once `...` was removed).

---

## 7. Verification results

- **Branch / HEAD:** `main` @ `5a1526d99a68241d40ccba759d8fc62586f4af86` — unchanged.
- **Tracked sources:** No modifications reported by `git diff --check`.
- **Warnings:** Pre-repair warnings reproduced in investigation; **absent** after removal in post-repair `git status`.
- **Untracked inventory:** Pre-existing untracked zips, lighthouseci outputs, and Phase 0 doc remain listed; no unexpected deletions.

---

## 8. Outstanding risks

| Risk | Severity | Notes |
|------|----------|--------|
| Recurrence of literal `...` folder | Low | Avoid creating directories from doc ellipsis; use explicit names |
| Empty untracked `auth/` / `feedback/` | Low | Spec placeholders; optional `.gitkeep` or ignore in a future website hygiene task |
| `.lighthouseci-r1-*` not covered by `.gitignore` | Low | May clutter `git status`; extend ignore in a separate reviewed change |
| Windows `\\?\` required for future ops on odd names | Medium | Any tool touching paths named `...` should use long-path literal APIs |

---

## 9. Recommendation — next steps

1. **Optional hygiene (separate task):** Add ignore rules for `.lighthouseci-r1-*` or align folder names with `.lighthouseci/` pattern in `AOVIAS Website/AOVIAS Web/.gitignore`.
2. **Optional:** Add `.gitkeep` under untracked scaffold dirs `components/auth/` and `components/feedback/` if they should remain in the tree, or remove them if obsolete.
3. **Do not commit** release ZIPs; keep them untracked or store outside the repo.
4. Re-run `git status` locally after pulling this report to confirm a clean warning-free experience on your machine.

---

*Report created as untracked documentation. Not staged or committed per task constraints.*
