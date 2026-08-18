# AOVIAS Studio — Source Context (V2.9.1)

V2.9.1 introduces a first-class **Source Context** model so Shared Runtime, Cursor
Workspace Runtime, and Claude Workspace Runtime each have explicit, independent source
identities.

## Concepts

| Concept | Role |
| -------- | ---- |
| **Source Context** | Identifies which worktree is authoritative, observed branch/HEAD, agent ownership, and Git validity. Read-only — never mutates Git. |
| **Shared source** | Integration worktree (`AOVIAS` on `main`). Canonical source for Shared Runtime discovery. |
| **Workspace source** | Agent worktree (`AOVIAS.Cursor` / `AOVIAS.Claude` on `agent/<agent>/…`). Canonical source for workspace runtimes. |
| **Discovery roots** | Folders scanned for runnable services (e.g. `ComplaincePlatform`, `WebApp`). Persisted **Solution Folders** are workspace discovery roots only. |
| **Runtime execution source** | Resolved at runtime start (V2.3/V2.4); not replaced by Source Context in V2.9.1. |

## Canonical contexts

| ContextId | Kind | RootPath | Expected branch | Owner |
| --------- | ---- | -------- | --------------- | ----- |
| `shared` | Shared | `V:\DevelopmentRepo\AOVIAS` | `main` (observed) | AOVIAS / Shared |
| `cursor` | Workspace | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/*` | Cursor |
| `claude` | Workspace | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/*` | Claude |

Branch and HEAD are **observed from Git** via D1 (`GitWorkspaceService`). They are not
hard-coded or persisted as authoritative state.

## Discovery boundary

`ServiceDiscoveryService` accepts a `SourceContext` and resolves roots through
`ISourceDiscoveryBoundary`:

- **Discover(Shared)** → only folders under the shared integration root.
- **Discover(Cursor)** → workspace discovery roots under `AOVIAS.Cursor`, or default app folders there.
- **Discover(Claude)** → workspace discovery roots under `AOVIAS.Claude`, or default app folders there.

Shared discovery **never** reads persisted Solution Folders. Configuring Cursor or Claude
folders cannot invalidate Shared Runtime discovery.

## Invariants

1. Configuring workspace discovery roots must not alter Shared source context.
2. Cursor discovery must not fall back to `AOVIAS` or `AOVIAS.Claude`.
3. Claude discovery must not fall back to `AOVIAS` or `AOVIAS.Cursor`.
4. Workspace source failure blocks only that workspace context.
5. Source Context does not start processes, allocate ports, or replace `ServiceInstance`.

## V2.9.2 follow-up

V2.9.1 establishes the model and discovery boundary. **V2.9.2 Discovery Isolation** will
wire workspace-scoped discovery passes and complete runtime-facing isolation on top of this
foundation.
