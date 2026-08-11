# KB-004 — AI Answers and Citations

## Policy (share with customers)

AOVIAS AI assist is **advisory**. Accreditation decisions require human review. Answers should be grounded with citations when RAG / knowledge retrieval is used.

## Symptoms

- Assistant answers without sources  
- Citations point to wrong OE / document  
- AI features missing entirely  
- User expects autonomous workflow changes  

## Checks

1. **Entitlement:** `ai.assist` (and `ace.insights` if ACE) via  
   `GET /api/v1/licenses/entitlements/check?organizationId=&featureCode=ai.assist`  
2. Confirm AIOrchestrator service healthy; Gateway AI proxies not 503.  
3. Knowledge / RAG: ensure hospital knowledge content exists for the question domain.  
4. Autonomous / agentic workflow flags must remain **off** unless explicitly approved (Phase 0 safety posture).  
5. Ask user for Correlation ID from the AI request.

## Guidance for users

- Prefer questions scoped to a framework / OE / document.  
- Open cited sources before changing evidence or CAPA.  
- Do not paste PHI into prompts unless customer policy allows and environment is approved.  
- If citation missing, treat answer as untrusted and escalate to QM.

## Escalate when

- Systematic hallucination with fabricated document IDs  
- Prompt injection / jailbreak attempts succeeding  
- Model provider outage  

Collect diagnostics pack + AI request Correlation ID.
