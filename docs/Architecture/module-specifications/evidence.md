# Module Specification: Evidence

[← Module Catalog](../04-Module-Catalog.md)

**Implementation status:** **Complete** (~90%)  
**Routes:** `/app/evidence`, `/app/evidence/review-queue`, `/app/evidence/binder`

## Purpose

Collect, store, verify, and present compliance evidence linked to tasks and objective elements.

## Owns

- `Evidence`, versions, library views
- Verification workflow (review queue)

## Does NOT Own

- Task assignment (Tasks module)
- Scoring formulas (scoring services read evidence status)

## Business Rules

- Verification authority in evidence module ([06-Business-Rules.md](../06-Business-Rules.md))
- Binder is presentation; uploads via library/tasks

## Key APIs

- `/evidence/*`, `/evidence-binder/*`

## Store

- `EvidenceBinderStore` for binder page
