# Workflow Architecture

[← README](./README.md) · [Diagrams](./diagrams/)

---

## Journey Execution

```mermaid
sequenceDiagram
  participant User
  participant Journey UI
  participant Gateway
  participant Compliance
  participant KB as KnowledgeBase
  User->>Journey UI: Open /app/journey/execution
  Journey UI->>Gateway: GET journeys, OE structure
  Gateway->>Compliance: Proxy
  Gateway->>KB: Framework nodes (via KB API)
  User->>Journey UI: Complete OE wizard / tasks
  Journey UI->>Gateway: PATCH tasks, POST evidence
  Compliance->>Compliance: Obligation update events
```

---

## Finding Promotion

```mermaid
flowchart LR
  A[Finding created] --> B{Eligible?}
  B -->|Yes| C[POST promote]
  B -->|No| D[Remain finding]
  C --> E[ComplianceDeficiency]
  E --> F[DCC lifecycle]
  F --> G[CAPA optional]
```

Sprint 7B — promotion API on `FindingsController`.

---

## Deficiency Lifecycle

States managed on `ComplianceDeficiency` with audit trail and RCA fields (Sprint 8).

---

## Evidence Verification

1. Evidence uploaded (task/OE context).
2. Appears in library.
3. Review queue for verifier role.
4. Approved/rejected → status drives readiness/scoring inputs.

---

## Assignment Routing

Trigger: task/finding creation → routing engine → department assignment log.

---

## SLA & Escalation

1. SLA policy defines thresholds.
2. Background jobs detect breaches.
3. Escalation policy levels notify/escalate.

Hangfire: `compliance-escalation-job` (3 AM UTC).

---

## Copilot: Recommendation → Draft → Approval

```mermaid
sequenceDiagram
  participant User
  participant Copilot UI
  participant API
  participant Engine as Rule Engine
  participant Prop as Proposal Service
  User->>Copilot UI: Open workspace
  Copilot UI->>API: GET recommendations
  alt empty
    User->>Copilot UI: Refresh
    Copilot UI->>API: POST recommendations/refresh
    API->>Engine: Generate from SSOT reads
  end
  User->>Copilot UI: Generate draft
  Copilot UI->>API: POST drafts/generate
  User->>Copilot UI: Create proposal
  Copilot UI->>API: POST proposals/from-recommendation
  User->>Copilot UI: Approve + Execute
  Copilot UI->>API: approve → execute
  API->>API: ProposalExecutionService
```

**Rule:** No execute without approve.

---

## Analytics Pipeline

1. Operational changes → snapshots (compliance + daily analytics job).
2. Analytics Hub reads aggregated APIs.
3. Copilot analytics reads Copilot tables (separate surface).

---

## Onboarding Wizard

`/onboarding` → hospital, framework, assessment → internal obligation apply → journey bootstrap.

---

## Planned Workflows

- External auditor read-only review (**Sprint 14**)
- Policy approval workflow (**future**)
