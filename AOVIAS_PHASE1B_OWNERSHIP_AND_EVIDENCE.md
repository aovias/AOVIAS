# AOVIAS Phase 1B — Ownership Decisions and Evidence

**Date:** 2026-10-25  
**Baseline HEAD (verified):** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Scope:** Planning decisions only — **does not authorize file moves**

**Instruction basis:** User-directed Phase 1B planning decisions for O-1 and O-2 (Studio-initial placement).

---

## 1. Safety gate (mandatory)

| Worktree | Branch | HEAD | Status |
|----------|--------|------|--------|
| Main | `main` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | Tracked clean; untracked Phase reports, website ZIPs, Lighthouse dirs |
| Cursor | `agent/cursor/active` | `5a1526d…` | `?? scripts/dev/_phase3h_runtime_probe.py` |
| Claude | `agent/claude/active` | `5a1526d…` | `?? Aovias.CommandCenter/src/Aovias.CommandCenter.Api/Properties/` |

**Verified:** Matches expected gate. No probe execution. No Git writes in Phase 1B.

---

## 2. Decision O-1 — `Aovias.CommandCenter`

| Field | Value |
|-------|--------|
| **Decision ID** | O-1 |
| **Decision** | Assign **`Aovias.CommandCenter`** to **`AOVIAS.Studio`** initially |
| **Status** | **APPROVED PLANNING DECISION** (Phase 1B) — not implementation authorization |
| **Physical destination (eventual)** | `AOVIAS.Studio/Aovias.CommandCenter/` |
| **Implementation authorization** | **Still required** (Phase 1C+ human gate) |

### Evidence supporting Studio ownership (verified)

| Evidence | Source | Finding |
|----------|--------|---------|
| Solution membership | `Aovias.Platform.slnx` | Includes `Aovias.CommandCenter.Api` and `.Infrastructure` under `/Aovias.CommandCenter/` folder |
| Dedicated solution | `Aovias.CommandCenter/Aovias.CommandCenter.slnx` | Two first-party projects + transitive view of `Aovias.Configuration`, `Aovias.Deployment.Contracts`, Release contracts/SDK |
| Runtime role | `Aovias.CommandCenter.Infrastructure.csproj` `<Description>` | **"Operations Command Center: Fleet Registry, Runtime Agent polling, and fleet-wide aggregation"** |
| Service behavior | `FleetStatusService.cs` | Fleet health/drift/reconciliation/metrics; **communicates only through Runtime Agent APIs** — not PRAMAN Gateway or ComplianceService |
| Host independence | `Aovias.CommandCenter.Api/Program.cs` | Standalone ASP.NET host; explicitly **not** wired into Runtime Agent bootstrap pipeline |
| Project references | `Aovias.CommandCenter.Infrastructure.csproj` | **`Aovias.Configuration`**, **`Aovias.Release.Contracts`**, **`Aovias.Release.SDK`** — all Studio stack |
| Command decoupling | `ComplaincePlatform/**/*.csproj` grep | **No** `ProjectReference` to `Aovias.CommandCenter` or root `Aovias.*` |
| Consumer (test) | `Aovias.DeploymentValidation.Tests` | References **`Aovias.CommandCenter.Infrastructure`** for fleet/environment tests — Studio validation leg |
| Deployable | `Program.cs` + API controllers | Executable **`Aovias.CommandCenter.Api`** (Swagger in Development) |
| Docker / Compose | Repository search | **No** Dockerfile or Compose service for CommandCenter at HEAD |
| CI | `.github/workflows` | **No** dedicated CommandCenter workflow; covered indirectly via Platform slnx build (historical **PASS** in Phase 0B) |

### Naming disambiguation (required documentation)

| Name | Product | Mechanism |
|------|---------|-----------|
| **`Aovias.CommandCenter`** (repo folder) | **AOVIAS Studio** — fleet / environment operations API | .NET service `Aovias.CommandCenter.Api` |
| **Deficiency Command Center** | **AOVIAS Command (PRAMAN)** — audit/compliance UX | `WebApp/.../deficiency-command-center/`; routes like `/app/audit/deficiency-command-center` |
| **Compliance Command Center** | **PRAMAN WebApp** UI pattern | `WebApp/.../compliance-command-center.component.ts` |

**Verified:** No code import ties `Aovias.CommandCenter` to PRAMAN deficiency UI; collision is **terminology only**.

### Alternatives considered

| Alternative | Rejected for initial placement | Reason |
|-------------|-------------------------------|--------|
| `AOVIAS.Infrastructure` | Deferred | Service is product-facing ops API with Release/Runtime coupling, not static templates |
| `AOVIAS.Command` | Rejected | No MSBuild or runtime dependency on Command platform; different domain (fleet vs compliance SSOT) |
| Rename in Phase 1B | Out of scope | Requires separate reviewed rename (e.g. `Aovias.FleetCenter`) |

### Consequences

- Moves with **Studio wave** (W3 in Phase 1A proposal): update Platform slnx paths, CommandCenter slnx relative refs to Configuration/Release.
- Documentation and onboarding must **always qualify** "Command Center" with product name.
- DeploymentValidation tests remain co-located with Studio validation.

### Reversal conditions

- Discovery of **production dependency from `ComplaincePlatform` services** on CommandCenter API (none found at HEAD).
- Product decision to merge fleet ops into Runtime Agent host (would collapse projects — separate ADR).
- Human decision to rename/disambiguate before move (recommended long-term, not blocking planning).

### Contradictory findings

**None sufficient to trigger `REQUIRES HUMAN RECONSIDERATION`.**  
`Program.cs` calls it an "independent product area" — consistent with **Studio-side** ops product, not PRAMAN Command.

---

## 3. Decision O-2 — `Aovias.Configuration` and `Aovias.Deployment.Contracts`

| Field | Value |
|-------|--------|
| **Decision ID** | O-2 |
| **Decision** | Both **`Aovias.Configuration`** and **`Aovias.Deployment.Contracts`** → **`AOVIAS.Studio`** initially |
| **Status** | **APPROVED PLANNING DECISION** (Phase 1B) |
| **Physical destination (eventual)** | `AOVIAS.Studio/Aovias.Configuration/`, `AOVIAS.Studio/Aovias.Deployment.Contracts/` |
| **`AOVIAS.Shared` promotion** | **Not authorized** — requires cross-product consumers + versioning strategy |
| **Implementation authorization** | **Still required** |

### `Aovias.Configuration` — consumers (verified via ProjectReference grep)

| Consumer area | Projects |
|---------------|----------|
| Publishing | `Aovias.Publishing.Core`, tests indirectly |
| Runtime Agent | `Application`, `Infrastructure`, `SDK`, infrastructure tests |
| Release | `ReleaseManager`, `PackageBuilder`, `Release.SDK` |
| Bootstrap | `Bootstrap.Core`, `Bootstrap.Host`, `Bootstrap.Core.Tests` |
| Orchestration | `Aovias.Orchestration` |
| CSM | `ComplianceServiceManager.csproj` |
| CommandCenter | `Aovias.CommandCenter.Infrastructure` |
| DeploymentValidation | `Aovias.DeploymentValidation` |
| Tools | `PublishAllServices`, `PublishRuntimeAgent` |

**Verified:** **Zero** references from `ComplaincePlatform/**/*.csproj`.

### `Aovias.Deployment.Contracts` — consumers (verified)

| Consumer | Project |
|----------|---------|
| Publishing | `Publishing.Core`, `Publishing.Contracts` |
| Runtime Agent | `Runtime.Agent.Infrastructure` (+ tests) |
| Bootstrap | `Bootstrap.Core` |
| Release | `Release.Contracts` |
| DeploymentValidation | `Aovias.DeploymentValidation` |

**Verified:** **Zero** references from `ComplaincePlatform/**/*.csproj`.

### Responsibilities (inference from usage)

| Library | Role |
|---------|------|
| `Aovias.Configuration` | Settings binding, deployment path resolution, publishing/deployment profile resolution |
| `Aovias.Deployment.Contracts` | Deployment package schemas, admission/planning contracts consumed by Publishing and Runtime |

### Alternatives considered

| Alternative | Status |
|-------------|--------|
| Immediate move to `AOVIAS.Shared` | **Rejected** — no Command product consumer; would imply false "shared product" boundary |
| Split Configuration (Command vs Studio) | **Rejected** — no Command refs today |
| Leave at repo root permanently | **Rejected for target layout** — breaks product-folder goal; Studio-initial is stepping stone |

### Consequences

- Studio folder move updates **many** relative `ProjectReference` paths across Publishing, Agent, Bootstrap, CSM, tools.
- Docker COPY paths and `RuntimeAgentDockerContextStager` folder lists must include new prefixes in same wave as Studio tree.
- Future **Shared promotion** needs: documented Command/Website/AccredaOne consumer, neutral naming, semver policy.

### Reversal conditions

- New **Command or Website csproj** references either library (would trigger Shared reconsideration).
- Extraction of deployment contracts as **public AccredaOne boundary** (separate program — not current state).

### Contradictory findings

**None.** Reference graph **supports** Studio-initial placement.  
**Note:** `Aovias.CommandCenter.slnx` lists `Aovias.Deployment.Contracts` for solution visibility, but **`Aovias.CommandCenter.Infrastructure.csproj` does not reference Deployment.Contracts directly** — slnx includes it for developer convenience (verified slnx vs csproj).

---

## 4. Traceability summary

| ID | Status | Changes physical destination? | Move authorized? |
|----|--------|--------------------------------|------------------|
| O-1 | Approved planning | Yes — under `AOVIAS.Studio/` | **No** |
| O-2 | Approved planning | Yes — under `AOVIAS.Studio/` | **No** |

---

## 5. Remaining ownership items (not decided in Phase 1B)

Per Phase 1A: O-3 (`DeploymentValidation`), O-4 (`Runtime.Contracts`), O-5 (publish tools), O-6 (`AOVIAS.Shared` scope), O-7 (website path), O-8 (`preflight-docker-probe/`). See migration contract for provisional defaults.

---

*Planning artifact. Phase 1A deliverables preserved unchanged.*
