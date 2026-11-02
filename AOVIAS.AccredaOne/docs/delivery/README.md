# Delivery

Delivery records describe how assigned work is specified and checked. They do **not** authorize Phase 1.

Programme roadmap and gates: **DEV-PLAN-01** (Confluence page 491848).

| Path | Contents |
| --- | --- |
| [SETUP.md](SETUP.md) | How to use this repository today. |
| [FOUNDATION-SPRINT-0-REPORT.md](FOUNDATION-SPRINT-0-REPORT.md) | Historical Sprint 0 report (2026-10-31). |
| [DEV-01-GOVERNANCE-SYNCHRONIZATION-REPORT.md](DEV-01-GOVERNANCE-SYNCHRONIZATION-REPORT.md) | DEV-01 Phase 2 governance sync execution report. |
| `tasks/` | Engineering task specifications (`TASK-NNNN`), when assigned. |
| `test-plans/` | Test plans (`TP-NNNN`). |
| `test-reports/` | Test reports (`TR-NNNN`). |

**DEV-01:** Git ownership reconciliation is complete (uncommitted parent index changes remain outside this repo). Governance synchronization Phase 2 updated local indexes and gates (2026-11-02).

**DEV-02 / DEV-03 / DEV-04:** Local engineering foundation (toolchain pins, application shell, API foundation, persistence configuration) is documented under `docs/delivery/` and `docs/architecture/`. DEV-04 is published on canonical `main` (see DEV-04 report).

**DEV-05 / DEV-05R — CLOSED:** Automated checks published at merge **`070fb5b`** ([DEV-05-AUTOMATED-CHECKS-IMPLEMENTATION-REPORT.md](DEV-05-AUTOMATED-CHECKS-IMPLEMENTATION-REPORT.md)). CI remediation published at merge **`8a80da1`** ([DEV-05R-CI-REMEDIATION-REPORT.md](DEV-05R-CI-REMEDIATION-REPORT.md)). Remote GitHub Actions run [**41444633431**](https://github.com/aovias/AOVIAS/actions/runs/41444633431) passed backend and frontend jobs. Design proposal: [DEV-05-AUTOMATED-CHECKS-PROPOSAL.md](DEV-05-AUTOMATED-CHECKS-PROPOSAL.md). G1 **NOT PASSED**; Phase 1 **NOT AUTHORIZED**; DEV-06 not started.

Create tasks, test plans, and reports from `templates/` and register identifiers when assigned.

A task, test plan, or test report cannot authorize Phase 1 implementation or source-rights clearance.
