# AOVIAS demo seed report

Generated: 2026-07-13

## Target

- Organization: AOVIAS Multispeciality Hospital (`2759fad4-c895-4a5f-9e49-6ded5d378098`)
- Hospital: 25-bed multispeciality hospital (`d9f5554a-bf83-4262-b386-a31587b0ba06`)
- Account: `ekta.gupta@gmail.com`
- Framework: NABH Full Accreditation, `CP00000001`
- Seed behavior: deterministic and repeatable; the final convergence pass inserted 0 records, refreshed 5,619 manifest-owned records, and left 16 records unchanged.

## Verified source totals

- 72 active members, 72 people, 72 employments, 16 departments, and 5 organizational units/locations
- 72 professional profiles and 52 registrations
- 504 professional training records: 394 completed (78.2%) and 110 pending
- 6 committees and 24 committee meetings
- 503 scored Objective Elements and 503 compliance tasks
- Objective Element distribution: 126 implemented (25.0%), 211 in progress (41.9%), and 166 pending/not implemented (33.0%)
- 423 Objective Element/evidence links covering 420 Objective Elements; 83 Objective Elements intentionally have no evidence
- 423 evidence records
- 27 findings, 17 CAPAs, 15 risks, 16 scheduled audits, and 16 audit sessions
- CAPA distribution: 11 completed, 4 in progress, 2 open; 2 active CAPAs are overdue
- Audit distribution: 12 completed, 2 in progress, and 2 planned
- 194 controlled documents: 185 effective and 9 due for review
- 18 incidents: 15 closed and 3 under investigation
- 23 clinical indicators
- 72 enterprise-directory projection rows

The live compliance dashboard reports 503 tasks, 42 overdue tasks, 16 departments, 126 validated tasks, and 25% readiness from the materialized 25% / 42% / 33% Objective Element distribution.

## AI/read-model question checks

- Highest compliance gaps: supported by department-linked Objective Element and task state.
- Pending/overdue CAPAs: 6 active, including 2 overdue.
- Documents due for review: 9.
- Fire Safety training gaps: 16 pending training records.
- Open incidents: 3 under investigation.
- Objective Elements missing evidence: 83.
- Expired/expiring professional registrations: 2 expired and 5 expiring within 90 days.
- Pending internal audits: 4 (2 planned and 2 in progress).
- Most non-conformities: Clinical, with 8 findings.
- Equipment calibration overdue: unsupported because no equipment aggregate, API, or visible module is implemented.

## Verification results

- Seeder `verify`: passed all cross-service minimum-count and projection checks.
- Idempotency: passed; an immediate repeated seed inserted 0 records.
- Compliance backend tests: 193 passed, 0 failed.
- Angular production build: passed. Existing bundle-budget and unused-import warnings remain.
- Enterprise Directory Playwright suite: 24 passed, 8 explicitly skipped, 0 failed.
- Dashboard Playwright suite: 18 passed; 3 failed and 5 did not run because the QA page object still targets the retired `app-executive-summary .summary-card` DOM and one test does not dismiss the hospital selector overlay. Manual browser smoke testing confirmed the current dashboard renders populated cards and widgets.
- Browser smoke: login, populated compliance dashboard, authorization, and Enterprise Directory search passed. Directory search returned 72 active records and the expected Ekta Gupta / Administration / Hospital Administrator record.

## Unsupported capability gaps

- Equipment/biomedical asset management, calibration, AMC, maintenance, breakdown, and warranty tracking have no implemented aggregate/API/UI. Calibration-like PDFs are evidence only; no equipment records were fabricated.
- There is no standalone learning-management module. Training completion, assessment, certificates, and renewal gaps are represented through the implemented Professional Registry training records.
- There is no separate policy-management aggregate. Policies are represented as controlled documents in the implemented document-management system.

## Known platform warnings

- ComplianceService startup reports pending EF model changes while continuing successfully; no migration was generated as part of demo seeding.
- `Microsoft.OpenApi` 2.0.0 is reported by NuGet with a known high-severity advisory.
- Angular reports existing initial/component style budget warnings.
