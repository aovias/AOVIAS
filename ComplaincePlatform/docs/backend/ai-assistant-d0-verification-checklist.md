## PRAMAN AI Assistant – D0 Verification Checklist

This checklist defines how to verify the AI Assistant pipeline end‑to‑end after changes, with a focus on healthcare concepts like **NABH** and acronym handling.

### 1. Prerequisites

- **Runtime**: .NET SDK installed (8.x or later), Node.js for WebApp where UI checks are performed.
- **Data**: NABH frameworks and knowledge items seeded in Compliance and KnowledgeBase DBs (run existing NABH seed/verify scripts as per `seed-data` docs).
- **Services**: `ComplianceService`, `KnowledgeBase`, `Gateway`, and `AIOrchestratorService` buildable and startable in the chosen DEV/CI environment.

### 2. Backend Automated Suites

- **D0 Runtime Verifier (AI Orchestrator)**
  - **Command**:

    ```bash
    cd ComplaincePlatform
    dotnet test tests/D0RuntimeVerifier/D0RuntimeVerifier.csproj -c Release
    ```

  - **Expected result**: All scenarios report `PASS` in the summary:
    - `invalid-plan-missing-dependency`
    - `invalid-plan-forbidden-tool`
    - `context-version-mismatch`
    - `idempotency-under-retry`
    - `compensation-on-step2-failure`
    - `failure-classification-matrix`
    - `approval-state-machine`

- **KnowledgeBase Integration (NABH framework + ask pipeline)**
  - **Command**:

    ```bash
    cd ComplaincePlatform
    dotnet test Knowledgebase.Tests/Knowledgebase.Tests.csproj -c Release
    ```

  - **Expected result**:
    - NABH framework (`code = "NABH_HOSP_6TH"`, `name = "NABH"`) is returned by the frameworks API.
    - `Ai_ask` scenarios succeed and persist interactions without external errors.

### 3. AI Assistant Retrieval & Fallback Scenarios

- **3.1 NABH / acronym query via AI Orchestrator API**
  - With backend services running and NABH data seeded:
    - Obtain a canonical NABH framework id via the gateway knowledge‑base `frameworks` endpoint.
    - Issue a `POST` to `AIOrchestratorService`:

      ```bash
      curl -X POST "https://{orchestrator-host}/api/v1.0/ai/read/query" \
        -H "Content-Type: application/json" \
        -H "X-Hospital-Id: {hospitalId}" \
        -d '{
          "prompt": "NABH",
          "useCase": 2,
          "hospitalId": "{hospitalId}",
          "frameworkId": "{nabhFrameworkId}"
        }'
      ```

  - **Pass criteria**:
    - `sourceReferences` is non‑empty and contains `KnowledgeBase` or NABH‑related sources.
    - `relevanceScore` in the retrieval context is high enough to avoid generic “no information found in provided context” fallbacks.
    - `complianceHint` text is healthcare‑aware (NABH‑specific) rather than a generic system fallback.
  - `knowledgeHighlights` fact should surface the same NABH-focused summaries that feed the `KnowledgeBase` sources so the UI can flag the concept without hitting a fallback.

- **3.2 KnowledgeBase search path for general healthcare queries**
  - With backend services running and NABH knowledge seeded:
    - Issue a `POST` to `KnowledgeBase` `ai/ask` or `AIOrchestrator` `read/query` **without** `nodeId` but with a precise healthcare term (e.g., `"prompt": "NABH"`, `"frameworkId": "{nabhFrameworkId}"`).
    - Confirm the orchestrator still surfaces `sourceReferences` with type `KnowledgeBase`, and the `KnowledgeBase` `SearchAsync` call delivered at least one `KnowledgeItem` that matches the query even when no node context is provided.
  - **Pass criteria**:
    - Knowledge search results are non-empty, case-insensitively matching `NABH`, and ordered by version (the response can refer to the node only via the `KnowledgeBase` source entry).
    - Retrieval context facts/sources are populated so the AI response avoids “no information found” fallbacks and presents healthcare-aware details (NABH, accreditation context) even for high-level prompts.

- **3.3 Empty-context and graceful fallback behavior**
  - Using variants of short queries and missing framework/hospital ids, verify that:
    - Responses still contain professional, user‑friendly fallback messaging.
    - `sourceReferences` and `facts` are either meaningfully populated or clearly marked as limited, without exposing raw internal error messages.

### 4. Frontend AI Assistant UI Verification

- **WebApp smoke check for NABH query**
  - Start the Angular WebApp as per `WebApp/README.md` (e.g., `npm install && npm start`).
  - In the sidebar AI Assistant, issue queries like **“NABH”**, **“What is NABH accreditation?”**.
  - **Pass criteria**:
    - Answer is healthcare‑aware, referencing NABH frameworks or accreditation context.
    - User does **not** see raw retrieval failure text such as “No information found in provided context” or empty source lists.
    - Confidence indicators and any source metadata render correctly in the UI for both strong and degraded retrieval states.

### 5. Failure Handling and Child Issues

- For any failing D0 or integration scenario:
  - Capture: scenario name, request payload, retrieval diagnostics (facts, sources, relevance), confidence scores, backend logs, and UI behavior (if applicable).
  - Create a **child issue per failing scenario**, classifying the root cause into:
    - query preprocessing / acronym handling
    - vector or keyword retrieval
    - ranking / context assembly
    - fallback orchestration
    - frontend rendering
    - response contract / confidence / source‑mapping behavior

### 6. Post‑Verification

- When all scenarios pass:
  - Update PRA‑22 with “D0 verification PASSED” and reference the concrete runs (CI build id or local command output).
  - Record any newly covered healthcare retrieval behaviors (e.g., NABH acronym intelligence) in the relevant backend/frontend feature docs if their contracts or UX changed.
