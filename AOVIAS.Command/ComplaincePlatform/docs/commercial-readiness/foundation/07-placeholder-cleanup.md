# 07 — Placeholder Cleanup

**Date:** 26 July 2026

---

## Removed / Neutralized

| Item | Action |
|------|--------|
| WeatherForecast (Audit, Identity) | Deleted |
| Sarvam live API key | Cleared from committed base config |
| `LlmDraftContentGenerator` NotImplementedException selection risk | `CanGenerate` → false |
| AI autonomy enabled flags | Forced false |
| Control Center Capas orphan reads | Replaced with deficiency SSOT |

---

## Tracked Deferred Placeholders (Backlog — not silent)

| Item | Backlog ID | Notes |
|------|------------|-------|
| DefaultLLMAdapter / LLMExplanationAdapter | AI-0 | Template/synthetic |
| MockAIService (KnowledgeBase) | AI-0 | DI mock |
| ConsoleEmailService | CRP-006 | Real SMTP required |
| StubChannelProvider / StubConnectorProvider | P2 | Integration/comm stubs |
| Journey policies NABL/Pollution/Fire/Default | CRP-024 | Allow-all |
| SurveillanceDue = false | CRP-011 | Hide or implement |
| Evidence binder AI empty | CRP-021 | Hide AI affordance or wire |
| Gateway password expire/bulk placeholder handlers | CRP-061 | Implement or remove |
| ControlsController placeholder controls | CRP-060 | |
| CCC / Surveillance / Audit Simulation UI | CRP-012/013/011 | Nav hygiene (WebApp) |

---

## Policy Going Forward

1. No new `NotImplementedException` on request paths.  
2. Deferred work must reference a CRP / backlog ID in code comment.  
3. Fake providers allowed only when named `*Stub*` / `*Console*` and registered solely in Development (harden later).  
4. Do not ship Coming Soon nav for Critical healthcare claims.

---

## Search Hygiene

Full-repo TODO/FIXME sweep completed for Phase 0 prioritization. Remaining items are catalogued above — **not deleted blindly** where they represent intentional extension points.
