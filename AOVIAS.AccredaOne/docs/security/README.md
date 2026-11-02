# Security

Identity and security **direction** for AccredaOne is in Confluence **ADR-008** (approved direction; implementation details open). **Central Identity** platform design is in **CI-01** and **CI-02**, which are **draft** and separate from ADR-008's AccredaOne-level direction.

| Record | Page ID | Canonical status |
| --- | ---: | --- |
| ADR-008 | 622843 | Approved direction; implementation details open |
| CI-01 | 1015879 | Draft (v0.1) |
| CI-02 | 623127 | Draft (v0.1); Option A authorization boundary selected |

Central Identity is a **mandatory shared-platform planning workstream**. **Central Identity implementation and AccredaOne integration are not authorized.**

**Anonymous public discovery** must remain **independent of Central Identity**.

Production authentication and authorization must not be implemented in this repository while Phase 1 is **not authorized**.

## What belongs here later

- Security constraints traced to Confluence **D2**, **ADR-008**, or approved local **DEC** records
- Threat notes recorded as drafts when assigned
- Handling rules for credentials and accreditation content

## Current rules

- Do not commit secrets, credentials, tokens, private keys, or certificates.
- Do not invent a compliance control catalogue and treat it as a requirement.
- Do not copy security mechanisms from AOVIAS Command.
- Real framework source rights are **not verified**. See [../content-governance/README.md](../content-governance/README.md).

See [../traceability/CONFLUENCE-INDEX.md](../traceability/CONFLUENCE-INDEX.md).

## Local engineering security records

| Record | Purpose |
| --- | --- |
| [DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md](DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md) | **Temporary** PO acceptance of residual PostCSS advisories (Next.js 15.5.28 → PostCSS 8.4.31) for **controlled local development only** — not production clearance (decision date **2026-11-02**; review by **2026-12-02**) |

This acceptance does **not** authorize public deployment or npm dependency overrides.
