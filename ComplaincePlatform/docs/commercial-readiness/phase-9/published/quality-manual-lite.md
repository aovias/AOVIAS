# Quality Manual (Lite) — AOVIAS Command Mapping

**Purpose:** Show how AOVIAS Command supports a hospital quality management system (QMS) without replacing the hospital’s controlled quality manual.

## Scope

This lite manual describes **system support** for QMS processes: document control, compliance to standards, nonconformity/CAPA, risk, incidents, training awareness, and management review. Hospitals retain ownership of policies and clinical SOPs.

## Process map

| QMS process | AOVIAS support |
|-------------|----------------|
| Documented information | Document control / QDMS modules (`core.documents` / governance) |
| Competence / awareness | Training lite + task assignments |
| Operational planning | Framework journeys + department responsibility |
| Performance evaluation | Scoring projections, dashboards, ACE insights (if licensed) |
| Nonconformity & CAPA | Deficiency command center + CAPA domain |
| Risk-based thinking | Risk register (`governance.risk`) |
| Incident / patient safety | Incident + RCA modules |
| Internal audit | Audit management + auditor workspace |
| Management review | Committees / MR packages |

## Evidence integrity

- Evidence is bound to compliance journeys.
- When journey status ≥ Submitted, evidence is locked against silent edits.
- Changes after lock require governed lifecycle/admin paths—not informal overwrites.

## Roles in the QMS

- **Top management:** Hospital Owner views + management review.
- **Quality Manager:** Process owner for accreditation program in AOVIAS.
- **Process owners:** Department Heads for assigned standards.
- **Internal auditors:** Auditor role with package access.

## Records retention

Configure retention with hospital policy and IT backup schedules. AOVIAS stores operational records in service databases; hospitals must include those catalogs in backup/DR plans (see deployment guide).

## Continual improvement

Use CAPA aging, risk residual scores, and (if entitled) AI/ACE signals as inputs to management review—not as automatic compliance decisions.
