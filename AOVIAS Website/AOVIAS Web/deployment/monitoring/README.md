# Monitoring placeholders

Provider-agnostic hooks live in `apps/website/src/lib/ops/monitoring.ts`.

| Concern | Integration point | R1 status |
| --- | --- | --- |
| Health / liveness | `GET /api/health` | Implemented |
| Readiness | `GET /api/ready` | Implemented |
| Application logs | Container stdout / stderr | Native |
| Error reporting | Telemetry `trackError` + MonitoringProvider | Placeholder |
| Performance metrics | Future Web Vitals exporter | Placeholder |
| Uptime | External probe of `/api/health` | Documented |

Do not import Sentry/Datadog SDKs in pages. Register a `MonitoringProvider` at bootstrap in Release 2.
