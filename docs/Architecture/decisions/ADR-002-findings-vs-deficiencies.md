# ADR-002: Findings Separate from Deficiencies

**Status:** Accepted  
**Sprint:** 7A / 7B

## Context

Teams need lightweight observation tracking during internal QA without immediately creating governed deficiency records.

## Decision

`Finding` aggregate in findings workspace; `ComplianceDeficiency` as separate lifecycle with explicit **promotion** API.

## Rationale

Separates exploratory observations from accreditation-grade deficiency records with RCA and closure governance.

## References

- Sprint 7B Finding Promotion Report
- [13-Workflow-Architecture.md](../13-Workflow-Architecture.md)
