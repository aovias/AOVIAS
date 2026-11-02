# Setup

AccredaOne has **approved technology direction** in Confluence ADRs but **no authorized product implementation** and no application to build or run in this repository.

## Prerequisites

- Git
- Read [../../GOVERNANCE-STATUS.md](../../GOVERNANCE-STATUS.md) before installing or configuring anything for product work.

Repository-level **version pins** (`global.json`, `.nvmrc`) express the approved local toolchain baseline for future engineering. They do **not** authorize Phase 1 implementation, application code, databases, or deployment. See **DEV-02 — Local Development Toolchain** below.

## Obtain the repository

The repository root is the directory that contains this file's parents: `README.md`, `GOVERNANCE-STATUS.md`, and `AGENTS.md`.

A remote is not configured. Cloning instructions will be added when a human selects hosting. That selection is an outstanding decision and is not a technology-stack approval.

## First reading

1. [../../GOVERNANCE-STATUS.md](../../GOVERNANCE-STATUS.md)
2. [../../AGENTS.md](../../AGENTS.md)
3. [../../CONTRIBUTING.md](../../CONTRIBUTING.md)
4. [../traceability/CONFLUENCE-INDEX.md](../traceability/CONFLUENCE-INDEX.md)
5. [../traceability/CONVENTION.md](../traceability/CONVENTION.md)
6. [../traceability/REGISTER.md](../traceability/REGISTER.md)

Confirm G1 — Programme readiness gate has **not passed** and Phase 1 is **not authorized** before editing anything else.

## Day-to-day use

- Add product and engineering records under `docs/` from `templates/`.
- Register every new identifier.
- Put experiments under `prototypes/` with a brief.
- Leave `application/` free of code.

## Local Git

From the repository root:

```text
git status
git log --oneline
```

Commit messages that rely on approved scope use the trailers in the traceability convention. Do not cite identifiers that are not in the register as `Approved`.

## DEV-02 — Local Development Toolchain

This section records the **approved local development baseline** and how the repository pins versions. It is foundation and documentation only until a human approves the first development slice.

### Approved baseline (Product Owner)

| Component | Baseline |
| --- | --- |
| .NET SDK | **10.0.103** (pinned in repository root `global.json`) |
| Node.js | **24.x** (major line in repository root `.nvmrc`: `24`) |
| Package manager | **npm** (ships with Node.js) |
| Database (local dev direction) | **PostgreSQL 18.x** |
| Frontend direction | Next.js + React + TypeScript |
| Backend direction | ASP.NET Core 10 + C# |
| Containers | Docker Desktop + Docker Compose (when manifests exist) |

Technology **direction** is approved in Confluence ADRs; **implementation** in this repository remains **NOT AUTHORIZED** per [../../GOVERNANCE-STATUS.md](../../GOVERNANCE-STATUS.md).

### Workstation categories

Use these labels when preparing a Windows development machine:

1. **Already installed and verified (DEV-02 Stage 1 on reference workstation)** — .NET SDK 10.0.103, Node.js v24.13.0, npm 11.6.2, Git, Cursor; Docker **client** and Compose plugin present; Docker **daemon was not running** at Stage 1 check; no native PostgreSQL client or service detected.
2. **Approved but not yet configured in this repository** — PostgreSQL 18.x local instance or Compose service, Docker daemon running for local Compose, application projects (`package.json`, `.sln`, Docker manifests), environment files beyond examples.
3. **Optional developer tools** — Node version managers (nvm-windows, fnm, Volta, etc.), GUI database clients, additional shells, WSL distros for Linux-aligned workflows. The repository does not require a specific Node version manager; `.nvmrc` is a **major-line hint** for tools that read it.

### Windows development workstation prerequisites

- **Windows 10/11** (64-bit) with sufficient disk space for SDKs, containers, and clones.
- **Git** for version control.
- **Cursor** (or another editor) for agent-assisted development; follow [../../AGENTS.md](../../AGENTS.md).
- **.NET SDK 10.0.103** — required when building future backend projects from this repository.
- **Node.js 24.x** and **npm** — required when building future frontend projects from this repository.
- **Docker Desktop** with **Docker Compose** — prerequisite for containerized local dependencies when Compose manifests are added; the daemon must be running for `docker info` and container workflows.
- **PostgreSQL 18.x** — local development database baseline; installation or Compose wiring is **not** part of DEV-02 Stage 2. Do not assume a server is running.

Do not install or upgrade software as part of editing this document unless a separate task explicitly authorizes it.

### Repository version pins

**`global.json` (repository root)**

- Pins the .NET SDK to **10.0.103** with `rollForward: disable` and `allowPrerelease: false`.
- From any directory under the repository, `dotnet` commands use this policy when the working directory is inside the repo tree.
- If the pinned SDK is **not installed**, the .NET CLI fails with an error that names the required version. Install **exactly** [SDK 10.0.103](https://dotnet.microsoft.com/download) (or the current download entry for that patch) and re-run `dotnet --version`. The CLI does **not** silently roll forward to another feature band or major version.

**`.nvmrc` (repository root)**

- Contains a single line: `24` (approved **24.x** line; no patch pin in this file).
- Developers using nvm, fnm, or similar may run their tool’s “use” command in the repository root to select a 24.x runtime. Developers without a version manager should install Node.js 24.x from the official installer and confirm with `node --version`.

**npm**

- Use **npm** as the package manager when `package.json` exists. Do not substitute pnpm or Yarn unless a future decision record explicitly approves a change.

### Environment verification checklist

Run from PowerShell unless noted. Expected outputs are **examples** from the reference workstation; patch numbers may differ within the approved lines (Node 24.x, PostgreSQL 18.x when installed).

```powershell
git --version
# Example: git version 2.51.1.windows.1

dotnet --version
# From repository root after global.json exists:
# Expected: 10.0.103

node --version
# Expected: v24.x.x (major 24)

npm --version
# Example: 11.x.x (bundled with Node)

docker --version
# Example: Docker version 29.x.x

docker compose version
# Example: Docker Compose version v5.x.x

docker info
# Succeeds only when the Docker daemon is running; otherwise reports connection error — not a repository defect
```

**PostgreSQL (later stage — not required for DEV-02 Stage 2)**

- Do not install a client or create a database in this stage.
- When PostgreSQL 18.x is available locally or via Compose, verification might include:

```powershell
psql --version
# Expected when client installed: psql (PostgreSQL) 18.x

# Server check (only when a server is configured and running):
# psql -h localhost -U <role> -d <database> -c "SELECT version();"
```

Do not commit connection strings, passwords, or role names into the repository.

### Repository setup (clone and pins)

1. Obtain the repository root (directory containing `README.md`, `GOVERNANCE-STATUS.md`, `global.json`, and `.nvmrc`).
2. Run the verification checklist above from that root.
3. Confirm `dotnet --version` reports **10.0.103** when standing in the repository tree.
4. Confirm `node --version` reports **v24.***.
5. Read governance and traceability links in **First reading** before creating product artifacts.

A remote URL is not configured in this baseline; cloning instructions will be added when hosting is selected.

### Environment configuration and secrets

- **Never** commit secrets, API keys, tokens, passwords, or private certificates. See repository [../../.gitignore](../../.gitignore).
- Patterns ignored include `.env`, `.env.*` (with **`!.env.example`** allowed so safe templates remain trackable), `*.pem`, `*.key`, `credentials.json`, and similar.
- Use local-only files (for example `.env.local`) for machine-specific values when application configuration is introduced in a later authorized stage.
- Do not paste real accreditation source content or production credentials into issues, commits, or chat logs.

### DEV-02 environment verification checklist (summary)

| Check | Command | Notes |
| --- | --- | --- |
| Git | `git --version` | Required |
| .NET SDK pin | `dotnet --version` (from repo root) | Must match `global.json` |
| Node baseline | `node --version` | Major **24** |
| npm | `npm --version` | Required with Node |
| Docker CLI | `docker --version` | Client install |
| Compose | `docker compose version` | Plugin / V2 |
| Docker daemon | `docker info` | Requires Docker Desktop running |
| PostgreSQL | `psql --version` | **Deferred** — document only until configured |

## DEV-03 — Local reversible scaffolding (build and test)

DEV-03 provides **minimal** frontend and backend skeletons under `application/` for local validation only. This does **not** authorize Phase 1 product implementation. See [DEV-03-REVERSIBLE-SCAFFOLDING-REPORT.md](DEV-03-REVERSIBLE-SCAFFOLDING-REPORT.md).

From the repository root:

```powershell
# Backend API (ASP.NET Core 10)
dotnet build application/backend/AccredaOne.Api.csproj
dotnet run --project application/backend/AccredaOne.Api.csproj
# Health: http://localhost:5022/health (http profile in launchSettings.json)

# Backend test
dotnet test tests/AccredaOne.Api.Tests/AccredaOne.Api.Tests.csproj

# Frontend (Next.js 15)
cd application/frontend
npm run build
npm run dev
```

Database connection, VPS deployment, and production configuration are **not** included.

### DEV-03 increment 1 — application foundation commands

Same projects as above. Additional local checks:

```powershell
# From AOVIAS.AccredaOne
dotnet test tests/AccredaOne.Api.Tests/AccredaOne.Api.Tests.csproj

cd application/frontend
npm ci
npm run lint
npm run build
```

API routes in this increment: `GET /health`, `GET /ready`, `GET /api/v1/foundation`. Development OpenAPI: `GET /openapi/v1.json`. PostgreSQL is not connected by default. Configuration uses the `AccredaOne` section and environment variables such as `AccredaOne__ApiVersion`. See [../architecture/APPLICATION-FOUNDATION.md](../architecture/APPLICATION-FOUNDATION.md).

### DEV-04 — Persistence foundation commands

Same backend and frontend commands as DEV-03 increment 1. Additional documentation: [../architecture/PERSISTENCE-FOUNDATION.md](../architecture/PERSISTENCE-FOUNDATION.md) and [DEV-04-PERSISTENCE-FOUNDATION-REPORT.md](DEV-04-PERSISTENCE-FOUNDATION-REPORT.md).

Optional persistence settings (placeholders only; do not commit secrets):

```text
Persistence__EnableConnectivity=false
Persistence__ConnectionString=<from local secret store when authorized>
```

Live database connectivity remains **disabled** unless a future increment explicitly enables `Persistence__EnableConnectivity=true` with approved infrastructure.

## What this setup does not include

- Production application deployment or VPS Compose deployment (see DEV-02 closeout)
- Database creation or migration
- Authentication configuration
- Deployment
- Import of accreditation content
- A connection to AOVIAS Command
