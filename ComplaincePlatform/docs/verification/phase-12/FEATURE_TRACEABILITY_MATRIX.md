# Phase 12 — Feature Traceability Matrix

| ID | Requirement | Status | Evidence | Gap |
|----|-------------|--------|----------|-----|
| P0-1 | INTEGRATION_LAYER_GAP_ANALYSIS.md | **Implemented** | `docs/architecture/INTEGRATION_LAYER_GAP_ANALYSIS.md` | — |
| P0-2 | INTEGRATION_LAYER_DOMAIN_MODEL.md | **Implemented** | `docs/architecture/INTEGRATION_LAYER_DOMAIN_MODEL.md` | — |
| SC-1 | IntegrationService port 7218/5218 | **Implemented** | `launchSettings.json`, `Program.cs` | — |
| SC-2 | PramanIntegration database | **Implemented** | `appsettings.json`, migration `M12_IntegrationFoundation` | — |
| SC-3 | PramanServiceCatalog.Integration | **Implemented** | `PramanServiceCatalog.cs` | — |
| SC-4 | slnx entry | **Implemented** | `CompliancePlatform.slnx` | — |
| SC-5 | MediatR CQRS | **Implemented** | `IntegrationHandlers.cs`, controllers | — |
| SC-6 | AddPramanAuthorization | **Implemented** | `ServiceCollectionExtensions.cs` | — |
| DM-1 | IntegrationConnector | **Implemented** | `IntegrationReadModels.cs`, migration | — |
| DM-2 | ConnectorCredential encrypted | **Implemented** | `CredentialProtector`, `ConnectorAppService` | — |
| DM-3 | ConnectorProfile (named instance) | **Partial** | Merged into `IntegrationConnector`; no separate `ConnectorProfile` table | Plan listed separate model |
| DM-4 | SynchronizationJob/Run/Schedule | **Implemented** | Read models + `SynchronizationEngine` | — |
| DM-5 | MappingProfile/FieldMapping/TransformationRule | **Implemented** | DB + `MappingEngine` | — |
| DM-6 | WebhookSubscription/Delivery | **Implemented** | `WebhookService.cs` | — |
| DM-7 | ImportJob/ExportJob | **Implemented** | `ImportExportEngine.cs` | — |
| DM-8 | ConnectorHealth | **Implemented** | `ConnectorHealthService` | — |
| DM-9 | IntegrationAuditLog | **Implemented** | `IntegrationAuditService` | — |
| DM-10 | OutboxMessage | **Implemented** | `IntegrationOutboxWriter`, migration | — |
| DM-11 | Soft delete + RowVersion | **Partial** | Connector has both; not all entities have RowVersion | — |
| CF-1 | IConnectorProvider + Registry | **Implemented** | `ConnectorProviders.cs` | — |
| CF-2 | Tier 1 connectors (full) | **Implemented** | csv/json/webhook providers | csv-export pull minimal |
| CF-3 | Tier 2 LDAP/Azure AD | **Implemented** | `LdapConnectorProvider`, `AzureAdConnectorProvider` | Simulated health only |
| CF-4 | Tier 3 stubs (18 types) | **Implemented** | `StubConnectorProvider`, `ConnectorProviderRegistration.StubTypes` | — |
| CF-5 | Connector lifecycle states | **Implemented** | `ConnectorAppService.ValidateAndConnectAsync` | Draft/Archived transitions limited |
| CF-6 | PushAsync on provider interface | **Missing** | Interface has Pull only | Plan specified PushAsync |
| ME-1 | Configuration-driven mappings | **Implemented** | DB-stored profiles | — |
| ME-2 | Transformation types (7) | **Implemented** | `MappingEngine.ApplyRule` | Expression is pass-through |
| ME-3 | Default seed mapping examples | **Partial** | Only `TrimString` transformation rule seeded | Employment/Dept mapping examples not seeded |
| ME-4 | ValidateProfileAsync | **Implemented** | `MappingEngineTests` | — |
| SE-1 | Manual sync POST /run | **Implemented** | `SynchronizationsController` | — |
| SE-2 | Scheduled sync | **Implemented** | `SyncSchedulerHostedService` | Cron not parsed; fixed 6h interval |
| SE-3 | Incremental watermark | **Implemented** | `SynchronizationRun.Watermark` | — |
| SE-4 | Conflict detection | **Partial** | Simplistic date heuristic | Not hash-based per plan |
| SE-5 | Retry exponential backoff | **Missing** | `MaxRetries` on job unused | — |
| SE-6 | HTTP writes Person/Employment/Org | **Partial** | Employment only via `PlatformWriteClient` | Person/Org clients not registered |
| SE-7 | Two-way sync | **Partial** | Direction field exists | No push/pull round-trip |
| WH-1 | Inbound HMAC verification | **Implemented** | `WebhookService.ProcessInboundAsync` | — |
| WH-2 | Timestamp tolerance | **Missing** | — | Plan security table |
| WH-3 | Idempotency key | **Missing** | — | Plan security table |
| WH-4 | Outbound retry + DLQ | **Implemented** | `DeliverOutboundAsync` | — |
| WH-5 | Inbound triggers sync | **Missing** | Delivery recorded only | Real-time sync not wired |
| IE-1 | CSV import preview/execute | **Implemented** | `ImportExportEngine` | — |
| IE-2 | Excel import/export | **Implemented** | ClosedXML in engine | — |
| IE-3 | JSON import/export | **Implemented** | `ParseContent` | — |
| IE-4 | XML import/export | **Missing** | — | Plan listed basic XML |
| IE-5 | Rollback checkpoint | **Partial** | `RollbackCheckpointJson` stored | No rollback API |
| API-1 | All connector/sync/mapping/webhook/import/export APIs | **Implemented** | `IntegrationControllers.cs` | — |
| API-2 | connector-health + integration-audit | **Implemented** | Controllers | Gateway audit at `/audit` maps correctly |
| API-3 | Gateway proxy | **Implemented** | `IntegrationController`, `IntegrationProxyHandlers` | — |
| API-4 | ApiResponseFactory envelope | **Implemented** | All controllers | Inbound webhook returns raw Ok/Unauthorized |
| SEC-1 | Integration.* permissions (12) | **Implemented** | `AccessGovernanceMasterDataSeeder` | — |
| SEC-2 | AES-GCM credential encryption | **Implemented** | `CredentialProtector` | Dev key in appsettings |
| SEC-3 | IntegrationAuthorizationService | **Partial** | Registered, not called from handlers | `[RequirePermission]` used instead |
| EVT-1 | integration-events relay | **Implemented** | `IntegrationOutboxRelayBackgroundService` | — |
| EVT-2 | Publish sync/connector/import events | **Partial** | Started/Completed/Connected/Import/Export | HealthChanged/Disconnected missing |
| EVT-3 | Subscribe workforce/directory/compliance | **Partial** | workforce-events only | directory/compliance not subscribed |
| UI-1 | /app/integration routes (10) | **Implemented** | `routes.ts`, pages | — |
| UI-2 | Shared components (4) | **Partial** | Files exist | mapping-field-row, webhook-delivery-log unused |
| UI-3 | Sidebar nav | **Implemented** | `sidebar-nav.model.ts` | — |
| UI-4 | Loading/error states | **Missing** | — | — |
| UI-5 | Schedule editor | **Missing** | Sync list only | — |
| TST-1 | ≥35 tests | **Implemented** | 36 tests pass | No integration/API tests |
| DOC-1 | integration-layer-api-flows.md | **Implemented** | `docs/architecture/` | — |
| DOC-2 | EIWAM 11 docs | **Implemented** | `docs/architecture/eiwam/` | — |

**Legend:** Implemented = meets plan; Partial = scaffolded or incomplete; Missing = not found.
