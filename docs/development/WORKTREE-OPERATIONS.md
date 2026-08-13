# AOVIAS Worktree Operations Guide

Practical Git/PowerShell operations for the three-worktree model.

## Daily topology

```text
V:\DevelopmentRepo\AOVIAS          → main
V:\DevelopmentRepo\AOVIAS.Cursor   → agent/cursor/active (or agent/cursor/<task>)
V:\DevelopmentRepo\AOVIAS.Claude   → agent/claude/active (or agent/claude/<task>)
```

Health check (read-only):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "V:\DevelopmentRepo\AOVIAS\scripts\dev\worktree-status.ps1"
```

## Starting a task

1. Confirm agent ownership (Cursor → `AOVIAS.Cursor`, Claude → `AOVIAS.Claude`).
2. Run preflight:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "V:\DevelopmentRepo\AOVIAS\scripts\dev\agent-preflight.ps1" -Agent cursor
# or: -Agent claude
```

3. Ensure `main` is current on the integration worktree (human may update agents after merges).
4. In the agent worktree, create the task branch from `main` (see next section).
5. Report the task-start block from `AGENTS.md`.

## Creating a task branch

Cursor example:

```powershell
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" fetch  # only if remotes exist later; currently local-only
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" status --short --branch
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" checkout -b agent/cursor/<task-slug> main
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" branch --show-current
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" rev-parse HEAD
```

Claude example: use `AOVIAS.Claude` and `agent/claude/<task-slug>`.

Branch names: lowercase, hyphen-separated, meaningful. No generic names.

## Working

- Edit only inside the assigned worktree.
- Keep changes scoped to the task.
- Do not touch the other agent worktree or develop in `AOVIAS`.

## Testing

Run the validation appropriate to the change (dotnet build, Angular tests, targeted suites). Record results for the completion report.

## Committing

```powershell
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" status
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" diff
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" add <paths>
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" diff --cached
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" commit -m "feat: describe the logical change"
```

Use conventional commit messages. Never commit secrets, keys, `.env`, or certificates.

## Reporting

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "V:\DevelopmentRepo\AOVIAS\scripts\dev\agent-completion-check.ps1" -Agent cursor -BuildStatus PASS -TestStatus PASS
```

Emit the full `TASK COMPLETE` block from `AGENTS.md`, including commit SHA.

## Human review

Human uses `V:\DevelopmentRepo\AOVIAS` on clean `main`:

```powershell
git -C "V:\DevelopmentRepo\AOVIAS" log --oneline main..agent/cursor/<task-slug>
git -C "V:\DevelopmentRepo\AOVIAS" diff main...agent/cursor/<task-slug>
```

## Merging

Prefer a visible integration commit:

```powershell
git -C "V:\DevelopmentRepo\AOVIAS" checkout main
git -C "V:\DevelopmentRepo\AOVIAS" status --short --branch
git -C "V:\DevelopmentRepo\AOVIAS" merge --no-ff agent/cursor/<task-slug>
```

Then build/test/validate `main`. Preserve the task branch until validation completes.

## Returning worktree to active state

Only after merge + main validation + clean worktree + human confirmation:

```powershell
# Example: return Cursor worktree to active from main
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" status --short --branch
# Ensure no uncommitted work and task commits already on main
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" checkout agent/cursor/active
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" merge --ff-only main
```

If `active` diverged unexpectedly: **STOP** and ask a human. Never `reset --hard` an unmerged task.

Optional task branch deletion (human, after validation):

```powershell
git -C "V:\DevelopmentRepo\AOVIAS" branch -d agent/cursor/<task-slug>
```

## Handling conflicts

Do not blind-resolve. Identify files, commits, ownership, and intent. If both agents' work is involved: stop and escalate to a human.

## Recovering abandoned work

1. Locate the worktree and branch (`git worktree list --verbose`, `git branch -vv`).
2. Inspect status/diff; do not clean or reset.
3. Either finish and commit, or stash only with explicit human authorization, or leave for human decision.
4. Never discard uncommitted work silently.

## Checking worktree health

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "V:\DevelopmentRepo\AOVIAS\scripts\dev\worktree-status.ps1"
git -C "V:\DevelopmentRepo\AOVIAS" worktree list --verbose
git -C "V:\DevelopmentRepo\AOVIAS" rev-parse --git-common-dir
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" rev-parse --git-common-dir
git -C "V:\DevelopmentRepo\AOVIAS.Claude" rev-parse --git-common-dir
```

Common directory should resolve to the same repository under `AOVIAS\.git`.

## Production workflow

Canonical production loop (see also `AGENTS.md`):

```text
1. Roadmap identifies task
2. Human assigns one primary agent
3. Verify main clean; agent preflight
4. Create task branch from latest main in the agent worktree
5. Implement / test / review diff
6. Commit + completion report with SHA
7. Human reviews: git diff main...<task-branch>
8. Human merges on main: git merge --no-ff <task-branch>
9. Validate main
10. Return agent worktree to agent/<agent>/active and FF from main
```

Example — start a Cursor production task from latest main:

```powershell
git -C "V:\DevelopmentRepo\AOVIAS" status --short --branch
powershell -NoProfile -ExecutionPolicy Bypass -File "V:\DevelopmentRepo\AOVIAS\scripts\dev\agent-preflight.ps1" -Agent cursor
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" fetch  # only if remotes exist later
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" checkout agent/cursor/active
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" merge --ff-only main
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" checkout -b agent/cursor/<task-slug> main
```

Example — human integrate and retire:

```powershell
git -C "V:\DevelopmentRepo\AOVIAS" checkout main
git -C "V:\DevelopmentRepo\AOVIAS" merge --no-ff agent/cursor/<task-slug>
# validate main, then:
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" checkout agent/cursor/active
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" merge --ff-only main
```

Physical worktrees remain fixed; task branches are temporary. Do not begin implementation before preflight passes. Do not start a task from an obsolete task branch.

## Studio Local Development Orchestration (v1)

AOVIAS Studio observes and recommends; it executes only explicit user-authorized actions.

```text
Agent → Task → Workspace (D1) → Branch
  → Runtime Profile (D2) → Resource Telemetry (S3)
  → Resource Recommendation (D5) → Agent Session metadata (D4)
  → Local PR (D3) → Human Review → Explicit Merge → main
```

| Capability | Mutates |
|---|---|
| D1 Workspaces | Git read-only |
| D2 Runtime Profiles | Owned runtime processes only (explicit Start/Stop) |
| S3 Local Resources | Read-only telemetry |
| D5 Resource Intelligence | Advisory only (no auto switch/stop) |
| D4 Agent Sessions | Metadata only (no agent process control) |
| D3 Local PR | Explicit `--no-ff` merge after SHA-bound approval; target worktree must be on target branch |

Studio UI (V2.0): Local Development is one navigation destination with five tabs —
Overview · Git & PR · Agent Sessions · Services · Monitoring.

Service Instance Model (V2.1): distinguishes Service Definition (discovered service) from
Service Instance (Shared vs Workspace-scoped execution context). Read-only awareness only —
no isolated runtime execution.

Local Port Registry (V2.2): maps local TCP ports to ServiceInstance identity
(`%LocalAppData%\AoviasStudio\port-registry.json`). Registry assignment is authoritative for
configured ownership; OS listener state is a point-in-time availability check. Does not start,
stop, or kill processes. Does not auto-allocate or auto-resolve conflicts.

Shared Runtime (V2.3): explicit Plan → Validate → Confirm → Start → Health → Own → Stop
over D2 Runtime Profiles. Executes only Shared ServiceInstances from the canonical AOVIAS
source. Reuses healthy running services without claiming external ownership. Stops only
profile-owned services. Does not execute Cursor/Claude workspace instances.

Invariant: Studio must never silently modify Git, runtime, worktrees, or agents.
