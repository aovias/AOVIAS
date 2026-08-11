# Executive Summary

[← README](./README.md)

---

## Mission

Enable hospitals and healthcare groups to **operate compliance as a continuous system** — not a pre-audit scramble — so accreditation readiness, evidence integrity, and corrective action are always visible, accountable, and improvable.

---

## Vision: Healthcare Compliance Operating System

PRAMAN unifies **governance** (what must be true), **execution** (what teams do daily), and **assurance** (what auditors and leadership can prove). The platform slogan in product materials: **Always Audit Ready**.

---

## Target Customers

| Segment | Need |
|---------|------|
| Accredited / aspiring hospitals (NABH-first) | Journey execution, evidence, maturity, audit simulation |
| Multi-hospital groups | Organization context, hospital switching, consolidated analytics |
| Quality & accreditation teams | Findings, deficiencies, CAPA, SLA, Copilot recommendations |
| Leadership | Dashboard, Analytics Hub, Compliance Maturity, executive briefings |

---

## Competitive Advantages

1. **Governance vs execution separation** — maturity stages, obligations, and journey policies distinct from task/evidence execution.
2. **Evidence ownership & verification** — single verification authority; review queue; binder by objective element.
3. **Deterministic Copilot** — rule-engine recommendations with auditable rationale; human approval before execution.
4. **Transactional outbox** — domain events for projections and integration without dual-write risk.
5. **Framework knowledge graph** — KnowledgeBase service for standards, assessments, gap analysis.
6. **Enterprise Angular portal** — token-based design system, command-center layouts, deep linking.

---

## Unique Differentiators

| Differentiator | Implementation evidence |
|----------------|-------------------------|
| Persona-driven Copilot | 7 personas, workspace tabs, experience layer (Sprint 13E) |
| Finding → deficiency promotion | Separate aggregates with explicit promotion workflow (Sprint 7B) |
| Deficiency Command Center | DCC workspace in audit/findings UX (Sprint 6) |
| Assignment routing engine | Department ownership, routing logs (Sprint 9B) |
| SLA & escalation policies | Sprint 10 entities and jobs |
| Analytics daily snapshots | `AnalyticsDailySnapshot` (Sprint 12B) |

---

## Business Strategy (Platform)

1. **Land** with NABH full accreditation journey + evidence + dashboard.
2. **Expand** within hospital: findings, CAPA, analytics, Copilot.
3. **Scale** to multi-hospital intelligence, consultant network, additional standards (JCI, NABL, ISO) — see [27-Future-Vision.md](./27-Future-Vision.md).

---

## Technology Posture

- Microservices with **Gateway BFF** (not direct SPA → domain service calls).
- **ComplianceService** owns the majority of compliance domain data.
- **Angular 21** operator portal; Identity Razor UI legacy alongside JWT APIs.

---

## Current Maturity (Summary)

| Area | Status |
|------|--------|
| Journey & execution | **Strong** — operational maturity ~90%+ |
| Evidence & binder | **Strong** |
| Findings & deficiencies | **Strong** |
| Analytics Hub | **Good** — foundation in place |
| Compliance Copilot | **Good** — Sprint 13 complete; data generation on demand |
| External auditor workspace | **Planned** (~20% design only) |
| Policy / incident modules | **Future** (0–5%) |

Detail: [05-Module-Readiness-Matrix.md](./05-Module-Readiness-Matrix.md), [26-Current-Development-Stage.md](./26-Current-Development-Stage.md).
