# AOVIAS Studio — Local Development Orchestration
## S0 Assessment (Evidence-Based Current State)

| Field | Value |
| --- | --- |
| Phase | S0 — Architecture & Current-State Assessment |
| Agent | Cursor |
| Worktree | `V:\DevelopmentRepo\AOVIAS.Cursor` |
| Branch | `agent/cursor/studio-local-dev-orchestration-assessment` |
| Assessed HEAD | `9f488ab0a75072e90fa0bbb9f917f8d49787563e` |
| Assessment date | 2026-08-11 |
| Implementation in this task | **NONE** |

---

## 1. Executive answer

AOVIAS Studio already has a usable **Local Dev Services** control surface for discovering and running .NET Web API and Angular apps from registered solution folders. That surface is the correct foundation for a local development control plane.

What is missing for the north star (“local development control plane for parallel, AI-assisted engineering”) is **workspace/Git awareness**, **runtime profiles as first-class orchestration**, **Local PR as a Studio concept over Git**, **coding-agent session identity**, and **local machine resource awareness**. Studio already has partial Git CLI helpers (repair-oriented), remote Runtime Agent CPU/RAM telemetry (not local), and an AI Fix / Cursor CLI path — none of which implement D1–D5.

**Smallest clean architectural change:** extend Local Dev Services’ existing domain (`SolutionFolder` → `Service` → `Process`) with a workspace layer and profile layer under the existing `LOCAL DEVELOPMENT` navigation section, reusing `%LocalAppData%\AoviasStudio\` JSON persistence and `ProcessService` — do **not** invent a parallel service runner, Git library stack, or second Studio shell.

---

## 2. Application architecture (verified)

### Application type
- **WPF desktop application** (`WinExe`, `UseWPF`, `net10.0-windows`)
- Project: `ComplianceServiceManager/ComplianceServiceManager.csproj`
- Product branding in UI/startup: **AOVIAS Studio** / **AOVIAS Command**
- UI pattern: MVVM via **CommunityToolkit.Mvvm**; shell navigation via `ShellViewModel`

### Entry point & DI
- `App.xaml.cs` → `ConfigureServices()` → `ServiceProvider` → `MainWindow` + `ShellViewModel`
- Microsoft.Extensions.DependencyInjection singletons for Local Dev stack:
  - `SolutionFolderRegistry`
  - `ServiceDiscoveryService`
  - `ProcessService`, `BuildService`, `HealthCheckService`
  - `ServiceManagerService`
  - `MainViewModel`
  - `LocalDevServicesViewModel`
  - AI/repair: `GitSafetyService`, `CursorAutomationService`, `AIFixOrchestrator`, etc.

### Navigation
Shell sections (from `ShellViewModel.BuildNavigation`):
- PLATFORM / APPLICATIONS / OPERATIONS
- **LOCAL DEVELOPMENT** → Local Dev Services (badge `NEW`), AI Copilot
- ADMINISTRATION → Settings

Local Dev is intentionally separated from Platform → Runtime Agents (Runtime Agent is **not** local-dev discovery).

### Configuration / persistence (Studio-local)
| Store | Path / mechanism | Purpose |
| --- | --- | --- |
| Solution folders | `%LocalAppData%\AoviasStudio\solution-folders.json` | Registered discovery roots |
| Operation history | `%LocalAppData%\AoviasStudio\operations\` | Operation Engine history |
| Environments | `%AppData%\AOVIAS Command\environments.json` | Remote deployment environments |
| Fixed errors / repair analytics | `AppContext.BaseDirectory\Diagnostics\Analytics\` | AI repair state |
| Manager logs | `AppContext.BaseDirectory\Diagnostics\ManagerLogs\` | Serilog |
| Platform config | `appsettings.json` + `Aovias.Configuration` profiles | Runtime Agent / platform profiles |

**No SQLite** is used for Studio Local Dev preferences. Persistence pattern is **JSON files**.

### Service boundaries (Local Dev)
```
LocalDevServicesView (XAML)
  → LocalDevServicesViewModel (folders + refresh)
      → MainViewModel (UI commands: start/stop/open/AI fix)
          → ServiceManagerService (lifecycle + health timer)
              → ServiceDiscoveryService (fs discovery)
              → BuildService (dotnet build)
              → ProcessService (process start/stop/command)
              → HealthCheckService (HTTP readiness/health)
              → PortProcessResolver (netstat / TCP listeners)
          ← SolutionFolderRegistry (folder preference store)
```

---

## 3. Local Dev Services — traced capability

### UI surface (`LocalDevServicesView.xaml`)
- Add / Remove Solution Folder
- Refresh Discovery
- Discovered Services grid: Name, Type, Status, URL, PID, Since
- Actions: Start, Stop, Open
- Bulk: Start All, Stop All, Restart All
- Search filter via `MainViewModel.SearchText`

### Solution folder registration
- User picks any folder via `OpenFolderDialog`
- Normalized full path stored in `SolutionFolderRegistry`
- Defaults on first launch: `RootPathResolver.ResolveCompliancePlatformRoot()` (`ComplaincePlatform`) and `ResolveWebAppRoot()` (`WebApp`)
- **No allowlist** of roots (any existing directory can be added)

### Service discovery (`ServiceDiscoveryService`)
- Dynamic filesystem scan on refresh (not a static catalog)
- Per registered root:
  - All `*.csproj` under tree (skip `bin`/`obj`/`.artifacts`/`tests`)
  - Keep if: `Microsoft.NET.Sdk.Web` **or** AspNetCore refs, not test project, has `Program.cs`
  - Optional Angular: root has both `package.json` and `angular.json`
- Ports/URLs from `Properties/launchSettings.json` profiles (prefer HTTPS + Project profile); fallback Kestrel URLs in `appsettings.Development.json`; Angular port from `angular.json` serve options (default 4200)
- Results are **in-memory** on `ServiceManagerService.Services`; only folder list is persisted

### Process management (`ServiceManagerService` + `ProcessService`)
| Operation | Behavior |
| --- | --- |
| Start | Build (dotnet) unless Angular; check ports; `dotnet run --no-build --launch-profile` or `cmd /c ng serve`; track `Process` + PID |
| Stop | Kill owned process tree; else kill by tracked PID; else kill PID listening on service ports |
| Restart | Stop → delay → Start |
| Start All | `Task.WhenAll` over **all** services (fully parallel) |
| Health | 10s timer; HTTP `/readiness|/health|/liveness` or `/` for Angular |
| External attach | If port in use, attach to existing listener PID |
| Studio exit | Detaches; does **not** stop running services |

Stdout/stderr: redirected and logged via `ILoggingService`. Runtime URL captured from “Now listening on:” / Angular localhost URL lines.

### What Local Dev configuration currently exists
| Concern | Exists? | Where |
| --- | --- | --- |
| Service names | Yes | Project / folder name |
| Ports / URLs | Yes | launchSettings / angular.json / runtime capture |
| Project paths | Yes | Discovery |
| Launch commands | Implicit | Hard-coded `dotnet run` / `ng serve` |
| Environment variables | Partial | From selected launch profile + `ASPNETCORE_ENVIRONMENT` from UI |
| Profiles (Studio runtime profiles) | **No** | Only Visual Studio `launchSettings` profiles |
| Dependencies / startup order | **No** | Start All is parallel, unordered |
| Resource limits | **No** | — |

---

## 4. Git capabilities

### Verdict
**GIT CAPABILITY FOR WORKSPACE ORCHESTRATION IS NOT CURRENTLY PRESENT.**

Partial Git CLI usage exists for **AI repair safety / diagnostics only**:

| Component | Capability | Limitation |
| --- | --- | --- |
| `IGitSafetyService` / `GitSafetyService` | Create checkpoint branch `fix/session/{stamp}`; `status --short`; rollback via `reset --hard` + `clean -fd` + checkout | Repair-oriented; destructive rollback; no worktree API |
| `DiagnosticsCollector.CaptureGitAsync` | `rev-parse --abbrev-ref HEAD`, `status --short`, `diff` | Per-service working directory only |
| LibGit2Sharp | **Not referenced** | — |
| Worktree discovery | **Missing** | `ResolveRepositoryRoot` only checks for `.git` **directory**; linked worktrees use a `.git` **file** and can be mis-detected |
| Branch compare / merge / Local PR | **Missing** | — |
| Studio UI for Git | **Missing** | — |

Human/agent worktree operations live in repo docs/scripts (`AGENTS.md`, `docs/development/WORKTREE-OPERATIONS.md`, `scripts/dev/worktree-status.ps1`) — **outside Studio**.

---

## 5. Resource monitoring

| Capability | Status | Evidence |
| --- | --- | --- |
| Local CPU / RAM for Studio host | **Missing** | No PerformanceCounter / Process memory sampling in Local Dev |
| Overview CPU/RAM % | **Remote Runtime Agent observability** | `OverviewViewModel` ← `IObservabilityService.GetStateAsync()` / Runtime Agent |
| Per-service local resource use | **Missing** | Only PID + running-since |
| Disk monitoring (local) | **Missing** in Studio Local Dev | Remote host probe exists in Bootstrap (`TargetHostProbeService` via SSH) |

**Reuse guidance for D5:** do **not** overload Runtime Agent observability as “local laptop resources.” Introduce a distinct local host monitor later; optionally display Runtime Agent metrics only when a local Runtime Agent is the active target.

---

## 6. Agent / AI capabilities

| Concept | Current state |
| --- | --- |
| AI Copilot workspace | Placeholder (“coming soon”) |
| AI Fix on Local Dev services | Real: diagnostics → optional Cursor CLI (`COMPLIANCE_CURSOR_CLI_COMMAND`) → rebuild/validate |
| Cursor as coding agent session | **Not modeled** (CLI hook only) |
| Claude as coding agent session | **Not present** |
| Task / branch / worktree ownership | Documented in `AGENTS.md`; **not** in Studio domain |
| Runtime Agents workspace | Platform deployment agents + local Runtime Agent process control — **not** Cursor/Claude sessions |

Reusable for D4: session folder pattern under diagnostics, `GitSafety` checkpoint idea (but must not reuse destructive rollback for Local PR), Cursor prompt packaging. New domain needed for Agent Session / Task / ownership.

---

## 7. Security model (current boundaries)

### What exists
- Process redirection + logging of command lines via `ProcessService`
- Port conflict detection before start
- Environment registry / API key settings for **remote** Runtime Agents
- HTTPS developer certificate checks for local ASP.NET

### What does **not** exist (critical for future orchestration)
- Allowed roots (e.g. restrict to `V:\DevelopmentRepo\...`)
- Command allowlist / argument validation beyond hard-coded `dotnet`/`ng`/`git`/`cmd`
- Path containment checks when adding solution folders
- Audit trail of Git merge / destructive Git ops in Local Dev UI
- Secret redaction for logged environment variables
- Separation between “Studio-owned process” and “foreign process on same port” beyond attach/kill-by-port

### Material risks already in code
1. `SolutionFolderRegistry.Add` accepts any directory → unrestricted project build/run.
2. `ProcessService` can run arbitrary fileName/args (used for Cursor CLI via `cmd /c`).
3. `GitSafetyService.RollbackAsync` runs `git reset --hard` and `git clean -fd`.
4. Stop-by-port can kill **non-Studio** processes listening on discovered ports.
5. `StartAllAsync` can exhaust a 16 GB machine by launching every discovered service at once.

Future orchestration **must not** become an unrestricted command executor. S0 recommendation: design an allowlisted `ILocalDevExecutor` / workspace-root policy **before** D3 merge and D4 automation.

---

## 8. Capability matrix for D1–D5

Legend: **EXISTS** / **PARTIALLY EXISTS** / **MISSING** / **UNSAFE / REQUIRES DESIGN** / **UNKNOWN**

### D1 — Workspace Management
| Capability | Classification | Notes |
| --- | --- | --- |
| Repository discovery | PARTIALLY EXISTS | Walk-up `.git` dir only; no Studio UI |
| Worktree discovery | MISSING | Linked worktrees unsupported in resolver |
| Branch / HEAD detection | PARTIALLY EXISTS | Diagnostics / GitSafety only |
| Clean/dirty | PARTIALLY EXISTS | `status --short` in GitSafety/diagnostics |
| Ahead/behind | MISSING | — |
| Agent ownership | MISSING | Exists only in process docs (`AGENTS.md`) |
| Workspace registration | PARTIALLY EXISTS | Solution folders ≠ Git workspaces |
| Workspace health / refresh | PARTIALLY EXISTS | Service refresh only |

### D2 — Runtime Profiles
| Capability | Classification | Notes |
| --- | --- | --- |
| Service discovery / start / stop / health | EXISTS | Local Dev Services |
| Ports / launch env | PARTIALLY EXISTS | From launchSettings; no Studio profile overlay |
| Service groups / dependencies / ordering | MISSING | Start All parallel |
| Workspace association | MISSING | Folders only |
| Resource limits | MISSING | — |
| Profile persistence | MISSING | — |
| Profile validation | MISSING | — |

### D3 — Local PR
| Capability | Classification | Notes |
| --- | --- | --- |
| Diff / status capture | PARTIALLY EXISTS | Diagnostics only |
| Branch compare / merge / approval / lifecycle metadata | MISSING | — |
| Conflict detection | MISSING | — |
| Safe merge orchestration | UNSAFE / REQUIRES DESIGN | Existing GitSafety rollback is destructive |

### D4 — Agent Sessions
| Capability | Classification | Notes |
| --- | --- | --- |
| Cursor CLI hook | PARTIALLY EXISTS | Env-configured command |
| Session identity (Cursor/Claude/Task/Branch/PR) | MISSING | — |
| Runtime Agents UI | EXISTS (different domain) | Do not overload |

### D5 — Intelligent Resource Management
| Capability | Classification | Notes |
| --- | --- | --- |
| Local host CPU/RAM | MISSING | — |
| Remote agent CPU/RAM | EXISTS | Wrong scope for laptop orchestration |
| Auto profile selection | MISSING | Future |

---

## 9. Terminology — CURRENT vs PROPOSED

### CURRENT Local Dev domain terms
- **Solution Folder** — registered filesystem root for discovery
- **Service** (`ServiceInfoModel`) — runnable Web API or Angular app
- **ServiceKind** — `WebApi` | `AngularWebApp`
- **Launch Profile** — Visual Studio `launchSettings.json` profile (not Studio runtime profile)
- **Process / PID** — OS process association
- **Health / Status** — HTTP + process/port reconciliation
- **Environment** — ASP.NET environment name (UI: Development/QA/UAT)

### PROPOSED orchestration domain (extend, do not replace)
See Architecture document. Key rule: keep **Service** and **Solution Folder**; add **Workspace/Worktree/Branch** above; add **Runtime Profile** as a named service group; add **Agent Session** and **Local PR** as orchestration metadata.

---

## 10. Recommended next step

**S1 — D1 Workspace Management** as a read-mostly Studio capability:
- Discover/register `V:\DevelopmentRepo\AOVIAS*` worktrees
- Show branch, HEAD, clean/dirty, agent ownership mapping
- Associate Local Dev solution folders with a selected workspace
- Do **not** yet merge, rewrite GitSafety, or implement profiles

---

## 11. Validation performed for S0

- Git preflight: Cursor worktree CLEAN on `agent/cursor/active` → task branch created from `main` @ `9f488ab`
- Code tracing of Local Dev UI → ViewModel → services → process/port/health
- Repo searches for Git, LibGit2Sharp, worktree, CPU/RAM, AI/agent concepts
- No production code changes in this assessment

**Product implementation: NONE**
