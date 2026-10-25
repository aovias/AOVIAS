# PRAMAN Integration Layer Gap Analysis — EIWAM Phase 12

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 12 — Enterprise Integration Layer |
| **Prior work** | Phases 0–11 EIWAM (including EnterpriseDirectoryService) |
| **Status** | Investigation complete — implementation authorized |
| **Bounded context** | IntegrationService (new greenfield) |

## Executive summary

PRAMAN has mature **internal** integration primitives (Gateway HTTP proxy, per-service outbox, Azure Service Bus relay, source HTTP clients) but no **standardized external integration layer**. Phase 12 introduces `IntegrationService` as the connector, synchronization, mapping, webhook, import/export, monitoring, and audit bounded context—without owning Person, Employment, Professional, Organization, or Governance data.

**Integration never owns business data. It orchestrates connectors and calls existing PRAMAN HTTP APIs.**

---

## Investigation scope

| Area | Locations reviewed |
|------|-------------------|
| Gateway | `IProxyService`, MediatR proxy handlers, `AddBackendHttpClients` |
| Messaging | `OutboxRelayWorker`, `workforce-events`, `directory-events`, `compliance-events` |
| Outbox | Per-service `OutboxMessage`, dual-track projection/relay |
| Communication | `EventSubscriptionRule`, channel enum including Webhook (no provider) |
| Export | `ComplianceService/ReportExportService` (ClosedXML, QuestPDF) |
| Internal federation | `EnterpriseDirectoryService` source clients |
| Import | Offline `seed-data/` only; no runtime import APIs |
| Webhooks | No inbound controllers; outbound stub only |
| Connectors | No `Connector*` types or registry |

---

## Already exists

| Capability | Implementation |
|------------|----------------|
| Gateway proxy | 14+ named HttpClients, correlation forwarding, internal API key |
| Outbox relay | `OutboxRelayBackgroundService`, workforce/directory relays |
| Service Bus consumers | Communication, EnterpriseDirectory, AI proactive |
| AG evaluator | `IAuthorizationEvaluator`, RLS, field masks |
| Report export | CSV/Excel/PDF via ClosedXML/QuestPDF |
| Event subscriptions | Communication `EventSubscriptionProcessor` |
| Phase 10/11 scaffold | Greenfield service template (port, DI, migrations, Gateway, Angular) |

---

## Reusable

| Asset | Reuse pattern |
|-------|---------------|
| `EnterpriseDirectoryService` scaffold | Service structure, Program.cs, extensions |
| Source HTTP clients | Person/Workforce/Organization write path |
| `OutboxRelayWorker` | Poll-relay-retry → `integration-events` |
| `ReportExportService` | ClosedXML for Excel import/export |
| `AccessGovernanceMasterDataSeeder` | `Integration.*` permissions |
| `ApiResponseFactory` | Standard envelope |
| Communication event routing | Webhook trigger on platform events |

---

## Needs enhancement

| Area | Current | Phase 12 target |
|------|---------|----------------|
| Webhook channel | Enum only | Inbound verification + outbound delivery |
| Outbox entities | Duplicated per service | Integration-owned outbox + relay |
| External sync | Domain-specific (directory index) | Unified sync engine |
| Credentials | Per-service appsettings | Encrypted `ConnectorCredential` store |
| Observability | Basic retry counters | Connector health, audit trail, trace IDs |

---

## Missing

| Capability | Phase 12 deliverable |
|------------|---------------------|
| IntegrationService | Port 7218 greenfield service |
| Connector framework | `IConnectorProvider` + registry + lifecycle |
| Mapping engine | Configurable `MappingProfile` / `FieldMapping` |
| Sync orchestration | Jobs, schedules, runs, conflict detection |
| Import/export APIs | CSV, Excel, JSON with preview |
| Integration audit | Immutable `IntegrationAuditLog` per operation |
| Angular integration UI | `/app/integration` |
| EIWAM architecture package | Canonical platform documentation |

---

## Technical debt

- RabbitMQ documentation vs Azure Service Bus implementation
- No unified integration run history
- Communication `WebhookChannelProvider` documented but not implemented
- External system credentials in plain configuration

---

## Architecture decision

**SQL Server** for integration metadata. **Provider-based connector framework** with Tier 1 (full), Tier 2 (reference LDAP/Azure AD), Tier 3 (validated stubs). **Configuration over code** for mappings and schedules.

**Do not extend Enterprise Directory or Workforce Intelligence for external sync.**

---

## Implementation gate

Approved after this gap analysis and [`INTEGRATION_LAYER_DOMAIN_MODEL.md`](INTEGRATION_LAYER_DOMAIN_MODEL.md) are authored.
