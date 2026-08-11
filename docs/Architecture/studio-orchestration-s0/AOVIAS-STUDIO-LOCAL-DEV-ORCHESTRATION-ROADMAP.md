# AOVIAS Studio — Local Development Orchestration
## Implementation Roadmap (S0)

Assessment HEAD: `9f488ab`  
Implementation in S0: **NONE**

---

## Assumed vs derived dependency order

Assumed:

```
D1 → D2 → D3/D4 → Session orchestration → D5
```

**Derived (recommended):**

```
S0 Assessment (done)
  → S1 D1 Workspace Management (read-mostly)
      → S2 D2 Runtime Profiles (extends Local Dev Services)
          → S3 Resource Awareness foundation (local host metrics + Start All safety)
              → S4 D3 Local PR  ┐
              → S5 D4 Agent Sessions ┘ (parallel-safe after S1 models exist; both need S2 for “run profile with session”)
                  → S6 Development Session Orchestration (binds D1–D4)
                      → S7 D5 Intelligent Resource Management (auto profile selection)
```

### Why this differs slightly from the assumed chain
- **D3 does not require D2** for branch compare/diff/metadata, but merge-ready validation benefits from profile-based builds → schedule D3 after S2/S3.
- **D4 needs D1** (workspace/branch ownership) more than D2; profile attachment can be optional in S5 and completed in S6.
- **S3 before D3/D5** because Start All parallelism and 16 GB RAM are an immediate operational hazard once profiles encourage more starts.
- **D5 last**: recommendations need real local metrics (S3) and profile catalog (S2).

---

## S0 — Architecture / Assessment

| | |
| --- | --- |
| **Objective** | Evidence-based understanding; roadmap; no product feature code |
| **Dependencies** | None |
| **Code areas** | Read-only: `ComplianceServiceManager/**` Local Dev, GitSafety, AI, Overview |
| **UI areas** | Observe Local Dev Services only |
| **Risks** | Misreading Runtime Agents / deployment profiles as Local Dev concepts |
| **Acceptance** | Artifacts written; product code unchanged; Local PR ready |
| **Testing** | Preflight scripts; code path tracing |
| **Owner** | Cursor (this task) |

---

## S1 — D1 Workspace Management

| | |
| --- | --- |
| **Objective** | Studio can list AOVIAS workspaces/worktrees, show branch/HEAD/dirty, map agent ownership, associate solution folders |
| **Dependencies** | S0 |
| **Expected code areas** | NEW `Services/Workspaces/*`, `IGitWorkspaceService`; EXTEND `SolutionFolderRegistry` or parallel `WorkspaceRegistry`; DI in `App.xaml.cs` |
| **Expected UI areas** | Local Development hub → Workspaces section; Overview card optional |
| **Risks** | Linked worktree `.git` file detection; confusing Shell “workspace” naming; accidental write ops |
| **Acceptance** | Read-only Git status for `AOVIAS` / `AOVIAS.Cursor` / `AOVIAS.Claude`; dirty/clean accurate; no merge/reset |
| **Suggested testing** | Unit tests for git output parsing; manual verify three worktrees |
| **Independent owner** | Cursor **or** Claude (low overlap with Local Dev process code if Git service is new files) |

### Minimum changes
- Prefer CLI `git worktree list --porcelain`, `status --short`, `rev-parse`
- Persist workspace registration under `%LocalAppData%\AoviasStudio\workspaces.json`
- Ownership table initially static from path conventions in `AGENTS.md`

---

## S2 — D2 Runtime Profiles

| | |
| --- | --- |
| **Objective** | Named service groups with start/stop order, optional env overlays, workspace association; replace unsafe default Start All |
| **Dependencies** | S1 recommended (workspace association); can stub workspaceId |
| **Expected code areas** | NEW profile store; MODIFY `ServiceManagerService` (`StartProfileAsync`, ordered stop); keep discovery |
| **Expected UI areas** | Profiles section + Services grid “active profile” selector |
| **Risks** | Port collisions across worktrees; drift from launchSettings; over-configuring env |
| **Acceptance** | Create/select profile; start/stop profile; default profiles for UI-only / API-only / minimal; Start All disabled or confirms |
| **Suggested testing** | Profile serialization tests; ordered start unit tests with fakes |
| **Independent owner** | Cursor preferred (owns Local Dev process stack) |

### What Local Dev already provides (reuse)
Discovery, start/stop/restart, ports, health, PID, attach-external, build-before-start, launch env from launchSettings.

### Minimum new pieces
Groups, ordering, persistence, workspace binding, `maxParallelStarts`, shared-infra policy flag.

---

## S3 — Resource Awareness foundation

| | |
| --- | --- |
| **Objective** | Local host CPU/RAM sampling + thresholds; warn before large profile starts; **not** full intelligent selection |
| **Dependencies** | S2 (to know what would start) |
| **Expected code areas** | NEW `LocalHostMetricsService`; wire warnings into profile start |
| **Expected UI areas** | Small meter on Local Development header; confirm dialog when over threshold |
| **Risks** | Confusing with Overview Runtime Agent metrics; sampling overhead |
| **Acceptance** | Show local RAM/CPU; block or warn Start Profile when projected risk high; document shared-infra defaults |
| **Suggested testing** | Metrics provider fake; threshold policy tests |
| **Independent owner** | Claude or Cursor (new files; coordinate UI touch with Cursor if same view) |

---

## S4 — D3 Local PR

| | |
| --- | --- |
| **Objective** | Studio Local PR lifecycle over Git branches: compare, diff summary, validation hooks, approval, controlled merge |
| **Dependencies** | S1 required; S2/S3 recommended for validation via profile |
| **Expected code areas** | NEW `LocalPullRequestService` + JSON store; NEW merge service; **do not** reuse GitSafety rollback |
| **Expected UI areas** | Local PRs section; diff viewer (summary first) |
| **Risks** | Merge safety; concurrent agent branches; destructive Git; secret leakage in diffs |
| **Acceptance** | Create Local PR from task branch → `main`; show ahead/behind + file list; approve; merge `--no-ff` optional; conflict surfaced; audit entry |
| **Suggested testing** | Parse tests; integration against temp git repo |
| **Independent owner** | Claude strong for merge safety design; Cursor for UI — **serialize if same Git service files** |

---

## S5 — D4 Agent Sessions

| | |
| --- | --- |
| **Objective** | First-class Cursor/Claude/Human sessions linked to workspace, task, branch, optional profile/PR |
| **Dependencies** | S1 required; S4 optional link; S2 optional profile |
| **Expected code areas** | NEW session store/models; optional bridge to existing AI Fix session folders |
| **Expected UI areas** | Agent Sessions section; do not overload Runtime Agents |
| **Risks** | Naming collision with Runtime Agents; implying Studio controls Cursor/Claude processes |
| **Acceptance** | Register/view sessions; status lifecycle; link to workspace/branch; no remote agent confusion |
| **Suggested testing** | Serialization + mapping tests from path/branch conventions |
| **Independent owner** | Either agent if confined to new session module |

---

## S6 — Development Session Orchestration

| | |
| --- | --- |
| **Objective** | Bind D1–D4: “Start session” selects workspace + profile + tracks PR readiness |
| **Dependencies** | S1–S5 |
| **Expected code areas** | Application orchestrator; possibly Operation Engine reuse for long runs |
| **Expected UI areas** | Single “Development Session” flow inside Local Development |
| **Risks** | Scope creep; overlapping Operation Engine vs Local Dev |
| **Acceptance** | One click/path: attach session → start profile → refresh Git status → open/create Local PR |
| **Suggested testing** | End-to-end manual script on 16 GB machine |
| **Independent owner** | Prefer single owner (Cursor) to avoid merge contention |

---

## S7 — D5 Intelligent Resource Management

| | |
| --- | --- |
| **Objective** | Recommendations + automatic profile selection from local metrics and session intent |
| **Dependencies** | S2 + S3 (+ S6 preferred) |
| **Expected code areas** | Advisor service; optional ML-free heuristics |
| **Expected UI areas** | Recommendations banner; auto-select toggle (off by default) |
| **Risks** | Over-automation; killing wrong processes; false confidence |
| **Acceptance** | Suggest profile given free RAM; never auto-start without policy; explain why |
| **Suggested testing** | Heuristic unit tests with synthetic metrics |
| **Independent owner** | Either; keep isolated |

---

## UI roadmap (conceptual only)

### Current
```
LOCAL DEVELOPMENT
├── Local Dev Services
│   ├── Solution Folders
│   └── Discovered Services (start/stop/open)
└── AI Copilot (placeholder)
```

### Future (same nav section; internal structure)
```
LOCAL DEVELOPMENT
├── Local Development (hub)
│   ├── Workspaces
│   ├── Agent Sessions
│   ├── Runtime Profiles
│   ├── Services          ← today’s grid
│   └── Local Pull Requests
└── AI Copilot            ← remains separate until real
```

What belongs where:
- **Workspaces**: Git/worktree/ownership/health
- **Runtime Profiles**: which services run together
- **Services**: live process table (current strength)
- **Local PRs**: review/merge orchestration
- **Agent Sessions**: who is doing what on which branch
- **AI Copilot**: assisted diagnostics/ops — not the control plane itself

---

## Parallelization analysis

| Task | Owner | Dependencies | Parallel-safe? | Shared files? | Risk |
| --- | --- | --- | --- | --- | --- |
| S0 Assessment | Cursor | — | N/A | Docs only | Low |
| S1 Workspaces / Git read API | Claude **or** Cursor | S0 | Yes if new `Services/Workspaces` | Low if avoid `GitSafetyService` rewrite | Medium if touching GitSafety |
| S2 Runtime Profiles | Cursor | S1 optional | **No** vs other Local Dev edits | `ServiceManagerService`, Local Dev views | High merge contention |
| S3 Local metrics | Claude | S2 for hooks | Partial | Profile start path + new metrics files | Medium |
| S4 Local PR | Claude | S1 | Partial vs S5 | New PR module; Git write service | High if both edit Git write API |
| S5 Agent Sessions | Cursor | S1 | Yes vs S4 if contracts agreed | New session module | Low–Medium |
| S6 Orchestration | Cursor | S1–S5 | No | Hub UI + multiple services | High |
| S7 Intelligent resources | Either | S2+S3 | Yes if advisor-only | New advisor files | Low |

**Do not** assign Cursor and Claude simultaneous edits to `ServiceManagerService.cs`, `LocalDevServicesView.xaml`, or a shared Git write service without explicit file ownership.

---

## RAM / performance architecture constraints (16 GB)

Verified hazard: `ServiceManagerService.StartAllAsync` uses `Task.WhenAll` over **all** discovered services.

### Recommendations (policy, not S0 implementation)
- **Shared infrastructure by default** (one SQL/Redis/local infra stack — do not multiply per agent worktree).
- **Selective service startup** via Runtime Profiles; never treat Start All as default.
- **One active UI runtime by default** (single `ng serve` / one WebApp host).
- **Optional isolated service execution** only when ports/data dirs are explicitly profile-scoped.
- **Resource thresholds** in S3: warn/block when free RAM below configured floor (e.g. start blocked under 2–3 GB free — exact numbers tuned in S3).
- Prefer attaching to already-running services on shared ports (existing attach behavior) over starting duplicates.

### Profile catalog suggestion (S2)
| Profile | Intent |
| --- | --- |
| `minimal` | No services / docs only |
| `api-core` | Small set of APIs needed for current task |
| `ui-only` | Angular + minimal API backend |
| `full-local` | Explicit opt-in; requires confirmation + RAM check |
