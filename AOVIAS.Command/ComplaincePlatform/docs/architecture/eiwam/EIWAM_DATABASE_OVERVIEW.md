# EIWAM Database Overview

## Per-service database ownership

| Service | Database | Migration package |
|---------|----------|-------------------|
| IdentityService | PramanIdentity | IdentityService/Migrations |
| PersonService | PramanPerson | PersonService/Migrations |
| WorkforceService | PramanWorkforce | WorkforceService/Migrations |
| ProfessionalService | PramanProfessional | ProfessionalService/Migrations |
| OrganizationService | PramanOrganization | OrganizationService/Infrastructure/Migrations |
| AccessGovernanceService | PramanAccessGovernance | AccessGovernanceService/Migrations |
| GovernanceService | PramanGovernance | GovernanceService/Migrations |
| CommunicationService | PramanCommunication | CommunicationService/Migrations |
| WorkforceIntelligenceService | PramanWorkforceIntelligence | WorkforceIntelligenceService/Migrations |
| EnterpriseDirectoryService | PramanEnterpriseDirectory | EnterpriseDirectoryService/Migrations |
| **IntegrationService** | **PramanIntegration** | **IntegrationService/Migrations** |
| ComplianceService | Compliance | ComplianceService/Migrations |
| AuditService | PramanAudit | AuditService/Migrations |
| KnowledgeBase | PramanKnowledgeBase | KnowledgeBase/Infrastructure/Persistence/Migrations |
| AIOrchestratorService | PramanAI | AIOrchestratorService/Migrations |

## IntegrationService schema (Phase 12)

| Table | Purpose |
|-------|---------|
| Connectors | Connector instances, lifecycle, health |
| ConnectorCredentials | AES-GCM encrypted secrets |
| MappingProfiles | Named mapping sets |
| FieldMappings | Source → target field pairs |
| TransformationRules | String/Date/Lookup/Expression JSON |
| SynchronizationJobs | Sync configuration |
| SynchronizationSchedules | Cron schedules |
| SynchronizationRuns | Per-execution audit |
| WebhookSubscriptions | Inbound/outbound endpoints |
| WebhookDeliveries | Delivery attempts, DLQ |
| ImportJobs / ExportJobs | File transfer jobs |
| ConnectorHealth | Health check history |
| IntegrationAuditLogs | Immutable operation audit |
| OutboxMessages | Integration domain events |

## Cross-database rules

1. No foreign keys across service databases.
2. References use GUIDs validated at application layer.
3. Projections and sync use HTTP APIs, not linked servers.
4. Soft delete (`IsDeleted`) + `RowVersion` on integration entities.

## Backup and migration

Each service auto-migrates on startup via `Database.MigrateAsync()` in development/staging profiles. Production uses AOVIAS deployment packages with explicit migration steps.

## Connection string naming (OutboxRelayWorker)

| Key | Target |
|-----|--------|
| ComplianceConnection | Compliance outbox |
| EnterpriseDirectoryConnection | Directory outbox |
| IntegrationConnection | Integration outbox |
