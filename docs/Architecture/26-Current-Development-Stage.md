# Current Development Stage

[← README](./README.md) · [Readiness Matrix](./05-Module-Readiness-Matrix.md)

**Assessment date:** June 2026 (from codebase + sprint reports)

---

## Completed (Shipped in Code)

| Area | Evidence |
|------|----------|
| Journey execution & OE wizard | Routes, APIs, Sprint 1–3 reports |
| Compliance Maturity governance | Sprint 3 |
| Evidence ownership & review queue | Sprints 4–5 |
| Deficiency Command Center | Sprint 6 |
| Findings + promotion | Sprints 7A, 7B |
| Deficiency lifecycle & RCA | Sprint 8 |
| Assignment routing | Sprint 9B |
| SLA & escalation | Sprint 10 |
| CAPA domain foundation | Sprint 11 |
| Analytics platform foundation | Sprint 12B |
| AI context expansion design | Sprint 12C |
| **Compliance Copilot full stack** | Sprints 13A–13F + migration 13G |
| Gateway BFF for all major modules | Controllers present |
| Angular operator portal | 14 feature areas |

---

## In Progress / Partial

| Area | Gap |
|------|-----|
| CAPA UX end-to-end | Domain exists; UI maturity ~65% |
| Audit Service vs Compliance findings | Overlap, consolidation incomplete |
| Copilot data population | APIs work; empty until refresh |
| Server-side Copilot activity recording | TD-H02 |
| Production deployment | Docker dev only in repo |
| Automated E2E tests | Minimal |

---

## Planned (Documented, Not Built)

| Item | Target |
|------|--------|
| External Auditor Workspace | Sprint 14 |
| Policy Management | Sprint 15 |
| Incident Management | Sprint 16 |

---

## Future Vision Only

- Clinical indicators, credentialing, regulatory module
- Multi-hospital benchmarking network
- Full LLM agent with tool governance

---

## Technical Debt Summary

| Severity | Count (Copilot register) |
|----------|--------------------------|
| High | 4 (TD-H01–H04) |
| Medium | 5 |
| Low | 4 |

Full register: `ComplaincePlatform/docs/PlatformTechnicalDebtRegister.md`

---

## Known Risks

| Risk | Mitigation |
|------|------------|
| ComplianceService concentration | Future service split by bounded context |
| Dual findings models | Consolidation plan needed |
| Migration drift | Always use `dotnet ef migrations add` |
| KPI duplication across UI | SSOT project (TD-M05) |
| AI trust boundaries | Approval framework enforced |

---

## Production Readiness

| Criterion | Status |
|-----------|--------|
| Core journey + evidence + tasks | **Near** — needs hardening & tests |
| Security review | **Partial** |
| Observability (APM) | **Partial** — logging only |
| Copilot | **Beta** — functional in dev |
| External audit module | **Not ready** |
| Overall platform | **Pre-production / pilot-ready** for NABH journey customers |

---

## Active Services (Dev)

Gateway :5000, ComplianceService :5016, WebApp :4200 — required for full portal QA.
