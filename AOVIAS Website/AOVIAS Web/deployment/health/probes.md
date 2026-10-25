# Health probes

| Probe | Path | Success | Use |
| --- | --- | --- | --- |
| Liveness | `GET /api/health` | `200` + `{ "status": "ok" }` | Docker HEALTHCHECK, k8s liveness |
| Readiness | `GET /api/ready` | `200` + `{ "ready": true }` | k8s readiness, load balancer |

When `MAINTENANCE_MODE=true`, readiness returns `503` and public routes rewrite to `/maintenance`. Health remains `200` so the process is not killed during maintenance.
