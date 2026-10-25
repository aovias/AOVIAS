# 02 — Enterprise Knowledge Engine

**Phase:** 5.1  
**Sprint:** 1  
**Date:** 2026-07-26

## Delivered

| Item | Implementation |
|------|----------------|
| Embedding interface | `IEmbeddingService` + `HashingEmbeddingService` (deterministic local) |
| Vector index | `IVectorIndex` / `SqlVectorIndex` → `KnowledgeEmbeddingChunks` |
| Hybrid search | Vector cosine ∪ keyword ∪ KB ∪ context-bundle |
| API | `POST /api/v1/ai/knowledge/search` (AIOrchestrator + Gateway) |
| Indexer | `KnowledgeIndexerHostedService` + `SeedDemoIndexAsync` |
| Citations | Version-aware `DocumentId` / `Version` / `Excerpt` |

## Modes

- `keyword` — KB + context bundle filter
- `semantic` — SQL embedding cosine
- `hybrid` (default) — union + dedupe by source

## Migration

`20260726140000_Phase5_KnowledgeEngine` — tables `KnowledgeEmbeddingChunks`, `AceInsights`.

## Exit criteria

NL/hybrid search returns grounded hits with citations; keyword fallback works without external embedding APIs.
