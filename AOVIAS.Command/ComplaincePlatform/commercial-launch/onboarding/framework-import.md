# Framework Import & Activation

**Product:** AOVIAS Command v1.0.0  
**Audience:** Quality Manager, Implementation lead

AOVIAS ships seeded accreditation catalogs (NABH-oriented). Pilots typically **activate and scope** an existing catalog rather than authoring a new framework from scratch.

## 1. Choose in-scope standard

| Decision | Guidance |
|----------|----------|
| Framework family | Usually NABH (or customer-agreed standard already in catalog) |
| Version | One **active** version per framework family name |
| Scope | Full hospital vs phased chapters / departments |

Document scope in the pilot charter (success milestones M1–M3).

## 2. Catalog lifecycle

Via Framework Navigator (`/app/frameworks`) or Compliance APIs:

1. **Publish** draft catalog version (if importing custom content).
2. **Activate** the agreed version (deactivates prior active of same family).
3. **Retire / Archive** only when migrating versions — do not leave dual actives.

See Phase 1 docs: [`docs/commercial-readiness/phase-1/02-framework-management.md`](../../docs/commercial-readiness/phase-1/02-framework-management.md).

## 3. Import options

| Path | When to use |
|------|-------------|
| Seeded catalog | Default for v1.0.0 pilots |
| Demo / seed-data packs | Demo environments only — see `commercial-launch/demo/DEMO_RUNBOOK.md` |
| Custom OE / chapter mapping | After activation; use OE ownership matrix |

Do **not** invent parallel framework tables outside ComplianceService catalogs.

## 4. Ownership & journey bind

1. Assign OE ownership matrix for in-scope departments (`/app/organization/oe-ownership`).
2. Enroll hospital compliance journey for the active framework.
3. Confirm lifecycle starts in a valid state (ComplianceJourney + state machine only).
4. Spot-check: department head sees assigned elements; QM sees program rollup.

## 5. Validation checklist

| Check | Pass criteria |
|-------|---------------|
| Active version | Exactly one active per family |
| Journey | Enrolled; status visible on dashboard |
| Evidence | Upload succeeds; locked when journey ≥ Submitted |
| Scoring | Snapshot updates after evidence progress |
| Search | Framework / OE searchable from compliance search |

## Exit

Active framework bound to a journey with ownership assigned — hospital can collect evidence without spreadsheets for the agreed scope.
