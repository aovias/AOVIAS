# AOVIAS Studio — Local Development Orchestration
## Architecture Proposal (S0)

Status: **PROPOSAL ONLY** — no implementation in S0.  
Grounded in traced code under `ComplianceServiceManager/` at `9f488ab`.

---

## 1. Design principle

Extend Local Dev Services with the **smallest clean architectural change**:

1. Keep the existing service runner (`ServiceManagerService` / `ProcessService` / discovery).
2. Add a **Workspace** layer that understands AOVIAS’s three-worktree model.
3. Add **Runtime Profiles** as named, persisted service selections with ordering — not a second process engine.
4. Treat **Local PR** and **Agent Session** as Studio orchestration metadata over Git truth.
5. Keep local host resource monitoring separate from Runtime Agent observability.

---

## 2. CURRENT architecture (as built)

```
┌─────────────────────────────────────────────────────────────┐
│ AOVIAS Studio (WPF / ComplianceServiceManager)              │
│  ShellViewModel → LOCAL DEVELOPMENT                         │
│    ├─ Local Dev Services                                    │
│    └─ AI Copilot (placeholder)                              │
└────────────────────────────┬────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────┐
│ LocalDevServicesViewModel                                   │
│  SolutionFolderRegistry  (%LocalAppData%\AoviasStudio\)     │
│  MainViewModel (commands, logs, AI Fix panel)               │
└────────────────────────────┬────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────┐
│ ServiceManagerService                                       │
│  Discovery → Build → Process start/stop → Health (10s)      │
│  PortProcessResolver (TCP listeners / netstat PID)          │
└─────────────────────────────────────────────────────────────┘

Adjacent (do not conflate):
  Platform → Runtime Agents  (deployment agents + local Runtime Agent process)
  Overview CPU/RAM           (Runtime Agent observability, not laptop)
  GitSafetyService           (AI repair checkpoints; CLI git)
  Aovias.Configuration       (deployment/runtime profiles — different concept)
```

### CURRENT conceptual model

```
SolutionFolder (registered path)
  └── Service (discovered project)
        ├── LaunchProfile (launchSettings / ng serve)
        ├── Environment variables (partial)
        ├── Process / PID (optional)
        └── Health / Status / Ports / URLs
```

---

## 3. PROPOSED conceptual model

```
Repository (AOVIAS git common dir)
  └── Workspace (physical worktree path)
       ├── Ownership (Human | Cursor | Claude)
       ├── Worktree metadata (path, linked gitdir)
       ├── Branch / HEAD / Dirty / AheadBehind
       ├── SolutionFolders[] (reuse registry; scoped or associated)
       ├── AgentSession? (optional active coding session)
       │     ├── Agent (Cursor | Claude | Human)
       │     ├── TaskId / Branch
       │     ├── RuntimeProfileRef
       │     └── LocalPRRef?
       └── RuntimeProfile (named service group for this workspace)
            └── Services[] (existing ServiceInfoModel identity: ProjectPath)
                 ├── Dependencies / StartOrder / StopOrder
                 ├── Port / Env overlays (optional)
                 └── Process binding (existing)

LocalPR (Studio metadata; Git remains source of truth)
  ├── SourceBranch / TargetBranch (usually main)
  ├── Validation results
  ├── Review / Approval state
  └── Merge outcome (executed via controlled Git ops)
```

### Distinctions (important)

| Term | Meaning in proposal | Do not confuse with |
| --- | --- | --- |
| Workspace | Git worktree checkout (`AOVIAS`, `AOVIAS.Cursor`, `AOVIAS.Claude`) | Shell “workspace” navigation page |
| Runtime Profile (Local Dev) | Named set of local services + order/env for a coding session | `Aovias.Configuration` deployment profile (`demo`, etc.) |
| Launch Profile | VS `launchSettings.json` profile (KEEP) | Runtime Profile |
| Agent Session | Cursor/Claude/Human task session | Runtime Agent (platform) |
| Local PR | Studio review/orchestration record over branches | GitHub/remote PR (optional later) |
| Solution Folder | Existing discovery root (KEEP) | Repository root (may coincide) |

---

## 4. Component disposition

| Component | Disposition | Rationale |
| --- | --- | --- |
| `LocalDevServicesView` / ViewModel | **MODIFY** | Evolve into Local Development hub sections; keep service grid |
| `SolutionFolderRegistry` | **REUSE / EXTEND** | Persist under `%LocalAppData%\AoviasStudio\`; add workspace associations |
| `ServiceDiscoveryService` | **REUSE** | Keep discovery rules; optionally scope by workspace roots |
| `ServiceManagerService` | **REUSE / MODIFY** | Add profile-aware start sets + ordered start/stop; fix Start All default |
| `ProcessService` | **REUSE / HARDEN** | Wrap with allowlisted executor for orchestration commands |
| `PortProcessResolver` / `HealthCheckService` / `BuildService` | **REUSE** | Already fit D2 |
| `PortRecoveryService` | **REUSE (optional)** | Port conflict mitigation; policy needed so agents don’t silently diverge ports |
| `IGitSafetyService` | **MODIFY carefully / DEPRECATE destructive path for orchestration** | Repair checkpoints ≠ Local PR; introduce new `IGitWorkspaceService` |
| New `IGitWorkspaceService` | **NEW** | Status, branch, worktree list, diff, ahead/behind; prefer CLI git first |
| New Workspace registry / models | **NEW** | JSON under `AoviasStudio\workspaces\` |
| New Runtime Profile store | **NEW** | JSON under `AoviasStudio\runtime-profiles\` |
| New Local PR store | **NEW** | JSON under `AoviasStudio\local-prs\` (Git remains truth) |
| New Agent Session store | **NEW** | JSON under `AoviasStudio\agent-sessions\` |
| Local host resource monitor | **NEW** (D5 / S3 foundation) | Distinct from Runtime Agent observability |
| Runtime Agent observability CPU/RAM | **REUSE for platform only** | Not laptop orchestration |
| `AiCopilotViewModel` | **REUSE later** | Placeholder; D4 may feed it, not replace Local Dev |
| `RuntimeAgentsViewModel` | **KEEP SEPARATE** | Platform domain |
| LibGit2Sharp | **DEFER** | Not present; CLI git + scripts sufficient initially |
| New database (SQLite) | **DEPRECATE idea for S0–S5** | JSON stores already established |

---

## 5. Layered architecture (target)

```
┌──────────────────────────────────────────────────────────────┐
│ UI (WPF) — LOCAL DEVELOPMENT section                         │
│  Workspaces | Sessions | Profiles | Services | Local PRs     │
└───────────────────────────┬──────────────────────────────────┘
                            │ ViewModels
┌───────────────────────────▼──────────────────────────────────┐
│ Application services                                         │
│  WorkspaceService · RuntimeProfileOrchestrator               │
│  LocalPullRequestService · AgentSessionService               │
│  LocalResourceAdvisor (later)                                │
└───────────────┬───────────────────────────┬──────────────────┘
                │                           │
┌───────────────▼────────────┐ ┌────────────▼──────────────────┐
│ Existing Local Dev stack   │ │ New infrastructure            │
│ Discovery / Manager /      │ │ GitWorkspaceService (CLI)     │
│ Process / Health / Ports   │ │ Json stores under AoviasStudio│
└────────────────────────────┘ │ LocalProcessMetrics (later)   │
                               │ PathPolicy / CommandAllowlist │
                               └───────────────────────────────┘
```

---

## 6. Git abstraction (proposed)

**Do not** expand `GitSafetyService` into a general Git API.

Proposed split:

| Service | Responsibility | Safety |
| --- | --- | --- |
| `IGitWorkspaceService` (NEW) | read: worktree list, branch, status, diff, ahead/behind; write: checkout/create branch (policy-gated) | No hard reset by default |
| `IGitSafetyService` (EXISTING) | AI Fix checkpoints only | Keep isolated; eventually remove `clean -fd` from default paths or require explicit confirm |
| `ILocalMergeService` (NEW, D3) | merge with conflict detection, no force | Human approval gate in UI |

Implementation preference: wrap `git` CLI (already used), optionally call existing `scripts/dev/worktree-status.ps1` for consistency with agent ops.

Worktree detection must handle:
- `.git` directory (main worktree)
- `.git` file with `gitdir:` (linked worktrees `AOVIAS.Cursor` / `AOVIAS.Claude`)

---

## 7. Runtime orchestration (proposed)

Runtime Profile = persisted document:

```json
{
  "id": "cursor-ui-focused",
  "displayName": "Cursor UI focused",
  "workspaceId": "aovias-cursor",
  "services": [
    { "projectPath": "...\\WebApp\\package.json", "startOrder": 10 },
    { "projectPath": "...\\ComplianceService\\....csproj", "startOrder": 20, "dependsOn": [] }
  ],
  "sharedInfrastructurePolicy": "prefer-shared",
  "maxParallelStarts": 2,
  "notes": "Avoid Start All; one Angular host"
}
```

Orchestrator behavior:
- Start profile services in `startOrder`, respecting `maxParallelStarts`
- Stop in reverse order
- Reuse `ServiceManagerService` for actual process ops
- Default profiles for 16 GB: **shared infra**, **API-only**, **UI-only**, **minimal**

---

## 8. Local PR management (proposed)

Local PR is **not** a second VCS. It is Studio metadata:

- Identity, source/target branch, workspace id, agent session id
- Cached validation outcomes (build/test commands allowlisted)
- Review checklist / approval actor
- Link to Git merge commit SHA when merged

Persistence: `%LocalAppData%\AoviasStudio\local-prs\{id}.json`  
Source of truth for code: Git branches in the workspace worktree.

---

## 9. Agent session management (proposed)

Map AOVIAS operating contract into Studio:

| Field | Example |
| --- | --- |
| Agent | Cursor / Claude / Human |
| WorkspacePath | `V:\DevelopmentRepo\AOVIAS.Cursor` |
| Branch | `agent/cursor/...` |
| Task | Task id / title |
| RuntimeProfileId | optional |
| LocalPrId | optional |
| Status | Active / Blocked / ReadyForReview / Merged / Abandoned |

Do **not** model Cursor/Claude as Runtime Agents.

---

## 10. Security architecture (requirements — not implemented in S0)

Before D3 write operations and D4 automation:

1. **PathPolicy**: allow registered DevelopmentRepo roots only (default `V:\DevelopmentRepo\AOVIAS*`).
2. **CommandAllowlist**: `git`, `dotnet`, `ng`/`npm` with validated args; no raw `cmd /c` user strings in orchestration paths.
3. **Process ownership**: prefer Studio-started PIDs; killing by port requires confirmation.
4. **Audit log**: append-only JSONL under `AoviasStudio\audit\` for start/stop/merge/git writes.
5. **Secrets**: never log env var values; never commit Studio stores with secrets.

---

## 11. UI boundaries (conceptual)

Keep one **LOCAL DEVELOPMENT** nav section. Avoid proliferating top-level items.

Recommended evolution of the existing Local Dev Services screen into a tabbed/sectioned hub:

```
Local Development
├── Workspaces          (D1)
├── Runtime Profiles    (D2)
├── Services            (CURRENT grid — default tab)
├── Local Pull Requests (D3)
└── Agent Sessions      (D4)
```

AI Copilot remains a sibling nav item until it has real content; D4 may later deep-link sessions into Copilot.

---

## 12. Anti-goals

- Do not replace Local Dev with a new app.
- Do not use deployment `ProfileManager` as Local Dev Runtime Profiles.
- Do not treat Runtime Agent CPU/RAM as laptop capacity.
- Do not implement unrestricted shell execution for “orchestration convenience.”
- Do not force LibGit2Sharp in S1.
- Do not duplicate SQL Server / Redis / full API stacks per agent by default.
