# AOVIAS Command — Release Notes v1.0.0

**Release date:** 2026-07-26  
**Codename:** Commercial Launch Candidate → GA  
**Semantic version:** 1.0.0

## Summary

AOVIAS Command v1.0.0 is the first commercially releasable enterprise healthcare compliance platform for hospital accreditation readiness (NABH-oriented), quality governance, and AI-assisted operations.

## Included capability (Phases 0–7)

| Area | Highlights |
|------|------------|
| Core compliance | Journey lifecycle SSOT, frameworks, evidence, CAPA (deficiency SSOT), audit, tasks |
| Governance | Document control, training lite, incidents/RCA, risk register, committees/MR, knowledge graph |
| Hospital experience | Role homes, navigation, productivity |
| Intelligence (AIL) | Grounded assistant, knowledge/RAG, evidence reviewer, advisor, risk/predictive, ACE, reporting |
| Commercial platform | Licensing, tenant isolation, setup wizard, backup/upgrade scripts, compose deploy |
| Enterprise readiness | Security headers, Key Vault, Redis optional, OTel, Polly CB, maintenance mode, k6 harness |

## Installation

See Phase 6 install docs and `docs/commercial-readiness/phase-9/published/deployment-guide.md`.

## Breaking / deferred (honest)

- Payment processor / self-serve Stripe renewals — not included
- Full multi-region HA / AlwaysOn SQL — ops responsibility
- Paid penetration test engagement — ASVS lite + header scan shipped
- Real customer testimonials — templates + anonymized sample only until signed pilots

## Upgrade

Follow `scripts/ops/upgrade.ps1` with pre-backup. Apply all EF migrations for Organization (licensing), AIOrchestrator (knowledge/ACE), Compliance, Identity, etc.

## Checksums / SBOM

Generate via `scripts/release/generate-checksums.ps1` and `scripts/release/generate-sbom.ps1` after publish.
