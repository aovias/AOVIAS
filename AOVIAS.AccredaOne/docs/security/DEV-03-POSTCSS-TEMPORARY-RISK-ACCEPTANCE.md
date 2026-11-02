# DEV-03 — PostCSS temporary risk acceptance

**Record type:** Temporary security risk acceptance (development only)

**Decision date:** 2 November 2026

**Approved by:** Product Owner

**Repository:** AccredaOne nested Git

**Related delivery:** DEV-03 reversible scaffolding; PostCSS read-only investigation (2026-11-02)

This record does **not** change programme gates in [../../GOVERNANCE-STATUS.md](../../GOVERNANCE-STATUS.md). It does **not** grant production security clearance, Phase 1 authorization, or deployment approval.

---

## Decision

**Temporary risk acceptance** for the residual **PostCSS** advisories reported by `npm audit`, restricted to **controlled local development** of the DEV-03 placeholder frontend scaffold.

---

## Affected dependency

| Item | Value |
| --- | --- |
| Direct dependency | **Next.js 15.5.28** |
| Transitive dependency | **PostCSS 8.4.31** |
| Parent of PostCSS | **next@15.5.28** (`postcss@"8.4.31"` in Next package metadata) |
| npm audit (production tree, `--omit=dev`) | **One high** and **associated moderate** findings on **postcss**; **next** reported moderate **via** postcss |

Advisories include (non-exhaustive): GHSA-qx2v-qp2m-jg93, GHSA-6g55-p6wh-862q, GHSA-fxqj-rqcc-2cmp, GHSA-r28c-9q8g-f849 — see PostCSS investigation report in delivery/security correspondence.

**React / React DOM (unchanged under this acceptance):** **19.3.1** (compatible with Next 15.5.28 peers).

---

## Current application context

- **DEV-03** placeholder scaffold only.
- **Developer-controlled CSS** only (repository CSS modules and global styles).
- **No** user CSS uploads.
- **No** externally supplied CSS ingestion pipeline.
- **No** public deployment at time of acceptance.
- Build and development tooling process **repository CSS** through Next.js (PostCSS in toolchain at build/dev time).

---

## Accepted scope

**Controlled local development only** — developer workstations and local `npm run dev` / `npm run build` for the AccredaOne frontend scaffold, subject to the restrictions below.

---

## Conditions of acceptance (mandatory)

1. **No public production deployment** using the affected dependency tree while this exception is in effect without reassessment and replacement remediation or an updated acceptance record.
2. **No user-uploaded or externally supplied CSS** processed through PostCSS without prior security reassessment and PO approval.
3. **Do not apply npm overrides** or force PostCSS dependency changes **under this approval**.
4. **Do not upgrade to Next.js 16** under this approval.
5. **Keep Next.js 15.5.28** and **React 19.3.1** unchanged under this approval (unless a separate PO decision supersedes this record).
6. **Continue to report** residual `npm audit` findings accurately in delivery and security documentation.
7. **Reassess** this risk before any public deployment, CI/CD release pipeline, external CSS ingestion, or material frontend architecture change.

---

## Review conditions

| Trigger | Action |
| --- | --- |
| Official **Next.js 15** security backport that bumps **PostCSS** beyond vulnerable ranges | Review for remediation; end or replace this acceptance when patched tree is adopted |
| Any **public deployment** or **externally supplied CSS** | Mandatory reassessment **before** proceeding |
| **Periodic review deadline** | **2 December 2026** — confirm exception still valid or close with remediation |
| Renewal | **Temporary** — does **not** automatically renew |

---

## Explicit distinctions

| Statement | Status |
| --- | --- |
| Vulnerable **PostCSS 8.4.31** transitive dependency | **Confirmed** (version within published advisory ranges) |
| Practical exploitability in **current scaffold** | **Conditional**; assessed **low** given developer-only CSS and no untrusted CSS input path |
| This record | **Risk acceptance**, **not remediation** |
| `npm audit` PostCSS-related findings | **Remain open** |
| Development-only **minimatch / ESLint** advisories | **Separate findings** — not covered by this acceptance |
| Production security / production-ready | **Not implied** |

The application is **not** described as vulnerability-free or production-ready.

---

## Programme context (unchanged)

| Control | Status |
| --- | --- |
| G1 — Programme readiness gate | **NOT PASSED** |
| Phase 1 | **NOT AUTHORIZED** |
| Source rights | **NOT VERIFIED** |
| DEV-02 development baseline | **PARTIAL** (VPS Postgres not deployed) |
| DEV-03 scaffolding | **SCAFFOLDING COMPLETE**, subject to this documented dependency exception |

---

## Traceability

- Frontend scaffold: [../delivery/DEV-03-REVERSIBLE-SCAFFOLDING-REPORT.md](../delivery/DEV-03-REVERSIBLE-SCAFFOLDING-REPORT.md)
- Security index: [README.md](README.md)
