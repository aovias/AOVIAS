# AOVIAS Phase 1D-B — W1 Readiness Report

**Date:** 2026-10-25  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`

---

## 1. Safety gate

| Worktree | Branch | HEAD | Notes |
|----------|--------|------|-------|
| Main | `main` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | Phase 1C–1D artifacts largely **untracked** |
| Cursor | `agent/cursor/active` | `5a1526d…` | `??` probe only; **no** `repository-layout.json` |
| Claude | `agent/claude/active` | `5a1526d…` | Untracked Properties preserved |

Probe **not** executed. No Git writes in Phase 1D-B.

---

## 2. Inventory coverage

| Area | Covered in closure doc | Gap |
|------|------------------------|-----|
| Layout resolvers + legacy | Yes | — |
| Publish CLIs | Yes | — |
| Bootstrap / CSM appsettings examples | Yes | Legacy `V1.0` paths pre-date repo layout |
| Preflight walk | Yes | — |
| Docker/Compose/CI | Yes (minimal W1 touch) | — |
| Tracked templates under config | Yes (5 templates) | — |
| Gitignored storage/secrets | Documented | Manual operator steps |
| `deployment.metadata.json` full inventory | **No** | INV-2 deferred |

---

## 3. Manifest distribution analysis

| Question | Finding |
|----------|---------|
| Is `repository-layout.json` tracked? | **No** — `git ls-files repository-layout.json` empty at audit time |
| Present on Main? | **Yes** (untracked file on disk) |
| Present on Cursor? | **No** |
| Production loading | Walk-up from `AppContext.BaseDirectory`; finds manifest only if file exists in ancestor chain |
| Legacy fallback when manifest absent | **Publishing/CSM platform root** uses legacy folder walks — **same as pre-1C** |
| Could fallback **mask** a bad W1? | **Yes**, if move completes but **legacy markers still only check** `aovias-config` at repo root — root discovery **fails silently** to `..` fallback while ConfigRoot combines still point at old relative segment |
| Partial migration detection | **Weak** without dual-marker tests + post-move integration test |
| Recommended guard | (1) **Track manifest on `main` before W1**; (2) **dual-marker** transition in legacy; (3) add **`layoutSchemaVersion` / W1 flag** in manifest optional; (4) post-move test asserting **new path exists** and **old path absent** |

**Phase 1D-B did not change** manifest or fallback behavior (per scope).

### Tests to prove distribution safe (at execution time)

1. All layout contract tests pass on **Main** after manifest commit.
2. After ff-only, run same filter on **Cursor** worktree (manifest must exist tracked).
3. Test: platform root resolution with manifest **deleted** still returns correct root via legacy **dual-marker**.
4. Test: with **only** new infrastructure path present, resolution succeeds.
5. Test: with **only** old path present (rollback simulation), resolution succeeds during transition window.

---

## 4. Proposed W1 bundle (summary)

See `AOVIAS_PHASE1DB_W1_EXECUTION_AND_ROLLBACK_PLAN.md`:

- Single `git mv` of config tree into new `AOVIAS.Infrastructure/` parent.
- Same-merge updates to legacy discovery, manifest current markers, Publish tools, preflight walk, and layout tests.
- **Prerequisite human commit** of layout contract stack to `main`.
- Rollback via merge revert + storage backup restore.

---

## 5. Build/test labeling

| Result | Label |
|--------|--------|
| PublishingEngine / layout tests (Phase 1C) | **Historical + last run PASS** (18–27 tests in 1C-C) |
| Platform slnx NU1903 | **Historical FAIL** (Phase 0B) — out of W1 scope |
| Website npm | **Historical FAIL** — out of scope |
| Phase 1D-B | **No broad builds run** |

---

## 6. Readiness verdict

Dependency closure and execution plan are **complete enough for a human authorization decision**, subject to explicit **pre-execution** requirements:

1. Track `repository-layout.json` and layout integration on `main`.
2. Execute W1 only as **one atomic merge** with resolver/tool updates.
3. Use **dual-marker** transition or equivalent to avoid fallback masking.

### **READY FOR HUMAN AUTHORIZATION**

This status **does not authorize** running W1. It authorizes a human **go/no-go** on the documented bundle.

---

## 7. Deliverable paths

- `V:\DevelopmentRepo\AOVIAS\AOVIAS_PHASE1DB_W1_DEPENDENCY_CLOSURE.md`
- `V:\DevelopmentRepo\AOVIAS\AOVIAS_PHASE1DB_W1_EXECUTION_AND_ROLLBACK_PLAN.md`
- `V:\DevelopmentRepo\AOVIAS\AOVIAS_PHASE1DB_READINESS_REPORT.md`

---

## 8. Smallest safe next step

1. Human **integrator commit** of Phase 1C/1D layout work (including `repository-layout.json`) on `main`.  
2. Human **explicit W1 execution authorization** referencing this plan.  
3. Implement on **agent task branch** in `AOVIAS.Cursor` per `AGENTS.md` (not on integration tree without branch policy).  
4. **Do not** `git mv` until pre-move tests and dual-marker code are in place on that branch.

---

*End of Phase 1D-B readiness report.*
