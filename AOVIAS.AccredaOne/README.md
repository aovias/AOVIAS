# AOVIAS.AccredaOne

**Status:** Scaffold only — no runtime, no public API, no service implementation in this directory.

---

## Product purpose

AccredaOne is intended as a **separate public discovery platform** for standards-aware content, search, and read-oriented experiences aimed at external audiences. It is distinct from the PRAMAN operator portal and from internal compliance operations tooling.

This folder establishes **product ownership and architectural boundary** in the repository layout. It does **not** deliver product functionality.

---

## Ownership boundary

| Area | Owner | Notes |
|------|--------|--------|
| **AOVIAS.AccredaOne** (this tree) | AccredaOne product | Public discovery surface (future) |
| **ComplaincePlatform/KnowledgeBase** | **AOVIAS Command** | Internal SSOT; authenticated Gateway usage today |

AccredaOne must **not** depend on direct calls to internal Command microservice APIs or ungoverned access to Command databases. KnowledgeBase remains Command-owned until an explicit, reviewed publishing boundary exists.

---

## Integration principle

When integration is implemented (future phases), AccredaOne should consume content through an **intentional public publishing or API boundary** — for example a BFF, projection layer, or published artifact pipeline — not by reusing internal Command contracts or tenant-scoped operational endpoints.

No such boundary is defined or implemented by this scaffold.

---

## Publication lifecycle — open questions (future decisions)

Human and product decisions still required before any public consumption model:

1. **Ownership** — who authors, curates, and approves public-facing material versus internal KnowledgeBase records.
2. **Review and approval** — workflow states, roles, and escalation before content becomes public.
3. **Publication status** — draft, scheduled, live, retired; visibility rules.
4. **Provenance** — source citations, regulatory disclaimers, and traceability to authoritative internal records.
5. **Versioning** — immutable published versions vs rolling updates; cache and CDN invalidation.
6. **Removal and retention** — takedown, legal hold, and archival policy for public URLs.
7. **Public URL stability** — slug strategy, redirects, and breaking-change policy for SEO and external links.

These items are **not** resolved in the scaffold phase.

---

## Architecture status

- **No** application code, projects, solutions, Docker images, or CI workflows in this directory.
- **No** public HTTP API, GraphQL schema, or authentication model.
- **No** dependency on or relocation of `ComplaincePlatform/KnowledgeBase`.

See `ACCREDAONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md` at the repository root for background on KnowledgeBase vs a future public read model (assessment only; not a commitment to a specific design).

---

## Repository layout contract

The machine-readable contract is `repository-layout.json` at the repository root.

AccredaOne is recorded there as a product with **scaffold** migration status. Path resolvers for Command, Studio, and Publishing are **unchanged** by this folder; production discovery continues to use the **current** layout profile only.

Unresolved contract items that still apply to AccredaOne (examples): public API shape, BFF ownership, and whether any future shared libraries belong in `AOVIAS.Shared` under a defined versioning strategy.

---

## What this scaffold does not authorize

- Physical migration of `aovias-config`, Command, Studio, or Website trees.
- Moving or copying KnowledgeBase into this directory.
- Enabling target-layout paths in production resolvers.
- Declaring that an AccredaOne API or service is available for use.

Physical migration waves require separate human review per Phase 1B/1C/1D migration contracts.
