# Demo seed manifest

Target scope:

- Account: `ekta.gupta@gmail.com` (resolved, never created)
- Organization: `2759FAD4-C895-4A5F-9E49-6DED5D378098`
- Hospital: `D9F5554A-BF83-4262-B386-A31587B0BA06`

Planned deterministic data:

- Hospital profile/settings and 15 hospital master-department links
- 72 synthetic Indian staff profiles, contacts, addresses and employments
- 15 positions, position/department assignments and reporting relationships
- Professional profiles, qualifications, registrations and mandatory training
- Four governance committees and memberships
- One compliance journey and exactly 100 OE assessments: 25 score 5, 42 score 3, 33 score 1
- 18 tasks/evidence packs and evidence versions
- Eight findings, five CAPAs, six risks and three scheduled audits
- Six controlled documents with versions and reviews
- Five incidents with investigations and three RCAs
- Five clinical indicators with six monthly measurements each
- Compliance, analytics and dashboard snapshots

Runtime support is schema-dependent. `demo-seed-manifest.json`, emitted by every command, is the authoritative execution report and lists any aggregate skipped because the deployed database lacks the corresponding implemented table/columns.

All names and operational content are synthetic. No patient-identifiable data or credentials are included.
