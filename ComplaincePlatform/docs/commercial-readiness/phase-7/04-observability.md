# 04 — Observability

**Phase:** 7.4  
**Sprint:** 3  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| OpenTelemetry | `Shared.Logging` → `AddAoviasOpenTelemetry` (ASP.NET Core + HttpClient) |
| OTLP exporter | When `OpenTelemetry:OtlpEndpoint` is set |
| Azure Monitor | When `APPLICATIONINSIGHTS_CONNECTION_STRING` or `ApplicationInsights:ConnectionString` is set |
| Request events | `PramanObservabilityMiddleware` → Serilog + Activity tags (no `Console.WriteLine`) |
| Ops summary | `GET /api/v1/ops/observability/summary` (AdminOnly) — config/stub dependency inventory |

## Configuration

```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:4317"
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=..."
  }
}
```

Unset exporters = no-op (instrumentation still registered; nothing exported).

Wired in:

- `Gateway/Program.cs` → `builder.AddAoviasOpenTelemetry("Gateway")`
- `ComplianceService/Program.cs` → `builder.AddAoviasOpenTelemetry("ComplianceService")`

## Alerting runbook

1. **Enable export** — set OTLP and/or Application Insights connection string in the target environment.
2. **Baseline SLOs** (suggested):
   - Gateway p95 latency > 2s for 5m → Warning
   - HTTP 5xx rate > 1% for 5m → Critical
   - Dependency failure (SQL / Redis) from `/health/ready` → Critical
3. **Azure Monitor** — create alert rules on Application Insights failure rate and availability tests against Gateway `/health`.
4. **OTLP** — route to your collector (Grafana/Tempo/Jaeger); alert on span error rate for `service.name` in {Gateway, ComplianceService}.
5. **Ops inventory** — `GET /api/v1/ops/observability/summary` confirms Redis/OTLP/App Insights config presence (does not replace live health probes).
6. **Hangfire** — monitor failed jobs in `/hangfire` (auth required outside Development); alert on recurring job consecutive failures.
7. **On-call** — correlate with `X-Correlation-ID` / Serilog `CorrelationId` before escalating.

## Exit

Telemetry exporters optional by config; structured observability logs; admin summary endpoint for commercial ops checks.
