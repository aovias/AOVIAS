# AOVIAS Helm Chart

**Status:** Phase 7.6 sample — Gateway + ComplianceService templates. Not a full production install path yet.

## Contents

```
charts/aovias/
  Chart.yaml
  values.yaml
  README.md
  templates/
    _helpers.tpl
    configmap.yaml
    deployment.yaml   # Gateway + ComplianceService
    service.yaml
    ingress.yaml
```

## Install (sample)

```bash
helm upgrade --install aovias ./charts/aovias \
  --set ingress.host=praman.example.com \
  --set gateway.image.tag=1.0.0 \
  --set compliance.image.tag=1.0.0
```

Pilot / commercial Compose remains the supported path until packaging hardens:

```bash
docker compose -f docker-compose.dev.yml -f docker-compose.commercial.yml up -d --build
# or
./scripts/deploy.ps1 -Build -Detach
```

## Rolling update strategy

Both Deployments use:

| Setting | Value | Rationale |
|---------|-------|-----------|
| `strategy.type` | `RollingUpdate` | Zero-downtime pod replacement |
| `maxUnavailable` | `0` | Never drop below desired ready pods |
| `maxSurge` | `1` | Bring one new pod up before terminating old |

**Operator checklist**

1. Bump `gateway.image.tag` / `compliance.image.tag` in values (or `--set`).
2. `helm upgrade aovias ./charts/aovias`.
3. Watch `kubectl rollout status deploy/aovias-gateway` and `deploy/aovias-compliance`.
4. Confirm `/readiness` on new pods before traffic shifts (probes configured).
5. For maintenance windows, set `gateway.env.MAINTENANCE_MODE=true` or mount `App_Data/maintenance.lock` (Gateway returns 503 + `Retry-After`).

## Secrets

Do **not** put JWT secrets, SQL passwords, or API keys in `values.yaml`. Use ExternalSecrets / sealed-secrets and reference via `envFrom` / volume mounts in a follow-on values overlay.

## Next steps

1. Add Identity + Organization Deployments mirroring Compose.
2. Wire SQL connection strings from a Secret.
3. Document cutover from Compose → Helm in Phase 6 deployment automation docs.
