# Phase 49 — Plan 01 Summary

## Completed

- Extended `journey-api.model.ts` with split-API DTOs: `JourneySessionDto`, `JourneyDashboardDto`, `JourneyChaptersIndexDto`, `PatchObjectiveElementResponseDto`, `PatchOperationalModeRequestDto`, evidence register response, etc.
- Refactored `JourneyDataService` to use `/journeys/{journeyId}/…` paths; added `getSession`, `getDashboard`, `getChaptersIndex`, `getChapterStandards`, `getObjectiveElement`, `patchObjectiveElement` (with query params), `patchOperationalMode`, evidence multipart/JSON, delete, list evidence/history; kept legacy `getJourneyWorkspace` as deprecated.
- Extended `BaseApiService.patch` to accept optional query params (for `operationalMode` on PATCH OE).
- Mapper: `mergeJourneyWorkspaceFromSplit`, `mapChapterIndexItemToJourneyChapter`, `mapComplianceStandardDto`, `mapOverviewMetricsApiToDomain`; classification int bands; `name`/`title` on standards; OE `title`, `version`, `changedSinceLastAudit`.
- Added `journey-workspace.mapper.spec.ts` unit tests.

## Commits

- (orchestrator) Single phase commit recommended: `feat(49): journey API binding — split services and store integration`
