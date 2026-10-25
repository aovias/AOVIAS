# Customer Success Playbook

**Product:** AOVIAS Command v1.0.0  
**Motion:** Pilot-led → expand seats/modules → manual renewal

## CS ownership map

| Phase | CS focus | Primary artifacts |
|-------|----------|-------------------|
| Prefill / close | Scope, success milestones | Sales handoff + proposal |
| Implement | M0–M1 | [`../onboarding/`](../onboarding/) |
| Adopt | M2 | Health review, training |
| Prove | M3 | Case study, ROI worksheet |
| Renew | T−60 | [`renewal-strategy.md`](renewal-strategy.md) |

## Cadence

| Touch | Timing | Agenda |
|-------|--------|--------|
| Kickoff | Week 0 | Charter, admins, license, training dates |
| Standup (async OK) | 2× / week until M1 | Blockers |
| Health review | Day 30 | [`health-review-template.md`](health-review-template.md) |
| QBR | Quarterly | [`qbr-template.md`](qbr-template.md) |
| Renewal | T−60 / T−30 / T−14 | Manual key path |

## Health signals

Pull from `GET /api/v1/commercial/customer-health?organizationId=` plus product usage:

| Signal | Green | Yellow | Red |
|--------|-------|--------|-----|
| License | Active | Grace | Expired / Revoked |
| Evidence velocity | Rising / steady | Flat 14d | Zero 14d |
| CAPA aging | Within goal | > goal | Critical pile-up |
| Support | < 2 open S2 | Recurring S2 | Open S1 |
| Sponsor engagement | Attends reviews | Misses 1 | Unreachable |

## Plays

1. **Adoption stall:** Schedule Dept Head clinic; verify OE ownership.  
2. **License risk:** Start renewal early; temporary extension key if PO pending.  
3. **Value proof:** Complete ROI worksheet; draft anonymized case study.  
4. **Exec risk:** Use [`escalation-procedures.md`](escalation-procedures.md).

## Non-goals

CS does not patch production SQL, mutate journey status, or promise payment portal features.
