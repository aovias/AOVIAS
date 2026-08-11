# 08 — Deployment Automation

**Phase:** 6.8  
**Sprint:** 5  
**Date:** 2026-07-26

## Principle

**Extend the existing Compose plane** in `ComplaincePlatform`. Sibling AOVIAS tools (Bootstrap, Publishing, Deployment Center) remain the packaging/orchestration owners — this repo does not invent a second deploy plane.

## Delivered

| Capability | Implementation |
|------------|----------------|
| Commercial Compose overlay | `docker-compose.commercial.yml` (adds Gateway on top of `docker-compose.dev.yml`) |
| Deploy scripts | `scripts/deploy.ps1`, `scripts/deploy.sh` |
| Env validation | `scripts/ops/Validate-Environment.ps1` |
| Helm skeleton | `charts/aovias/` (Chart.yaml + README only) |

## Deploy

```powershell
# PowerShell
.\scripts\deploy.ps1 -Build -Detach
```

```bash
# Bash
chmod +x scripts/deploy.sh
./scripts/deploy.sh --build -d
```

Equivalent manual command:

```bash
docker compose -f docker-compose.dev.yml -f docker-compose.commercial.yml up -d --build
```

### Required env

| Variable | Required | Notes |
|----------|----------|-------|
| `SA_PASSWORD` | Yes | SQL SA password for `sqlserver-dev` |
| `ASPNETCORE_CERT_PASSWORD` | Recommended | Dev HTTPS PFX password |
| JWT / InternalApi keys | Recommended | Match across Gateway + services |

Copy `.env.example` → `.env` before first run.

## Services brought up

From `docker-compose.dev.yml` + commercial overlay:

- SQL Server
- Identity, Organization, Compliance, AI Orchestrator (and Audit if present in base file)
- **Gateway** (commercial overlay)

## Helm

`charts/aovias` is a **future-ready skeleton**. Do not treat `helm install` as supported until templates/values land. See chart README.

## Relationship to AOVIAS packaging

`scripts/build-praman-aovpkg.ps1` publishes service folders for Deployment Center / Package Builder. Compose remains the local/pilot runtime; `.aovpkg` remains the release artifact path.

## Exit

Operator can validate env and bring up Gateway + core services with one script, without diverging from the repo’s Dockerfile.dev.dotnet pattern.
