# Implementation Playbook (CS + Services)

**Companion to:** [`../onboarding/implementation-checklist.md`](../onboarding/implementation-checklist.md)

## Roles

| Role | Responsibility |
|------|----------------|
| CSM | Outcomes, milestones, sponsor |
| Implementation engineer | Deploy, validate, license activate |
| Hospital ORG_ADMIN | Access, org structure |
| Quality Manager | Framework, journey, ownership |
| AOVIAS Support | Break/fix after go-live |

## Stages

### Stage 0 — Handoff from Sales

- Receive signed scope, SKU entitlements, go-live target  
- Create CS account plan; schedule kickoff  
- Confirm deployment model and IT contacts  

### Stage 1 — Deploy

- Follow [`../onboarding/deployment-validation.md`](../onboarding/deployment-validation.md)  
- Activate license ([`../licensing/activation-guide.md`](../licensing/activation-guide.md))  
- Exit: M0  

### Stage 2 — Configure

- [`../onboarding/initial-configuration.md`](../onboarding/initial-configuration.md)  
- [`../onboarding/framework-import.md`](../onboarding/framework-import.md)  
- [`../onboarding/administrator-setup.md`](../onboarding/administrator-setup.md)  
- Exit: M1  

### Stage 3 — Enable

- Execute [`../onboarding/training-schedule.md`](../onboarding/training-schedule.md)  
- Smoke real evidence + one CAPA path  
- Introduce support KB + diagnostics  
- Exit: training complete; M2 in progress  

### Stage 4 — Stabilize

- Day-30 health review  
- Close critical support themes  
- Capture ROI baseline numbers  

## RACI (abbreviated)

| Activity | CSM | Impl Eng | ORG_ADMIN | QM |
|----------|-----|----------|-----------|-----|
| Kickoff | A | C | C | C |
| Deploy/validate | C | A | C | I |
| License | C | A/R | R | I |
| Framework/journey | C | C | C | A |
| Training | A | C | C | R |
| M1 sign-off | A | C | R | R |

A=Accountable R=Responsible C=Consulted I=Informed
