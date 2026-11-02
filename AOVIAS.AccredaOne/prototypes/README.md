# Prototypes

This folder is for **controlled experiments** within the **synthetic-data boundary** only. A prototype is not production software and does not authorize production implementation.

Phase 1 is **not authorized**. The **first development slice** is **not approved**. Canonical **technology and architecture directions** are approved in Confluence **within each ADR's scope**; see [../GOVERNANCE-STATUS.md](../GOVERNANCE-STATUS.md) and [../docs/traceability/CONFLUENCE-INDEX.md](../docs/traceability/CONFLUENCE-INDEX.md). **D3** architecture reconciliation remains **not approved**.

Prototype work does **not** authorize real accreditation source content, production authentication, persistence, ingestion, search, scoring, deployment, or promotion into `application/`.

## Control rules

A prototype may be added only when all of the following are true:

1. A human assignment asks for the experiment.
2. The prototype directory contains a brief that states the question, the non-goals, the disposal condition, and the date.
3. The brief cites `DEC`, `REQ`, and Confluence `ADR` identifiers when it relies on them, consistent with each record's canonical status. The brief must not treat an experiment as establishing product scope or implementation authorization.
4. The prototype uses **synthetic data only** within the approved controlled-prototype boundary. It does not include unverified or protected accreditation content.
5. The prototype does not implement production authentication, persistence, ingestion, search, scoring, or deployment.
6. The prototype does not copy AOVIAS Command code, schemas, or content.
7. The prototype does not add dependencies to `application/`.

## Promotion

Nothing in this folder is promoted into `application/` while Phase 1 is not authorized. A promising prototype does not approve implementation or source rights. If a result should be kept, write a `Draft` local `DEC` or follow Confluence ADR process; do not treat drafts as approved authority.

## Layout

```text
prototypes/
  <short-name>/
    BRIEF.md
    ...experiment files
```

No prototypes are present.
