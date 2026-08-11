# Sprint G2 — Platform Certification & Release Candidate Baseline — Status and Runbook

## Why this document exists instead of a certification report

Sprint G2's acceptance criteria require **five consecutive successful end-to-end deployment cycles, each including rollback and redeployment, run on real infrastructure with no manual intervention.** That is Tier 4 (End-to-End Verification) evidence per `IMPLEMENTATION_GUIDELINES.md` §11 — the highest evidence tier this program recognizes, and one that can only come from actually running the cycles.

Two things block that right now, and both are hard blockers, not effort-related:

1. **This sandbox cannot execute any cycle.** No .NET SDK, no path to acquire one (the egress proxy allowlists a small set of hosts and returns `403 blocked-by-allowlist` for nuget.org/dot.net/GitHub/Microsoft package sources — confirmed by direct probing in Sprint G1), no outbound SSH, no VPS target. This is identical to G1's blocker.
2. **G2's own stated scope is "using the stabilized platform from G1"** — and G1 has not actually been executed yet. The G1 runbook (`G1-EXECUTION-RUNBOOK.md`) was prepared and one real pre-flight defect was found and fixed (the Bootstrap Runtime Agent install sequence), but no one has reported back actual command output from a real SDK+VPS run. There is no "stabilized platform from G1" to certify yet — G1 is still an open, unexecuted prerequisite.

Given that, **I am not producing the 8-section G2 deliverable** (Executive Summary / Certification Results / Deployment Cycle Summary / Performance Metrics / Defects Found / Defects Fixed / Remaining Risks / Recommendation) as if certification were attempted. Zero cycles have run. That is not "certification failed" (which would call for a prioritized stabilization backlog and a G2.1 sprint per your own acceptance rules) — it is "certification has not started," which is a different, more basic state that neither of G2's two branches (Ready to Resume / Additional Stabilization Required) actually describes. Filling in that template with invented pass/fail data or a fabricated defect backlog would violate this program's standing Execution Honesty Rule for no real benefit.

**Recommendation for `ROADMAP_STATUS_REVIEW.md` / roadmap state right now:** leave the Platform Integration & Certification Phase marked open/in-progress, not complete and not failed. Do not open a G2.1 Stabilization Sprint — that sprint exists to fix defects *found by running cycles*, and none have been run yet. The correct next action is simply: **execute G1 for real, report the results, then execute the cycle procedure below.**

---

## What's ready now: the certification procedure, for whenever a real environment exists

To avoid a second round-trip once G1's real results are in, this section is the full G2 cycle procedure and tracking template, prepared now.

### One certification cycle

Each cycle is Stages 1–10 of `G1-EXECUTION-RUNBOOK.md` (Restore → Build → Test → Publish → Package → Bootstrap → Registration/Capabilities → Admission → Deploy → Health) run against a **fresh environment** (a newly provisioned VPS, or the same VPS after a full teardown — decide which "fresh" means for your infrastructure budget before starting; a real fresh VPS per cycle is the stronger proof but 5–10 of them has a real cost), plus three additional steps this sprint adds on top of G1's:

**11. Rollback**
```bash
curl -X POST https://<vps-host>:<agent-port>/api/deployment/rollback/<service-id>
```
Pass criteria: the previous release's containers come back up, certification re-runs and passes, `LastDeploymentPlanStore` reflects the rolled-back state.

**12. Redeploy**
Re-run Stage 8 (Deploy) from `G1-EXECUTION-RUNBOOK.md` for the same service(s).
Pass criteria: deployment succeeds again from the rolled-back state — this is the specific behavior ADR-0013 exists to guarantee (rollback and deploy share one machinery, so this should not need any different handling than a first deploy).

**13. Reconciliation (post-cycle)**
```bash
curl https://<vps-host>:<agent-port>/api/deployment/reconciliation/history?limit=5
```
Pass criteria: reconciliation cycles are present and show no unexpected drift after the rollback/redeploy sequence.

**A cycle fails the moment any stage fails.** Per this sprint's own rule ("every failure must stop the current run," "no manual fixes between certification runs"), do not patch anything mid-cycle — let it fail, record it, move to root-cause analysis only after the run stops, fix if the defect is certification-blocking, then start a fresh cycle count (a fix does not let you resume mid-sequence and still call it one of the 5 consecutive cycles).

### Cycle log — fill in one row per attempt

| Cycle # | Start | End | Duration | Result | Manual intervention? | Root cause (if failed) |
|---|---|---|---|---|---|---|
| 1 | | | | | | |
| 2 | | | | | | |
| 3 | | | | | | |
| 4 | | | | | | |
| 5 | | | | | | |
| 6 (stretch) | | | | | | |
| 7 (stretch) | | | | | | |
| 8 (stretch) | | | | | | |
| 9 (stretch) | | | | | | |
| 10 (stretch) | | | | | | |

### Metrics to capture per successful cycle

| Metric | Cycle 1 | Cycle 2 | Cycle 3 | Cycle 4 | Cycle 5 | ... |
|---|---|---|---|---|---|---|
| Build duration | | | | | | |
| Publish duration | | | | | | |
| Bootstrap duration | | | | | | |
| Deployment duration | | | | | | |
| Health verification duration | | | | | | |
| Rollback duration | | | | | | |
| Total cycle time | | | | | | |

Success rate = successful cycles ÷ total attempted cycles. Failure categories use the same taxonomy G1 introduced: Build / Configuration / Runtime / Infrastructure / Deployment / Integration / Architecture / Documentation.

---

## Once real cycles have actually run

Send back the filled-in cycle log, metrics table, and raw console output/error messages for any failed cycle. I will then write the real Sprint G2 report — Executive Summary through Recommendation — from that evidence, and only then will `ROADMAP_STATUS_REVIEW.md` get updated to reflect an actual certification outcome (Engineering Certified, or a prioritized stabilization backlog with a G2.1 recommendation) — never before real cycles exist to back the claim.
