# 03 — Scalability

**Phase:** 7.3  
**Sprint:** 2  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| SignalR Redis backplane | Gateway `AddStackExchangeRedis` when Redis configured — multi-instance hub fan-out |
| File storage provider | `Storage:Provider=Local` (default) or `AzureBlob` → `AzureBlobFileStorageService` |
| Optional Redis compose | `redis-dev` service under compose profile `redis` |
| Horizontal cache | Shared Redis for idempotency + dashboard/framework cache keys across instances |

## Storage provider switch

```json
{
  "Storage": {
    "Provider": "Local"
  }
}
```

Azure Blob:

```json
{
  "Storage": {
    "Provider": "AzureBlob",
    "AzureBlob": {
      "ConnectionString": "<azure-storage-connection-string>",
      "ContainerName": "aovias-files"
    }
  }
}
```

URIs: `local://...` (disk) vs `azureblob://{container}/{blob}`. Existing local URIs are not auto-migrated when switching providers.

## Multi-instance notes

- **SignalR:** Redis backplane required for sticky-session-free Gateway scale-out.
- **Idempotency / dashboard caches:** Redis shares entries; in-process version bump for invalidation is best-effort across nodes (TTL provides eventual consistency).
- **Hangfire:** Continues on SQL Server storage (ComplianceService); scale Hangfire servers carefully against the same storage.

## Exit

Gateway can scale SignalR with Redis; evidence files can target Azure Blob without code changes beyond config.
