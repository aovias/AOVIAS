# DEV-02 — Development baseline closeout report

Date: 2026-11-02

Repository: AccredaOne nested Git (`A1-ProjectSetup`)

Related commit: `518a3b9` — toolchain pins and SETUP documentation

## Closeout recommendation

**Status: PARTIAL**

Repository and workstation toolchain baseline is **verified and committed**. VPS-first runtime (Docker Compose, PostgreSQL 18, `accredaone-dev` project) remains **designed but not deployed**. DEV-02 cannot be marked **COMPLETE** until PO authorizes and executes VPS database staging.

## Approved baseline and version pins

| Component | Baseline | Repository pin |
| --- | --- | --- |
| .NET SDK | 10.0.103 | `global.json` (`rollForward: disable`) |
| Node.js | 24.x | `.nvmrc` → `24` |
| Package manager | npm | With Node.js |
| PostgreSQL (VPS direction) | 18.x | Not deployed |
| Frontend direction | Next.js + React + TypeScript | DEV-03 scaffold |
| Backend direction | ASP.NET Core 10 + C# | DEV-03 scaffold |
| Runtime host (selected model) | VPS Docker; laptop for edit/build | See §VPS |

## Workstation verification (2026-11-02)

| Check | Result | State |
| --- | --- | --- |
| Windows / disk | C: ~345 GB free; V: ~69 GB free | **Verified** |
| `dotnet --version` (repo root) | 10.0.103 | **Verified** |
| `node --version` | v24.13.0 | **Verified** |
| `npm --version` | 11.6.2 | **Verified** |
| Git | 2.51.1.windows.1 | **Verified** |
| Git author (nested repo) | Name and email configured | **Verified** |
| Remote | None | **Verified** |
| SSH to VPS (`aovias@187.127.154.201`, key auth) | Reachable; read-only checks OK | **Verified** |
| Cursor | Present on workstation (prior audits) | **Not re-verified this session** |

No credentials, keys, or tokens are recorded in this report.

## VPS-first operating model (preserved)

| Layer | Role |
| --- | --- |
| Laptop | Edit, local builds, tests, SSH tunnel client |
| VPS | Docker Engine, future PostgreSQL 18 and dev services |
| Database access | Private network or SSH tunnel only; **no public 5432** |
| Isolation target | Compose project **`accredaone-dev`**, network **`accredaone-internal`**, dedicated DB volume, resource limits |

## VPS audit evidence (read-only, 2026-11-02)

Prior Stage 2A audit plus same-day connectivity check:

| Item | Evidence |
| --- | --- |
| Host | `aovias-demo-01`, Ubuntu 24.04.4, 2 vCPU, ~7.8 GiB RAM, **no swap** |
| Docker | Engine 29.6.x, Compose v5.x, **rootful** daemon |
| Workloads | `website` (127.0.0.1:3000), `openhands-canvas` (127.0.0.1:8000); Caddy on :80/:443 |
| Port 5432 | **Not listening** |
| SSH user | In `docker` group → effective root via socket |
| Headroom | ~6.3 GiB RAM available at check; ~86 GB disk free on `/` |

**Infrastructure changes during DEV-02 closeout: NO**

## Item classification

| Item | Classification |
| --- | --- |
| Repo pins (`global.json`, `.nvmrc`) | **Verified** (committed) |
| SETUP DEV-02 toolchain section | **Verified** (committed) |
| Workstation SDK alignment | **Verified** |
| VPS SSH read-only access | **Verified** |
| Compose project `accredaone-dev` | **Pending** (PO authorization) |
| Network `accredaone-internal` | **Pending** |
| PostgreSQL 18 container/volume | **Pending** |
| SSH tunnel runbook for developers | **Designed** (SETUP partial); full VPS runbook **Pending** |
| Rootless vs rootful isolation decision | **Pending** PO sign-off |
| GitHub org/repo remote | **Blocked** (org identity not verified at handle `aovias`) |

## Remaining prerequisites (separate PO authorization)

1. Approve VPS Compose manifest and paths (stay clear of `/opt/aovias` unless explicitly shared).
2. Deploy PostgreSQL 18 with **no published 5432**, memory/CPU limits, and backup location.
3. Document SSH tunnel command for developers (local port → VPS private DB).
4. Confirm isolation model (shared rootful with guardrails vs rootless AccredaOne daemon vs separate VPS).
5. Optional: add swap or enforce container memory caps on VPS to mitigate OOM (no swap today).
6. Configure GitHub organization/repository and remote when identity is resolved.

## Risks (unchanged from audit)

- **OOM** on VPS with no swap under memory spikes.
- **Shared rootful Docker** — `aovias` user has full daemon access; operational discipline required.
- **Frontend npm audit (DEV-03)** — transitive **PostCSS** advisories via **Next.js 15.5.28**; tracked under DEV-03 scaffolding and temporary development risk acceptance (not DEV-02 VPS scope).

## DEV-02 closeout status

| Criterion | Met? |
| --- | --- |
| Local toolchain pins and docs | **Yes** |
| Workstation verification | **Yes** |
| VPS runtime deployed | **No** |
| PostgreSQL available | **No** |

**Overall: PARTIAL**
