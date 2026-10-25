# Implementation Timeline — AOVIAS Command

**Assumption:** Single hospital, Command Core or Enterprise, customer-hosted SQL. Adjust for multi-site.

## Phase A — Kickoff (Week 0–1)

| Day | Activity | Owner |
|-----|----------|-------|
| 1 | MSA signed; license issued | Sales / Commercial ops |
| 2 | Kickoff; environments confirmed | CSM + Hospital IT |
| 3–5 | Deploy Gateway + services; migrations | Partner / IT |
| 5 | Activate license; assign admin seats | Admin |

**Exit:** Health green; license Active; admin can sign in.

## Phase B — Configure (Week 1–3)

| Activity | Owner |
|----------|-------|
| Org / hospital / departments | Admin + QM |
| Framework selection & mapping | QM + Consultant |
| Role invites (Dept Heads) | Admin |
| Evidence ownership rules review | QM |
| Optional: governance modules enable | QM |
| Optional: AI entitlements enable + policy brief | QM + CISO |

**Exit:** Assigned elements; users can open role homes.

## Phase C — Pilot departments (Week 3–5)

| Activity | Owner |
|----------|-------|
| 2–3 departments collect real evidence | Dept Heads |
| Review workflow rehearsal | QM |
| CAPA sample from mock finding | QM |
| Demo scenarios on staging (optional) | CSM |

**Exit:** Pilot evidence submitted on sample journeys.

## Phase D — Expand & harden (Week 5–8)

| Activity | Owner |
|----------|-------|
| Remaining departments onboarded | QM |
| Backup/upgrade dry-run | IT |
| Audit package rehearsal | QM + Auditor |
| Management review dry-run (if entitled) | Leadership |

**Exit:** Ready for assessment window planning.

## Optional accelerators

- Pre-seed staging with `DemoDataSeeder` for training (never production PHI)  
- Extra week for Enterprise AI/ACE change management  

## Risks

| Risk | Mitigation |
|------|------------|
| Migration drift | Dry-run upgrade scripts |
| Seat under-buy | Review usage API mid-pilot |
| AI policy delay | Ship Core first; add entitlements later |
