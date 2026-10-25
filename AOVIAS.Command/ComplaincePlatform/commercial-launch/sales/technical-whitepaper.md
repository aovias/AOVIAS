# Technical Whitepaper — AOVIAS Command Architecture (Commercial)

**Version:** 1.0.0 · **Classification:** Customer-facing technical overview

## Abstract

AOVIAS Command is a multi-service enterprise platform for hospital accreditation readiness. A Gateway fronts Identity, Organization, Compliance, Governance, AI orchestration, and supporting domains. Compliance lifecycle state is owned exclusively by journey aggregates and a validated state machine.

## Architecture

```
Clients → Gateway (auth, proxy, MediatR, messaging infra)
            ├─ Identity / Organization / Person
            ├─ Compliance (journeys, evidence, CAPA, audit)
            ├─ Governance (docs, risk, incidents, MR)
            ├─ AIOrchestrator (knowledge, assist, ACE)
            └─ Other supporting services
```

- Inter-service calls: HTTP via Gateway clients  
- Cross-service events: Outbox pattern; domain services broker-agnostic  
- API shape: `ApiResponse<T>` standardized codes  

## Compliance integrity

- Transitions only through `ComplianceJourneyService.TransitionJourneyAsync`
- Framework policies via `IFrameworkJourneyPolicy`
- Evidence mutations respect journey lock when status ≥ Submitted
- Score/lifecycle events project through designated projectors—not ad-hoc table writes

## Tenancy & licensing

Organization-scoped tenancy with license keys, seats, heartbeat, grace/expiry, and entitlement JSON (`core.compliance`, `ai.assist`, `governance.risk`, `ace.insights`, …). Soft gates at Gateway via entitlement middleware.

## Deployment

Package scripts produce versioned artifacts; Docker tags `aovias/command-*:1.0.0`; compose commercial stack; ops scripts for upgrade/backup. Optional Redis, Key Vault, OpenTelemetry.

## Security posture (summary)

See [security-whitepaper.md](./security-whitepaper.md). Correlation IDs required for supportability.

## What v1.0.0 does not claim

- Multi-region active-active HA as a turnkey product feature  
- Payment processor / Stripe renewals  
- Replacement for clinical EHR documentation  

## References

Published architecture & deployment guides under `docs/commercial-readiness/phase-9/published/`.
