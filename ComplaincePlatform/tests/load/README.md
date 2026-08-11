# Phase 7 — Load tests (k6)

Enterprise smoke / load scripts against the **Gateway** edge. They are best-effort CI helpers, not a full capacity study.

## Prerequisites

1. Install [k6](https://k6.io/docs/get-started/installation/) (`k6 version` must succeed).
2. Gateway reachable (default `http://localhost:5000`).
3. Optional: a JWT with Compliance scope for authenticated paths.

## Environment

| Variable | Required | Description |
|----------|----------|-------------|
| `BASE_URL` | No | Gateway base URL (default `http://localhost:5000`) |
| `TOKEN` | For auth paths | Bearer JWT (without the `Bearer ` prefix) |
| `LOGIN_EMAIL` / `LOGIN_PASSWORD` | Optional | Enables real login in `auth-login.js`; otherwise that script smokes `/health` |
| `SEARCH_Q` | No | Search query for `search.js` (default `nabh`) |
| `SOAK_DURATION` | No | Soak length (default `10m`) |
| `SOAK_RATE` | No | Soak arrivals/sec (default `2`) |

### PowerShell

```powershell
$env:BASE_URL = "http://localhost:5000"
$env:TOKEN = "<jwt>"

k6 run tests/load/auth-login.js
k6 run tests/load/dashboard.js
k6 run tests/load/search.js
k6 run tests/load/soak.js
```

### Bash

```bash
export BASE_URL=http://localhost:5000
export TOKEN="<jwt>"

k6 run tests/load/auth-login.js
k6 run tests/load/dashboard.js
k6 run tests/load/search.js
k6 run tests/load/soak.js
```

## Scripts

| Script | Purpose | Default profile |
|--------|---------|-----------------|
| `auth-login.js` | Login POST when credentials set; else `/health` smoke | 5 rps / 30s |
| `dashboard.js` | `/health` + `/api/v1/dashboard/summary` + `/api/v1/role-experience/home` | 10 rps / 1m |
| `search.js` | `/api/v1/compliance/search` | 8 rps / 1m |
| `soak.js` | Lower rate, longer duration | 2 rps / 10m |

## Ops integration

```powershell
.\scripts\ops\validate-production.ps1 -RunLoad
```

Runs `auth-login.js` then `dashboard.js` when `k6` is on `PATH`. Missing k6 is a soft skip (non-fatal).

## Chaos

See [`chaos-checklist.md`](chaos-checklist.md) for Redis / AIOrchestrator / SQL restart drills alongside these scripts.
