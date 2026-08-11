# AI Guide — AOVIAS Command v1.0.0

**Entitlements:** `ai.assist`, `ace.insights`  
**Audience:** Quality managers, consultants, AI operators

## Principles

1. **Advisory only** — AI never transitions journeys or closes CAPAs.
2. **Grounded** — Prefer knowledge/RAG and tenant context over free hallucination.
3. **Auditable** — Prompts/outputs subject to platform observability and hospital policy.
4. **No PHI in prompts** — Strip patient identifiers before asking the assistant.

## Capabilities by entitlement

### `ai.assist`

- Grounded assistant for role personas
- Evidence reviewer cues
- Knowledge retrieval against hospital/platform knowledge bases
- Drafting aids that still require human approval cards/workflows

### `ace.insights`

- Analytics / predictive readiness signals
- Insight surfaces for leadership and QM
- Soft-gated by license middleware when routes require the entitlement

## Operator checklist

- [ ] License includes AI entitlements before enabling UI features
- [ ] Model/orchestration config points to approved providers (Phase 5 AIL)
- [ ] Prompt management and evaluation harness used for regressions
- [ ] Users trained: AI suggestions are not accreditation proof

## Governance

- Hospitals should document AI use in their quality manual or AI policy addendum.
- Rejected AI drafts must not be treated as controlled documents until human-approved into QDMS.
- Disable AI entitlements on the license if a tenant pauses AI usage—do not rely on UI hide alone.

## Related

- [architecture-guide.md](./architecture-guide.md)
- [user-guide.md](./user-guide.md)
