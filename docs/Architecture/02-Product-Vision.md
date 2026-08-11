# Product Vision

[← README](./README.md)

---

## Why PRAMAN Exists

Hospital accreditation and regulatory compliance are **high-stakes, cross-functional workflows** spread across departments, documents, and timelines. Spreadsheets and ad-hoc trackers fail at scale. PRAMAN provides a **single operational system** where standards, obligations, evidence, findings, and corrective actions connect with audit trails and leadership visibility.

---

## Problems Solved

| Problem | PRAMAN response |
|---------|-----------------|
| Unclear readiness | Dashboard, maturity dashboard, snapshots, Analytics Hub |
| Evidence scattered | Evidence Library, Binder, Review Queue, verification workflow |
| Audit findings lost in email | Findings workspace, promotion to deficiencies, DCC |
| CAPA without traceability | CAPA domain, proposals, audit trails |
| SLA breaches invisible | SLA policies, escalation engine, alerts |
| AI hype without trust | Deterministic recommendations, approval cards, no auto-execute |

---

## Business Goals

1. Reduce time-to-accreditation readiness for NABH hospitals.
2. Increase evidence coverage and verification throughput.
3. Close the loop from observation → finding → deficiency → CAPA → evidence.
4. Give executives defensible KPIs without manual spreadsheet aggregation.
5. Introduce AI assistance that **augments** quality teams without bypassing governance.

---

## Long-Term Vision

Evolve from **NABH compliance platform** to **Healthcare Compliance Operating System** supporting multiple standards, workforce compliance, patient safety indicators, regulatory change management, and network benchmarking.

See [27-Future-Vision.md](./27-Future-Vision.md).

---

## Platform Philosophy

### Always Audit Ready

Compliance state is **continuously measured** (snapshots, analytics, maturity stages), not computed only before audits.

### Human-in-the-Loop AI

AI may **recommend**, **draft**, and **propose** actions. Humans **approve** and **execute**. Copilot does not mutate operational SSOT without approved proposals.

### Single Source of Truth per concern

| Concern | SSOT owner |
|---------|------------|
| Operational tasks | ComplianceService tasks |
| Evidence records | ComplianceService evidence |
| Verification status | Evidence module |
| Readiness KPIs (analytics) | Analytics snapshots + dashboard services |
| Recommendations | Copilot recommendation store (derived from SSOT reads) |

### Traceability over speed

Audit trails on findings, deficiencies, proposals, journey history, outbox events.

---

## Guiding Principles

1. **Enterprise first** — scalability, maintainability, security ([Architecture Constitution](../PRAMAN-Architecture-Constitution.md)).
2. **Bounded contexts** — explicit ownership; Gateway as integration hub.
3. **Non-breaking evolution** — API versioning, additive migrations.
4. **Token-driven UI** — no raw color literals in features (`DESIGN_RULES.md`).
5. **Document reality** — distinguish implemented vs planned in all specs.
