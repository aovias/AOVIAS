# ADR-006: Human Approval Before Copilot Execution

**Status:** Accepted  
**Date:** 2026-06-24 (Sprint 13D)

## Context

AI-assisted actions in healthcare compliance can change tasks, CAPA, or documentation. Autonomous execution creates accountability and safety risks.

## Decision

All Copilot-driven operational mutations flow through `CopilotActionProposal`: create → approve/reject → **execute** via `ProposalExecutionService`.

## Alternatives

- Auto-execute high-confidence recommendations — **rejected**.
- LLM tool calling without proposal — **rejected**.

## Consequences

- Extra UX steps for users.
- Clear audit trail (`CopilotProposalAuditEvents`).
- Aligns with [06-Business-Rules.md](../06-Business-Rules.md) AI trust model.
