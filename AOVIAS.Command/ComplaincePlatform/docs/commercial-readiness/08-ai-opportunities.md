# 08 — AI Opportunities Roadmap

**Program:** CRP v1.0  
**Assessment Date:** 26 July 2026  
**Rule:** Map where AI *should* exist vs what *actually* exists. Do not credit placeholders as capability.

---

## 1. AI Reality Check

| Layer | Status |
|-------|--------|
| AIOrchestratorService | Exists (profiles, drafts, assist, feature flags) |
| Copilot module (Compliance) | Exists (recommendations, proposals, personas, analytics) |
| Gateway AI proxies + AIFeatureFlags | Exists |
| WebApp Copilot + AI sidebar + AI admin | Exists |
| Real LLM production adapter | **Placeholder / incomplete** |
| `LlmDraftContentGenerator` | **`NotImplementedException`** |
| Default LLM adapter | Synthetic/template responses |
| Evidence binder AI suggestions | Empty until integration |
| Autonomous workflows flags | Present but should remain **off** for hospitals |

**AI production readiness: ~2.5 / 10**  
**AI demo readiness (scripted): ~5 / 10**

---

## 2. Opportunity Catalogue

### AO-01 Evidence Review Assist

| | |
|--|--|
| **Hospital value** | Speed QM/assessor review; flag missing attributes |
| **Exists today** | Review queue human workflow; binder AI empty |
| **Recommended AI** | Classify evidence type; check completeness vs OE requirement; suggest accept/reject rationale |
| **Data prerequisites** | OE requirement text, evidence metadata, historical decisions |
| **Risk** | False accept — must be advisory only |
| **Priority** | P0 for product differentiation |
| **Complexity** | M |
| **Sprint** | AI-1 |

### AO-02 Compliance Gap Suggestions

| | |
|--|--|
| **Hospital value** | After onboarding assessment, prioritized remediation plan |
| **Exists today** | Gap analysis from KnowledgeBase assessments |
| **Recommended AI** | Natural-language explanation of gaps; suggested owners/departments; effort estimates |
| **Priority** | P1 |
| **Complexity** | M |
| **Sprint** | AI-2 |

### AO-03 CAPA Suggestions

| | |
|--|--|
| **Hospital value** | Better RCA quality; SMART actions |
| **Exists today** | Deficiency CAPA fields; copilot create CAPA weak/non-SSOT |
| **Recommended AI** | Suggest root causes, actions, effectiveness checks from finding text + similar past CAPAs |
| **Prerequisite** | CAPA SSOT (WF-07) |
| **Priority** | P0 |
| **Complexity** | M |
| **Sprint** | AI-2 (after CAPA unify) |

### AO-04 Document Classification & Mapping

| | |
|--|--|
| **Hospital value** | Auto-tag policies/SOPs to NABH OEs |
| **Exists today** | Manual OE mapping; authoring studio; graph links |
| **Recommended AI** | Classify doc type; propose OE links; detect obsolete references |
| **Priority** | P1 |
| **Complexity** | M–L |
| **Sprint** | AI-3 |

### AO-05 Hospital Readiness Narrative

| | |
|--|--|
| **Hospital value** | Board-ready readiness story from scores/open CAPAs/docs |
| **Exists today** | Readiness reports + dashboards (deterministic) |
| **Recommended AI** | Executive summary generation with citations to live metrics |
| **Priority** | P1 |
| **Complexity** | S–M |
| **Sprint** | AI-2 |

### AO-06 Chat Assistant (In-app)

| | |
|--|--|
| **Hospital value** | “Where is evidence for AAC.3.a?” guided navigation |
| **Exists today** | AI sidebar `/ai/query`, `/ai/assist` |
| **Recommended AI** | Retrieval-augmented over KB + hospital graph; tool-calling into safe read APIs |
| **Risk** | Hallucination over compliance — require citations |
| **Priority** | P0 |
| **Complexity** | L |
| **Sprint** | AI-1 |

### AO-07 Knowledge Search (Enterprise)

| | |
|--|--|
| **Hospital value** | Find people, docs, OEs, incidents in one NL query |
| **Exists today** | Directory search + graph search (separate) |
| **Recommended AI** | NL→structured query with RLS masking |
| **Priority** | P2 |
| **Complexity** | L |
| **Sprint** | AI-4 |

### AO-08 Gap Detection (Continuous)

| | |
|--|--|
| **Hospital value** | Detect drift (expired docs, overdue CAPA, missing evidence) |
| **Exists today** | SLA/escalation, Hangfire snapshots; surveillance stub |
| **Recommended AI** | Anomaly detection on indicators + compliance posture; proactive suggestions (`EnableAIProactive`) |
| **Prerequisite** | Real surveillance engine |
| **Priority** | P1 |
| **Complexity** | L |
| **Sprint** | AI-3 |

### AO-09 Report Generation

| | |
|--|--|
| **Hospital value** | Auto-draft internal audit / management review packs |
| **Exists today** | Deterministic report exports |
| **Recommended AI** | Draft narrative sections with mandatory human approve |
| **Priority** | P2 |
| **Complexity** | M |
| **Sprint** | AI-4 |

### AO-10 Policy Analysis

| | |
|--|--|
| **Hospital value** | Compare hospital SOP vs NABH intent |
| **Exists today** | Controlled docs + requirement text |
| **Recommended AI** | Clause coverage scoring; conflict detection |
| **Priority** | P2 |
| **Complexity** | L |
| **Sprint** | AI-5 |

### AO-11 Incident Clustering / Learning

| | |
|--|--|
| **Hospital value** | Detect systemic themes across incidents |
| **Exists today** | Incidents + lessons; docs mention ML clustering as future |
| **Recommended AI** | Cluster near-misses; suggest system CAPA |
| **Priority** | P3 |
| **Complexity** | L |
| **Sprint** | AI-6 |

### AO-12 Onboarding AI Setup Assistant

| | |
|--|--|
| **Hospital value** | Faster first-time setup |
| **Exists today** | UI “AI Setup Assistant” wizard (mostly deterministic steps) |
| **Recommended AI** | Suggest departments/frameworks from hospital profile; draft invite lists |
| **Priority** | P2 |
| **Complexity** | S |
| **Sprint** | AI-3 |

### AO-13 Copilot Action Proposals (Governed)

| | |
|--|--|
| **Hospital value** | Suggest creating tasks/CAPAs with approval |
| **Exists today** | Copilot proposals/executions scaffolding; autonomy flags |
| **Recommended AI** | Keep human-in-the-loop; never auto-execute in v1 hospitals |
| **Priority** | P1 (governance) |
| **Complexity** | M |
| **Sprint** | AI-1 |

### AO-14 AI Identity / Prompt Governance

| | |
|--|--|
| **Hospital value** | Safe, auditable AI usage |
| **Exists today** | AI admin pages for profiles/prompts/policies/executions |
| **Recommended AI** | Production prompt registry, eval harness, red-team tests |
| **Priority** | P0 (platform) |
| **Complexity** | M |
| **Sprint** | AI-0 (prerequisite) |

---

## 3. What Must NOT Be Sold as AI Today

| Claim | Why forbidden |
|-------|---------------|
| “AI reviews evidence automatically” | Binder suggestions empty; adapters placeholder |
| “AI writes production CAPAs” | CAPA SSOT broken; LLM draft NotImplemented |
| “Autonomous compliance workflows” | Flags exist; unsafe without governance + accuracy |
| “AI replaces NABH consultant” | No validated clinical/compliance model |

---

## 4. AI Architecture Recommendations

1. **Replace placeholder adapters** with one production provider (Azure OpenAI / approved Indian region LLM) behind `IILMAdapter`.  
2. **Keep AIFeatureFlags defaults** for hospitals: Read/Assist **on** (after quality bar); Action/Proactive/Autonomous **off**.  
3. **Mandatory citation** for any compliance answer (OE id, doc id, evidence id).  
4. **Human approval** for any write proposal (task/CAPA/doc).  
5. **Eval harness** in CI: golden questions for NABH OE lookup + evidence completeness.  
6. **PII scrubbing** before LLM calls (patient safety narratives).  
7. **Per-tenant AI keys / quotas** tied to subscription plans (links to licensing).  
8. **Audit every AI invocation** (already partly modeled — enforce retention).  

---

## 5. Phased AI Roadmap

### AI-0 — Foundation (blocker)

- Real LLM adapter  
- Remove NotImplemented generators  
- Secrets in Key Vault  
- Prompt/policy governance hard gates  
- Disable autonomy flags in prod config  

### AI-1 — Assistive Read (demo + pilot)

- Chat with citations over KB + hospital graph (read APIs)  
- Evidence completeness hints  
- Copilot proposals (create task) with approve  

### AI-2 — Quality Narratives

- Gap explanation  
- CAPA drafting assist (after CAPA SSOT)  
- Readiness executive summary  

### AI-3 — Document & Drift

- Doc→OE mapping suggestions  
- Continuous gap detection (with surveillance engine)  
- Onboarding suggestions  

### AI-4 — Search & Reports

- Unified NL search  
- Audit/MR draft packs  

### AI-5+ — Advanced

- Policy analysis  
- Incident clustering  
- Forecasting with Workforce Intelligence  

---

## 6. Success Metrics (AI)

| Metric | Pilot target |
|--------|--------------|
| Citation accuracy | ≥ 95% |
| Evidence hint precision | ≥ 80% |
| QM time-to-review evidence | −30% |
| Hallucination incidents (critical) | 0 |
| Autonomous writes without approval | 0 |

---

## 7. Dependency Map

```
AI-0 Foundation
   ├─► AI-1 Chat + Evidence hints
   │      └─► AI-2 CAPA/Gap/Readiness narratives
   ├─► CAPA SSOT (non-AI engineering)
   ├─► Surveillance engine (non-AI)
   └─► Licensing quotas for AI usage
```

---

## 8. Closing Statement (AI)

AOVIAS Command has an **AI product surface** (services, flags, admin, copilot UI) but not yet an **AI production capability**.  

The commercial path is: **governed assistive AI with citations**, never autonomous compliance. Treat AI-0/AI-1 as the only honest near-term roadmap.
