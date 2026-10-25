# Phase 7 — Chaos / resilience checklist

Manual drills for pilot-grade enterprise readiness. Pair with k6 smoke (`auth-login.js`, `dashboard.js`) before and after each fault. Record observed RTO and whether Gateway returned graceful errors (4xx/503) vs cascade failures (unbounded 5xx / hung clients).

**Environment prep**

- Stack up via commercial/dev compose.
- Capture baseline: `GET /health`, `GET /readiness`, one authenticated dashboard call.
- Optional: `k6 run tests/load/dashboard.js` at low rate during the drill.

---

## CHAOS-01 — Redis down

**Intent:** Distributed cache / SignalR backplane failure must not take down the Gateway process; IdempotencyMiddleware and caches should fall back to memory or degrade.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Confirm Redis container/service is running (if configured) | Baseline healthy |
| 2 | Stop Redis (`docker stop <redis>` or stop Windows service) | Connection drops |
| 3 | Hit `GET /health` and `GET /api/v1/dashboard/summary` (with TOKEN) | Gateway process alive; requests succeed or degrade without hang |
| 4 | Exercise SignalR notification path if available | Clients reconnect or fail soft; no Gateway crash |
| 5 | Restart Redis; re-smoke | Cache/backplane recovers |

**Pass criteria:** No process crash; p95 latency returns after Redis restore; no poisoned idempotency keys required for recovery.

**Notes / deferrals:** Multi-region Redis failover is **out of scope** for Phase 7.

---

## CHAOS-02 — AIOrchestrator kill

**Intent:** AI features fail closed/soft; core compliance paths remain usable.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Baseline: non-AI journey/dashboard call succeeds | OK |
| 2 | Stop `AIOrchestratorService` process/container | Downstream unreachable |
| 3 | Call a Gateway AI proxy (e.g. knowledge search / advisor) | 502/503/timeout with ApiResponse envelope — not Gateway crash |
| 4 | Call core paths: `/health`, dashboard summary, compliance search | Still succeed |
| 5 | Restart AIOrchestrator; re-smoke AI path | Recovers |

**Pass criteria:** Core compliance UX remains operable; AI endpoints degrade; Polly timeout/circuit breaker (Phase 7.5) limits blast radius.

---

## CHAOS-03 — SQL restart

**Intent:** Transient SQL outage during EF `EnableRetryOnFailure` window is absorbed; prolonged outage surfaces readiness failure.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Baseline login + one journey/dashboard read | OK |
| 2 | Restart SQL Server / container (`docker restart <sql>`) | Brief connection loss |
| 3 | During restart window, retry Gateway authenticated read | Retries succeed once SQL is back (within ~30s policy) |
| 4 | If SQL stopped longer than retry window | `/readiness` unhealthy; Gateway returns errors without infinite hang |
| 5 | Confirm SQL up; smoke login + dashboard | Full recovery |

**Pass criteria:** No data corruption; RTO for SQL bounce within pilot target (align with Phase 6 DR notes / `scripts/ops` backup-restore). Record wall-clock recovery time.

**Related:** `scripts/ops/backup-databases.ps1`, `restore-database.ps1`, foundation backup doc.

---

## Drill log template

| Drill | Date | Operator | Observed RTO | Pass? | Notes |
|-------|------|----------|--------------|-------|-------|
| Redis down | | | | | |
| AIOrchestrator kill | | | | | |
| SQL restart | | | | | |

## Honest gaps

- No automated chaos mesh (Chaos Toolkit / Litmus) in-repo.
- No multi-AZ / multi-region HA proof.
- Paid pen-test and formal SRE game-day tooling are Phase 8+ / commercial ops follow-on.
